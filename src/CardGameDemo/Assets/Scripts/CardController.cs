using System;
using UnityEngine;
using UnityEngine.UI;

public class CardFace : IComparable<CardFace>, IEquatable<CardFace>
{
    // rank encoding: 2, 3, 4, 5, 6, 7, 8, 9, T(ten), J(jack), Q(queen), K(king), A(ace), Z(Joker)
    // suit encoding: D(diamond), C(club), H(heart), S(spade), B(black joker), R(red joker)

    public CardController Controller { get; private set; }
    public string Rank { get; set; } = string.Empty;
    public string Suit { get; set; } = string.Empty;

    public CardFace(CardController controller)
    {
        Controller = controller;
    }

    public int CompareTo(CardFace other)
    {
        const string rankOrder = "23456789TJQKAZ";
        const string suitOrder = "DCHSBR";

        int rank1Index = rankOrder.IndexOf(Rank);
        int rank2Index = rankOrder.IndexOf(other.Rank);

        if (rank1Index != rank2Index)
            return rank1Index.CompareTo(rank2Index);

        int suit1Index = suitOrder.IndexOf(Suit);
        int suit2Index = suitOrder.IndexOf(other.Suit);

        return suit1Index.CompareTo(suit2Index);
    }

    public static bool operator ==(CardFace a, CardFace b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.CompareTo(b) == 0;
    }

    public static bool operator !=(CardFace a, CardFace b)
    {
        return !(a == b);
    }

    public static bool operator >(CardFace a, CardFace b)
    {
        return a.CompareTo(b) > 0;
    }

    public static bool operator <(CardFace a, CardFace b)
    {
        return a.CompareTo(b) < 0;
    }

    public static bool operator >=(CardFace a, CardFace b)
    {
        return a.CompareTo(b) >= 0;
    }

    public static bool operator <=(CardFace a, CardFace b)
    {
        return a.CompareTo(b) <= 0;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj == null || GetType() != obj.GetType())
            return false;

        return Equals(obj as CardFace);
    }

    public bool Equals(CardFace other)
    {
        return Rank == other.Rank && Suit == other.Suit;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rank, Suit);
    }
}

public class CardController : MonoBehaviour
{
    public CardFace CardFace { get; private set; }
    public Text RankText;
    public Text SuitText;

    private bool _isFacingUp = false;

    public bool IsFacingUp
    {
        get => _isFacingUp;
        set
        {
            _isFacingUp = value;
            UpdateDisplay();
        }
    }

    public string Rank
    {
        get => CardFace.Rank;
        set
        {
            CardFace.Rank = value;
            UpdateDisplay();
        }
    }

    public string Suit
    {
        get => CardFace.Suit;
        set
        {
            CardFace.Suit = value;
            UpdateDisplay();
        }
    }

    public CardController()
    {
        CardFace = new CardFace(this);
    }

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
    }

    private void UpdateDisplay()
    {
        if (_isFacingUp)
        {
            RankText.text = CardFace.Rank;
            SuitText.text = CardFace.Suit;
            RankText.enabled = true;
            SuitText.enabled = true;
        }
        else
        {
            RankText.enabled = false;
            SuitText.enabled = false;
        }
    }
}
