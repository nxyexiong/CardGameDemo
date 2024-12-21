using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public DataType type = DataType.Request;
        public string data = string.Empty;

        public static C2SData Build<T>(DataType type, T data)
        {
            return new C2SData { type = type, data = JsonConvert.SerializeObject(data) };
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
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

        public static S2CData? From(string rawData)
        {
            return JsonConvert.DeserializeObject<S2CData>(rawData);
        }

        public Request? GetRequest()
        {
            if (type != DataType.Request) return null;
            return Request.From(data);
        }

        public Response? GetResponse()
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
                type = typeof(T).FullName ?? string.Empty,
                data = JsonConvert.SerializeObject(data),
            };
        }

        public static Request? From(string rawData)
        {
            return JsonConvert.DeserializeObject<Request>(rawData);
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
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
                data = JsonConvert.SerializeObject(data),
            };
        }

        public static Response? From(string rawData)
        {
            return JsonConvert.DeserializeObject<Response>(rawData);
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
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
        public GameStateInfo gameStateInfo = new();
        public Collection<string> availableActions = new();
    }

    [Serializable]
    public class GameStateInfo
    {
        public int playerId = -1;
        public Collection<PlayerInfo> playerInfos = new();
        public int dealer = -1;
        public int aggressor = -1;
        public int activePlayer = -1;
        public long timerStartTimestampMs = -1;
        public long timerIntervalMs = -1;
    }

    [Serializable]
    public class PlayerInfo
    {
        public string name = string.Empty;
        public int netWorth = -1;
        public int bet = -1;
        public bool isFolded = false;
        public Collection<string> mainHand = new();
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
