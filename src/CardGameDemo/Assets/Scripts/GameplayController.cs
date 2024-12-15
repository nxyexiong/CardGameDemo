using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Networking;
using UnityEngine;
using UnityEngine.UI;

public class GameplayController : MonoBehaviour
{
    private enum GameState
    {
        Start,
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

    private Networking.Client _client;
    private long _tsDelta = 0; // server time - local time
    private Networking.GameStateInfo _localGameStateInfo = new();
    private GameState _gameState = GameState.Start;
    private readonly List<GameObject> _mainHandCards = new(); // card objects
    private readonly List<GameObject> _actions = new(); // action button objects

    void Start()
    {
        // init client
        _client = GetComponent<Networking.Client>();
        _client.SetListener((UpdateGameStateRequest r) => OnUpdateGameStateRequest(r));

        // next state
        _gameState = GameState.WaitingForServer;
    }

    void Update()
    {
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
            _tsDelta = request.ServerTimestampMs - Common.TimeUtils.GetTimestampMs(DateTime.Now);

            // update game state
            _localGameStateInfo.ApplyDelta(request.GameStateInfoDelta);
            UpdateGameState();

            // update action list
            UpdateActionList(request.AvailableActions);

            // next state
            if (request.AvailableActions.Count > 0)
                _gameState = GameState.WaitingForAction;
        }
        else
        {
            Debug.LogError($"OnUpdateGameStateRequest, " +
                $"unknown request {request.GetType().FullName} for state {_gameState}");
        }

        return new UpdateGameStateResponse();
    }

    private void UpdateGameState()
    {
        // set global state
        DealerText.text = _localGameStateInfo.PlayerInfos[_localGameStateInfo.Dealer].Name;
        AggressorText.text = _localGameStateInfo.PlayerInfos[_localGameStateInfo.Aggressor].Name;
        ActivePlayerText.text = _localGameStateInfo.PlayerInfos[_localGameStateInfo.ActivePlayer].Name;
        SetTimer();

        // sort players
        var playerCount = _localGameStateInfo.PlayerInfos.Count;
        var playerInfos = new List<Networking.PlayerInfo>();
        var curId = _localGameStateInfo.PlayerId;
        for (var i = 0; i < playerCount; i++)
        {
            var playerInfo = _localGameStateInfo.PlayerInfos[curId++ % playerCount];
            playerInfos.Add(playerInfo);
        }

        // set player state
        if (playerInfos.Count > 0)
        {
            Player0NameText.text = playerInfos[0].Name;
            Player0NetWorthText.text = playerInfos[0].NetWorth.ToString();
            Player0BetText.text = playerInfos[0].Bet.ToString();
            Player0IsFoldedText.text = playerInfos[0].IsFolded.ToString();
            UpdateMainHand(playerInfos[0].MainHand);
        }
        if (playerInfos.Count > 1)
        {
            Player1NameText.text = playerInfos[1].Name;
            Player1NetWorthText.text = playerInfos[1].NetWorth.ToString();
            Player1BetText.text = playerInfos[1].Bet.ToString();
            Player1IsFoldedText.text = playerInfos[1].IsFolded.ToString();
        }
        if (playerInfos.Count > 2)
        {
            Player2NameText.text = playerInfos[2].Name;
            Player2NetWorthText.text = playerInfos[2].NetWorth.ToString();
            Player2BetText.text = playerInfos[2].Bet.ToString();
            Player2IsFoldedText.text = playerInfos[2].IsFolded.ToString();
        }
        if (playerInfos.Count > 3)
        {
            Player3NameText.text = playerInfos[3].Name;
            Player3NetWorthText.text = playerInfos[3].NetWorth.ToString();
            Player3BetText.text = playerInfos[3].Bet.ToString();
            Player3IsFoldedText.text = playerInfos[3].IsFolded.ToString();
        }
    }

    private void SetTimer()
    {
        var nowMs = Common.TimeUtils.GetTimestampMs(DateTime.Now);
        var startTimeMs = _localGameStateInfo.TimerStartTimestampMs;
        var intervalMs = _localGameStateInfo.TimerIntervalMs;
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
            var request = new DoActionRequest { Action = action.ToString() };
            //if (action == Action.xxx)
            //{
            //    // fill data if needed
            //}
            _client.Request<DoActionRequest, DoActionResponse>(request).ContinueWith((rspTask) =>
            {
                OnActionComplete(action, data, rspTask.Result.Success);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            Debug.LogError($"DoAction, " +
                $"unknown action {action} for state {_gameState}");
        }
    }

    private void OnActionComplete(Action action, string data, bool success)
    {
        if (success)
        {
            _gameState = GameState.WaitingForServer;
            UpdateActionList(new List<string>());
        }
        else
        {
            Debug.LogError($"OnActionComplete, " +
                $"action denied {action}:{data} for state {_gameState}");
        }
    }
}
