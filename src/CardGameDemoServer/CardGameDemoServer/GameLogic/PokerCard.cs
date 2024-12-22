using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardGameDemoServer.GameLogic
{
    internal class PokerCard
    {
        public const string RankOrder = "23456789TJQKAZ";
        public const string SuitOrder = "DCHSBR";

        public string Rank { get; set; } = string.Empty;
        public string Suit { get; set; } = string.Empty;

        public int CompareTo(PokerCard other)
        {
            int rank1Index = RankOrder.IndexOf(Rank);
            int rank2Index = RankOrder.IndexOf(other.Rank);

            if (rank1Index != rank2Index)
                return rank1Index.CompareTo(rank2Index);

            int suit1Index = SuitOrder.IndexOf(Suit);
            int suit2Index = SuitOrder.IndexOf(other.Suit);

            return suit1Index.CompareTo(suit2Index);
        }

        public string RawData() => $"{Rank}{Suit}";

        public static PokerCard? From(string raw)
        {
            var rank = raw.Substring(0, 1);
            var suit = raw.Substring(1, 1);
            if (!RankOrder.Contains(rank) || !SuitOrder.Contains(suit))
                return null;
            return new PokerCard { Rank = rank, Suit = suit };
        }
    }
}
