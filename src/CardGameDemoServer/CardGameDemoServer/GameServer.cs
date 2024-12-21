using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Networking;

namespace CardGameDemoServer
{
    internal class GameServer
    {
        private enum GameState
        {
            Start,
            WaitingForPlayer,
            WaitingForAction,
            End,
        }

        private class ClientInfo
        {
            public Socket? Socket { get; set; } = null;
            public int PlayerId { get; set; } = -1;
        }

        private class ResponseContext
        {
            public int Seq { get; set; } = -1;
            public Action? Callback { get; set; } = null;
        }

        private readonly int _port;
        private readonly bool _isIpv6;
        private readonly int _initNetWorth;

        private bool _running;
        private int _seq;
        private Task? _loopTask;
        private GameState _gameState;
        private GameStateInfo _gameStateInfo;
        private readonly Dictionary<string, ClientInfo> _clients; // profile id -> ClientInfo
        private readonly Dictionary<int, ResponseContext> _responseContexts; // seq -> context

        public GameServer(
            int port = 8800,
            bool isIpv6 = false,
            IEnumerable<string>? profileIds = null,
            int initNetWorth = 500)
        {
            _port = port;
            _isIpv6 = isIpv6;
            _initNetWorth = initNetWorth;

            _running = false;
            _seq = 0;
            _loopTask = null;
            _gameState = GameState.Start;
            _gameStateInfo = new();
            _clients = [];
            for (var i = 0; i < (profileIds?.Count() ?? 0); i++)
            {
                var profileId = profileIds?.ElementAt(i) ?? string.Empty;
                _clients[profileId] = new ClientInfo { Socket = null, PlayerId = i };
                _gameStateInfo.playerInfos.Add(new());
            }
            _responseContexts = [];
        }

        public void Start()
        {
            Console.WriteLine("[+] server is starting...");
            _running = true;
            _gameState = GameState.WaitingForPlayer;
            _loopTask = Task.Run(() => Loop());
            Console.WriteLine("[+] server is started");
        }

        public void Stop()
        {
            Console.WriteLine("[+] server is stopping...");
            _running = false;
            _loopTask?.Wait();
            Console.WriteLine("[+] server is stopped");
        }

        private void Loop()
        {
            while (_running)
            {
                Socket? sock = null;
                List<Socket> clients = [];
                Dictionary<Socket, List<byte>> buffers = [];
                var buf = new byte[2048];
                try
                {
                    sock = new Socket(
                        _isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    sock.Bind(new IPEndPoint(0, _port));
                    sock.Listen();

                    while (_running)
                    {
                        // handle server socket
                        if (sock.Poll(0, SelectMode.SelectError))
                        {
                            Console.WriteLine($"[-] server socket error");
                            break;
                        }
                        if (sock.Poll(0, SelectMode.SelectRead))
                        {
                            Console.WriteLine($"[+] server accept");
                            var client = sock.Accept();
                            clients.Add(client);
                            buffers[client] = [];
                        }

                        // handle client sockets
                        List<Socket> removeClients = [];
                        foreach (var client in clients)
                        {
                            if (client.Poll(0, SelectMode.SelectError))
                            {
                                Console.WriteLine($"[-] client socket error");
                                removeClients.Add(client);
                                continue;
                            }
                            if (client.Poll(0, SelectMode.SelectRead))
                            {
                                var r = client.Receive(buf);
                                if (r <= 0)
                                {
                                    Console.WriteLine($"[-] client disconnected");
                                    removeClients.Add(client);
                                    continue;
                                }
                                try
                                {
                                    buffers[client].AddRange(buf);
                                    if (buffers[client].Count > 1000 * 1000)
                                        throw new InvalidDataException("client buffer exceeded");
                                    if (buffers[client].Count < 2) continue;
                                    var msgLen = buffers[client].ElementAt(0) * 256 + buffers[client].ElementAt(1);
                                    if (buffers[client].Count < msgLen) continue;
                                    var msgStr = Encoding.UTF8.GetString(buffers[client].Skip(2).Take(msgLen).ToArray());
                                    buffers[client].RemoveRange(0, 2 + msgLen);
                                    HandleClientMsg(client, msgStr);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[-] client logic exception: {ex}");
                                    removeClients.Add(client);
                                    continue;
                                }
                            }
                        }
                        foreach (var client in removeClients)
                        {
                            buffers.Remove(client);
                            clients.Remove(client);
                        }

                        // wait 1ms in case nothing happens
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] server socket exception: {ex}");
                }
                finally
                {
                    buffers.Clear();
                    foreach (var client in clients)
                        client.Close();
                    clients.Clear();
                    sock?.Close();
                    sock = null;
                }
            }
        }

        private void HandleClientMsg(Socket client, string msg)
        {
            Console.WriteLine($"[+] handle client msg, {client}: {msg}");
            var c2sData = C2SData.From(msg);
            if (c2sData?.type == C2SData.DataType.Request)
            {
                var request = c2sData.GetRequest() ?? throw new InvalidDataException("client request is null");
                HandleClientRequest(client, request);
            }
            else if (c2sData?.type == C2SData.DataType.Response)
            {
                var response = c2sData.GetResponse() ?? throw new InvalidDataException("client response is null");
                HandleClientResponse(client, response);
            }
        }

        private void HandleClientRequest(Socket client, Request request)
        {
            var seq = request.seq;
            var typeName = request.type;
            Console.WriteLine($"[+] handle client request, {client}: {seq}-{typeName}");

            // get response
            string? responseStr = null;
            Action? requestDoneCallback = null;
            switch (typeName)
            {
                case $"{nameof(Networking)}.{nameof(HandshakeRequest)}":
                    responseStr = GetResponseDataForHandshake(client, request.data, out requestDoneCallback);
                    break;
                case $"{nameof(Networking)}.{nameof(DoActionRequest)}":
                    responseStr = GetResponseDataForDoAction(client, request.data, out requestDoneCallback);
                    break;
                default:
                    Console.WriteLine($"[-] handle client request, unknown type {typeName}");
                    break;
            }
            if (responseStr == null)
                throw new InvalidDataException($"[-] handle client request, response string is null: {client}, {typeName}");

            // send response
            var response = new Response { seq = seq, data = responseStr };
            var s2cData = new S2CData { type = S2CData.DataType.Response, data = response.RawData() };
            SendMsg(client, s2cData.RawData());

            // on request done
            requestDoneCallback?.Invoke();
        }

        private void HandleClientResponse(Socket client, Response response)
        {
            var seq = response.seq;
            Console.WriteLine($"[+] handle client response, {client}: {seq}");
            if (!_responseContexts.TryGetValue(seq, out var context))
            {
                Console.WriteLine($"[-] handle client response, unknown seq: {seq}");
                return;
            }
            context.Callback?.Invoke();
            _responseContexts.Remove(seq);
        }

        private string GetResponseDataForHandshake(Socket client, string requestData, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;
            var request = JsonConvert.DeserializeObject<HandshakeRequest>(requestData) ??
                throw new InvalidDataException("parse HandshakeRequest failed");

            // check profile id
            var profileId = request.profileId;
            if (!_clients.TryGetValue(profileId, out ClientInfo? clientInfo))
            {
                Console.WriteLine($"[-] invalid profile id: {profileId}");
                return JsonConvert.SerializeObject(new HandshakeResponse { success = false });
            }

            // update client info
            clientInfo.Socket = client;
            _gameStateInfo.playerInfos[clientInfo.PlayerId].name = request.name;

            // on request done
            requestDoneCallback = () =>
            {
                var updated = false;
                if (_gameState == GameState.WaitingForPlayer)
                {
                    var missingPlayer = false;
                    foreach (var kv in _clients)
                        if (kv.Value.Socket == null)
                            missingPlayer = true;
                    if (!missingPlayer)
                    {
                        updated = true;
                        InitNewGame();
                        _gameState = GameState.WaitingForAction;
                    }
                }
                if (!updated) UpdateGameStateForClient(profileId);
            };

            return JsonConvert.SerializeObject(new HandshakeResponse { success = true });
        }

        private string GetResponseDataForDoAction(Socket client, string requestData, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;

            var clientInfo = _clients.Where(x => x.Value.Socket == client).Select(x => x.Value).FirstOrDefault();
            if (clientInfo == null)
            {
                Console.WriteLine($"[-] client info is null: {client}");
                return JsonConvert.SerializeObject(new DoActionResponse { success = false });
            }

            requestDoneCallback = () =>
            {
                var request = JsonConvert.DeserializeObject<DoActionRequest>(requestData);
                if (request == null)
                {
                    Console.WriteLine($"[-] do action request parse failed: {requestData}");
                    return;
                }
                OnDoAction(clientInfo.PlayerId, request.action);
            };

            return JsonConvert.SerializeObject(new DoActionResponse { success = true });
        }

        private void InitNewGame()
        {
            // TODO
        }

        private void OnDoAction(int playerId, string actionName)
        {
            // TODO
        }

        private void UpdateGameStateForClients()
        {
            foreach (var kv in _clients)
            {
                var profileId = kv.Key;
                UpdateGameStateForClient(profileId);
            }
        }

        private void UpdateGameStateForClient(string profileId)
        {
            Console.WriteLine($"[+] update game state for client: {profileId}");
            var clientInfo = _clients[profileId];
            if (clientInfo.Socket == null)
            {
                Console.WriteLine($"[*] missing socket for client: {profileId}");
                return;
            }

            var sendGameStateInfo = _gameStateInfo.Copy();
            var playerId = clientInfo.PlayerId;
            sendGameStateInfo.playerId = playerId;
            for (var i = 0; i < sendGameStateInfo.playerInfos.Count; i++)
            {
                if (i != playerId)
                {
                    sendGameStateInfo.playerInfos[i].mainHand.Clear();
                    sendGameStateInfo.playerInfos[i].availableActions.Clear();
                }
            }

            var request = new UpdateGameStateRequest
            {
                serverTimestampMs = GetTimestampMs(DateTime.Now),
                gameStateInfo = sendGameStateInfo,
            };
            SendRequest(clientInfo.Socket, request, null);
        }

        private void SendRequest<T>(Socket client, T request, Action? callback)
        {
            var req = Request.Build(_seq++, request);
            var s2cData = new S2CData { type = S2CData.DataType.Request, data = req.RawData() };
            var context = new ResponseContext { Seq = req.seq, Callback = callback };
            _responseContexts[req.seq] = context;
            SendMsg(client, s2cData.RawData());
        }

        private static void SendMsg(Socket sock, string msg)
        {
            var data = Encoding.UTF8.GetBytes(msg);
            var msgLen = data.Length;
            var send = new byte[2 + msgLen];
            send[0] = (byte)(msgLen / 256);
            send[1] = (byte)(msgLen % 256);
            Array.Copy(data, 0, send, 2, msgLen);
            while (send.Length > 0)
            {
                var w = sock.Send(send);
                if (w <= 0)
                    throw new InvalidDataException($"[-] send msg failed: {w}");
                send = send.Skip(w).ToArray();
            }
        }

        public static long GetTimestampMs(DateTime dateTime)
        {
            return dateTime.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
