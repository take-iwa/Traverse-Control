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
        private Byte sirialNum = 0x00;

        public TcpManager()
        {

        }
        
        public void CloseCurrentSession()
        {
            stream.Close();
            tcpClient.Close();
            stream = null;
            tcpClient = null;
        }

        /*------------------------------------*/
        /* TCPクライアント 送受信                */
        /*------------------------------------*/
        public void SendTcpRequest(String server, Int32 port, Byte command)
        {
            try
            {
                // 接続
                if (tcpClient == null)
                {
                    tcpClient = new TcpClient(server, port);
                    stream = tcpClient.GetStream();
                }

                // コマンドデータの作成
                Byte[] data;
                data = MakeCommandData(command);

                // コマンド送信
                stream.Write(data, 0, data.Length);

                // レスポンス受信
                data = new Byte[2048];
                String responseData = String.Empty;
                Int32 resSize = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data);

                // 応答データの確認
                Int32 rCheck = CheckResponseData(responseData, resSize);
                if(rCheck != 0)
                {
                    MessageBox.Show("Error: Recv !! -> " + rCheck + "\n");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Message: " + e.Message + "\n" + "StackTrace: " + e.StackTrace + "\r\n");
            }

            // シリアル番号更新
            if ((sirialNum + 1) < 0xff)
            {
                sirialNum++;
            }
            else
            {
                sirialNum = 0x00;
            }

            // 応答データの取得
            //resbuf[20];

        }
        /*---------------------------------*/
        /* 応答データのチェック               */
        /*---------------------------------*/

        private Int32 CheckResponseData(String resbuf, Int32 reslen)
        {
            Int32 rc;

            rc = 0;

            // 全データ長のチェック
            if (reslen != 40)   // 10 ワードの読み出しに対し，応答は 40 バイト(218ヘッダ(12バイト)+メモバスデータ(28バイト))
            {
                rc = -1;
                return (rc);
            }
            // パケットタイプチェック
            if (resbuf[0] != 0x19)    // メモバス応答でない
            {
                rc = -2;
                return (rc);
            }
            // シリアル番号チェック
            if (sirialNum != resbuf[1]) // コマンドのシリアル番号と一致していない
            {
                rc = -3;
                return (rc);
            }
            // 伝文中の全データ長のチェック
            if ((resbuf[6] != 0x28) && (resbuf[7] != 0x00)) // 40 バイト = 218 ヘッダ (12 バイト )+ メモバスデータ (28 バイト )
            {
                rc = -4;
                return (rc);
            }
            // メモバスデータ長チェック
            if ((resbuf[12] != 0x1A) || (resbuf[13] != 0x00))   // 26 バイト
            {
                rc = -5;
                return (rc);
            }
            // MFC のチェック
            if (resbuf[14] != 0x20)   // MFC は 0x20 固定
            {
                rc = -6;
                return (rc);
            }
            // SFC のチェック
            if (resbuf[15] != 0x09)   // SFC は 0x09(保持レジスタの内容読み出し)
            {
                rc = -7;
                return (rc);
            }
            // レジスタ数のチェック
            if ((resbuf[18] != 0x0A) || (resbuf[19] != 0x00))   // 10 ワードでない
            {
                rc = -8;
                return (rc);
            }

            // レジスタデータの読み出しresbuf[20] 以降
            return (rc);
        }
        
        /*----------------------------------*/
        /* 拡張メモバスプロトコル コマンド作成   */
        /*----------------------------------*/
        private Byte[] MakeCommandData(Byte cmd)
        {
            Byte[] sendbuf = new Byte[2048];

            // 218 ヘッダ部の作成
            // データ種別設定
            sendbuf[0] = 0x11;      // 拡張メモバス(指令コマンド)

            // シリアル番号設定(送信毎にインクリメントさせる)
            sendbuf[1] = sirialNum;

            // 送信先チャネル番号設定
            sendbuf[2] = 0x00;      // PLC 側チャネルは不定なので 0 固定で可

            // 送信先チャネル番号設定
            sendbuf[3] = 0x00;      // パソコンにチャネルの概念がないため 0 固定
            sendbuf[4] = 0x00;      // 予約
            sendbuf[5] = 0x00;      // 予約

            // 全データ数設定(218 ヘッダの先頭からメモバスデータの最後まで)
            sendbuf[6] = 0x16;      // L(22 バイト =218 ヘッダ(12 バイト)+ メモバスデータ(10 バイト))
            sendbuf[7] = 0x00;      // H
            sendbuf[8] = 0x00;      // 予約
            sendbuf[9] = 0x00;      // 予約
            sendbuf[10] = 0x00;     // 予約
            sendbuf[11] = 0x00;     // 予約

            // メモバスデータ部の作成
            // Length は MFC からデータの最後まで 
            sendbuf[12] = 0x08;     // メモバスデータ長(L)
            sendbuf[13] = 0x00;     // メモバスデータ長(H)

            // MFC は 0x20 固定
            sendbuf[14] = 0x20;

            // SFC
            switch (cmd)
            {
                case 0x09:// 保持レジスタの内容読み出し(拡張)
                    sendbuf[15] = 0x09;
                    break;
                default:
                    break;
            }

            // CPU 番号設定
            sendbuf[16] = 0x10;     // 相手は CPU1 とする。マルチの場合は 1-4。自分の CPU 番号は 0 固定。
            sendbuf[17] = 0x00;     // Spare は 0 固定

            // リファレンス番号設定
            sendbuf[18] = 0x00;     // Adr(L) 先頭アドレスは MW0 とする
            sendbuf[19] = 0x00;     // Adr(H)

            // レジスタ数設定
            sendbuf[20] = 0x0A;     // DataNum(L) 先頭アドレスから10 ワード読み出す
            sendbuf[21] = 0x00;     // DataNum(H)

            return sendbuf;
        }
    }
}
