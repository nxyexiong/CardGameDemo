using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;


namespace Networking
{
    public class SocketClient
    {
        private const int _bufSize = 2048;

        private Socket _socket;
        private bool _isIpv6;
        private IPEndPoint _endPoint;
        private readonly List<byte[]> _sendingQueue;
        private readonly List<byte[]> _recvingQueue;

        public SocketClient()
        {
            _socket = null;
            _isIpv6 = false;
            _endPoint = null;
            _sendingQueue = new List<byte[]>();
            _recvingQueue = new List<byte[]>();
        }

        public void Connect(string ip, int port, bool isIpv6 = false)
        {
            Disconnect();
            _isIpv6 = isIpv6;
            _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Disconnect()
        {
            _endPoint = null;
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
        }

        public void Update()
        {
            if (_endPoint == null)
            {
                Disconnect();
                return;
            }

            var disconnected = false;
            do
            {
                // create socket if needed
                if (_socket == null)
                {
                    _socket = new Socket(
                        _isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    _socket.Blocking = false;
                    try
                    {
                        _socket.Connect(_endPoint);
                    }
                    catch (SocketException sockEx)
                    {
                        if (sockEx.SocketErrorCode != SocketError.WouldBlock){
                            disconnected = true;
                            break;
                        }
                    }
                }

                try
                {
                    // handle error
                    if (_socket.Poll(0, SelectMode.SelectError))
                    {
                        disconnected = true;
                        break;
                    }

                    // handle read
                    if (_socket.Poll(0, SelectMode.SelectRead))
                    {
                        var buf = new byte[_bufSize];
                        var msg = new List<byte>();
                        var r = _socket.Receive(buf);
                        if (r <= 0)
                        {
                            disconnected = true;
                            break;
                        }
                        msg.AddRange(buf.Take(r));
                        if (msg.Count < 2) continue;
                        var len = msg[0] * 256 + msg[1];
                        if (msg.Count < len + 2) continue;
                        _recvingQueue.Add(msg.Skip(2).Take(len).ToArray());
                        msg.RemoveRange(0, len + 2);
                    }

                    // handle write
                    if (_socket.Poll(0, SelectMode.SelectWrite))
                    {
                        var data = _sendingQueue.FirstOrDefault();
                        if (data != null)
                        {
                            int w = _socket.Send(data);
                            if (w <= 0)
                            {
                                disconnected = true;
                                break;
                            }
                            _sendingQueue.Insert(0, data.Skip(w).ToArray());
                        }
                    }
                }
                catch
                {
                    disconnected = true;
                    break;
                }
            }
            while (false);

            if (disconnected)
            {
                _socket?.Close();
                _socket = null;
            }
        }

        public void SendMsg(byte[] msg)
        {
            _sendingQueue.Add(msg);
        }

        public bool ReadMsg(out byte[] msg)
        {
            msg = _recvingQueue.FirstOrDefault();
            if (msg != null)
                _recvingQueue.RemoveAt(0);
            return msg != null;
        }
    }
}