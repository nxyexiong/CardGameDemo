using System;

namespace CardGameDemoServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var server = new GameServer(8800, false, ["aaa"], 500, 30 * 1000);
            server.Start();

            var cancelled = false;
            while (!cancelled)
                Thread.Sleep(100);
            Console.CancelKeyPress += (_, _) => { cancelled = true; };

            server.Stop();
        }
    }
}