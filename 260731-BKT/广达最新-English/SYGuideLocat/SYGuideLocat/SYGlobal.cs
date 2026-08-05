 using DBSP;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Profinet.Inovance;
using HZH_Controls.Forms;
using SY.Common;
using SY.Common.TaskThreadPool;
using SY.IOCP;
using SY.UICommon;
using SYGuideLocat.From;
using SYGuideLocat.From.EpsonCom;
using SYGuideLocat.From.PLCCom;
using SYGuideLocat.IOCP;
using SYGuideLocat.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using HalconDotNet;

namespace SYGuideLocat
{
    /// <summary>
    /// 日志类
    /// </summary>
    public class FromLog_Model
    {
        public string text { get; set; }

        public Color bgColor { get; set; }

        public object Data { get; set; }

        public DateTime ErrorBegionTime { get; set; }

        public Exception Error { get; set; }
    }

    /// <summary>
    /// 日志颜色枚举
    /// </summary>
    public enum BgColorGrade
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    /// <summary>
    /// 今日数据
    /// </summary>
    public class NowData
    {
        public int iOKCount = 0;
        public int iNGCount = 0;
    }
    /// <summary>
    /// 数据传递
    /// </summary>
    public static class GetData
    {
        public static string SelectModel;//视觉模组
        public static string textcode;//视觉模型
        public static string BtText;//标头
        public static string iTrrCount;
    }
    /// <summary>
    /// 
    /// </summary>
    public class NowDataitem
    {
        public string key { get; set; }

        public int iNumber { get; set; }
        public int iCount { get; set; }


    }

    /// <summary>
    /// 程序运行公共资源 统一初始化 释放 
    /// </summary>
    public class SYGlobal : YSGlobal, IDisposable
    {
        #region 全局参数
        // 公共资源 全局配置文件 Config 通信启动 界面通知函数委托s
        public static SYConfig _Sysconf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/SystemPar.config");

        public static SYConfig _FromSysconf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/FromSystemPar.config");

        public static SYConfig _CameraConfig = new SYConfig(AppContext.BaseDirectory + "SYDataReady/CameraConfig.config");

        public static SYConfig _CamConfig = new SYConfig(AppContext.BaseDirectory + "SYDataReady/CamConfig.config");//标头曝光

        public static SYConfig _TemplateModelConfig = new SY.Common.SYConfig(AppContext.BaseDirectory + "SYDataReady/TemplateModel.config");

        public static SYConfig _VisModel = new SY.Common.SYConfig(AppContext.BaseDirectory + "SYDataReady/VisModel.config");

        public static SYConfig _IoDataSysconf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/IoDataSystemPar.config");

        public static SYConfig _RobotSysconf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/RobotSystemPar.config");

        /// <summary>
        /// 机种参数
        /// </summary>
        public static SYConfig _RobotJZParConf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/RobotJZParConf.config");


        public static SYConfig _ErrorDataSysconf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/ErrorDataSystemPar.config");

        public static SYConfig _ErrorSysconf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/ErrorSystemPar.config");

        public static SYConfig _PowerSysConf = new SYConfig(AppContext.BaseDirectory + "SYDataReady/PowerSystemPar.config");

        public static SYConfig _Sandata = new SYConfig(AppContext.BaseDirectory + "SYDataReady/Sandata.config");

        // 线程池
        public static IThreadPool _ThreadTask = ThreadPoolFactory.CreatePool();
        // 日志记录
        public static List<FromLog_Model> _LogFromDataList = new List<FromLog_Model>();
        // 用户登录信息
        public static DataSet _UserInfo = null;
        public static NowData _NowData = new NowData();

        public static InovanceTcpNet busTcpClient = null;
        public static IReadWriteNet readWrite = null;

        // plc数据全局通知事件
        public delegate void PlcTcpFun(object args);
        public static event PlcTcpFun _PlcTcpClickFun = null;

        public static List<PLCSigModel> _PlcDataList = new List<PLCSigModel>();

        public static List<PLCPointItem> _PLCtemList = new List<PLCPointItem>();

        public static List<EpsonPointItem> _EpItemList = new List<EpsonPointItem>();

        public static List<RobotData> _RobotDataList = new List<RobotData>();

        public static SY.Common.SYJsonObject _ProjectConfigFrom = null;
        public static SY.Common.SYJsonObject _HomeConfigFromData = null;
        public static SY.Common.SYJsonObject _ReportConfigFrom = null;
        public static SY.Common.SYJsonObject _QIEConfigFrom = null;
        public static SY.Common.SYJsonObject _SMTConfigFrom = null;
        public static SY.Common.SYJsonObject _SFConfigFrom = null;
        public static SY.Common.SYJsonObject _ConfingFrom = null;

        public static SY.IOCP.SYTCPSrv _EpsonTcp = null;


        public static SerialPort port;
        //串口数据
        public static string comstr;

        /// <summary>
        /// 机器人触发信号通讯
        /// </summary>
        public static SY.IOCP.SYTCPSrv _TcpSrv = null;

        /// <summary>
        /// 机器人以太网通信
        /// </summary>
        public static EpsonTCPCli _epsonTCP = null;

        /// <summary>
        /// 机器人点位信息
        /// </summary>
        public static List<RobotData> _ListRobotData = new List<RobotData>();



        /// <summary>
        /// 机器人以太网通信
        /// </summary>
        public static SY.IOCP.SYTCPCli _CodeTCP = null;



        [DllImport("RegTool.dll")]
        public static extern void RegTool(IntPtr handle);

        public delegate void portFunction(string str);
        public static event portFunction _portFunction;

        private static Form _form;


        public static QMSDBSP DBSPsevr;

        public static QIEPortal.Portal QIEsevr;
        public static SFAutoTest.Portal SFsevr;

        public static string QIEstate;
        public static string SFstate;
        #endregion

        #region 自定义函数方法
        public Form G_Form
        {
            get { return _form; }
            set { _form = value; }
        }

        public static void AddLogFrom(string text, BgColorGrade ColorG = BgColorGrade.Red, object Data = null, Exception ex = null)
        {
            lock (SYGlobal._LogFromDataList)
            {
                Color bgcolor = Color.Red;
                switch (ColorG)
                {
                    case BgColorGrade.Red:
                        bgcolor = Color.Red;
                        break;
                    case BgColorGrade.Blue:
                        bgcolor = Color.Blue;
                        break;
                    case BgColorGrade.Green:
                        bgcolor = Color.Green;
                        break;
                    case BgColorGrade.Yellow:
                        bgcolor = Color.YellowGreen;
                        break;
                    default:
                        break;
                }

                string Txt = DateTime.Now.ToString($"HH:mm >>") + text;

                _LogFromDataList.Add(new FromLog_Model() { text = Txt, bgColor = bgcolor, Data = Data, ErrorBegionTime = DateTime.Now, Error = ex });

                SaveFile(Txt);
            }
        }

        public static void SaveFile(string txt)
        {
            string SaveFile = "";

            try
            {

                SaveFile = _ConfingFrom["SaveFile"];
                string FileData = Path.Combine(SaveFile, DateTime.Now.ToString("yyyy-MM-dd"), "Debug");
                if (!Directory.Exists(FileData))
                {
                    Directory.CreateDirectory(FileData);
                }
                SYLog._DirFile = Path.Combine(SaveFile, DateTime.Now.ToString("yyyy-MM-dd")); ;
            }
            catch
            {
                SaveFile = AppContext.BaseDirectory;
            }
            SYLog.PrintLog(txt);
        }

        public static void CheckUserInfo(out int iType)
        {
            if (CheckUserInfo())
            {
                iType = Convert.ToInt32(_UserInfo.Tables[0].Rows[0]["TYPE"]);
            }
            else
            {
                iType = -1;
            }
        }

        public static bool CheckUserInfo()
        {
            if (_UserInfo == null || _UserInfo.Tables[0].Rows.Count < 1)
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Global内置函数 程序运行

        /// <summary>
        /// 程序退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void ApplicationExit(object sender, EventArgs e)
        {
            Dispose();
        }
        /// <summary>
        /// 线程ui出错
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void ThreadExceptionUI(object sender, ThreadExceptionEventArgs e)
        {
            //SYLog.PrintLog(e.Exception);
            FrmTips.ShowTipsError(_form, e.Exception.Message);
        }
        /// <summary>
        /// 线程非ui出错
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public override void ThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            //SYLog.PrintLog(ex);
            FrmTips.ShowTipsError(_form, ex.Message);
        }

        /// <summary>
        /// 程序启动
        /// </summary>
        public override void ApplicationStart()
        {
            //RegTool((IntPtr)0);

            try
            {
                // _epsonTCP = new EpsonTCPCli();
                _HomeConfigFromData = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["HomeConfigFrom"]);
                _ProjectConfigFrom = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["ProjectConfigFrom"]);
                _ReportConfigFrom = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["ReportConfigFrom"]);

                _QIEConfigFrom = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["QIEConfigFrom"]);
                _SMTConfigFrom = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["SMTConfigFrom"]);
                _SFConfigFrom = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["SFConfigFrom"]);
                _ConfingFrom = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["ConfingFrom"]);

                try
                {
                    SYJsonObject jsonitem = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["Com_Project"]);
                    SYJsonObject T1 = new SYJsonObject(SYGlobal._RobotJZParConf[jsonitem["FanAn"]]);
                    T1["Txt_LSN"] = "";
                    T1["Txt_RSN"] = "";
                    SYGlobal._RobotJZParConf[jsonitem["FanAn"]] = T1.ToString();
                    SYGlobal._RobotJZParConf.Save();
                }
                catch { }


                //// 启动PLC
                StartServer();
                //串口
                //port = new SerialPort(_ReportConfigFrom["cbxComPort"], int.Parse(_ReportConfigFrom["cbxBaudRate"]), (Parity)int.Parse(_ReportConfigFrom["cbxParity"]), int.Parse(_ReportConfigFrom["cbxDataBits"]), (StopBits)int.Parse(_ReportConfigFrom["cbxStopBits"]));
                //port.Open();
                //SYGlobal.port.DataReceived += new SerialDataReceivedEventHandler(Port_DateRecived);
                //AddLogFrom("打开串口:" + _ReportConfigFrom["cbxComPort"], BgColorGrade.Green);

                try
                {
                    DBSPsevr = new QMSDBSP();
                    Boolean SmtBol = DBSPsevr.ConnectDB(20, _SMTConfigFrom["textBox_SvrIP"], _SMTConfigFrom["textBox_DBName"], _SMTConfigFrom["textBox_SP_Name"]);
                    AddLogFrom("链接SMT:" + _SMTConfigFrom["textBox_SvrIP"] + $"-----{SmtBol}", BgColorGrade.Green);
                }
                catch { }
                //QIEsevr = new QIEPortal.Portal();
                //QIEstate = QIEsevr.ConnectServer();

                //SFsevr = new SFAutoTest.Portal();
                //SFstate = SFsevr.ConnectServer(_SMTConfigFrom["textBox_SvrIP"], _SMTConfigFrom[""]);

                _TcpSrv = new SYTCPSrv(_ReportConfigFrom["Text_ROBOT_IP"], Convert.ToInt32(_ReportConfigFrom["Text_ROBOT_PROE"]));
                _TcpSrv.Start();
                AddLogFrom("开启tcp服务端口:" + _ReportConfigFrom["Text_ROBOT_IP"] + ":" + _ReportConfigFrom["Text_ROBOT_PROE"], BgColorGrade.Green);



                _EpsonTcp = new SY.IOCP.SYTCPSrv(SYGlobal._ReportConfigFrom["Text_ROBOT_IP1"], Convert.ToUInt16(SYGlobal._ReportConfigFrom["Text_ROBOT_PROE2"]));
                _EpsonTcp.Start();
                AddLogFrom("开启tcp服务端口:" + _ReportConfigFrom["Text_ROBOT_IP1"] + ":" + _ReportConfigFrom["Text_ROBOT_PROE2"], BgColorGrade.Green);
                _EpsonTcp.OnRecv += _t_OnRecv;


                if (Convert.ToBoolean(SYGlobal._ReportConfigFrom["CODE_Bol"]))
                {
                    _CodeTCP = new SY.IOCP.SYTCPCli(SYGlobal._ReportConfigFrom["Text_CODE_IP1"], Convert.ToUInt16(SYGlobal._ReportConfigFrom["Text_CODE_PROE2"]));
                    _CodeTCP.Start();
                    AddLogFrom("链接扫码枪:" + _ReportConfigFrom["Text_CODE_IP1"] + ":" + _ReportConfigFrom["Text_CODE_PROE2"], BgColorGrade.Green);
                }


                //initLocationData();

                // 打开局域网相机
                StartCamDev();

                // 开启PLC数据自动清空
                _ThreadTask.QueueWorkItem(TaskCliPclData, 1000, null);

            }
            catch (Exception ex)
            {
                SYLog.PrintLog(ex);
                FrmTips.ShowTipsError(_form, ex.Message);
            }
        }


        public static string SendValue = "";
        public static string _EpsonTcpSend(string Sends)
        {
            SendValue = "";

            if (_EpsonTcp != null)
            {
                _EpsonTcp.Send(System.Text.Encoding.Default.GetBytes(Sends));
            }

            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(100);
                if (SendValue != "")
                {
                    break;
                }
            }

            if (SendValue == "")
            {
                throw new Exception("获取失败");
            }
            return SendValue;
        }

        private void _t_OnRecv(System.Net.IPEndPoint remote, byte[] data)
        {
            SendValue = System.Text.Encoding.Default.GetString(data);
        }

        private void StartCamDev()
        {

            SYGlobal._ThreadTask.QueueWorkItem(delegate (object args)
            {
                DH_CamDev.GetDevName();
                Dictionary<string, string> CamDic = SYGlobal._CameraConfig.GetValueAll();
                foreach (var item in CamDic)
                {
                    try
                    {
                        DH_CamDev _DH = new DH_CamDev();
                        _DH.SetCamera(item.Value);
                        if (_DH.OpenCamera())
                        {
                            _DH.StartGrab();
                            DH_CamDev.VDatas.Add(_DH);
                            SYGlobal.AddLogFrom("打开相机:" + item.Key, BgColorGrade.Green);
                        }
                    }
                    catch
                    {
                    }
                }
            }, 0, null);
        }

        public void Port_DateRecived(object sender, SerialDataReceivedEventArgs e)
        {
            comstr = SYGlobal.port.ReadExisting();
            _portFunction?.Invoke(comstr);
        }


        private Boolean dex = true;
        public void TaskCliPclData(object args)
        {
            try
            {
                string Time = _HomeConfigFromData["Time_CliTime"];

                DateTime des = Convert.ToDateTime(Time);
                DateTime des1 = des.AddHours(12);

                DateTime dattime = DateTime.Now;

                if (dattime.Hour == des.Hour && dattime.Minute == des.Minute && Convert.ToBoolean(_HomeConfigFromData["Sw_CNBol"]) && dex)
                {
                    SavePLCData(dattime.ToString("yyyy-MM-dd HH:mm.ss"));
                    string srrdata = _HomeConfigFromData["Com_ClerIndex"];
                    string Keydatas = srrdata.Split('-')[0];
                    SYGlobal.busTcpClient.Write(Keydatas, true);
                    SYGlobal.busTcpClient.Write(Keydatas, true);
                    Thread.Sleep(2000);
                    SYGlobal.busTcpClient.Write(Keydatas, false);
                    SYGlobal.busTcpClient.Write(Keydatas, false);
                    AddLogFrom("数据清空触发:" + Keydatas, BgColorGrade.Red);
                    dex = false;


                }
                else if (dattime.Hour == des1.Hour && dattime.Minute == des1.Minute && Convert.ToBoolean(_HomeConfigFromData["Sw_CNBol"]) && dex)
                {

                    SavePLCData(dattime.ToString("yyyy-MM-dd HH:mm.ss"));

                    string srrdata = _HomeConfigFromData["Com_ClerIndex"];
                    string Keydatas = srrdata.Split('-')[0];
                    SYGlobal.busTcpClient.Write(Keydatas, true);
                    SYGlobal.busTcpClient.Write(Keydatas, true);
                    Thread.Sleep(2000);
                    SYGlobal.busTcpClient.Write(Keydatas, false);
                    SYGlobal.busTcpClient.Write(Keydatas, false);
                    AddLogFrom("数据清空触发:" + Keydatas, BgColorGrade.Red);
                    dex = false;
                }
                else if (dattime.Hour != des1.Hour && dattime.Minute != des1.Minute && Convert.ToBoolean(_HomeConfigFromData["Sw_CNBol"]))
                {
                    dex = true;
                }
            }
            catch { }
            _ThreadTask.QueueWorkItem(TaskCliPclData, 1000, null);
        }


        public void SavePLCData(string FileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"-----------------当前时间:{FileName}--------------------\r\n");

            foreach (PLCSigModel item in _PlcDataList)
            {
                sb.Append($"{item.Key}     {item.IoInfoName}    {item.Value}\r\n");
            }

            sb.Append($"---------------------结尾--------------------------\r\n");
            string SaveFile = "";

            try
            {
                SaveFile = Path.Combine(_ConfingFrom["SaveFile"], DateTime.Now.ToString("yyyy-MM-dd"), "Debug"); ;
            }
            catch
            {
                SaveFile = Path.Combine(AppContext.BaseDirectory, "Debug");
            }
            if (!Directory.Exists(SaveFile))
            {
                Directory.CreateDirectory(SaveFile);
            }

            FileStream fs = new FileStream(Path.Combine(SaveFile, DateTime.Now.ToString("yyyyMMdd") + "PLC.txt"), FileMode.Append);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(sb.ToString());
            sw.Close();
            fs.Close();
        }


        /// <summary>
        /// 加载所有点位信息
        /// </summary>
        public void initLocationData()
        {
            Dictionary<string, string> val = _RobotSysconf.GetValueAll();

            foreach (var item in val)
            {
                JavaScriptSerializer json = new JavaScriptSerializer();
                RobotData items = json.Deserialize<RobotData>(item.Value);
                _ListRobotData.Add(items);
            }
        }

        public static void StartServer()
        {
            if (!int.TryParse(_ReportConfigFrom["Text_PLC_PROE"], out int port))
            {
                throw new Exception(DemoUtils.PortInputWrong);
            }

            if (!byte.TryParse("1", out byte station))
            {
                throw new Exception("Station input is wrong！");
            }

            busTcpClient?.ConnectClose();
            busTcpClient = new InovanceTcpNet(_ReportConfigFrom["Text_PLC_IP"], port, (byte)station);

            busTcpClient.AddressStartWithZero = true;
            busTcpClient.IsCheckMessageId = true;

            busTcpClient.SetLoginAccount("", "");
            busTcpClient.Series = InovanceSeries.H3U;
            busTcpClient.ReceiveTimeOut = 1000;
            busTcpClient.DataFormat = DataFormat.CDAB;

            OperateResult connect = busTcpClient.ConnectServer();
            if (!connect.IsSuccess)
            {
                throw new Exception(HslCommunication.StringResources.Language.ConnectedFailed + connect.Message + Environment.NewLine +
                    "Error: " + connect.ErrorCode);
            }
            readWrite = busTcpClient;

            AddLogFrom("PLC链接成功", BgColorGrade.Green);
            InitDataPlcTask();
            StartPlcTask(null);

        }
        public static void InitDataPlcTask()
        {
            _PlcDataList.Clear();
            Dictionary<string, string> value = _IoDataSysconf.GetValueAll();

            foreach (var item in value)
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                PLCSigModel itemplc = js.Deserialize<PLCSigModel>(item.Value);
                _PlcDataList.Add(itemplc);
            }
        }


        private static int iPlcTaskErrorIndex = 0;
        public static void StartPlcTask(object args)
        {
            try
            {
                foreach (PLCSigModel item in _PlcDataList)
                {
                    item.UpValue = item.Value;
                    // 设置输入值
                    if (item.IoShowData == "输入框" || item.IoShowData == "标签")
                    {
                        if (item.KeyType == "button_read_bool")
                        {
                            OperateResult<bool> read = readWrite.ReadBool(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_byte")
                        {
                            //OperateResult<byte> read = (OperateResult<byte>)itemse.readByteMethod.Invoke(readWrite, new object[] { item.Key });
                            //itemse.SetTextValue(read.Content);
                            //readWrite.read
                        }
                        else if (item.KeyType == "button_read_short")
                        {
                            OperateResult<short> read = readWrite.ReadInt16(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_ushort")
                        {
                            OperateResult<ushort> read = readWrite.ReadUInt16(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_int")
                        {
                            OperateResult<int> read = readWrite.ReadInt32(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_uint")
                        {
                            OperateResult<uint> read = readWrite.ReadUInt32(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_long")
                        {
                            OperateResult<long> read = readWrite.ReadInt64(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_ulong")
                        {
                            OperateResult<ulong> read = readWrite.ReadUInt64(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_float")
                        {
                            OperateResult<float> read = readWrite.ReadFloat(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_double")
                        {
                            OperateResult<double> read = readWrite.ReadDouble(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_string")
                        {
                            OperateResult<string> read = readWrite.ReadString(item.Key, ushort.Parse(item.iStrEndLength.ToString()), GetEncodingFromIndex(item.iEncodingIndex));
                            item.Value = (read.Content);
                        }
                    }
                    // 改变按钮颜色
                    if (item.IoShowData == "按钮" || item.IoShowData == "LED")
                    {
                        if (item.KeyType == "button_read_bool")
                        {
                            OperateResult<bool> read = readWrite.ReadBool(item.Key);
                            item.Value = (read.Content);
                        }
                        else if (item.KeyType == "button_read_int")
                        {
                            OperateResult<short> read = readWrite.ReadInt16(item.Key);
                            item.Value = (read.Content);
                        }
                    }
                    if (item.UpValue != null)
                    {
                        try
                        {
                            int multiple = 1;
                            if (item.Valuenum != 0)
                            {
                                for (int I = 1; I <= item.Valuenum; I++)
                                {
                                    multiple = multiple * 10;
                                }
                            }
                            item.Value = (Convert.ToDouble(item.Value) / Convert.ToDouble(multiple));
                        }
                        catch { }
                        if (!item.UpValue.Equals(item.Value))
                        {
                            _PlcTcpClickFun?.Invoke(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SYLog.PrintLog("PLC错误" + ex.Message);
            }
            try
            {
                OperateResult<int> read1 = readWrite.ReadInt32("D6000");

                if (!read1.IsSuccess)
                {
                    iPlcTaskErrorIndex += 1;
                }
            }
            catch
            {
                iPlcTaskErrorIndex += 1;
            }
            if (iPlcTaskErrorIndex > 10)
            {
                AddLogFrom("数据读取未产生变化,重新连接", BgColorGrade.Red);
                iPlcTaskErrorIndex = 0;
                busTcpClient.ConnectClose();
                //StartServer();
            }
            else
            {
                _ThreadTask.QueueWorkItem(StartPlcTask, 0, null);
            }
        }

        private static Encoding GetEncodingFromIndex(int index)
        {
            switch (index)
            {
                case 0: return Encoding.ASCII;
                case 1: return Encoding.Unicode;
                case 2: return Encoding.BigEndianUnicode;
                case 3: return Encoding.UTF8;
                case 4: return Encoding.UTF32;
                case 5: return Encoding.Default;
                case 6: return Encoding.GetEncoding("gb2312");
                default: return Encoding.ASCII;
            }
        }

        public void Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
        }
        public void Close()
        {
            try
            {
                _TcpSrv?.Close();
                busTcpClient?.ConnectClose();
                _EpsonTcp?.Close();
                _CodeTCP?.Close();
                _epsonTCP?.Logout();
                foreach (var item in DH_CamDev.VDatas)
                {
                    item.Close();
                }
            }
            catch
            {

            }
        }
        #endregion
    }
}
