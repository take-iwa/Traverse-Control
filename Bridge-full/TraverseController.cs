using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bridge_full
{
    class TraverseController
    {
        private Byte sirialNum = 0x00;

        public TraverseController()
        {
        }

        public void InitTraverseControl(string ipadr, int port)
        {
            // PLCの全データの取得
            BackupPlcAllData(ipadr, port);
        }

        // トラバース制御実行
        // 引数：制御ID
        public bool TraverseControl(string ipadr, int port, int ctlId)
        {
            long rcv = 0;
            var result = false;

            switch (ctlId)
            {
                case 1:
                    // オサ幅を狭くする(強)
                    ControlReedWidth(ipadr, port, "cls", 2);
                    break;

                case 2:
                    // オサ位置を右へ移動(強)
                    ControlReedPosition(ipadr, port, "r", 2);
                    break;

                case 3:
                    // オサ位置を右へ移動(弱)+オサ幅を狭める(弱)
                    ControlReedPosition(ipadr, port, "r", 1);
                    ControlReedWidth(ipadr, port, "cls", 1);
                    break;

                case 4:
                    // オサ位置を左へ移動(強)
                    ControlReedPosition(ipadr, port, "l", 2);
                    break;

                case 5:
                    // オサ位置を左へ移動(弱)+オサ幅を狭める(弱)
                    ControlReedPosition(ipadr, port, "l", 2);
                    ControlReedWidth(ipadr, port, "cls", 1);
                    break;

                case 6:
                    // オサ位置を左へ移動(弱)+オサ幅を広げる(弱)
                    ControlReedPosition(ipadr, port, "l", 2);
                    ControlReedWidth(ipadr, port, "opn", 1);
                    break;

                case 7:
                    // オサ位置を右へ移動(弱)+オサ幅を広げる(弱)
                    ControlReedPosition(ipadr, port, "r", 2);
                    ControlReedWidth(ipadr, port, "opn", 1);
                    break;

                case 8:
                    // オサ幅を広げる(強)
                    ControlReedWidth(ipadr, port, "opn", 2);
                    break;

                case 9:
                    // 制御なし
                    break;

                case 10:
                    // 警報を鳴らす
                    result = ControlAlarm(ipadr, port);
                    break;

                case 11:
                    // 速度 取得
                    rcv = ControlSpeed(ipadr, port);
                    if (rcv != -1)
                    {
                        result = true;
                    }
                    break;

                case 12:
                    // 張力 取得
                    rcv = ControlTension(ipadr, port);
                    if (rcv != -1)
                    {
                        result = true;
                    }
                    break;

                case 13:
                    // トラバース位置
                    rcv = ControlTraversePosition(ipadr, port);
                    if (rcv != -1)
                    {
                        result = true;
                    }
                    break;

                case 14:
                    // トラバース速度
                    rcv = ControlTraverseSpeed(ipadr, port);
                    if (rcv != -1)
                    {
                        result = true;
                    }
                    break;

                case 15:
                    // オサ移動幅
                    rcv = ControlReedMovementArea(ipadr, port);
                    if (rcv != -1)
                    {
                        result = true;
                    }
                    break;

                case 16:
                    // ピッチ幅
                    rcv = ControlPitchWidth(ipadr, port);
                    if (rcv != -1)
                    {
                        result = true;
                    }
                    break;

                default:
                    // 制御なし
                    break;
            }

            return true;
        }

        /* ----------------ラッパー-------------------------------- */
        // オサ幅制御
        private bool ControlReedWidth(string ipadr, int port, string width, int level)
        {
            var result = false;
            var regAdr = new Byte[2];

            if (width == "opn")
            {
                // オサ開く MW1043 → 0x0413
                regAdr[0] = 0x04;
                regAdr[1] = 0x13;
            }
            else // width == "cls"
            {
                // オサ閉じる MW1044 → 0x0414
                regAdr[0] = 0x04;
                regAdr[1] = 0x14;
            }


            Byte[] getCmd = MakeCommandData(regAdr, 0x03);
            long rcv = GetPlcValue(ipadr, port, getCmd);
            if (rcv != -1)
            {
                Byte[] setCmd = MakeCommandData(regAdr, 0x06);
                result = SetPlcValue(ipadr, port, setCmd);
            }

            return result;
        }

        // オサ位置制御
        private bool ControlReedPosition(string ipadr, int port, string direction, int level)
        {
            var result = false;

            // オサ位置 MW1040.1	→ 0x0410.1
            Byte[] regAdr = { 0x04, 0x10 };

            Byte[] getCmd = MakeCommandData(regAdr, 0x09);
            long rcv = GetPlcValue(ipadr, port, getCmd);
            if (rcv != -1)
            {
                Byte[] setCmd = MakeCommandData(regAdr, 0x10);
                result = SetPlcValue(ipadr, port, setCmd);
            }

            return result;
        }

        // 警報制御
        private bool ControlAlarm(string ipadr, int port)
        {
            var result = false;

            // 警報　　　　　　MW1042	→ 0x0412
            Byte[] regAdr = { 0x04, 0x12 };

            Byte[] getCmd = MakeCommandData(regAdr, 0x03);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return result;
        }

        // 速度 取得
        private long ControlSpeed(string ipadr, int port)
        {
            // 速度　　　　　　MW1100.1	→ 0x044C.D
            Byte[] regAdr = { 0x04, 0x4C };

            Byte[] getCmd = MakeCommandData(regAdr, 0x09);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return rcv;
        }

        // 張力 取得
        private long ControlTension(string ipadr, int port)
        {
            // 張力　　　　　　MW1102.3	→ 0x044E.F
            Byte[] regAdr = { 0x04, 0x4E };

            Byte[] getCmd = MakeCommandData(regAdr, 0x09);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return rcv;
        }

        // トラバース位置 取得
        private long ControlTraversePosition(string ipadr, int port)
        {
            // トラバース位置　MW1104.1	→ 0x0450.1
            Byte[] regAdr = { 0x04, 0x50 };

            Byte[] getCmd = MakeCommandData(regAdr, 0x09);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return rcv;
        }

        // トラバース速度 取得
        private long ControlTraverseSpeed(string ipadr, int port)
        {
            // トラバース速度　MW2203	→ 0x089B
            Byte[] regAdr = { 0x08, 0x9B };

            Byte[] getCmd = MakeCommandData(regAdr, 0x03);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return rcv;
        }

        // オサ移動幅 取得
        private long ControlReedMovementArea(string ipadr, int port)
        {
            // オサ移動幅　　　MW2204	→ 0x089C
            Byte[] regAdr = { 0x08, 0x9C };

            Byte[] getCmd = MakeCommandData(regAdr, 0x03);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return rcv;
        }

        // ピッチ幅 取得
        private long ControlPitchWidth(string ipadr, int port)
        {
            // ピッチ幅　　　　MW2205	→ 0x089D
            Byte[] regAdr = { 0x08, 0x9D };

            Byte[] getCmd = MakeCommandData(regAdr, 0x03);
            long rcv = GetPlcValue(ipadr, port, getCmd);

            return rcv;
        }

        // 全データ取得・保存
        private void BackupPlcAllData(string ipadr, int port)
        {
            var regDict = new Dictionary<String, short>
            {
                { "オサ位置1", 0x0410 },
                { "オサ位置2", 0x0411 },
                { "警報", 0x0412 },
                { "オサ開く", 0x0413 },
                { "オサ閉じる", 0x0414 },
                { "速度1", 0x044C },
                { "速度2", 0x044D },
                { "張力1", 0x044E },
                { "張力2", 0x044F },
                { "トラバース位置1", 0x0450 },
                { "トラバース位置2", 0x0451 },
                { "トラバース速度", 0x089B },
                { "オサ移動幅", 0x089C },
                { "ピッチ幅", 0x089D }
            };

            // 出力ファイル
            var filePath = @"./backup/PlcData_backup.txt";
            var sw = new StreamWriter(filePath, false, Encoding.GetEncoding("UTF-8"));

            try
            {
                foreach (KeyValuePair<String, short> pair in regDict)
                {
                    Byte[] regAdr = new Byte[2];
                    regAdr[1] = (byte)pair.Value;
                    regAdr[0] = (byte)(pair.Value >> 8);

                    Byte[] getCmd = MakeCommandData(regAdr, 0x09);
                    long rcv = GetPlcValue(ipadr, port, getCmd);
                    
                    var text = pair.Key + ": 0x" + rcv.ToString("x4") + "\r\n";
                    sw.Write(text);
                }
            }
            finally
            {
                sw.Close();
            }
        }

        /* ----------------コマンド系-------------------------------- */
        /*----------------------------------*/
        /* メモバスプロトコル コマンド作成     */
        /*----------------------------------*/
        private Byte[] MakeCommandData(Byte[] regAdr, Byte cmd)
        {
            var sendbuf = new Byte[2048];

            // 218 ヘッダ部の作成
            // データ種別設定
            sendbuf[0] = 0x11;      // メモバス(指令コマンド)

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

            // SFC は　Read or　Write
            sendbuf[15] = cmd;

            // CPU 番号設定
            sendbuf[16] = 0x10;     // 相手は CPU1 とする。マルチの場合は 1-4。自分の CPU 番号は 0 固定。

            // Spare は 0 固定
            sendbuf[17] = 0x00;

            // リファレンス番号設定
            sendbuf[18] = regAdr[1];     // Adr(L)
            sendbuf[19] = regAdr[0];     // Adr(H)

            // レジスタ数設定
            sendbuf[20] = cmd == 9 ? (byte)0x02:(byte)0x01;     // DataNum(L)
            sendbuf[21] = 0x00;     // DataNum(H)

            return sendbuf;
        }

        /*---------------------------------*/
        /* 応答データのチェック               */
        /*---------------------------------*/

        private Int32 CheckResponseData(String resbuf, Int32 reslen, Byte[] cmdbuf)
        {
            var rc = 0;
            
            // 全データ長のチェック
            if (reslen != (20 + cmdbuf[20]*2))   // 応答サイズは (218ヘッダ(12バイト)+メモバスデータ(8バイト) + レジスタ数ｘ2)
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
            if (resbuf[1] != cmdbuf[1]) // コマンドのシリアル番号と一致していない
            {
                rc = -3;
                return (rc);
            }
            // 伝文中の全データ長のチェック
            if ((resbuf[6] != (20 + cmdbuf[20] * 2)) && (resbuf[7] != 0x00)) // 218 ヘッダ (12 バイト )+ メモバスデータ(8バイト) + レジスタ数ｘ2
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
            if (resbuf[14] != cmdbuf[14])   // MFC は 0x20 固定
            {
                rc = -6;
                return (rc);
            }
            // SFC のチェック
            if (resbuf[15] != cmdbuf[15])   // SFC　コマンドと一致しない
            {
                rc = -7;
                return (rc);
            }
            // リファレンズ番号のチェック
            if ((resbuf[18] != cmdbuf[18]) || (resbuf[19] != cmdbuf[19]))   // コマンドと一致しない
            {
                rc = -8;
                return (rc);
            }

            if (rc < 0)
            {
                MessageBox.Show("Error: Recv !! -> " + rc + "\n");
            }

            // レジスタデータの読み出しresbuf[20] 以降
            return (rc);
        }

/* ----------------PLC通信系-------------------------------- */
        // 設定値取得
        private long GetPlcValue(string ipadr, int port, Byte[] cmd)
        {
            var tcp = new TcpManager();

            Byte[] rcv = tcp.SendTcpClient(ipadr, port, cmd);

            var respData = Encoding.ASCII.GetString(rcv);
            CheckResponseData(respData, respData.Length, cmd);

            long targetVal = rcv[20];

            // シリアル番号更新
            SirialNumUpdate();

            return targetVal;
        }

        // 設定値変更
        private bool SetPlcValue(string ipadr, int port, Byte[] cmd)
        {
            var tcp = new TcpManager();

            tcp.SendTcpClient(ipadr, port, cmd);

            // シリアル番号更新
            SirialNumUpdate();

            return true;
        }

        // シリアル番号更新
        private void SirialNumUpdate()
        {
            if (sirialNum == 0xff)
            {
                sirialNum = 0x00;
            }
            else
            {
                sirialNum++;
            }
        }
    }
}
