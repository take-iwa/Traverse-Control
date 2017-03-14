using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Printty_win.Plc
{
    class TcpManager
    {
        private String currentServer = null;
        private int currentPort;
        private TcpClient tcpClient = null;
        private NetworkStream stream = null;
        private bool isInProgress = false;

        public TcpManager() { 
           
        }


        public void CloseCurrentSession() {
            stream.Close();
            tcpClient.Close();
            stream = null;
            tcpClient = null;
        }

        public bool getIsInProgress() {
            return isInProgress;
        }

        public async Task SendTcpRequest(String server, int port, String command)
        {
            isInProgress = true;

            try
            {
                if ((currentServer != server || currentPort!=port) && currentServer!=null) {
                    CloseCurrentSession();
                    currentServer = server;
                    currentPort = port;
                }

                if (tcpClient == null)
                {
                    tcpClient = new TcpClient(server, port);
                    stream = tcpClient.GetStream();
                }
                        
                Byte[] data; 
                data = System.Text.Encoding.ASCII.GetBytes(command+"\r");
                stream.Write(data, 0, data.Length);
                data = new Byte[65535];
                //レスポンスデータ不要
                //String responseData = String.Empty;
                //Int32 bytes =  stream.Read(data, 0, data.Length);
                //responseData = System.Text.Encoding.ASCII.GetString(data);
                //responseData = HexStringConverter.convert(responseData);
                //isInProgress = false;
                //return responseData;
                                    
            }
            catch (Exception e)
            {
                isInProgress = false;
                //return "Message: "+e.Message+"\n"+"StackTrace: "+e.StackTrace;
            }
        }

    }
}

