using System;
using Newtonsoft.Json;

namespace Networking
{

    public class CSData
    {
        public enum DataType : int
        {
            Request = 0,
            Response = 1,
        }

        public DataType Type { get; set; } = DataType.Request;
        public string Data { get; set; } = string.Empty;

        public static CSData From(string rawData)
        {
            return JsonConvert.DeserializeObject<CSData>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Request
    {
        public int Seq { get; set; } = -1;
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;

        public static Request From(string rawData)
        {
            return JsonConvert.DeserializeObject<Request>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Response
    {
        public int Seq { get; set; } = -1;
        public string Data { get; set; } = string.Empty;

        public static Response From(string rawData)
        {
            return JsonConvert.DeserializeObject<Response>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class HandshakeRequest
    {
        public string ProfileId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public static HandshakeRequest From(string rawData)
        {
            return JsonConvert.DeserializeObject<HandshakeRequest>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class HandshakeResponse
    {
        public bool Success { get; set; } = false;

        public static HandshakeResponse From(string rawData)
        {
            return JsonConvert.DeserializeObject<HandshakeResponse>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class UpdateGameStateRequest
    {
        public long ServerTimestampMs { get; set; } = -1;
        public GameStateInfo GameStateInfo { get; set; } = new();

        public static UpdateGameStateRequest From(string rawData)
        {
            return JsonConvert.DeserializeObject<UpdateGameStateRequest>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class GameStateInfo
    {
        public int PlayerId { get; set; } = -1;
        public List<PlayerInfo> PlayerInfos { get; set; } = new List<PlayerInfo>();
        public int Dealer { get; set; } = -1;
        public int Aggressor { get; set; } = -1;
        public int ActivePlayer { get; set; } = -1;
        public long TimerStartTimestampMs { get; set; } = -1;
        public long TimerIntervalMs { get; set; } = -1;

        public GameStateInfo Copy()
        {
            var ret = new GameStateInfo
            {
                PlayerId = PlayerId,
                PlayerInfos = new List<PlayerInfo>(),
                Dealer = Dealer,
                Aggressor = Aggressor,
                ActivePlayer = ActivePlayer,
                TimerStartTimestampMs = TimerStartTimestampMs,
                TimerIntervalMs = TimerIntervalMs,
            };

            foreach (var playerInfo in PlayerInfos)
                ret.PlayerInfos.Add(playerInfo.Copy());

            return ret;
        }
    }

    public class PlayerInfo
    {
        public string Name { get; set; } = string.Empty;
        public int NetWorth { get; set; } = -1;
        public int Bet { get; set; } = -1;
        public bool IsFolded { get; set; } = false;
        public List<string> MainHand { get; set; } = new List<string>();
        public List<string> AvailableActions { get; set; } = new List<string>();

        public PlayerInfo Copy()
        {
            var ret = new PlayerInfo
            {
                Name = Name,
                NetWorth = NetWorth,
                Bet = Bet,
                IsFolded = IsFolded,
                MainHand = new List<string>(),
                AvailableActions = new List<string>(),
            };

            foreach (var card in MainHand)
                ret.MainHand.Add(card);
            foreach (var availableAction in AvailableActions)
                ret.AvailableActions.Add(availableAction);

            return ret;
        }
    }

    public class UpdateGameStateResponse
    {
        public bool Success { get; set; } = false;

        public static UpdateGameStateResponse From(string rawData)
        {
            return JsonConvert.DeserializeObject<UpdateGameStateResponse>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DoActionRequest
    {
        public string Action { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;

        public static DoActionRequest From(string rawData)
        {
            return JsonConvert.DeserializeObject<DoActionRequest>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DoActionResponse
    {
        public bool Success { get; set; } = false;

        public static DoActionResponse From(string rawData)
        {
            return JsonConvert.DeserializeObject<DoActionResponse>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
