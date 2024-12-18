using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Networking;

public class GameplayController : MonoBehaviour
{
    private enum GameState
    {
        Start,
        WaitingForHandshake,
        WaitingForServer,
        WaitingForAction,
        GameOver,
    }

    private enum Action
    {
        FollowBet,
        RaiseBet,
        Fold,
        Showdown,
    }

    public Text DealerText;
    public Text AggressorText;
    public Text ActivePlayerText;
    public Text TimerText;

    public Text Player0NameText;
    public Text Player0NetWorthText;
    public Text Player0BetText;
    public Text Player0IsFoldedText;

    public Text Player1NameText;
    public Text Player1NetWorthText;
    public Text Player1BetText;
    public Text Player1IsFoldedText;

    public Text Player2NameText;
    public Text Player2NetWorthText;
    public Text Player2BetText;
    public Text Player2IsFoldedText;

    public Text Player3NameText;
    public Text Player3NetWorthText;
    public Text Player3BetText;
    public Text Player3IsFoldedText;

    public Text InstructionText;
    public GameObject CardPrefab;
    public GameObject ActionButtonPrefab;
    public Transform MainHandTransform;
    public Transform ActionTransform;

    private string _profileId = string.Empty;
    private string _name = string.Empty;
    private Client _client;
    private long _tsDelta = 0; // server time - local time
    private GameStateInfo _localGameStateInfo = new();
    private GameState _gameState = GameState.Start;
    private readonly List<GameObject> _mainHandCards = new(); // card objects
    private readonly List<GameObject> _actions = new(); // action button objects

    void Start()
    {
        // TODO: profile id
        _profileId = PlayerPrefs.GetString("PlayerName") ?? string.Empty;
        _name = PlayerPrefs.GetString("PlayerName") ?? string.Empty;

        // init client
        _client = GetComponent<Client>();
        _client.SetListener((UpdateGameStateRequest r) => OnUpdateGameStateRequest(r));

        // next state
        _gameState = GameState.Start;
    }

    void Update()
    {
        if (_gameState == GameState.Start)
        {
            var request = new HandshakeRequest { profileId = _profileId, name = _name };
            _ = _client.SendRequest(request, (HandshakeResponse response) =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"HandshakeResponse is null or not success: " +
                        $"{response?.success.ToString() ?? "null"}");
                    _gameState = GameState.Start;
                    return;
                }
                _gameState = GameState.WaitingForServer;
            });
            _gameState = GameState.WaitingForHandshake;
        }
    }

    void OnDestroy()
    {
        _client.RemoveListener<UpdateGameStateRequest>();
    }

    private UpdateGameStateResponse OnUpdateGameStateRequest(UpdateGameStateRequest request)
    {
        if (_gameState == GameState.WaitingForServer)
        {
            // sync time
            _tsDelta = request.serverTimestampMs - Common.TimeUtils.GetTimestampMs(DateTime.Now);

            // update game state
            UpdateGameState(request.gameStateInfo);

            // update action list
            UpdateActionList(request.availableActions);

            // next state
            if (request.availableActions.Count > 0)
                _gameState = GameState.WaitingForAction;

            return new UpdateGameStateResponse { success = true };
        }
        else
        {
            Debug.LogError($"OnUpdateGameStateRequest, " +
                $"unknown request {request.GetType().FullName} for state {_gameState}");

            return new UpdateGameStateResponse { success = false };
        }
    }

    private void UpdateGameState(GameStateInfo gameStateInfo)
    {
        _localGameStateInfo = gameStateInfo;
        _localGameStateInfo.playerInfos.Clear();

        // set global state
        DealerText.text = gameStateInfo.playerInfos[gameStateInfo.dealer].name;
        AggressorText.text = gameStateInfo.playerInfos[gameStateInfo.aggressor].name;
        ActivePlayerText.text = gameStateInfo.playerInfos[gameStateInfo.activePlayer].name;
        SetTimer(gameStateInfo);

        // sort players
        var playerCount = gameStateInfo.playerInfos.Count;
        var playerInfos = new List<PlayerInfo>();
        var curId = gameStateInfo.playerId;
        for (var i = 0; i < playerCount; i++)
        {
            var playerInfo = gameStateInfo.playerInfos[curId++ % playerCount];
            playerInfos.Add(playerInfo);
            _localGameStateInfo.playerInfos.Add(playerInfo);
        }

        // TODO: hide or show player info panels

        // set player state
        if (playerInfos.Count > 0)
        {
            Player0NameText.text = playerInfos[0].name;
            Player0NetWorthText.text = playerInfos[0].netWorth.ToString();
            Player0BetText.text = playerInfos[0].bet.ToString();
            Player0IsFoldedText.text = playerInfos[0].isFolded.ToString();
            UpdateMainHand(playerInfos[0].mainHand);
        }
        if (playerInfos.Count > 1)
        {
            Player1NameText.text = playerInfos[1].name;
            Player1NetWorthText.text = playerInfos[1].netWorth.ToString();
            Player1BetText.text = playerInfos[1].bet.ToString();
            Player1IsFoldedText.text = playerInfos[1].isFolded.ToString();
        }
        if (playerInfos.Count > 2)
        {
            Player2NameText.text = playerInfos[2].name;
            Player2NetWorthText.text = playerInfos[2].netWorth.ToString();
            Player2BetText.text = playerInfos[2].bet.ToString();
            Player2IsFoldedText.text = playerInfos[2].isFolded.ToString();
        }
        if (playerInfos.Count > 3)
        {
            Player3NameText.text = playerInfos[3].name;
            Player3NetWorthText.text = playerInfos[3].netWorth.ToString();
            Player3BetText.text = playerInfos[3].bet.ToString();
            Player3IsFoldedText.text = playerInfos[3].isFolded.ToString();
        }
    }

    private void SetTimer(GameStateInfo gameStateInfo)
    {
        var nowMs = Common.TimeUtils.GetTimestampMs(DateTime.Now);
        var startTimeMs = gameStateInfo.timerStartTimestampMs;
        var intervalMs = gameStateInfo.timerIntervalMs;
        var timeLeftMs = startTimeMs + intervalMs - nowMs;

        if (timeLeftMs <= 0)
            TimerText.text = "waiting...";
        else
            TimerText.text = (timeLeftMs / 1000).ToString();
    }

    private void UpdateMainHand(IEnumerable<string> cardFaceRaws)
    {
        // add or remove objects
        var newCardCount = cardFaceRaws.Count();
        while (_mainHandCards.Count > newCardCount)
        {
            Destroy(_mainHandCards.First());
            _mainHandCards.RemoveAt(0);
        }
        while (_mainHandCards.Count < newCardCount)
        {
            var card = Instantiate(CardPrefab);
            card.transform.SetParent(MainHandTransform, false);
            _mainHandCards.Add(card);
        }

        // set position
        for (var i = 0; i < _mainHandCards.Count; i++)
        {
            var card = _mainHandCards[i];
            var cardRectTransform = card.GetComponent<RectTransform>();
            var width = cardRectTransform.rect.width;
            var height = cardRectTransform.rect.height;
            cardRectTransform.anchoredPosition = new Vector2(
                width * (0.5f + 1.0f * i) + 5.0f * i,
                -height * 0.5f);
        }

        // set state
        for (var i = 0; i < _mainHandCards.Count; i++)
        {
            var card = _mainHandCards[i];
            var cardController = card.GetComponent<CardController>();
            cardController.SetCardFaceFromRaw(cardFaceRaws.ElementAt(i));
            cardController.IsFacingUp = true;
        }
    }

    private void UpdateActionList(IEnumerable<string> actionRaws)
    {
        // add or remove objects
        var newActionCount = actionRaws.Count();
        while (_actions.Count > newActionCount)
        {
            Destroy(_actions.First());
            _actions.RemoveAt(0);
        }
        while (_actions.Count < newActionCount)
        {
            var actionBtn = Instantiate(ActionButtonPrefab);
            actionBtn.transform.SetParent(ActionTransform, false);
            _actions.Add(actionBtn);
        }

        // set position
        for (var i = 0; i < _actions.Count; i++)
        {
            var actionBtn = _actions[i];
            var actionBtnRectTransform = actionBtn.GetComponent<RectTransform>();
            var width = actionBtnRectTransform.rect.width;
            var height = actionBtnRectTransform.rect.height;
            actionBtnRectTransform.anchoredPosition = new Vector2(
                width * 0.5f,
                -height * (0.5f + 1.0f * i) + 5.0f * i);
        }

        // set content
        for (var i = 0; i < _actions.Count; i++)
        {
            var actionBtnController = _actions[i].GetComponent<ActionButtonController>();
            var actionRaw = actionRaws.ElementAt(i);
            actionBtnController.ActionName = actionRaw;
            actionBtnController.Text.text = GetActionButtonText(actionRaw);
        }

        // add listener
        for (var i = 0; i < _actions.Count; i++)
        {
            var actionBtn = _actions[i];
            actionBtn.GetComponent<Button>().onClick.AddListener(() => OnActionButtonClick(actionBtn));
        }
    }

    private string GetActionButtonText(string actionRaw)
    {
        if (!Enum.TryParse(actionRaw, out Action action))
            return "unknown";
        if (action == Action.FollowBet)
            return "Follow bet";
        else if (action == Action.RaiseBet)
            return "Raise bet";
        else if (action == Action.Fold)
            return "Fold";
        else if (action == Action.Showdown)
            return "Showdown";
        return "unassigned";
    }

    private void OnActionButtonClick(GameObject actionBtn)
    {
        var actionBtnController = actionBtn.GetComponent<ActionButtonController>();
        var actionRaw = actionBtnController.ActionName;
        if (Enum.TryParse(actionRaw, out Action action))
        {
            DoAction(action, string.Empty);
            return;
        }
        Debug.LogError($"OnActionButtonClick, unknown action {actionRaw}");
    }

    private void DoAction(Action action, string data)
    {
        if (_gameState == GameState.WaitingForAction)
        {
            // build request
            var request = new DoActionRequest { action = action.ToString() };
            //if (action == Action.xxx)
            //{
            //    // fill data if needed
            //}
            _client.SendRequest(request, (DoActionResponse response) =>
            {
                if (response == null || !response.success)
                {
                    Debug.LogError($"DoAction, response is null or not success: " +
                        $"{response?.success.ToString() ?? "null"}");
                    return;
                }
                OnActionComplete(action, data);
            });
        }
        else
        {
            Debug.LogError($"DoAction, " +
                $"unknown action {action} for state {_gameState}");
        }
    }

    private void OnActionComplete(Action action, string data)
    {
        _gameState = GameState.WaitingForServer;
        UpdateActionList(new List<string>());
    }
}
