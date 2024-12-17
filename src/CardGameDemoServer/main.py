import time
from game_server import GameServer


def main():
    game_server = GameServer(profile_ids=['aaa', 'bbb', 'ccc', 'ddd'])
    game_server.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print('[+] keyboard interrupt')
    game_server.stop()


if __name__ == '__main__':
    main()
