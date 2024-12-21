import socket
import select
import threading
import time
import protocol


GAME_STATE_START = 0
GAME_STATE_WAITING_FOR_PLAYERS = 1
GAME_STATE_WAITING_FOR_ACTION = 2
GAME_STATE_END = 10000


class GameServer:
    def __init__(
            self,
            port: int = 8800,
            profile_ids: list = [],
            init_net_worth: int = 500):
        self.request_handlers = {
            protocol.HandshakeRequest.type_name(): self.handle_client_handshake_request,
            protocol.DoActionRequest.type_name(): self.handle_client_do_action_request,
        }
        self.response_handlers = {
            protocol.UpdateGameStateResponse.type_name(): self.handle_client_update_game_state_response,
        }

        self.port = port
        self.profile_ids = profile_ids
        self.init_net_worth = init_net_worth

        self.running = False
        self.seq = 0
        self.loop_thread = None
        self.client_profile_ids = {} # conn -> profile id
        self.response_type_names = {} # seq -> type name for the response
        self.response_contexts = {} # seq -> context for the response
        self.game_state = GAME_STATE_START
        self.game_state_info = protocol.GameStateInfo()
        for i in range(len(profile_ids)):
            self.game_state_info.playerInfos.append(protocol.PlayerInfo())

    def start(self):
        print('[+] Server is starting...')
        self.running = True
        self.game_state = GAME_STATE_WAITING_FOR_PLAYERS
        self.loop_thread = threading.Thread(target=self.loop)
        self.loop_thread.start()
        print('[+] Server is started')

    def stop(self):
        print('[+] Server is stopping...')
        self.running = False
        if self.loop_thread is not None:
            while self.loop_thread.is_alive():
                time.sleep(1)
        print('[+] Server stopped')

    def loop(self):
        while self.running:
            socks = []
            print('[+] Server start to create socket')
            try:
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.setblocking(False)
                sock.bind(('', self.port))
                sock.listen()
                socks.append(sock)

                print('[+] Server start to accept')
                msgs = {}
                while self.running:
                    reads, _, exceptions = select.select(socks, [], socks, 1)
                    # handle reads
                    if sock in reads:
                        # handle accept
                        conn, addr = sock.accept()
                        print(f'[+] Server accepted client: {id(conn)}:{addr}')
                        conn.setblocking(False)
                        socks.append(conn)
                        msgs[conn] = b''
                    for read in reads:
                        # handle client
                        if read == sock:
                            continue
                        r = read.recv(2048)
                        if not r:
                            print(f'[-] client {id(read)} disconnect')
                            socks.remove(read)
                            msgs.pop(read, None)
                            continue
                        if msgs.get(read, None) is None:
                            print(f'[-] client {id(read)} no msg buffer')
                            socks.remove(read)
                            msgs.pop(read, None)
                            continue
                        msgs[read] += r
                        if len(msgs[read]) < 2:
                            continue
                        msg_len = msgs[read][0] * 256 + msgs[read][1]
                        if len(msgs[read]) < 2 + msg_len:
                            continue
                        msg_str = msgs[read][2:2 + msg_len].decode("utf-8")
                        msgs[read] = msgs[read][2 + msg_len:]
                        self.handle_client_msg(read, msg_str)
                    # handle exceptions
                    if sock in exceptions:
                        print('[-] server socket select exception')
                        break
                    for exception in exceptions:
                        if exception == sock:
                            continue
                        print(f'[-] client {id(exception)} socket select exception')
                        socks.remove(exception)
                        msgs.pop(exception, None)

                print('[+] Server socket closed')
            except Exception as ex:
                print(f'[-] server socket exception: {ex}')
            finally:
                for s in socks:
                    s.close()
                socks.clear()

    def on_client_request_done(self):
        if self.game_state == GAME_STATE_WAITING_FOR_PLAYERS:
            missing_player = False
            for profile_id in self.profile_ids:
                if profile_id not in self.client_profile_ids.values():
                    missing_player = True
                    break
            if not missing_player:
                self.update_game_state_for_clients()
                self.game_state = GAME_STATE_WAITING_FOR_ACTION
        elif self.game_state == GAME_STATE_WAITING_FOR_ACTION:
            self.update_game_state_for_clients()

    def handle_client_msg(self, conn, msg_str):
        print(f'[+] handle client msg {id(conn)}')
        c2s_data = protocol.C2SData.from_raw_data(msg_str)
        if c2s_data.type == 0:
            request = protocol.Request.from_raw_data(c2s_data.data)
            self.handle_client_request(conn, request)
        elif c2s_data.type == 1:
            response = protocol.Response.from_raw_data(c2s_data.data)
            self.handle_client_response(conn, request)

    def handle_client_request(self, conn, request):
        seq = request.seq
        type_name = request.type
        print(f'[+] handle client request {id(conn)}, {seq}, {type_name}')
        handler = self.request_handlers.get(type_name, None)
        response_data = ''
        if not handler:
            print(f'[-] client {id(conn)} unknown request type {type_name}')
        else:
            response_data = handler(conn, request.data)
        response = protocol.Response(seq, response_data)
        s2c_data = protocol.S2CData(1, response.raw_data())
        GameServer.send_data(conn, s2c_data.raw_data())
        self.on_client_request_done()

    def handle_client_response(self, conn, response):
        seq = response.seq
        print(f'[+] handle client response {id(conn)}, {seq}')
        type_name = self.response_type_names.get(seq, None)
        context = self.response_contexts.get(seq, None)
        if not type_name or not context:
            print(f'[-] client {id(conn)} unknown response sequence {seq}')
            return
        handler = self.response_handlers.get(type_name, None)
        if not handler:
            print(f'[-] client {id(conn)} unknown response type {type_name}')
        handler(conn, response.data)

    def handle_client_handshake_request(self, conn, request_raw):
        request = protocol.HandshakeRequest.from_raw_data(request_raw)

        # check profile id
        profile_id = request.profileId
        if profile_id not in self.profile_ids:
            print(f'[-] unknown profile id: {profile_id}')
            return protocol.HandshakeResponse(success=False).raw_data()
        print(f'[+] handle client handshake {id(conn)}, {profile_id}')

        # remove old connection
        self.client_profile_ids[conn] = profile_id
        for k in list(self.client_profile_ids.keys()):
            if self.client_profile_ids[k] == profile_id and k != conn:
                self.client_profile_ids.pop(k)

        # update player info
        player_id = self.profile_ids.index(profile_id)
        self.game_state_info.playerInfos[player_id].name = request.name

        return protocol.HandshakeResponse(success=True).raw_data()

    def handle_client_do_action_request(self, conn, request_raw):
        # todo
        pass

    def handle_client_update_game_state_response(self, conn, response_raw):
        pass

    def update_game_state_for_clients(self):
        for conn, profile_id in self.client_profile_ids.items():
            if profile_id not in self.profile_ids:
                print(f'[-] update game state for clients, cannot find profile id {profile_id}')
                continue
            player_id = self.profile_ids.index(profile_id)
            update_game_state_info = protocol.GameStateInfo.from_raw_data(self.game_state_info.raw_data())
            update_game_state_info.playerId = player_id
            for i in range(len(update_game_state_info.playerInfos)):
                if i != player_id:
                    update_game_state_info.playerInfos[i].mainHand = []
            # todo: dedicated request function
            request = protocol.Request(
                seq=self.seq,
                type=protocol.UpdateGameStateRequest.type_name(),
                data=update_game_state_info.raw_data())
            self.seq += 1
            s2c_data = protocol.S2CData(0, request.raw_data())
            GameServer.send_data(conn, s2c_data.raw_data())

    def send_data(conn, raw_data):
        try:
            send_data = raw_data.encode('utf-8')
            send_len = len(send_data)
            send_data = bytes([send_len >> 8, send_len % 256]) + send_data
            conn.sendall(send_data)
        except Exception as ex:
            print(f'send data failed: {ex}')


if __name__ == '__main__':
    game_server = GameServer(profile_ids=['aaa'])
    #game_server = GameServer(profile_ids=['aaa', 'bbb', 'ccc', 'ddd'])
    game_server.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print('[+] keyboard interrupt')
    game_server.stop()
