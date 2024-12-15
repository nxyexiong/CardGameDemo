using System;
using System.Collections.ObjectModel;
using UnityEngine;


namespace Networking
{
    [Serializable]
    public class C2SData
    {
        public enum DataType : int
        {
            Request = 0,
            Response = 1,
        }

        public DataType Type { get; set; } = DataType.Request;
        public string Data { get; set; } = string.Empty;

        public static C2SData Build<T>(DataType type, T data)
        {
            return new C2SData { Type = type, Data = JsonUtility.ToJson(data) };
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

        public DataType Type { get; set; } = DataType.Request;
        public string Data { get; set; } = string.Empty;

        public static S2CData From(string rawData)
        {
            return JsonUtility.FromJson<S2CData>(rawData);
        }

        public Request GetRequest()
        {
            if (Type != DataType.Request) return null;
            return Request.From(Data);
        }

        public Response GetResponse()
        {
            if (Type != DataType.Response) return null;
            return Response.From(Data);
        }
    }

    [Serializable]
    public class Request
    {
        public int Seq { get; set; } = -1;
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;

        public static Request Build<T>(int seq, T data)
        {
            return new Request
            {
                Seq = seq,
                Type = typeof(T).FullName,
                Data = JsonUtility.ToJson(data),
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
        public int Seq { get; set; } = -1;
        public string Data { get; set; } = string.Empty;

        public static Response Build<T>(int seq, T data)
        {
            return new Response
            {
                Seq = seq,
                Data = JsonUtility.ToJson(data),
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
    public class UpdateGameStateRequest
    {
        public long ServerTimestampMs { get; set; } = -1;
        public GameStateInfo GameStateInfoDelta { get; set; } = new();
        public Collection<string> AvailableActions { get; set; } = new();
    }

    [Serializable]
    public class GameStateInfo
    {
        public bool IsPlayerIdChanged { get; set; } = false;
        public int PlayerId { get; set; } = -1;

        public Collection<PlayerInfo> PlayerInfos { get; set; } = new();

        public bool IsDealerChanged = false;
        public int Dealer { get; set; } = -1;

        public bool IsAggressorChanged = false;
        public int Aggressor { get; set; } = -1;

        public bool IsActivePlayerChanged = false;
        public int ActivePlayer { get; set; } = -1;

        public bool IsTimerStartTimestampMsChanged { get; set; } = false;
        public long TimerStartTimestampMs { get; set; } = -1;

        public bool IsTimerIntervalMsChanged { get; set; } = false;
        public long TimerIntervalMs { get; set; } = -1;

        public void ApplyDelta(GameStateInfo delta)
        {
            if (delta.IsPlayerIdChanged)
                PlayerId = delta.PlayerId;

            while (delta.PlayerInfos.Count > PlayerInfos.Count)
                PlayerInfos.Add(delta.PlayerInfos[PlayerInfos.Count]);
            while (delta.PlayerInfos.Count < PlayerInfos.Count)
                PlayerInfos.RemoveAt(PlayerInfos.Count - 1);
            for (var i = 0; i < PlayerInfos.Count; i++)
                PlayerInfos[i].ApplyDelta(delta.PlayerInfos[i]);

            if (delta.IsDealerChanged)
                Dealer = delta.Dealer;
            if (delta.IsAggressorChanged)
                Aggressor = delta.Aggressor;
            if (delta.IsActivePlayerChanged)
                ActivePlayer = delta.ActivePlayer;
            if (delta.IsTimerStartTimestampMsChanged)
                TimerStartTimestampMs = delta.TimerStartTimestampMs;
            if (delta.IsTimerIntervalMsChanged)
                TimerIntervalMs = delta.TimerIntervalMs;
        }
    }

    [Serializable]
    public class PlayerInfo
    {
        public bool IsNameChanged { get; set; } = false;
        public string Name { get; set; } = string.Empty;

        public bool IsNetWorthChanged { get; set; } = false;
        public int NetWorth { get; set; } = -1;

        public bool IsBetChanged { get; set; } = false;
        public int Bet { get; set; } = -1;

        public bool IsIsFoldedChanged { get; set; } = false;
        public bool IsFolded { get; set; } = false;

        public bool IsMainHandChanged { get; set; } = false;
        public Collection<string> MainHand { get; set; } = new();

        public void ApplyDelta(PlayerInfo delta)
        {
            if (delta.IsNameChanged)
                Name = delta.Name;
            if (delta.IsNetWorthChanged)
                NetWorth = delta.NetWorth;
            if (delta.IsBetChanged)
                Bet = delta.Bet;
            if (delta.IsIsFoldedChanged)
                IsFolded = delta.IsFolded;
            if (delta.IsMainHandChanged)
                MainHand = delta.MainHand;
        }
    }

    [Serializable]
    public class UpdateGameStateResponse
    {
    }

    [Serializable]
    public class DoActionRequest
    {
        public string Action { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    [Serializable]
    public class DoActionResponse
    {
        public bool Success { get; set; } = false;
    }
}
