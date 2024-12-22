using System;
using System.Net.Sockets;
using Newtonsoft.Json;
using CardGameDemoServer.Networking;

namespace CardGameDemoServer.States
{
    internal class PlayersTurnState : BaseState
    {
        public PlayersTurnState(
            ServerGameStateInfo serverGameStateInfo,
            GameStateInfo gameStateInfo,
            Dictionary<string, ClientInfo> clients,
            Dictionary<GameState, BaseState> stateMap,
            Action<Socket, string, string, Action?> sendRequest) :
            base(serverGameStateInfo, gameStateInfo, clients, stateMap, sendRequest)
        {
        }

        protected override void OnEnter(object? data)
        {
            UpdateStateData();
            UpdateGameStateForClients();
        }

        protected override void OnLeave()
        {
        }

        protected override string? OnRequest(Socket socket, string requestType, string requestRaw, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;

            if (requestType == nameof(DoGeneralActionRequest))
                return GetResponseDataForDoGeneralAction(socket, requestRaw, out requestDoneCallback);

            return null;
        }

        private string GetResponseDataForDoGeneralAction(Socket client, string requestData, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;

            var clientInfo = _clients.Where(x => x.Value.Socket == client).Select(x => x.Value).FirstOrDefault();
            if (clientInfo == null)
            {
                Console.WriteLine($"[-] client info is null: {client.RemoteEndPoint}");
                return JsonConvert.SerializeObject(new DoGeneralActionResponse { Success = false, Data = string.Empty });
            }

            requestDoneCallback = () =>
            {
                var request = JsonConvert.DeserializeObject<DoGeneralActionRequest>(requestData);
                if (request == null)
                {
                    Console.WriteLine($"[-] do action request parse failed: {requestData}");
                    return;
                }
                OnDoGeneralAction(clientInfo.PlayerId, request.Action, request.Data);
            };

            return JsonConvert.SerializeObject(new DoGeneralActionResponse { Success = true, Data = string.Empty });
        }

        private void OnDoGeneralAction(int playerId, GeneralAction action, string data)
        {
            var playerCount = _gameStateInfo.PlayerInfos.Count;
            var playerInfo = _gameStateInfo.PlayerInfos[playerId];
            var stateData = PlayersTurnStateData.From(playerInfo.StateData);

            if (!stateData.GeneralActions.Contains(action))
            {
                Console.WriteLine($"[-] player id {playerId} invalid general action {action}");
                return;
            }

            if (action == GeneralAction.FollowBet)
            {
            }
            else if (action == GeneralAction.RaiseBet)
            {
                var raiseBetData = RaiseBetData.From(data);
                if (raiseBetData.Bet <= 0 ||
                    (raiseBetData.Bet % 5) != 0 ||
                    raiseBetData.Bet > playerInfo.NetWorth - playerInfo.Bet)
                {
                    Console.WriteLine($"[-] player id {playerId} invalid raise bet amount {raiseBetData.Bet}");
                    return;
                }
                playerInfo.Bet += raiseBetData.Bet;
                _gameStateInfo.Aggressor = playerId;
            }
            else if (action == GeneralAction.Fold)
            {
                playerInfo.IsFolded = true;
            }
            else if (action == GeneralAction.Showdown)
            {
                // TODO: calculate result
                Next(GameState.RoundResult, null);
                return;
            }

            _gameStateInfo.ActivePlayer = (_gameStateInfo.ActivePlayer + 1) % playerCount;
            ResetTimer();
            Next(GameState.PlayersTurn, null);
        }

        private void UpdateStateData()
        {
            var highestBet = 0;
            for (var playerId = 0; playerId < _gameStateInfo.PlayerInfos.Count; playerId++)
                if (_gameStateInfo.PlayerInfos[playerId].Bet > highestBet)
                    highestBet = _gameStateInfo.PlayerInfos[playerId].Bet;

            for (var playerId = 0; playerId < _gameStateInfo.PlayerInfos.Count; playerId++)
            {
                var playerInfo = _gameStateInfo.PlayerInfos[playerId];
                var stateData = new PlayersTurnStateData { GeneralActions = [] };

                if (playerId == _gameStateInfo.ActivePlayer)
                {
                    if (playerId == _gameStateInfo.Aggressor)
                    {
                        if (_serverGameStateInfo.IsAggressorsFirstTurn)
                        {
                            // aggressor's first turn
                            stateData.GeneralActions.Add(GeneralAction.FollowBet);
                            stateData.GeneralActions.Add(GeneralAction.RaiseBet);
                            stateData.GeneralActions.Add(GeneralAction.Fold);
                            _serverGameStateInfo.IsAggressorsFirstTurn = false;
                        }
                        else
                        {
                            // aggressor's second turn
                            stateData.GeneralActions.Add(GeneralAction.RaiseBet);
                            stateData.GeneralActions.Add(GeneralAction.Showdown);
                        }
                    }
                    else
                    {
                        // non agressor
                        if (highestBet <= playerInfo.NetWorth)
                            stateData.GeneralActions.Add(GeneralAction.FollowBet);
                        if (highestBet < playerInfo.NetWorth)
                            stateData.GeneralActions.Add(GeneralAction.RaiseBet);
                        stateData.GeneralActions.Add(GeneralAction.Fold);
                    }
                }
                else
                {
                    // non active player
                }

                playerInfo.StateData = stateData.RawData();
            }
        }

    }
}
