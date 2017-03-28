using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Bridge_full
{
    class TcpManager
    {
        private TcpClient tcpClient = null;
        private NetworkStream stream = null;

        public TcpManager()
        {

        }

        public void CloseCurrentSession()
        {
            if (stream != null)
            {
                stream.Close();
                tcpClient.Close();
                stream = null;
                tcpClient = null;
            }
        }

        /*------------------------------------*/
        /* TCPクライアント 送受信                */
        /*------------------------------------*/
        public Byte[] SendTcpClient(String server, Int32 port, Byte[] command)
        {
            Byte[] data = new Byte[256];

            try
            {
                // 接続
                if (tcpClient == null)
                {
                    IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, 2049);
                    TcpClient tcpClient = new TcpClient(ipLocalEndPoint);
                    tcpClient.Connect(server, port);

                    //tcpClient = new TcpClient(server, port);
                    Console.WriteLine("サーバー({0}:{1})と接続しました({2}:{3})。",
                    ((System.Net.IPEndPoint)tcpClient.Client.RemoteEndPoint).Address,
                    ((System.Net.IPEndPoint)tcpClient.Client.RemoteEndPoint).Port,
                    ((System.Net.IPEndPoint)tcpClient.Client.LocalEndPoint).Address,
                    ((System.Net.IPEndPoint)tcpClient.Client.LocalEndPoint).Port);
                    stream = tcpClient.GetStream();
                }

                // コマンド送信
                stream.Write(command, 0, command.Length);

                // レスポンス受信
                Int32 resSize = stream.Read(data, 0, data.Length);
                if (resSize == 0)
                {
                    MessageBox.Show("Error: Recv Size = 0\r\n");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Message: " + e.Message + "\n" + "StackTrace: " + e.StackTrace + "\r\n");
            }
            finally
            {
                // Close everything.
                CloseCurrentSession();
            }

            return data;
        }
    }
}
