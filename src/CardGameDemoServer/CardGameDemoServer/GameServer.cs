using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGameDemoServer
{
    internal class GameServer
    {
        private int _port;
        private List<string> _profileIds;
        private int _initNetWorth;

        private bool _running;

        public GameServer(
            int port = 8800,
            IEnumerable<string>? profileIds = null,
            int initNetWorth = 500)
        {
            _port = port;
            _profileIds = profileIds?.ToList() ?? [];
            _initNetWorth = initNetWorth;
        }
    }
}
