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

namespace TestClientCommunication
{
    public partial class FrmClient : Form
    {
        private string mServerIP;
        private int mServerPort;

        public FrmClient()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            UDPReceive();
        }

        private void UDPReceive()
        {
            Task.Factory.StartNew(() =>
            {
                UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, int.Parse(textBox3.Text)));
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] buff = client.Receive(ref endpoint);
                    string msg = Encoding.UTF8.GetString(buff);
                    rtxt_ShowMsg.Invoke(new Action(() =>
                    {
                        rtxt_ShowMsg.AppendText(msg);
                    }));
                    if (msg.StartsWith("约定好的标识字符串"))
                    {
                        string[] arrMsg = msg.Split('|');
                        mServerIP = arrMsg[1];
                        mServerPort = int.Parse(arrMsg[2]);
                        if(radioButton1.Checked)
                        {
                            TCPRequestBySocket();
                        }
                        else
                        {
                            TCPRequestByTcpClient();
                        }
                        break;
                    }
                    Thread.Sleep(100);
                }
            });
        }

        private void TCPRequestBySocket()
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(mServerIP), mServerPort);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(serverEndPoint);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    byte[] buffer = new byte[2048];
                    int iRealLength = socket.Receive(buffer, SocketFlags.None);
                    if (iRealLength > 0)
                    {
                        string strMsg = Encoding.UTF8.GetString(buffer);
                        rtxt_ShowMsg.Invoke(new Action(() =>
                        {
                            rtxt_ShowMsg.AppendText("收到服务器数据=" + strMsg);
                        }));

                        //将数据再发回去
                        socket.Send(buffer);
                    }
                }
            });
        }

        private void TCPRequestByTcpClient()
        {
            TcpClient tcpClient = new TcpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(mServerIP), mServerPort);
            tcpClient.Connect(serverEndPoint);
            //接收数据
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    NetworkStream ns = tcpClient.GetStream();
                    List<byte> list = new List<byte>();
                    byte[] buffer = new byte[2048];
                    while (ns.Read(buffer, 0, buffer.Length) > 0)
                    {
                        list.AddRange(buffer);
                    }
                    if (list.Count > 0)
                    {
                        string strMsg = Encoding.UTF8.GetString(list.ToArray());
                        rtxt_ShowMsg.Invoke(new Action(() =>
                        {
                            rtxt_ShowMsg.AppendText("收到服务器数据=" + strMsg);
                        }));

                        //将数据再发回去
                        ns.Write(list.ToArray(), 0, list.Count);
                        ns.Flush();
                    }
                }
            });
        }
    }
}
