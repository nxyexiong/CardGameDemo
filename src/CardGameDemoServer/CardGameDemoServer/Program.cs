using System;

namespace CardGameDemoServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var server = new GameServer(
                port: 8800,
                isIpv6: false,
                profileIds: ["aaa"],
                initNetWorth: 500);
            server.Start();

            var cancelled = false;
            while (!cancelled)
                Thread.Sleep(100);
            Console.CancelKeyPress += (_, _) => { cancelled = true; };

            server.Stop();
        }
    }
}