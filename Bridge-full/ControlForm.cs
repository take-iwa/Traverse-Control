using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Phidgets;
using Phidgets.Events;
using System.Reflection;
using System.Configuration;

namespace Bridge_full
{
    // ���̓X�e�[�^�X
    struct TensionStatus
    {
        public double TenInt;       // �������͂����臒l
        public double Ten;          // ���݂̒���
        public double TenMax;       // �ő勖�e����
        public double TenMin;       // �ŏ����e����
        public double TenAve;       // �T���v�����O���ԓ��̕��ϒl
        public double Slope;        // �L�����u���[�V�����F�X��
        public double Intercept;    // �L�����u���[�V�����FY�ؕ�
        public bool isGotAve;       // ����l�擾�t���O
    }

    // ���̓f�[�^
    struct BridgeDataEventData
    {
        public int Index;          // �|�[�g
        public double Value;       // �f�[�^
    }

    public partial class ControlForm : Form
    {
        private Color TENSION_STATUS_NONE = Color.YellowGreen;      // ���̓X�e�[�^�X ����
        private Color TENSION_STATUS_WIDTH_WIDE = Color.Tomato;     // ���̓X�e�[�^�X �� �L��
        private Color TENSION_STATUS_WIDTH_NARROW = Color.SkyBlue;  // ���̓X�e�[�^�X �� ����
        private Color TENSION_STATUS_MORE = Color.Crimson;          // ���̓X�e�[�^�X �͈͒���
        private Color TENSION_STATUS_LESS = Color.DodgerBlue;       // ���̓X�e�[�^�X �͈͖���
        private List<double> dataL = new List<double>();            // �����̓��X�g
        private List<double> dataR = new List<double>();            // �E���̓��X�g
        private TensionStatus LeftTenStatus;                        // �����̓X�e�[�^�X
        private TensionStatus RightTenStatus;                       // �E���̓X�e�[�^�X
        private double PermiRange;                                  // ���e�͈�(%)
        private string LogFileName;                                 // ���O�t�@�C����

        private Bridge br;
        private ErrorEventBox errorBox;
        private BridgeDataEventData conVlu;

        public ControlForm()
        {
            InitializeComponent();
            errorBox = new ErrorEventBox();
        }

        private void ControlForm_Load(object sender, EventArgs e)
        {
            br = new Bridge();

            br.Attach += new AttachEventHandler(Br_Attach);
            br.Detach += new DetachEventHandler(Br_Detach);
            br.Error += new Phidgets.Events.ErrorEventHandler(Br_Error);

            br.BridgeData += new BridgeDataEventHandler(Br_Data);

            OpenCmdLine(br);
            
            // �\���t�@�C������ݒ�擾
            var appPath = Assembly.GetExecutingAssembly().Location;
            var configPath = Path.GetDirectoryName(appPath);
            var configFile = configPath + "\\app.config";
            var exeFileMap = new ExeConfigurationFileMap { ExeConfigFilename = configFile };
            var config = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);

            RightIntercept.Text = config.AppSettings.Settings["Calib_R_y"].Value;
            LeftIntercept.Text = config.AppSettings.Settings["Calib_L_y"].Value;
            RightSlope.Text = config.AppSettings.Settings["Calib_R_x"].Value;
            LeftSlope.Text = config.AppSettings.Settings["Calib_L_x"].Value;
            
            IpAddressBox.Text = config.AppSettings.Settings["PlcIpAdr"].Value;
            PortBox.Text = config.AppSettings.Settings["PlcPort"].Value;

            //�g���o�[�X����f�[�^�ꎮ�o�b�N�A�b�v
            TraverseController ctl = new TraverseController();
            ctl.InitTraverseControl(IpAddressBox.Text, Int32.Parse(PortBox.Text));
            
            // ���O�o�͐ݒ�
            IntLogging();
        }
        
        // �ڑ��@Bridge Attach event handler...populate the fields and controls
        void Br_Attach(object sender, AttachEventArgs e)
        {
            var attached = (Bridge)sender;
            attachedTxt.Text = attached.Attached.ToString();
            nameTxt.Text = attached.Name;
            serialTxt.Text = attached.SerialNumber.ToString();
            versiontxt.Text = attached.Version.ToString();
            numBridgesTxt.Text = attached.bridges.Count.ToString();

            dataRateBar.Minimum = attached.DataRateMax / 8;
            dataRateBar.Maximum = attached.DataRateMin / 8;

            dataRateBox.Enabled = true;
            dataRateBar.Enabled = true;
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

            // �\���t�@�C������u���b�W�̐ݒ�擾
            var appPath = Assembly.GetExecutingAssembly().Location;
            var configPath = Path.GetDirectoryName(appPath);
            var configFile = configPath + "\\app.config";
            var exeFileMap = new ExeConfigurationFileMap { ExeConfigFilename = configFile };
            var config = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);
            bridgeCmbL.SelectedIndex = Int32.Parse(config.AppSettings.Settings["BridgeChoice_L"].Value);
            bridgeCmbR.SelectedIndex = Int32.Parse(config.AppSettings.Settings["BridgeChoice_R"].Value);

            gainCmb.Text = config.AppSettings.Settings["GainIndex"].Value;

            dataRateBox.Text = config.AppSettings.Settings["DataRng"].Value;
            dataRateBar.Value = Int32.Parse(dataRateBox.Text.ToString()) / 8;
            br.DataRate = dataRateBar.Value;
                
        }

        // �ؒf�@Bridge Detach event handler...Clear all the fields and disable all the controls
        void Br_Detach(object sender, DetachEventArgs e)
        {
            var detached = (Bridge)sender;
            attachedTxt.Text = detached.Attached.ToString();
            nameTxt.Clear();
            serialTxt.Clear();
            versiontxt.Clear();
            numBridgesTxt.Clear();

            dataRateBox.Enabled = false;
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
        }

        // �ڑ��G���[�����@error handler...display the error description in a messagebox
        void Br_Error(object sender, Phidgets.Events.ErrorEventArgs e)
        {
            var phid = (Phidget)sender;
            DialogResult result;
            switch (e.Type)
            {
                case PhidgetException.ErrorType.PHIDGET_ERREVENT_BADPASSWORD:
                    phid.close();
                    TextInputBox dialog = new TextInputBox("Error Event",
                        "Authentication error: This server requires a password.", "Please enter the password, or cancel.");
                    result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        OpenCmdLine(phid, dialog.password);
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

        // �ψʃC�x���g�@BridgeChange event handler...display the data from a bridge change event
        void Br_Data(object sender, BridgeDataEventArgs e)
        {
            // �I�[�g�L�����u���[�V�������Ȃ�
            if (CalibStartBtn.Enabled == false)
            {
                AutoCalibration(e);
                return;
            }

            // �L�����u���[�V�������f
            BridgeDataEventData convertValue = AddCalibration(e);

            // �����l���ݒ�
            if ((LeftTenStatus.TenInt == 0) | (RightTenStatus.TenInt == 0))
            {
                // �����l�ݒ��
                InitialTensionSetting(convertValue);
            }
            // �����l�ݒ��
            else
            {
                // �l�擾�E�\���X�V
                if( MonitorTension(convertValue))
                {
                    // �g���o�[�X����
                    ControlTraversal();
                }
            }

            // �\���X�V�ƃ��O�o��
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

        // �L�����ύX�@Enabled
        private void EnCheck_CheckedChanged(object sender, EventArgs e)
        {
            br.bridges[(int)bridgeCmbL.SelectedIndex].Enabled = enCheck.Checked;
            br.bridges[(int)bridgeCmbR.SelectedIndex].Enabled = enCheck.Checked;
            
            // ���莞�̐ݒ胍�O
            if (enCheck.Checked)
            {
                OutputLog("\nDataRate," + br.DataRate.ToString() + "ms\n");
                OutputLog("\nGain," + gainCmb.Text + "ms\n");
                OutputLog("Calibretion,Left Value = " + 
                    LeftTenStatus.Slope.ToString() + "x + " +
                    LeftTenStatus.Intercept.ToString() + "\n");
                OutputLog(",Right Value = " + RightTenStatus.Slope.ToString() + "x + " +
                    RightTenStatus.Intercept.ToString() + "\n");
            }
        }

        // �Q�C���ݒ�ύX�@Gain
        private void GainCmb_SelectedIndexChanged(object sender, EventArgs e)
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

        // �Q�C���I���@Gain Select
        int GainLUT(BridgeInput.Gains val)
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

        // ���`�F���l���ύX�@Selected Left Tension's Index
        private void BridgeCmbL_SelectedIndexChanged(object sender, EventArgs e)
        {
            enCheck.Checked = br.bridges[(int)bridgeCmbL.SelectedIndex].Enabled;
            gainCmb.SelectedIndex = GainLUT(br.bridges[(int)bridgeCmbL.SelectedIndex].Gain);
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

        // �E�`�����l���ύX�@Selected Right Tension's Index
        private void BridgeCmbR_SelectedIndexChanged(object sender, EventArgs e)
        {
            enCheck.Checked = br.bridges[(int)bridgeCmbR.SelectedIndex].Enabled;
            gainCmb.SelectedIndex = GainLUT(br.bridges[(int)bridgeCmbR.SelectedIndex].Gain);
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

        // �T���v�����O���g���ύX�@Data Rage 
        private void DataRateBar_Scroll(object sender, EventArgs e)
        {
            dataRateBox.Text = (dataRateBar.Value * 8).ToString();
            br.DataRate = dataRateBar.Value * 8;
        }

        // ����l�ύX�@Initial Left Value
        private void InitialLeftValueTxt_TextChanged(object sender, EventArgs e)
        {
            LeftTenStatus.TenInt = double.Parse(initialLeftValueTxt.Text);
            LeftTenStatus.TenMax = LeftTenStatus.TenInt + PermiRange;
            LeftTenStatus.TenMin = LeftTenStatus.TenInt - PermiRange;
        }

        // �E��l�ύX�@Initial Right Value
        private void InitialRightValueTxt_TextChanged(object sender, EventArgs e)
        {
            RightTenStatus.TenInt = double.Parse(initialRightValueTxt.Text);
            RightTenStatus.TenMax = RightTenStatus.TenInt + PermiRange;
            RightTenStatus.TenMin = RightTenStatus.TenInt - PermiRange;
        }

        // ���e�͈͐ݒ�ύX�@Permissible Range
        private void PermiRangeTxt_TextChanged(object sender, EventArgs e)
        {
            PermiRange = double.Parse(permiRangeTxt.Text);
            if (PermiRange > 0)
            {
                LeftTenStatus.TenMax = LeftTenStatus.TenInt + PermiRange;
                LeftTenStatus.TenMin = LeftTenStatus.TenInt - PermiRange;
                RightTenStatus.TenMax = RightTenStatus.TenInt + PermiRange;
                RightTenStatus.TenMin = RightTenStatus.TenInt - PermiRange;
            }
        }

        // �L�����u���[�V�����X�^�[�g
        private void CalibStartBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("�Z���T�[�𖳕��׏�Ԃɂ��Ă��������B\n");

            if (br.Attached)
            {
                CalibStartBtn.Enabled = false;
                br.bridges[(int)bridgeCmbL.SelectedIndex].Enabled = true;
                br.bridges[(int)bridgeCmbR.SelectedIndex].Enabled = true;
            }
        }

        // �I�[�g�L�����u���[�V����
        private void AutoCalibration(BridgeDataEventArgs e)
        {
            // �T���v�����O���͏����l�̏ꍇ�Ɠ����ɂ���
            var calibEndCount = Int32.Parse(ConfigurationManager.AppSettings.Get("InitSampNum"));

            // �l�i�[
            if ((dataL.Count < calibEndCount) | (dataR.Count < calibEndCount))
            {
                if (e.Index == (int)bridgeCmbL.SelectedIndex)
                    dataL.Add(e.Value);
                if (e.Index == (int)bridgeCmbR.SelectedIndex)
                    dataR.Add(e.Value);
            }
            else
            {
                // �Z���T�[OFF
                br.bridges[(int)bridgeCmbL.SelectedIndex].Enabled = false;
                br.bridges[(int)bridgeCmbR.SelectedIndex].Enabled = false;

                // �L�����u���[�V�����̐ݒ�
                double sum = 0;
                foreach (double data in dataL)
                {
                    sum += data;
                }
                LeftTenStatus.Intercept = (sum / dataL.Count);
                LeftIntercept.Text = LeftTenStatus.Intercept.ToString();
                dataL.Clear();

                sum = 0;
                foreach (double data in dataR)
                {
                    sum += data;
                }
                RightTenStatus.Intercept = (sum / dataR.Count);
                LeftIntercept.Text = RightTenStatus.Intercept.ToString();
                dataR.Clear();

                // �{�^�����A
                CalibStartBtn.Enabled = true;
            }
        }

        // �L�����u���[�V�������f
        private BridgeDataEventData AddCalibration(BridgeDataEventArgs e)
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

        // �����l�ݒ�
        private void InitialTensionSetting(BridgeDataEventData e)
        {
            var initEndCount = Int32.Parse(ConfigurationManager.AppSettings.Get("InitSampNum"));

            // �����l�擾
            if ((dataL.Count < initEndCount) | (dataR.Count < initEndCount))
            {
                if (e.Index == (int)bridgeCmbL.SelectedIndex)
                    dataL.Add(e.Value);
                if (e.Index == (int)bridgeCmbR.SelectedIndex)
                    dataR.Add(e.Value);
            }
            else
            {
                double sum = 0;
                // �����l�ݒ�(���ϒl)
                if (LeftTenStatus.TenInt == 0)
                {
                    foreach (double data in dataL)
                    {
                        sum += data;
                    }
                    LeftTenStatus.TenInt = (sum / dataL.Count);
                    initialLeftValueTxt.Text = LeftTenStatus.TenInt.ToString();
                    LeftTenStatus.TenMax = LeftTenStatus.TenInt + PermiRange;
                    LeftTenStatus.TenMin = LeftTenStatus.TenInt - PermiRange;
                    OutputLog(",,,,L_Init," + LeftTenStatus.TenInt.ToString() + "\n");
                    dataL.Clear();
                }
                else if (RightTenStatus.TenInt == 0)
                {
                    foreach (double data in dataR)
                    {
                        sum += data;
                    }
                    RightTenStatus.TenInt = (sum / dataR.Count);
                    initialRightValueTxt.Text = RightTenStatus.TenInt.ToString();
                    RightTenStatus.TenMax = RightTenStatus.TenInt + PermiRange;
                    RightTenStatus.TenMin = RightTenStatus.TenInt - PermiRange;
                    OutputLog("\n,,,,R_Init," + RightTenStatus.TenInt.ToString() + "\n");
                    dataR.Clear();
                }
            }
        }

        // �����ڈ��Ď�
        private bool MonitorTension(BridgeDataEventData e)
        {
            var controlflg = false;
            var ctlEndCount = Int32.Parse(ConfigurationManager.AppSettings.Get("CtlSampNum"));

            // Left
            if (e.Index == (int)bridgeCmbL.SelectedIndex)
            {
                LeftTenStatus.Ten = e.Value;
                dataL.Add(e.Value);
                if (dataL.Count >= ctlEndCount)
                {
                    double sum = 0;
                    foreach (double data in dataL)
                    {
                        sum += data;
                    }
                    LeftTenStatus.TenAve = (sum / dataL.Count);
                    LeftTenStatus.isGotAve = true;
                }
            }
            // Right
            else if (e.Index == (int)bridgeCmbR.SelectedIndex)
            {
                RightTenStatus.Ten = e.Value;
                dataR.Add(e.Value);
                if (dataR.Count >= ctlEndCount)
                {
                    double sum = 0;
                    foreach (double data in dataR)
                    {
                        sum += data;
                    }
                    RightTenStatus.TenAve = (sum / dataR.Count);
                    RightTenStatus.isGotAve = true;
                }
            }
            else
            {
                // ���Ȃ��͂�
            }

            // �X�e�[�^�X�\���X�V
            // 1) Tl > TlMax�ATr > TrMax�̏ꍇ�@�E�E�E�@�������X�� 
            if ((LeftTenStatus.Ten > LeftTenStatus.TenMax) & (RightTenStatus.Ten > RightTenStatus.TenMax))
            {
                // ���L
                widthState.BackColor = TENSION_STATUS_WIDTH_WIDE;
                leftState.BackColor = TENSION_STATUS_MORE;
                rightState.BackColor = TENSION_STATUS_MORE;
            }
            // 2) Tl > TlMax�ATr < TrMin�̏ꍇ�E�E�E�@�������A�E����
            else if ((LeftTenStatus.Ten > LeftTenStatus.TenMax) & (RightTenStatus.Ten < RightTenStatus.TenMin))
            {
                // �����
                widthState.BackColor = TENSION_STATUS_NONE;
                leftState.BackColor = TENSION_STATUS_MORE;
                rightState.BackColor = TENSION_STATUS_LESS;
            }
            // 3) Tl > TlMax�ATr = OK �̏ꍇ�E�E�E�@������
            else if ((LeftTenStatus.Ten > LeftTenStatus.TenMax) &
                (RightTenStatus.Ten > RightTenStatus.TenMin) & (RightTenStatus.Ten < RightTenStatus.TenMax))
            {
                // ���L�A�����
                widthState.BackColor = TENSION_STATUS_WIDTH_WIDE;
                leftState.BackColor = TENSION_STATUS_MORE;
                rightState.BackColor = TENSION_STATUS_NONE;
            }
            // 4) Tl < TlMin�ATr > TrMax�̏ꍇ�E�E�E�@������A�E����
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMin) & (RightTenStatus.Ten > RightTenStatus.TenMax))
            {
                // �E���
                widthState.BackColor = TENSION_STATUS_NONE;
                leftState.BackColor = TENSION_STATUS_LESS;
                rightState.BackColor = TENSION_STATUS_MORE;
            }
            // 5) Tl = OK�ATr > TrMax�̏ꍇ�E�E�E�@�E����
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMax) & (LeftTenStatus.Ten > LeftTenStatus.TenMin) &
                (RightTenStatus.Ten > RightTenStatus.TenMax))
            {
                // ���L�A�E���
                widthState.BackColor = TENSION_STATUS_WIDTH_WIDE;
                leftState.BackColor = TENSION_STATUS_NONE;
                rightState.BackColor = TENSION_STATUS_MORE;
            }
            // 6) Tl < TlMin�ATr = OK�̏ꍇ�E�E�E�@������
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMin) &
                (RightTenStatus.Ten > RightTenStatus.TenMin) & (RightTenStatus.Ten < RightTenStatus.TenMax))
            {
                // �����A�E���
                widthState.BackColor = TENSION_STATUS_WIDTH_NARROW;
                leftState.BackColor = TENSION_STATUS_LESS;
                rightState.BackColor = TENSION_STATUS_NONE;
            }
            // 7) Tl = OK�ATr < TrMin�̏ꍇ�E�E�E�@�E����
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMax) & (LeftTenStatus.Ten > LeftTenStatus.TenMin) &
                (RightTenStatus.Ten < RightTenStatus.TenMin))
            {
                // �����A�����
                widthState.BackColor = TENSION_STATUS_WIDTH_NARROW;
                leftState.BackColor = TENSION_STATUS_NONE;
                rightState.BackColor = TENSION_STATUS_LESS;
            }
            // 8) Tl < TlMin�ATr < TrMin�̏ꍇ�E�E�E�@������X��
            else if ((LeftTenStatus.Ten < LeftTenStatus.TenMin) & (RightTenStatus.Ten < RightTenStatus.TenMin))
            {
                // ����
                widthState.BackColor = TENSION_STATUS_WIDTH_NARROW;
                leftState.BackColor = TENSION_STATUS_LESS;
                rightState.BackColor = TENSION_STATUS_LESS;
            }
            // 9) Tl,Tr = OK �̏ꍇ
            else
            {
                // ���x�ǂ�
                widthState.BackColor = TENSION_STATUS_NONE;
                leftState.BackColor = TENSION_STATUS_NONE;
                rightState.BackColor = TENSION_STATUS_NONE;
            }

            // �����X�V���ꂽ��t���O���Ă�
            controlflg = LeftTenStatus.isGotAve & RightTenStatus.isGotAve;

            return controlflg;
        }

        // �g���o�[�X����p�ϐ��@���Z�b�g
        private void ControlValueReset()
        {
            // ���X�g�N���A
            dataL.Clear();
            dataR.Clear();

            // �t���O���Z�b�g
            LeftTenStatus.isGotAve = false;
            RightTenStatus.isGotAve = false;
        }

        // �g���o�[�X����
        private bool ControlTraversal()
        {
            var result = false;
            var ctlId = 0;
            var trvCtlr = new TraverseController();

            // ���䃍�W�b�N
            // 1) ������
            if ((LeftTenStatus.TenAve > LeftTenStatus.TenMax) & (RightTenStatus.TenAve > RightTenStatus.TenMax))
            {
                // �I�T������������(��)
                ctlId = 1;
            }
            // 2) �������A�E����
            else if ((LeftTenStatus.TenAve > LeftTenStatus.TenMax) & (RightTenStatus.TenAve < RightTenStatus.TenMin))
            {
                // �I�T�ʒu���E�ֈړ�(��)
                ctlId = 2;
            }
            // 3) ������
            else if ((LeftTenStatus.TenAve > LeftTenStatus.TenMax) &
                (RightTenStatus.TenAve > RightTenStatus.TenMin) & (RightTenStatus.TenAve < RightTenStatus.TenMax))
            {
                // �I�T�ʒu���E�ֈړ�(��)
                // �I�T�������߂�(��)
                ctlId = 3;
            }
            // 4) ������A�E����
            else if ((LeftTenStatus.TenAve < LeftTenStatus.TenMin) & (RightTenStatus.TenAve > RightTenStatus.TenMax))
            {
                // �I�T�ʒu�����ֈړ�(��)
                ctlId = 4;
            }
            // 5) �E����
            else if ((LeftTenStatus.TenAve < LeftTenStatus.TenMax) & (LeftTenStatus.TenAve > LeftTenStatus.TenMin) &
                (RightTenStatus.TenAve > RightTenStatus.TenMax))
            {
                // �I�T�ʒu�����ֈړ�(��)
                // �I�T�������߂�(��)
                ctlId = 5;
            }
            // 6) ������
            else if ((LeftTenStatus.TenAve < LeftTenStatus.TenMin) &
                (RightTenStatus.TenAve > RightTenStatus.TenMin) & (RightTenStatus.TenAve < RightTenStatus.TenMax))
            {
                // �I�T�����L����(��)
                // �I�T�ʒu�����ֈړ�(��)
                ctlId = 6;
            }
            // 7) �E����
            else if ((LeftTenStatus.TenAve < LeftTenStatus.TenMax) & (LeftTenStatus.TenAve > LeftTenStatus.TenMin) &
                (RightTenStatus.TenAve < RightTenStatus.TenMin))
            {
                // �I�T�����L����(��)
                // �I�T�ʒu���E�ֈړ�(��)
                ctlId = 7;
            }
            // 8) ������X��
            else if ((LeftTenStatus.TenAve < LeftTenStatus.TenMin) & (RightTenStatus.TenAve < RightTenStatus.TenMin))
            {
                // �I�T�����L����(��)
                ctlId = 8;
            }
            // 9) ����͈�
            else
            {
                // ����Ȃ�
                ctlId = 9;
            }

            // ����J�n
            result = trvCtlr.TraverseControl(IpAddressBox.Text, Int32.Parse(PortBox.ToString()), ctlId);

            // ���Z�b�g
            ControlValueReset();

            return result;
        }

        // ***log�o�͏����ݒ�***
        private void IntLogging()
        {
            // �o�͐�
            var appPath = Assembly.GetExecutingAssembly().Location;
            var logPath = Path.GetDirectoryName(appPath) + "\\log";
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            Directory.SetCurrentDirectory(logPath);

            // log�t�@�C�����X�V
            LogFileName = "log_" + DateTime.Now.ToString("yyMMdd_HHmmss");
        }

        // ***log�o��***
        private void OutputLog(string data)
        {
            var append = true;
            // csv�t�@�C���ɏ����o��
            var sLogFile = LogFileName + ".csv";
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

        // �E�B���h�E�N���[�Y�����@When the form is being close, make sure to stop all the motors and close the Phidget.
        private void ControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            br.Attach -= new AttachEventHandler(Br_Attach);
            br.Detach -= new DetachEventHandler(Br_Detach);
            br.Error -= new Phidgets.Events.ErrorEventHandler(Br_Error);

            br.BridgeData -= new BridgeDataEventHandler(Br_Data);

            // run any events in the message queue - otherwise close will hang if there are any outstanding events
            Application.DoEvents();

            br.close();

            br = null;

            // ���݂̐ݒ���\���t�@�C���ɕۑ�
            try
            {
                // �o�̓t�@�C��
                var appPath = Assembly.GetExecutingAssembly().Location;
                var configPath = Path.GetDirectoryName(appPath);
                var configFile = configPath + "\\app.config";
                var exeFileMap = new ExeConfigurationFileMap { ExeConfigFilename = configFile };
                var config = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);
                if (bridgeCmbL.SelectedIndex >= 0)
                {
                    config.AppSettings.Settings["BridgeChoice_L"].Value = bridgeCmbL.SelectedIndex.ToString();
                    config.AppSettings.Settings["BridgeChoice_R"].Value = bridgeCmbR.SelectedIndex.ToString();
                    config.AppSettings.Settings["GainIndex"].Value = gainCmb.SelectedIndex.ToString();
                    config.AppSettings.Settings["InitVal_L"].Value = initialLeftValueTxt.Text.ToString();
                    config.AppSettings.Settings["InitVal_R"].Value = initialRightValueTxt.Text.ToString();
                    config.AppSettings.Settings["PermRng"].Value = permiRangeTxt.Text.ToString();
                    config.AppSettings.Settings["Calib_L_x"].Value = LeftSlope.Text.ToString();
                    config.AppSettings.Settings["Calib_L_y"].Value = LeftIntercept.Text.ToString();
                    config.AppSettings.Settings["Calib_R_x"].Value = RightSlope.Text.ToString();
                    config.AppSettings.Settings["Calib_R_y"].Value = RightIntercept.Text.ToString();
                    config.AppSettings.Settings["DataRng"].Value = dataRateBox.Text.ToString();
                    config.AppSettings.Settings["PlcIpAdr"].Value = IpAddressBox.Text.ToString();
                    config.AppSettings.Settings["PlcPort"].Value = PortBox.Text.ToString();
                    config.Save();
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
        // �R�}���h���C������@Parses command line arguments and calls the appropriate open
        #region Command line open functions
        private void OpenCmdLine(Phidget p)
        {
            OpenCmdLine(p, null);
        }
        private void OpenCmdLine(Phidget p, String pass)
        {
            int serial = -1;
            String logFile = null;
            int port = 5001;
            String host = null;
            bool remote = false, remoteIP = false;
            string[] args = Environment.GetCommandLineArgs();
            String appName = args[0];

            try
            { // Parse the flags
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