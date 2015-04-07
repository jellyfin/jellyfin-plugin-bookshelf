using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.TVHclient.Helper;
using MediaBrowser.Plugins.TVHclient.HTSP_Responses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace MediaBrowser.Plugins.TVHclient.HTSP
{
    public class HTSConnectionAsync
    {
        private volatile Boolean _connected;
        private volatile int _seq = 0;

        private readonly object _lock;
        private readonly HTSConnectionListener _listener;
        private readonly String _clientName;
        private readonly String _clientVersion;
        private readonly ILogger _logger;

        private int _serverProtocolVersion;
        private string _servername;
        private string _serverversion;
        private string _diskSpace;

        private readonly ByteList _buffer;
        private readonly SizeQueue<HTSMessage> _receivedMessagesQueue;
        private readonly SizeQueue<HTSMessage> _messagesForSendQueue;
        private readonly Dictionary<int, HTSResponseHandler> _responseHandlers;

        private Thread _receiveHandlerThread;
        private Thread _messageBuilderThread;
        private Thread _sendingHandlerThread;
        private Thread _messageDistributorThread;

        private Socket _socket = null;

        public HTSConnectionAsync(HTSConnectionListener listener, String clientName, String clientVersion, ILogger logger)
        {
            _logger = logger;

            _connected = false;
            _lock = new object();

            _listener = listener;
            _clientName = clientName;
            _clientVersion = clientVersion;

            _buffer = new ByteList();
            _receivedMessagesQueue = new SizeQueue<HTSMessage>(int.MaxValue);
            _messagesForSendQueue = new SizeQueue<HTSMessage>(int.MaxValue);
            _responseHandlers = new Dictionary<int, HTSResponseHandler>();

        }

        public void stop()
        {
            if (_receiveHandlerThread != null && _receiveHandlerThread.IsAlive)
            {
                _receiveHandlerThread.Abort();
            }
            if (_messageBuilderThread != null && _messageBuilderThread.IsAlive)
            {
                _messageBuilderThread.Abort();
            }
            if (_sendingHandlerThread != null && _sendingHandlerThread.IsAlive)
            {
                _sendingHandlerThread.Abort();
            }
            if (_messageDistributorThread != null && _messageDistributorThread.IsAlive)
            {
                _messageDistributorThread.Abort();
            }
            if (_socket != null && _socket.Connected)
            {
                _socket.Close();
            }
            _connected = false;
        }

        public void open(String hostname, int port)
        {
            if (_connected)
            {
                return;
            }

            Monitor.Enter(_lock);
            try
            {
                // Establish the remote endpoint for the socket.
                IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // connect to server
                _socket.Connect(remoteEP);

                _connected = true;

                ThreadStart ReceiveHandlerRef = new ThreadStart(ReceiveHandler);
                _receiveHandlerThread = new Thread(ReceiveHandlerRef);
                _receiveHandlerThread.IsBackground = true;
                _receiveHandlerThread.Start();

                ThreadStart MessageBuilderRef = new ThreadStart(MessageBuilder);
                _messageBuilderThread = new Thread(MessageBuilderRef);
                _messageBuilderThread.IsBackground = true;
                _messageBuilderThread.Start();

                ThreadStart SendingHandlerRef = new ThreadStart(SendingHandler);
                _sendingHandlerThread = new Thread(SendingHandlerRef);
                _sendingHandlerThread.IsBackground = true;
                _sendingHandlerThread.Start();

                ThreadStart MessageDistributorRef = new ThreadStart(MessageDistributor);
                _messageDistributorThread = new Thread(MessageDistributorRef);
                _messageDistributorThread.IsBackground = true;
                _messageDistributorThread.Start();
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected exception : {0}", ex.ToString());
                if (_listener != null)
                {
                    _listener.onError(ex);
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        public Boolean authenticate(String username, String password)
        {
           HTSMessage helloMessage = new HTSMessage();
            helloMessage.Method = "hello";
            helloMessage.putField("clientname", _clientName);
            helloMessage.putField("clientversion", _clientVersion);
            helloMessage.putField("htspversion", HTSMessage.HTSP_VERSION);
            helloMessage.putField("username", username);

            LoopBackResponseHandler loopBackResponseHandler = new LoopBackResponseHandler();
            sendMessage(helloMessage, loopBackResponseHandler);
            HTSMessage helloResponse = loopBackResponseHandler.getResponse();
            if (helloResponse != null)
            {
                _serverProtocolVersion = helloResponse.getInt("htspversion");
                _servername = helloResponse.getString("servername");
                _serverversion = helloResponse.getString("serverversion");

                byte[] salt = helloResponse.getByteArray("challenge");
                byte[] digest = SHA1helper.GenerateSaltedSHA1(password, salt);

                HTSMessage authMessage = new HTSMessage();
                authMessage.Method = "authenticate";
                authMessage.putField("username", username);
                authMessage.putField("digest", digest);
                sendMessage(authMessage, loopBackResponseHandler);
                HTSMessage authResponse = loopBackResponseHandler.getResponse();
                if (authResponse != null)
                {
                    Boolean auth = authResponse.getInt("noaccess", 0) != 1;
                    if (auth)
                    {
                        HTSMessage getDiskSpaceMessage = new HTSMessage();
                        getDiskSpaceMessage.Method = "getDiskSpace";
                        sendMessage(getDiskSpaceMessage, loopBackResponseHandler);
                        HTSMessage diskSpaceResponse = loopBackResponseHandler.getResponse();
                        if (diskSpaceResponse != null)
                        {
                            _diskSpace = diskSpaceResponse.getLong("freediskspace") + " / " + diskSpaceResponse.getLong("totaldiskspace");
                        }

                        HTSMessage enableAsyncMetadataMessage = new HTSMessage();
                        enableAsyncMetadataMessage.Method = "enableAsyncMetadata";
                        sendMessage(enableAsyncMetadataMessage, null);
                    }
                    return auth;
                }
            }
            return false;
        }

        public int getServerProtocolVersion()
        {
            return _serverProtocolVersion;
        }

        public string getServername()
        {
            return _servername;
        }

        public string getServerversion()
        {
            return _serverversion;
        }

        public string getDiskspace()
        {
            return _diskSpace;
        }

        public void sendMessage(HTSMessage message, HTSResponseHandler responseHandler)
        {
            // loop the sequence number
            if (_seq == int.MaxValue)
            {
                _seq = int.MinValue;
            }
            else
            {
                _seq++;
            }

            // housekeeping verry old response handlers
            if (_responseHandlers.ContainsKey(_seq))
            {
                _responseHandlers.Remove(_seq);
            }

            message.putField("seq", _seq);
            _messagesForSendQueue.Enqueue(message);
            _responseHandlers.Add(_seq, responseHandler);
        }

        private void SendingHandler()
        {
            while (_connected)
            {
                try
                {
                    HTSMessage message = _messagesForSendQueue.Dequeue();
                    byte[] data2send = message.BuildBytes();
                    int bytesSent = _socket.Send(data2send);
                    if (bytesSent != data2send.Length)
                    {
                        _logger.Error("Sending not complete! \nBytes sent: " + bytesSent + "\nMessage bytes: " + data2send.Length + "\nMessage: " +
                        message.ToString());
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected exception : {0}", ex.ToString());
                    if (_listener != null)
                    {
                        _listener.onError(ex);
                    }
                }
            }
        }

        private void ReceiveHandler()
        {
            byte[] readBuffer = new byte[1024];
            while (_connected)
            {
                try
                {
                    int bytesReveived = _socket.Receive(readBuffer);
                    _buffer.appendCount(readBuffer, bytesReveived);
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected exception : {0}", ex.ToString());
                    if (_listener != null)
                    {
                        _listener.onError(ex);
                    }
                }
            }
        }

        private void MessageBuilder()
        {
            while (_connected)
            {
                byte[] lengthInformation = _buffer.getFromStart(4);
                long messageDataLength = HTSMessage.uIntToLong(lengthInformation[0], lengthInformation[1], lengthInformation[2], lengthInformation[3]);
                byte[] messageData = _buffer.extractFromStart((int)messageDataLength + 4); // should be long !!!
                HTSMessage response = HTSMessage.parse(messageData, _logger);
                _receivedMessagesQueue.Enqueue(response);
            }
        }

        private void MessageDistributor()
        {
            while (_connected)
            {
                HTSMessage response = _receivedMessagesQueue.Dequeue();
                if (response.containsField("seq"))
                {
                    int seqNo = response.getInt("seq");
                    if (_responseHandlers.ContainsKey(seqNo))
                    {
                        HTSResponseHandler currHTSResponseHandler = _responseHandlers[seqNo];
                        if (currHTSResponseHandler != null)
                        {
                            _responseHandlers.Remove(seqNo);
                            currHTSResponseHandler.handleResponse(response);
                        }
                    }
                    else
                    {
                        _logger.Fatal("[TVHclient] MessageDistributor: HTSResponseHandler for seq = '" + seqNo + "' not found!");
                    }
                }
                else
                {
                    // auto update messages
                    if (_listener != null)
                    {
                        _listener.onMessage(response);
                    }
                }
            }
        }
    }
}
