using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace Networking
{
    public class Client : MonoBehaviour
    {
        private Networking.SocketClient _socket = null;
        private int _seq = 0;
        private readonly Dictionary<int, TaskCompletionSource<string>> _c2sRspDict = new(); // seq -> tcs
        private readonly Dictionary<string, Func<string, string>> _s2cHandlers = new(); // request type name -> internal callback

        void Start()
        {
            _socket = new SocketClient();
            _socket.Connect("127.0.0.1", 8800);
        }

        void Update()
        {
            _socket.Update();
            while (_socket.ReadMsg(out var msg))
            {
                var data = System.Text.Encoding.UTF8.GetString(msg);
                Debug.Log($"connection on message:\r\n{data}");

                // parse S2CData
                S2CData s2cData;
                try
                {
                    s2cData = S2CData.From(data);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Update, parsing S2CData failed: {ex}");
                    continue;
                }

                // handle request or response
                if (s2cData.Type == S2CData.DataType.Request)
                {
                    // parse request
                    Networking.Request req;
                    try
                    {
                        req = Networking.Request.From(s2cData.Data);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, parsing Request failed: {ex}");
                        continue;
                    }

                    // call request handler
                    var rspData = string.Empty;
                    if (_s2cHandlers.TryGetValue(req.Type, out var handler))
                        rspData = handler.Invoke(req.Data);

                    // build response
                    C2SData c2sData;
                    try
                    {
                        Networking.Response rsp = Networking.Response.Build(req.Seq, rspData);
                        c2sData = C2SData.Build(C2SData.DataType.Response, rsp.RawData());
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, building C2SData failed: {ex}");
                        continue;
                    }

                    // send data
                    _socket.SendMsg(System.Text.Encoding.UTF8.GetBytes(c2sData.RawData()));
                }
                else if (s2cData.Type == S2CData.DataType.Response)
                {
                    // build response
                    Networking.Response rsp;
                    try
                    {
                        rsp = Networking.Response.From(s2cData.Data);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, building Response failed: {ex}");
                        continue;
                    }

                    // notify
                    if (_c2sRspDict.TryGetValue(rsp.Seq, out var tcs))
                        tcs.SetResult(rsp.Data);
                }
            }
        }

        void OnDestroy()
        {
            _socket.Disconnect();
            _socket = null;
        }

        // return default if failed
        public async Task<TResponse> Request<TRequest, TResponse>(TRequest request)
        {
            var seq = _seq++;

            // build C2SData
            C2SData c2sData;
            try
            {
                var data = Networking.Request.Build(seq, request);
                c2sData = C2SData.Build(C2SData.DataType.Request, data);
            }
            catch (Exception ex)
            {
                Debug.Log($"Request, building C2SData failed: {ex}");
                return default;
            }

            // send and wait for response
            var tcs = new TaskCompletionSource<string>();
            _c2sRspDict[seq] = tcs;
            _socket.SendMsg(System.Text.Encoding.UTF8.GetBytes(c2sData.RawData()));
            var rspRaw = await tcs.Task;
            _c2sRspDict.Remove(seq);

            // parse response
            if (string.IsNullOrEmpty(rspRaw))
                return default;
            TResponse response;
            try
            {
                response = JsonUtility.FromJson<TResponse>(rspRaw);
            }
            catch (Exception ex)
            {
                Debug.Log($"Request, building TResponse failed: {ex}");
                return default;
            }

            return response;
        }

        public void SetListener<TRequest, TResponse>(Func<TRequest, TResponse> func)
        {
            _s2cHandlers[typeof(TRequest).FullName] = (string reqData) =>
            {
                // parse request
                TRequest req;
                try
                {
                    req = JsonUtility.FromJson<TRequest>(reqData);
                }
                catch (Exception ex)
                {
                    Debug.Log($"_s2cHandlers, parsing TRequest failed: {ex}");
                    return null;
                }

                // invoke handler
                var rsp = func.Invoke(req);

                // build response string
                string rspRaw;
                try
                {
                    rspRaw = JsonUtility.ToJson(rsp);
                }
                catch (Exception ex)
                {
                    Debug.Log($"_s2cHandlers, parsing response string failed: {ex}");
                    return null;
                }

                return rspRaw;
            };
        }

        public void RemoveListener<TRequest>()
        {
            _s2cHandlers.Remove(typeof(TRequest).FullName);
        }
    }
}
