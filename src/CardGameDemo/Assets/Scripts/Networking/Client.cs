using System;
using System.Collections.Generic;
using UnityEngine;


namespace Networking
{
    public class Client : MonoBehaviour
    {
        private SocketClient _socket = null;
        private int _seq = 0;

        // seq -> internal callback (response)
        private readonly Dictionary<int, Action<string>> _c2sHandlers = new();

        // request type name -> internal callback (request -> response)
        private readonly Dictionary<string, Func<string, string>> _s2cHandlers = new();

        void Start()
        {
            _socket = new SocketClient();
            _socket.Connect("127.0.0.1", 8800);
        }

        void Update()
        {
            _socket.Update();
            while (_socket.ReadMsg(out var msg))
            {
                var data = System.Text.Encoding.UTF8.GetString(msg);
                Debug.Log($"connection on message:\r\n{data}");

                // parse S2CData
                S2CData s2cData;
                try
                {
                    s2cData = S2CData.From(data);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Update, parsing S2CData failed: {ex}");
                    continue;
                }

                // handle request or response
                if (s2cData.type == S2CData.DataType.Request)
                {
                    // parse request
                    Request req;
                    try
                    {
                        req = Request.From(s2cData.data);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, parsing Request failed: {ex}");
                        continue;
                    }

                    // call request handler
                    var rspData = string.Empty;
                    if (_s2cHandlers.TryGetValue(req.type, out var handler))
                        rspData = handler.Invoke(req.data);

                    // build response
                    C2SData c2sData;
                    try
                    {
                        Response rsp = Response.Build(req.seq, rspData);
                        c2sData = C2SData.Build(C2SData.DataType.Response, rsp.RawData());
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, building C2SData failed: {ex}");
                        continue;
                    }

                    // send data
                    _socket.SendMsg(System.Text.Encoding.UTF8.GetBytes(c2sData.RawData()));
                }
                else if (s2cData.type == S2CData.DataType.Response)
                {
                    // build response
                    Response rsp;
                    try
                    {
                        rsp = Response.From(s2cData.data);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, building Response failed: {ex}");
                        continue;
                    }

                    // notify
                    if (_c2sHandlers.TryGetValue(rsp.seq, out var callback))
                        callback.Invoke(rsp.data);
                }
            }
        }

        void OnDestroy()
        {
            _socket.Disconnect();
            _socket = null;
        }

        // must be called in main thread
        // return false if sending is failed, callback gives default if failed
        public bool SendRequest<TRequest, TResponse>(TRequest request, Action<TResponse> callback)
        {
            var seq = _seq++;

            // build C2SData
            C2SData c2sData;
            try
            {
                var data = Request.Build(seq, request);
                c2sData = C2SData.Build(C2SData.DataType.Request, data);
            }
            catch (Exception ex)
            {
                Debug.Log($"Request, building C2SData failed: {ex}");
                return false;
            }

            // setup handler
            _c2sHandlers[seq] = new Action<string>((rspRaw) =>
            {
                _c2sHandlers.Remove(seq);
                if (string.IsNullOrEmpty(rspRaw))
                {
                    Debug.Log($"Request, response is null or empty");
                    callback.Invoke(default);
                    return;
                }
                TResponse response;
                try
                {
                    response = JsonUtility.FromJson<TResponse>(rspRaw);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Request, building TResponse failed: {ex}");
                    callback.Invoke(default);
                    return;
                }
                callback.Invoke(response);
            });

            // send
            _socket.SendMsg(System.Text.Encoding.UTF8.GetBytes(c2sData.RawData()));

            return true;
        }

        // must be called in main thread
        public void SetListener<TRequest, TResponse>(Func<TRequest, TResponse> func)
        {
            _s2cHandlers[typeof(TRequest).FullName] = (string reqData) =>
            {
                // parse request
                TRequest req;
                try
                {
                    req = JsonUtility.FromJson<TRequest>(reqData);
                }
                catch (Exception ex)
                {
                    Debug.Log($"_s2cHandlers, parsing TRequest failed: {ex}");
                    return null;
                }

                // invoke handler
                var rsp = func.Invoke(req);

                // build response string
                string rspRaw;
                try
                {
                    rspRaw = JsonUtility.ToJson(rsp);
                }
                catch (Exception ex)
                {
                    Debug.Log($"_s2cHandlers, parsing response string failed: {ex}");
                    return null;
                }

                return rspRaw;
            };
        }

        // must be called in main thread
        public void RemoveListener<TRequest>()
        {
            _s2cHandlers.Remove(typeof(TRequest).FullName);
        }
    }
}
