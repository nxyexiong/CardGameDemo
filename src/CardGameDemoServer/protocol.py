import json


class S2CData:
    # type: 0 - request, 1 - response
    # data: content raw string
    def __init__(
            self,
            type: int = 0,
            data: str = ''):
        self.type = type
        self.data = data

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return S2CData(**json.loads(raw_data))


class C2SData:
    # type: 0 - request, 1 - response
    # data: content raw string
    def __init__(
            self,
            type: int = 0,
            data: str = ''):
        self.type = type
        self.data = data

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return C2SData(**json.loads(raw_data))


class Request:
    def __init__(
            self,
            seq: int = -1,
            type: str = '',
            data: str = ''):
        self.seq = seq
        self.type = type
        self.data = data

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return Request(**json.loads(raw_data))


class Response:
    def __init__(
            self,
            seq: int = -1,
            data: str = ''):
        self.seq = seq
        self.data = data

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return Response(**json.loads(raw_data))


class HandshakeRequest:
    def __init__(
            self,
            profileId: str = '',
            name: str = ''):
        self.profileId = profileId
        self.name = name

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return HandshakeRequest(**json.loads(raw_data))
    
    def type_name():
        return 'Networking.HandshakeRequest'


class HandshakeResponse:
    def __init__(
            self,
            success: bool = False):
        self.success = success

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return HandshakeResponse(**json.loads(raw_data))
    
    def type_name():
        return 'Networking.HandshakeResponse'


class UpdateGameStateRequest:
    def __init__(
            self,
            serverTimestampMs: int = -1,
            gameStateInfoDelta: str = '', # need to parse
            availableActions: list = []):
        self.serverTimestampMs = serverTimestampMs
        self.gameStateInfoDelta = gameStateInfoDelta
        self.availableActions = availableActions

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return UpdateGameStateRequest(**json.loads(raw_data))
    
    def type_name():
        return 'Networking.UpdateGameStateRequest'
    
    def get_game_state_info_delta(self):
        return GameStateInfo.from_raw_data(self.gameStateInfoDelta)


class GameStateInfo:
    def __init__(
            self,
            isPlayerIdChanged: bool = False,
            playerId: int = -1,
            playerInfos: list = [], # str list, need to parse
            isDealerChanged: bool = False,
            dealer: int = -1,
            isAggressorChanged: bool = False,
            aggressor: int = -1,
            isActivePlayerChanged: bool = False,
            activePlayer: int = -1,
            isTimerStartTimestampMsChanged: bool = False,
            timerStartTimestampMs: int = -1,
            isTimerIntervalMsChanged: bool = False,
            timerIntervalMs: int = -1):
        self.isPlayerIdChanged = isPlayerIdChanged
        self.playerId = playerId
        self.playerInfos = playerInfos
        self.isDealerChanged = isDealerChanged
        self.dealer = dealer
        self.isAggressorChanged = isAggressorChanged
        self.aggressor = aggressor
        self.isActivePlayerChanged = isActivePlayerChanged
        self.activePlayer = activePlayer
        self.isTimerStartTimestampMsChanged = isTimerStartTimestampMsChanged
        self.timerStartTimestampMs = timerStartTimestampMs
        self.isTimerIntervalMsChanged = isTimerIntervalMsChanged
        self.timerIntervalMs = timerIntervalMs

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return GameStateInfo(**json.loads(raw_data))
    
    def get_player_infos(self):
        ret = []
        for player_info in self.playerInfos:
            ret.append(PlayerInfo.from_raw_data(player_info))
        return ret


class PlayerInfo:
    def __init__(
            self,
            isNameChanged: bool = False,
            name: str = '',
            isNetWorthChanged: bool = False,
            netWorth: int = 0,
            isBetChanged: bool = False,
            bet: int = 0,
            isIsFoldedChanged: bool = False,
            isFolded: bool = False,
            isMainHandChanged: bool = False,
            mainHand: list = []):
        self.isNameChanged = isNameChanged
        self.name = name
        self.isNetWorthChanged = isNetWorthChanged
        self.netWorth = netWorth
        self.isBetChanged = isBetChanged
        self.bet = bet
        self.isIsFoldedChanged = isIsFoldedChanged
        self.isFolded = isFolded
        self.isMainHandChanged = isMainHandChanged
        self.mainHand = mainHand

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return PlayerInfo(**json.loads(raw_data))


class UpdateGameStateResponse:
    def __init__(
            self,
            success: bool = False):
        self.success = success

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return UpdateGameStateResponse(**json.loads(raw_data))
    
    def type_name():
        return 'Networking.UpdateGameStateResponse'


class DoActionRequest:
    def __init__(
            self,
            action: str = '',
            data: str = ''):
        self.action = action
        self.data = data

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return DoActionRequest(**json.loads(raw_data))
    
    def type_name():
        return 'Networking.DoActionRequest'


class DoActionResponse:
    def __init__(
            self,
            success: bool = False):
        self.success = success

    def raw_data(self):
        return json.dumps(self.__dict__)

    def from_raw_data(raw_data: str):
        return DoActionResponse(**json.loads(raw_data))
    
    def type_name():
        return 'Networking.DoActionResponse'


if __name__ == '__main__':
    obj = HandshakeRequest(profileId='abc')
    raw = obj.raw_data()
    print(raw)
    obj = HandshakeRequest.from_raw_data(raw)
    print(obj.profileId)
