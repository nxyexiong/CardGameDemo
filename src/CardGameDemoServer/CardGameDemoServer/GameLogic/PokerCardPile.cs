using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGameDemoServer.GameLogic
{
    internal class PokerCardPile
    {
        private static readonly Random _rng = new();
        private List<PokerCard> _pile = [];

        public void Init(bool haveJoker = false)
        {
            foreach (var suit in PokerCard.SuitOrder)
            {
                foreach (var rank in PokerCard.RankOrder)
                {
                    if (rank == 'Z' && !haveJoker) continue;
                    if (rank == 'Z' && suit != 'R' && suit != 'B') continue;
                    if (rank != 'Z' && (suit == 'R' || suit == 'B')) continue;
                    _pile.Add(new PokerCard { Rank = $"{rank}", Suit = $"{suit}" });
                }
            }
        }

        public void Shuffle()
        {
            _pile = _pile.OrderBy(_ => _rng.Next()).ToList();
        }

        public int Count()
        {
            return _pile.Count;
        }

        public PokerCard? Draw()
        {
            var ret = _pile.FirstOrDefault();
            if (ret != null) _pile.Remove(ret);
            return ret;
        }
    }
}
