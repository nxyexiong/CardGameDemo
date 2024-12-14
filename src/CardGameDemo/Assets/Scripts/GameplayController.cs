using UnityEngine;
using UnityEngine.UI;

public class GameplayController : MonoBehaviour
{
    public GameObject CardPrefab;
    public Transform MainHandTransform;

    void Start()
    {
        var card = Instantiate(CardPrefab);
        card.transform.SetParent(MainHandTransform, false);
        var cardRectTransform = card.GetComponent<RectTransform>();
        cardRectTransform.anchoredPosition = new Vector2(0, 0);
        var cardController = card.GetComponent<CardController>();
        cardController.Rank = "5";
        cardController.Suit = "H";
        cardController.IsFacingUp = true;

        card = Instantiate(CardPrefab);
        card.transform.SetParent(MainHandTransform, false);
        cardRectTransform = card.GetComponent<RectTransform>();
        cardRectTransform.anchoredPosition = new Vector2(cardRectTransform.rect.width * 1.1f, 0);
        cardController = card.GetComponent<CardController>();
        cardController.Rank = "T";
        cardController.Suit = "S";
        cardController.IsFacingUp = true;

        card = Instantiate(CardPrefab);
        card.transform.SetParent(MainHandTransform, false);
        cardRectTransform = card.GetComponent<RectTransform>();
        cardRectTransform.anchoredPosition = new Vector2(cardRectTransform.rect.width * 2.2f, 0);
        cardController = card.GetComponent<CardController>();
        cardController.Rank = "Z";
        cardController.Suit = "R";
        cardController.IsFacingUp = true;
    }

    void Update()
    {
    }
}
