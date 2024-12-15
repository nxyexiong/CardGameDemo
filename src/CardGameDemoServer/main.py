import socket


def main():
    pass


if __name__ == '__main__':
    main()


# test
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.bind(('', 8800))
sock.listen()
conn, addr = sock.accept()
with conn:
    print('connected')
    while True:
        conn.send(b'\x00\x04test')
        #data = conn.recv(2048)
