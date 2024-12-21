using System;
using Networking;
using Newtonsoft.Json;


namespace CardGameDemoServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var handshakeRequest = new HandshakeRequest
            {
                profileId = "aaa",
                name = "bbb",
            };
            Console.WriteLine($"test {JsonConvert.SerializeObject(handshakeRequest)}");
        }
    }
}