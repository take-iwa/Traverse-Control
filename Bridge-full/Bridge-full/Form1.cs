using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Phidgets;
using Phidgets.Events;
using System.Reflection;

namespace Bridge_full
{
    //張力ステータス
    struct TensionStatus
    {
        public double TenInt;      //初期張力からの閾値
        public double Ten;         //現在の張力
        public double TenMax;      //最大許容張力
        public double TenMin;      //最小許容張力
        public double Slope;       //キャリブレーション：傾き
        public double Intercept;   //キャリブレーション：Y切片
    }

    //張力データ
    struct BridgeDataEventData
    {
        public int Index;          //ポート
        public double Value;       //データ
    }

    public partial class Form1 : Form
    {
        public const int TENSION_DATA_RATE = 496;                   //サンプリング周波数(8ms刻み)
        public const int TENSION_SAMPLE_NUM = 120;                  //サンプル数 
        public Color TENSION_STATUS_NONE = Color.YellowGreen;       //張力ステータス 正常
        public Color TENSION_STATUS_WIDTH_WIDE = Color.Tomato;      //張力ステータス 幅 広い
        public Color TENSION_STATUS_WIDTH_NARROW = Color.SkyBlue;   //張力ステータス 幅 狭い
        public Color TENSION_STATUS_MORE = Color.Crimson;           //張力ステータス 範囲超え
        public Color TENSION_STATUS_LESS = Color.DodgerBlue;        //張力ステータス 範囲未満
        private List<double> initialdataL = new List<double>();     //初期左張力リスト
        private List<double> initialdataR = new List<double>();     //初期右張力リスト
        private TensionStatus LeftTenStatus;                        //左張力ステータス
        private TensionStatus RightTenStatus;                       //右張力ステータス
        public double PermiRange;                                   //許容範囲
        public string LogFileName;                                  //ログファイル名

        private Bridge br;
        private ErrorEventBox errorBox;
        private BridgeDataEventData conVlu;

        public Form1()
        {
            InitializeComponent();
            errorBox = new ErrorEventBox();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            br = new Bridge();

            br.Attach += new AttachEventHandler(br_Attach);
            br.Detach += new DetachEventHandler(br_Detach);
            br.Error += new Phidgets.Events.ErrorEventHandler(br_Error);

            br.BridgeData += new BridgeDataEventHandler(br_Data);

            openCmdLine(br);

            //出力先
            string appPath = Assembly.GetExecutingAssembly().Location;
            string logPath = Path.GetDirectoryName(appPath)+"\\log";
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            Directory.SetCurrentDirectory(logPath);

            //logファイル名更新
            LogFileName = "log_" + DateTime.Now.ToString("yyMMdd_HHmmss");
        }

        //Bridge Attach event handler...populate the fields and controls
        void br_Attach(object sender, AttachEventArgs e)
        {
            Bridge attached = (Bridge)sender;
            attachedTxt.Text = attached.Attached.ToString();
            nameTxt.Text = attached.Name;
            serialTxt.Text = attached.SerialNumber.ToString();
            versiontxt.Text = attached.Version.ToString();
            numBridgesTxt.Text = attached.bridges.Count.ToString();

            dataRateBar.Minimum = attached.DataRateMax / 8;
            dataRateBar.Maximum = attached.DataRateMin / 8;

            dataRateBox.Enabled = true;
            dataRateBar.Enabled = true;
            //dataRateBar.Value = TENSION_DATA_RATE;
            bridgeCmbL.Enabled = true;
            bridgeCmbR.Enabled = true;
            enCheck.Enabled = true;
            enCheck.Checked = false;
            gainCmb.Enabled = true;
            leftValueTxt.Enabled = true;
            rightValueTxt.Enabled = true;
            initialLeftValueTxt.Enabled = true;
            initialRightValueTxt.Enabled = true;
            permiRangeTxt.Enabled = true;
            RightIntercept.Enabled = true;
            LeftIntercept.Enabled = true;
            RightSlope.Enabled = true;
            LeftSlope.Enabled = true;

            br.DataRate = TENSION_DATA_RATE;
            dataRateBar.Value = br.DataRate / 8;
            dataRateBox.Text = br.DataRate.ToString();

            bridgeCmbL.SelectedIndex = 1;
            bridgeCmbR.SelectedIndex = 0;

            RightIntercept.Text = "0";
            LeftIntercept.Text = "0";
            RightSlope.Text = "1";
            LeftSlope.Text = "1";
        }

        //Bridge Detach event handler...Clear all the fields and disable all the controls
        void br_Detach(object sender, DetachEventArgs e)
        {
            Bridge detached = (Bridge)sender;
            attachedTxt.Text = detached.Attached.ToString();
            nameTxt.Clear();
            serialTxt.Clear();
            versiontxt.Clear();
            numBridgesTxt.Clear();

            dataRateBox.Enabled = false;
            dataRateBox.Text = "";
            dataRateBar.Enabled = false;
            bridgeCmbL.Enabled = false;
            bridgeCmbR.Enabled = false;
            enCheck.Enabled = false;
            gainCmb.Enabled = false;
            leftValueTxt.Enabled = false;
            rightValueTxt.Enabled = false;
            initialLeftValueTxt.Enabled = false;
            initialRightValueTxt.Enabled = false;
            permiRangeTxt.Enabled = false;
            RightIntercept.Enabled = false;
            LeftIntercept.Enabled = false;
            RightSlope.Enabled = false;
            LeftSlope.Enabled = false;

            leftValueTxt.Clear();
            rightValueTxt.Clear();
            initialLeftValueTxt.Clear();
            initialRightValueTxt.Clear();
            permiRangeTxt.Clear();

            RightIntercept.Clear();
            LeftIntercept.Clear();
            RightSlope.Clear();
            LeftSlope.Clear();
        }

        //error handler...display the error description in a messagebox
        void br_Error(object sender, Phidgets.Events.ErrorEventArgs e)
        {
            Phidget phid = (Phidget)sender;
            DialogResult result;
            switch (e.Type)
            {
                case PhidgetException.ErrorType.PHIDGET_ERREVENT_BADPASSWORD:
                    phid.close();
                    TextInputBox dialog = new TextInputBox("Error Event",
                        "Authentication error: This server requires a password.", "Please enter the password, or cancel.");
                    result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        openCmdLine(phid, dialog.password);
                    else
                        Environment.Exit(0);
                    break;
                default:
                    if (!errorBox.Visible)
                        errorBox.Show();
                    break;
            }
            errorBox.addMessage(DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + ": " + e.Description);
        }

        //BridgeChange event handler...display the data from a bridge change event
        void br_Data(object sender, BridgeDataEventArgs e)
        {
            //キャリブレーション
            BridgeDataEventData convertValue = AddCalibretion(e);

            //初期値未設定
            if ((LeftTenStatus.TenInt == 0) | (RightTenStatus.TenInt == 0))
            {
                //初期値設定へ
                InitialTensionSetting(convertValue);
            }
            //初期値設定済
            else
            {
                //接圧監視へ
                MonitorTension(convertValue);
            }

            if (convertValue.Index == bridgeCmbL.SelectedIndex)
            {
                leftValueTxt.Text = convertValue.Value.ToString();
                OutputLog("L," + convertValue.Value.ToString() + ",");
            }
            if (convertValue.Index == bridgeCmbR.SelectedIndex)
            {
                rightValueTxt.Text = convertValue.Value.ToString();
                OutputLog("R," + convertValue.Value.ToString() + "\n");
            }
        }

        //Enabled
        private void enCheck_CheckedChanged(object sender, EventArgs e)
        {
            br.bridges[(int)bridgeCmbL.SelectedIndex].Enabled = enCheck.Checked;
            br.bridges[(int)bridgeCmbR.SelectedIndex].Enabled = enCheck.Checked;
            if (enCheck.Checked)
            {
                OutputLog("\nDataRate," + br.DataRate.ToString() + "ms\n");
                OutputLog("Calibretion,Left Value = " + 
                    LeftTenStatus.Slope.ToString() + "x + " +
                    LeftTenStatus.Intercept.ToString() + "\n");
                OutputLog(",Right Value = " + RightTenStatus.Slope.ToString() + "x + " +
                    RightTenStatus.Intercept.ToString() + "\n");
            }
        }

        //Gain
        private void gainCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (gainCmb.SelectedIndex == 0)
            {
                br.bridges[(int)bridgeCmbL.SelectedIndex].Gain = BridgeInput.Gains.GAIN_1;
                if (bridgeCmbR.SelectedIndex != -1)
                    br.bridges[(int)bridgeCmbR.SelectedIndex].Gain = BridgeInput.Gains.GAIN_1;
            }
            else if (gainCmb.SelectedIndex == 1)
            {
                br.bridges[(int)bridgeCmbL.SelectedIndex].Gain = BridgeInput.Gains.GAIN_8;
                if (bridgeCmbR.SelectedIndex != -1)
                    br.bridges[(int)bridgeCmbR.SelectedIndex].Gain = BridgeInput.Gains.GAIN_8;
            }
            else if (gainCmb.SelectedIndex == 2)
            {
                br.bridges[(int)bridgeCmbL.SelectedIndex].Gain = BridgeInput.Gains.GAIN_16;
                if (bridgeCmbR.SelectedIndex != -1)
                    br.bridges[(int)bridgeCmbR.SelectedIndex].Gain = BridgeInput.Gains.GAIN_16;
            }
            else if (gainCmb.SelectedIndex == 3)
            {
                br.bridges[(int)bridgeCmbL.SelectedIndex].Gain = BridgeInput.Gains.GAIN_32;
                if (bridgeCmbR.SelectedIndex != -1)
                    br.bridges[(int)bridgeCmbR.SelectedIndex].Gain = BridgeInput.Gains.GAIN_32;
            }
            else if (gainCmb.SelectedIndex == 4)
            {
                br.bridges[(int)bridgeCmbL.SelectedIndex].Gain = BridgeInput.Gains.GAIN_64;
                if (bridgeCmbR.SelectedIndex != -1)
                    br.bridges[(int)bridgeCmbR.SelectedIndex].Gain = BridgeInput.Gains.GAIN_64;
            }
            else if (gainCmb.SelectedIndex == 5)
            {
                br.bridges[(int)bridgeCmbL.SelectedIndex].Gain = BridgeInput.Gains.GAIN_128;
                if (bridgeCmbR.SelectedIndex != -1)
                    br.bridges[(int)bridgeCmbR.SelectedIndex].Gain = BridgeInput.Gains.GAIN_128;
            }
        }

        //Gain Select
        int gainLUT(BridgeInput.Gains val)
        {
            if (val == BridgeInput.Gains.GAIN_1)
                return 0;
            if (val == BridgeInput.Gains.GAIN_8)
                return 1;
            if (val == BridgeInput.Gains.GAIN_16)
                return 2;
            if (val == BridgeInput.Gains.GAIN_32)
                return 3;
            if (val == BridgeInput.Gains.GAIN_64)
                return 4;
            if (val == BridgeInput.Gains.GAIN_128)
                return 5;

            return 0;
        }

        //Selected Left Tension's Index
        private void bridgeCmbL_SelectedIndexChanged(object sender, EventArgs e)
        {
            enCheck.Checked = br.bridges[(int)bridgeCmbL.SelectedIndex].Enabled;
            gainCmb.SelectedIndex = gainLUT(br.bridges[(int)bridgeCmbL.SelectedIndex].Gain);
            try
            {
                leftValueTxt.Text = br.bridges[(int)bridgeCmbL.SelectedIndex].BridgeValue.ToString();
            }
            catch (PhidgetException ex)
            {
                switch (ex.Type)
                {
                    case PhidgetException.ErrorType.PHIDGET_ERR_UNKNOWNVAL:
                        leftValueTxt.Text = "Unknown";
                        break;
                }
            }
        }

        //Selected Right Tension's Index
        private void bridgeCmbR_SelectedIndexChanged(object sender, EventArgs e)
        {
            enCheck.Checked = br.bridges[(int)bridgeCmbR.SelectedIndex].Enabled;
            gainCmb.SelectedIndex = gainLUT(br.bridges[(int)bridgeCmbR.SelectedIndex].Gain);
            try
            {
                rightValueTxt.Text = br.bridges[(int)bridgeCmbR.SelectedIndex].BridgeValue.ToString();
            }
            catch (PhidgetException ex)
            {
                switch (ex.Type)
                {
                    case PhidgetException.ErrorType.PHIDGET_ERR_UNKNOWNVAL:
                        rightValueTxt.Text = "Unknown";
                        break;
                }
            }
        }

        //Data Rage 
        private void dataRateBar_Scroll(object sender, EventArgs e)
        {
            dataRateBox.Text = (dataRateBar.Value * 8).ToString();
            br.DataRate = dataRateBar.Value * 8;
        }

        //Initial Left Value
        private void initialLeftValueTxt_TextChanged(object sender, EventArgs e)
        {
            LeftTenStatus.TenInt = double.Parse(initialLeftValueTxt.Text);
            LeftTenStatus.TenMax = LeftTenStatus.TenInt + PermiRange;
            LeftTenStatus.TenMin = LeftTenStatus.TenInt - PermiRange;
            //initialdataL.Clear();
        }

        //Initial Right Value
        private void initialRightValueTxt_TextChanged(object sender, EventArgs e)
        {
            RightTenStatus.TenInt = double.Parse(initialRightValueTxt.Text);
            RightTenStatus.TenMax = RightTenStatus.TenInt + PermiRange;
            RightTenStatus.TenMin = RightTenStatus.TenInt - PermiRange;
            //initialdataR.Clear();
        }

        //Permissible Range
        private void permiRangeTxt_TextChanged(object sender, EventArgs e)
        {
            PermiRange = double.Parse(permiRangeTxt.Text);
            LeftTenStatus.TenMax = LeftTenStatus.TenInt + PermiRange;
            LeftTenStatus.TenMin = LeftTenStatus.TenInt - PermiRange;
            RightTenStatus.TenMax = RightTenStatus.TenInt + PermiRange;
            RightTenStatus.TenMin = RightTenStatus.TenInt - PermiRange;
        }

        //キャリブレーション
        BridgeDataEventData AddCalibretion(BridgeDataEventArgs e)
        {
            try
            {
                if (e.Index == (int)bridgeCmbL.SelectedIndex)
                {
                    LeftTenStatus.Slope = Double.Parse(LeftSlope.Text);
                    LeftTenStatus.Intercept = Double.Parse(LeftIntercept.Text);
                    conVlu.Index = (int)bridgeCmbL.SelectedIndex;

                    conVlu.Value = Math.Round((LeftTenStatus.Slope * e.Value) + LeftTenStatus.Intercept, 5);
                }

                if (e.Index == (int)bridgeCmbR.SelectedIndex)
                {
                    RightTenStatus.Slope = Double.Parse(RightSlope.Text);
                    RightTenStatus.Intercept = Double.Parse(RightIntercept.Text);
                    conVlu.Index = (int)bridgeCmbR.SelectedIndex;

                    conVlu.Value = Math.Round((RightTenStatus.Slope * e.Value) + RightTenStatus.Intercept, 5);
                }
            }
            catch { }
            
            return conVlu;
        }

        //初期値設定
        private void InitialTensionSetting(BridgeDataEventData e)
        {
            //初期値取得
            if ((initialdataL.Count < TENSION_SAMPLE_NUM) | (initialdataR.Count < TENSION_SAMPLE_NUM))
            {
                if (e.Index == (int)bridgeCmbL.SelectedIndex)
                    initialdataL.Add(e.Value);
                if (e.Index == (int)bridgeCmbR.SelectedIndex)
                    initialdataR.Add(e.Value);
            }
            else
            {
                double sum = 0;
                //初期値設定(平均値)
                if (LeftTenStatus.TenInt == 0)
                {
                    foreach (double data in initialdataL)
                    {
                        sum += data;
                    }
                    LeftTenStatus.TenInt = (sum / initialdataL.Count);
                    initialLeftValueTxt.Text = LeftTenStatus.TenInt.ToString();
                    LeftTenStatus.TenMax = LeftTenStatus.TenInt + PermiRange;
                    //LeftTenStatus.TenMin = LeftTenStatus.TenInt;
                    LeftTenStatus.TenMin = LeftTenStatus.TenInt - PermiRange;
                    OutputLog(",,,,L_Init," + LeftTenStatus.TenInt.ToString() + "\n");
                }
                else if (RightTenStatus.TenInt == 0)
                {
                    foreach (double data in initialdataR)
                    {
                        sum += data;
                    }
                    RightTenStatus.TenInt = (sum / initialdataR.Count);
                    initialRightValueTxt.Text = RightTenStatus.TenInt.ToString();
                    RightTenStatus.TenMax = RightTenStatus.TenInt + PermiRange;
                    //RightTenStatus.TenMin = RightTenStatus.TenInt;
                    RightTenStatus.TenMin = RightTenStatus.TenInt - PermiRange;
                    OutputLog("\n,,,,R_Init," + RightTenStatus.TenInt.ToString() + "\n");
                }
            }
        }

        //耳糸接圧監視
        private void MonitorTension(BridgeDataEventData e)
        {
            //L
            if (e.Index == (int)bridgeCmbL.SelectedIndex)
            {
                LeftTenStatus.Ten = e.Value;
            }
            //R
            else if (e.Index == (int)bridgeCmbR.SelectedIndex)
            {
                RightTenStatus.Ten = e.Value;
            }
            else
            {
                //なし
            }

            //ステータス更新
            //1) Tl > TlMax、Tr > TrMaxの場合　・・・　両耳高傾向 
            if ((LeftTenStatus.Ten > LeftTenStatus.TenMax) & (RightTenStatus.Ten > RightTenStatus.TenMax))
            {
                //幅広
                widthState.BackColor = TENSION_STATUS_WIDTH_WIDE;
                leftState.BackColor = TENSION_STATUS_MORE;
                rightState.BackColor = TENSION_STATUS_MORE;
            }
            //2) Tl > TlMax、Tr < TrMinの場合・・・　左耳高、右耳低
            else if ((LeftTenStatus.Ten > LeftTenStatus.TenMax) & (RightTenStatus.Ten < RightTenStatus.TenMin))
            {
                //左寄り
                widthState.BackColor = TENSION_STATUS_NONE;
                leftState.BackColor = TENSION_STATUS_MORE;
                rightState.BackColor = TENSION_STATUS_LESS;
            }
            //3) Tl > TlMax、Tr = OK の場合・・・　左耳高
            else if ((LeftTenStatus.Ten > LeftTenStatus.TenMax) &
                (RightTenStatus.Ten > RightTenStatus.TenMin) & (RightTenStatus.Ten < RightTenStatus.TenMax))
            {
                //幅広、左寄り
                widthState.BackColor = TENSION_STATUS_WIDTH_WIDE;
                leftState.BackColor = TENSION_STATUS_MORE;
                rightState.BackColor = TENSION_STATUS_NONE;
            }
            //4) Tl < TlMin、Tr > TrMaxの場合・・・　左耳低、右耳高
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMin) & (RightTenStatus.Ten > RightTenStatus.TenMax))
            {
                //右寄り
                widthState.BackColor = TENSION_STATUS_NONE;
                leftState.BackColor = TENSION_STATUS_LESS;
                rightState.BackColor = TENSION_STATUS_MORE;
            }
            //5) Tl = OK、Tr > TrMaxの場合・・・　右耳高
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMax) & (LeftTenStatus.Ten > LeftTenStatus.TenMin) &
                (RightTenStatus.Ten > RightTenStatus.TenMax))
            {
                //幅広、右寄り
                widthState.BackColor = TENSION_STATUS_WIDTH_WIDE;
                leftState.BackColor = TENSION_STATUS_NONE;
                rightState.BackColor = TENSION_STATUS_MORE;
            }
            //6) Tl < TlMin、Tr = OKの場合・・・　左耳低
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMin) &
                (RightTenStatus.Ten > RightTenStatus.TenMin) & (RightTenStatus.Ten < RightTenStatus.TenMax))
            {
                //幅狭、右寄り
                widthState.BackColor = TENSION_STATUS_WIDTH_NARROW;
                leftState.BackColor = TENSION_STATUS_LESS;
                rightState.BackColor = TENSION_STATUS_NONE;
            }
            //7) Tl = OK、Tr < TrMinの場合・・・　右耳低
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMax) & (LeftTenStatus.Ten > LeftTenStatus.TenMin) &
                (RightTenStatus.Ten < RightTenStatus.TenMin))
            {
                //幅狭、左寄り
                widthState.BackColor = TENSION_STATUS_WIDTH_NARROW;
                leftState.BackColor = TENSION_STATUS_NONE;
                rightState.BackColor = TENSION_STATUS_LESS;
            }
            //8) Tl < TlMin、Tr < TrMinの場合・・・　両耳低傾向
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMin) & (RightTenStatus.Ten < RightTenStatus.TenMin))
            {
                //幅狭
                widthState.BackColor = TENSION_STATUS_WIDTH_NARROW;
                leftState.BackColor = TENSION_STATUS_LESS;
                rightState.BackColor = TENSION_STATUS_LESS;
            }
            //9) Tl,Tr = OK の場合
            else
            {
                //丁度良い
                widthState.BackColor = TENSION_STATUS_NONE;
                leftState.BackColor = TENSION_STATUS_NONE;
                rightState.BackColor = TENSION_STATUS_NONE;
            }
        }

        // ***log出力***
        private void OutputLog(string data)
        {
            var append = true;
            // csvファイルに書き出し
            string sLogFile = LogFileName + ".csv";
            try
            {
                using (StreamWriter w = new StreamWriter(@sLogFile, append))
                {
                    w.Write(data);
                    w.Close();
                }
            }
            catch(System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //When the form is being close, make sure to stop all the motors and close the Phidget.
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            br.Attach -= new AttachEventHandler(br_Attach);
            br.Detach -= new DetachEventHandler(br_Detach);
            br.Error -= new Phidgets.Events.ErrorEventHandler(br_Error);

            br.BridgeData -= new BridgeDataEventHandler(br_Data);

            //run any events in the message queue - otherwise close will hang if there are any outstanding events
            Application.DoEvents();

            br.close();

            br = null;
        }
        
        //Parses command line arguments and calls the appropriate open
        #region Command line open functions
        private void openCmdLine(Phidget p)
        {
            openCmdLine(p, null);
        }
        private void openCmdLine(Phidget p, String pass)
        {
            int serial = -1;
            String logFile = null;
            int port = 5001;
            String host = null;
            bool remote = false, remoteIP = false;
            string[] args = Environment.GetCommandLineArgs();
            String appName = args[0];

            try
            { //Parse the flags
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-"))
                        switch (args[i].Remove(0, 1).ToLower())
                        {
                            case "l":
                                logFile = (args[++i]);
                                break;
                            case "n":
                                serial = int.Parse(args[++i]);
                                break;
                            case "r":
                                remote = true;
                                break;
                            case "s":
                                remote = true;
                                host = args[++i];
                                break;
                            case "p":
                                pass = args[++i];
                                break;
                            case "i":
                                remoteIP = true;
                                host = args[++i];
                                if (host.Contains(":"))
                                {
                                    port = int.Parse(host.Split(':')[1]);
                                    host = host.Split(':')[0];
                                }
                                break;
                            default:
                                goto usage;
                        }
                    else
                        goto usage;
                }
                if (logFile != null)
                    Phidget.enableLogging(Phidget.LogLevel.PHIDGET_LOG_INFO, logFile);
                if (remoteIP)
                    p.open(serial, host, port, pass);
                else if (remote)
                    p.open(serial, host, pass);
                else
                    p.open(serial);
                return; //success
            }
            catch { }
        usage:
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Invalid Command line arguments." + Environment.NewLine);
            sb.AppendLine("Usage: " + appName + " [Flags...]");
            sb.AppendLine("Flags:\t-n   serialNumber\tSerial Number, omit for any serial");
            sb.AppendLine("\t-l   logFile\tEnable phidget21 logging to logFile.");
            sb.AppendLine("\t-r\t\tOpen remotely");
            sb.AppendLine("\t-s   serverID\tServer ID, omit for any server");
            sb.AppendLine("\t-i   ipAddress:port\tIp Address and Port. Port is optional, defaults to 5001");
            sb.AppendLine("\t-p   password\tPassword, omit for no password" + Environment.NewLine);
            sb.AppendLine("Examples: ");
            sb.AppendLine(appName + " -n 50098");
            sb.AppendLine(appName + " -r");
            sb.AppendLine(appName + " -s myphidgetserver");
            sb.AppendLine(appName + " -n 45670 -i 127.0.0.1:5001 -p paswrd");
            MessageBox.Show(sb.ToString(), "Argument Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Application.Exit();
        }
        #endregion
    }
}