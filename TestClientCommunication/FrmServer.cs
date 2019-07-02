using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloseReason = SuperSocket.SocketBase.CloseReason;

namespace TestClientCommunication
{
    public partial class FrmServer : Form
    {
        //取消UDP广播
        private static bool mCancelBroadcast = false;

        public FrmServer()
        {
            InitializeComponent();
        }

        private void FrmServer_Load(object sender, EventArgs e)
        {

        }

        #region UDP广播
        private void UDPBroadcast()
        {
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            //这个IP和端口代表向当前主机所在的网段中广播，且接收者的端口为7788
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(textBox4.Text), int.Parse(textBox3.Text));
            //广播内容包括一串标识符，服务端IP，服务端Port
            byte[] buffer = Encoding.UTF8.GetBytes($"约定好的标识字符串|{textBox1.Text}|{textBox2.Text}");

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (mCancelBroadcast)
                    {
                        break;
                    }

                    client.Send(buffer, buffer.Length, endpoint);
                    Thread.Sleep(1000);
                }
            });
        }
        #endregion


        #region TCP——使用Socket
        private void TCPListenBySocket()
        {
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, int.Parse(textBox2.Text));
            server.Bind(ipEnd);
            server.Listen(10);
            server.BeginAccept(new AsyncCallback(TCPClientConnected_Socket),server);
        }

        private void TCPClientConnected_Socket(IAsyncResult asyncResult)
        {
            Socket server = (Socket)asyncResult.AsyncState;
            Socket client = server.EndAccept(asyncResult);
            rtxt_ShowMsg.Invoke(new Action(() =>
            {
                rtxt_ShowMsg.AppendText("新客户端连接，客户端地址=" + client.RemoteEndPoint);
            }));

            Task.Factory.StartNew(() => {
                while(true)
                {
                    TCPReceiveClientMsg_Socket(client);
                    Thread.Sleep(1000);
                }
            });
        }

        private void TCPReceiveClientMsg_Socket(Socket socket)
        {
            byte[] buffer = new byte[socket.ReceiveBufferSize];
            int iRealLength = socket.Receive(buffer, SocketFlags.None);
            if (iRealLength > 0)
            {
                string strMsg = Encoding.UTF8.GetString(buffer);
                rtxt_ShowMsg.Invoke(new Action(() =>
                {
                    rtxt_ShowMsg.AppendText(strMsg);
                }));
            }
        }
        #endregion

        #region TCP——使用TcpListener
        private void TCPListenByTcpListener()
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            TcpListener tcpListener = new TcpListener(localEndPoint);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPClientConnected), tcpListener);
        }

        private void TCPClientConnected(IAsyncResult asyncResult)
        {
            //监听到一个新的客户端连接
            TcpListener tcpListener = asyncResult.AsyncState as TcpListener;
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(asyncResult);
            rtxt_ShowMsg.Invoke(new Action(() =>
            {
                rtxt_ShowMsg.AppendText("新客户端连接，客户端地址=" + tcpClient.Client.RemoteEndPoint);
            }));

            //开启一个线程专门接收此客户端的消息
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    TCPReceiveClientMsg(tcpClient);
                    Thread.Sleep(1000);
                }
            });
            //再次开始监听其它的客户端连接
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPClientConnected), tcpListener);
        }

        //接收到客户端发来的消息
        private void TCPReceiveClientMsg(TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();
            List<byte> list = new List<byte>();
            byte[] buffer = new byte[2048];
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
                list.AddRange(buffer);
            }
            if (list.Count > 0)
            {
                string strMsg = Encoding.UTF8.GetString(list.ToArray());
                rtxt_ShowMsg.Invoke(new Action(() =>
                {
                    rtxt_ShowMsg.AppendText($"客户端[{tcpClient.Client.RemoteEndPoint}]发来消息={strMsg}");
                }));
            }
        }
        #endregion

        #region TCP——使用SuperSocket
        private AppServer mAppServer;
        private void TCPListenBySuperSocket()
        {
            mAppServer = new AppServer();
            mAppServer.NewSessionConnected += AppServer_NewSessionConnected;
            mAppServer.SessionClosed += AppServer_SessionClosed;
            mAppServer.NewRequestReceived += AppServer_NewRequestReceived;
            IServerConfig serverConfig = new ServerConfig()
            {
                Ip = textBox1.Text,
                Port = int.Parse(textBox2.Text),
                TextEncoding = "UTF-8"
            };
            bool bSetupResult = mAppServer.Setup(serverConfig);

            if (!bSetupResult)//绑定端口
            {
                rtxt_ShowMsg.AppendText("绑定端口失败!");
            }
            if (!mAppServer.Start())//启动服务
            {
                rtxt_ShowMsg.AppendText("服务启动失败!");
            }
        }

        //新的客户端连接
        private void AppServer_NewSessionConnected(AppSession session)
        {
            rtxt_ShowMsg.AppendText("新客户端连接，客户端地址=" + session.RemoteEndPoint);
        }

        //客户端断开连接
        private void AppServer_SessionClosed(AppSession session, CloseReason value)
        {
            mCancelBroadcast = true;
            rtxt_ShowMsg.AppendText("客户端断开连接，客户端地址=" + session.RemoteEndPoint);
        }

        //客户端发来消息
        private void AppServer_NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            rtxt_ShowMsg.AppendText($"客户端消息={requestInfo.Body}，客户端地址={ session.RemoteEndPoint}");
        }
        #endregion

        private void Button1_Click(object sender, EventArgs e)
        {
            //TCP监听
            if(radioButton1.Checked)
            {
                TCPListenBySocket();
            }
            else if(radioButton2.Checked)
            {
                TCPListenByTcpListener();
            }
            else
            {
                TCPListenBySuperSocket();
            }

            //UDP广播
            UDPBroadcast();
        }
    }
}
