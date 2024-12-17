using System;
using System.Collections.ObjectModel;
using UnityEngine;


namespace Networking
{
    // only public field or field marked as serializable can be convert to json

    [Serializable]
    public class C2SData
    {
        public enum DataType : int
        {
            Request = 0,
            Response = 1,
        }

        public DataType type = DataType.Request;
        public string data = string.Empty;

        public static C2SData Build<T>(DataType type, T data)
        {
            return new C2SData { type = type, data = JsonUtility.ToJson(data) };
        }

        public string RawData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class S2CData
    {
        public enum DataType : int
        {
            Request = 0,
            Response = 1,
        }

        public DataType type = DataType.Request;
        public string data = string.Empty;

        public static S2CData From(string rawData)
        {
            return JsonUtility.FromJson<S2CData>(rawData);
        }

        public Request GetRequest()
        {
            if (type != DataType.Request) return null;
            return Request.From(data);
        }

        public Response GetResponse()
        {
            if (type != DataType.Response) return null;
            return Response.From(data);
        }
    }

    [Serializable]
    public class Request
    {
        public int seq = -1;
        public string type = string.Empty;
        public string data = string.Empty;

        public static Request Build<T>(int seq, T data)
        {
            return new Request
            {
                seq = seq,
                type = typeof(T).FullName,
                data = JsonUtility.ToJson(data),
            };
        }

        public static Request From(string rawData)
        {
            return JsonUtility.FromJson<Request>(rawData);
        }

        public string RawData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class Response
    {
        public int seq = -1;
        public string data = string.Empty;

        public static Response Build<T>(int seq, T data)
        {
            return new Response
            {
                seq = seq,
                data = JsonUtility.ToJson(data),
            };
        }

        public static Response From(string rawData)
        {
            return JsonUtility.FromJson<Response>(rawData);
        }

        public string RawData()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class HandshakeRequest
    {
        public string profileId = string.Empty;
        public string name = string.Empty;
    }

    [Serializable]
    public class HandshakeResponse
    {
        public bool success = false;
    }

    [Serializable]
    public class UpdateGameStateRequest
    {
        public long serverTimestampMs = -1;
        public GameStateInfo gameStateInfoDelta = new();
        public Collection<string> availableActions = new();
    }

    [Serializable]
    public class GameStateInfo
    {
        public bool isPlayerIdChanged = false;
        public int playerId = -1;

        public Collection<PlayerInfo> playerInfos = new();

        public bool isDealerChanged = false;
        public int dealer = -1;

        public bool isAggressorChanged = false;
        public int aggressor = -1;

        public bool isActivePlayerChanged = false;
        public int activePlayer = -1;

        public bool isTimerStartTimestampMsChanged = false;
        public long timerStartTimestampMs = -1;

        public bool isTimerIntervalMsChanged = false;
        public long timerIntervalMs = -1;

        public void ApplyDelta(GameStateInfo delta)
        {
            if (delta.isPlayerIdChanged)
                playerId = delta.playerId;

            while (delta.playerInfos.Count > playerInfos.Count)
                playerInfos.Add(delta.playerInfos[playerInfos.Count]);
            while (delta.playerInfos.Count < playerInfos.Count)
                playerInfos.RemoveAt(playerInfos.Count - 1);
            for (var i = 0; i < playerInfos.Count; i++)
                playerInfos[i].ApplyDelta(delta.playerInfos[i]);

            if (delta.isDealerChanged)
                dealer = delta.dealer;
            if (delta.isAggressorChanged)
                aggressor = delta.aggressor;
            if (delta.isActivePlayerChanged)
                activePlayer = delta.activePlayer;
            if (delta.isTimerStartTimestampMsChanged)
                timerStartTimestampMs = delta.timerStartTimestampMs;
            if (delta.isTimerIntervalMsChanged)
                timerIntervalMs = delta.timerIntervalMs;
        }
    }

    [Serializable]
    public class PlayerInfo
    {
        public bool isNameChanged = false;
        public string name = string.Empty;

        public bool isNetWorthChanged = false;
        public int netWorth = -1;

        public bool isBetChanged = false;
        public int bet = -1;

        public bool isIsFoldedChanged = false;
        public bool isFolded = false;

        public bool isMainHandChanged = false;
        public Collection<string> mainHand = new();

        public void ApplyDelta(PlayerInfo delta)
        {
            if (delta.isNameChanged)
                name = delta.name;
            if (delta.isNetWorthChanged)
                netWorth = delta.netWorth;
            if (delta.isBetChanged)
                bet = delta.bet;
            if (delta.isIsFoldedChanged)
                isFolded = delta.isFolded;
            if (delta.isMainHandChanged)
                mainHand = delta.mainHand;
        }
    }

    [Serializable]
    public class UpdateGameStateResponse
    {
        public bool success = false;
    }

    [Serializable]
    public class DoActionRequest
    {
        public string action = string.Empty;
        public string data = string.Empty;
    }

    [Serializable]
    public class DoActionResponse
    {
        public bool success = false;
    }
}
