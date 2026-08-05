using HalconDotNet;
using HslCommunication;
using HZH_Controls.Controls;
using HZH_Controls.Forms;
using QuantaApply.Algorithm;
using SY.Common;
using SY.UICommon;
using SY.UICommon.Controls;
using SY.UICommon.Forms.UserLogin;
using SYGuideLocat.Control;
using SYGuideLocat.From;
using SYGuideLocat.From.CameraData;
using SYGuideLocat.From.EpsonCom;
using SYGuideLocat.From.PLCCom;
using SYGuideLocat.IOCP;
using SYGuideLocat.Model;
using SYGuideLocat.SYControl;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SYGuideLocat
{
    public partial class MainFrom : HZH_Controls.Forms.FrmWithTitle
    {
        #region 属性
        // 日志记录条数
        private int iLogCount = 0;
        // 当前窗口句柄
        private string _strOneInte = "";
        private string _strMats = "";
        private string _strJGMS = "";
        private List<HomePLCItem> _LeftPLCItemList = new List<HomePLCItem>();
        AllshowWindow allshow = new AllshowWindow();
        Algorithmic alg = new Algorithmic();
        //下视觉运行点位像素坐标
        HTuple downPx1, downPx2, downPx3, downPx4;//4个标头
        HTuple downPy1, downPy2, downPy3, downPy4;
        HTuple downPa1, downPa2, downPa3, downPa4;
        //上视觉运行点位像素坐标
        HTuple UpPx1, UpPx2, UpPx3, UpPx4;//4个标头
        HTuple UpPy1, UpPy2, UpPy3, UpPy4;
        HTuple UpPa1, UpPa2, UpPa3, UpPa4;
        //最终放置位
        HTuple EndFWx1, EndFWx2, EndFWx3, EndFWx4;
        HTuple EndFWy1, EndFWy2, EndFWy3, EndFWy4;
        HTuple EndFWa1, EndFWa2, EndFWa3, EndFWa4;
        public static SYConfig HSDI;
        private Dictionary<string, SYJsonObject> _DownDicData = new Dictionary<string, SYJsonObject>();

        private string _ThisSN = "";
        private int _SN_NG_Count = 0;

        private List<int> ListData = new List<int>();



        #region 机种数据
        private List<RobotData> _RobotDataList = new List<RobotData>();
        // 当前方案昵称
        private string _strFanAn = "";
        private SYJsonObject _FanAnJsondata = new SYJsonObject();
        // 当前方案信息点位
        private List<string> _RobotTcpMess = new List<string>();
        private List<string> _RelsdataMess = new List<string>();
        #endregion

        #endregion

        public MainFrom()
        {
            this.isNarRow = true;
            InitializeComponent();
            alg._win = this.syHalconTool1;
            



        }

        /// <summary>
        /// 初始化当前机种点位信息
        /// </summary>
        /// <param name="Jz"></param>
        public void initJzdata(string Jz)
        {
            SYGlobal.AddLogFrom("Load current model:" + Jz);
            _RobotDataList.Clear();
            Dictionary<string, string> val = SYGlobal._RobotSysconf.GetValueAll();

            foreach (var item in val)
            {
                JavaScriptSerializer json = new JavaScriptSerializer();
                RobotData items = json.Deserialize<RobotData>(item.Value);
                if (items.JZ == Jz)
                {
                    _RobotDataList.Add(items);
                }
            }
        }

        /// <summary>
        ///  初始化当前机种信息
        /// </summary>
        public void InitBtnData()
        {
            SYJsonObject jsonitem = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["Com_Project"]);
            _strFanAn = jsonitem["FanAn"];
            _FanAnJsondata = new SY.Common.SYJsonObject(SYGlobal._RobotJZParConf[_strFanAn]);
            initJzdata(_strFanAn);
            this.Title = $"BKT automatic application - current model【{_strFanAn}】";

        }

        /// <summary>
        /// 初始化加载界面按钮PLC当前状态
        /// </summary>
        public void InitMainPlcBtn()
        {
            if (SYGlobal.busTcpClient.ConnectServer().IsSuccess)
            {
                // 单联机
                _strOneInte = SYGlobal._HomeConfigFromData["Com_OneInte"].ToString().Split('-')[0];
                // 手自动
                _strMats = SYGlobal._HomeConfigFromData["Com_Mats"].ToString().Split('-')[0];
                // 加工模式
                _strJGMS = SYGlobal._HomeConfigFromData["Com_JGMS"].ToString().Split('-')[0];
                bool bOneInte = (bool)SYGlobal._PlcDataList.Find(s => s.Key == _strOneInte).Value;
                bool bMats = (bool)SYGlobal._PlcDataList.Find(s => s.Key == _strMats).Value;
                bool bJGMS = (bool)SYGlobal._PlcDataList.Find(s => s.Key == _strJGMS).Value;
                ShowBtnData(ucBtnExt6, !bOneInte ? "联机模式" : "单机模式", !bOneInte ? Color.Green : Color.Red);
                ShowBtnData(ucBtnExt5, !bMats ? "手动模式" : "自动模式", !bMats ? Color.Green : Color.Red);
                ShowBtnData(ucBtnExt1, !bJGMS ? "加工模式" : "直通模式", !bJGMS ? Color.Green : Color.Red);
            }
        }


        private void ucNavigationMenu1_ClickItemed(object sender, EventArgs e)
        {
            switch (ucNavigationMenu1.SelectItem.Text)
            {
                case "CONFIGURATION":
                    ConfigDataCon spf = new ConfigDataCon();
                    spf.ShowDialog();
                    InitLeftPLCItemData(); 
                    break;
                case "Camera configuration":
                    CameraConfig CamCon = new CameraConfig(SYGlobal._CameraConfig, CamType.海康);
                    CamCon.ShowDialog();
                    break;
                case "Calibration":
                    XYZCaliData cd = new XYZCaliData(SYGlobal._TemplateModelConfig, SYGlobal._CameraConfig);
                    cd.ShowDialog();
                    break;
                case "Template Configuration":
                    CreateTempModelFrom ctt = new CreateTempModelFrom(SYGlobal._TemplateModelConfig, SYGlobal._CameraConfig);
                    ctt.ShowDialog();
                    break;
                case "PLC Monitoring Configuration":
                    SYPLCCon spl = new SYPLCCon(SYGlobal._IoDataSysconf);
                    spl.ShowDialog();
                    break;
                case "Data upload":
                    QMSCon qms = new QMSCon();
                    qms.ShowDialog();
                    break;
                case "Point configuration":
                    RecipeConfigFrom epson = new RecipeConfigFrom();
                    epson.ShowDialog();
                    InitBtnData();
                    break;
                case "automatic calibration":
                    EpsonCaliData epson1 = new EpsonCaliData(SYGlobal._TemplateModelConfig, SYGlobal._CameraConfig);
                    epson1.ShowDialog();
                    InitBtnData();
                    break;
                case "log in":
                    string strmysql = SYGlobal._ReportConfigFrom["TEXT_MYSQL"];
                    if (strmysql == "")
                    {
                        throw new Exception("数据库未部署");
                    }

                    if (strmysql.IndexOf("{pwd}") == -1)
                    {
                        throw new Exception("数据库不存在密码命令");
                    }
                    strmysql = strmysql.Replace("{pwd}", "FarStone1!");
                    LoginFrom login = new LoginFrom(strmysql, "和资亿");
                    login.ShowDialog();

                    SYGlobal._UserInfo = login.GetUserDs();
                    if (SYGlobal.CheckUserInfo())
                    {
                        string msg = $"登录成功 欢迎 :{SYGlobal._UserInfo.Tables[0].Rows[0]["USER"].ToString()}";
                        FrmTips.ShowTipsSuccess(this, msg);
                        SYGlobal.CheckUserInfo(out int iType);
                        SYFromFun1.InitPowerForm(this.Controls, SYGlobal._PowerSysConf[(this.Name + iType)]);
                    }
                    break;
                case "用户列表":
                    strmysql = SYGlobal._ReportConfigFrom["TEXT_MYSQL"];
                    if (strmysql == "")
                    {
                        throw new Exception("数据库未部署");
                    }
                    if (strmysql.IndexOf("{pwd}") == -1)
                    {
                        throw new Exception("数据库不存在密码命令");
                    }
                    strmysql = strmysql.Replace("{pwd}", "FarStone1!");
                    UserAdminFrom useradmin = new UserAdminFrom(strmysql);
                    useradmin.ShowDialog();
                    break;
                case "连接Robot":
                    string pwd = "";
                    try
                    {
                        pwd = SYGlobal._ReportConfigFrom["Text_ROBOT_PWD"];
                    }
                    catch { }
                    SYGlobal._epsonTCP = new EpsonTCPCli(SYGlobal._ReportConfigFrom["Text_ROBOT_IP1"], Convert.ToUInt16(SYGlobal._ReportConfigFrom["Text_ROBOT_PROE2"]), pwd);
                    FrmTips.ShowTipsSuccess(this, "连接成功");
                    break;
                case "Monitoring Windows":
                    allshow.ShowDialog();
                    allshow = new AllshowWindow();
                    break;
                default:
                    break;
            }
        }

        private void MainFrom_Load(object sender, EventArgs e)
        {
            // 监听PLC控制数据
            try
            {
                initFromLog(null);
                SYGlobal._PlcTcpClickFun += SYGlobal__PlcTcpClickFun;
                //SYGlobal.port.DataReceived += new SerialDataReceivedEventHandler(Port_DateRecived);
                SYGlobal._TcpSrv.OnRecv += _TcpSrv_OnRecv;
                // 扫码枪数据监听
                InitLeftPLCItemData();
                InitBtnData();
                SYGlobal._CodeTCP.OnRecv += _CodeTCP_OnRecv;
            }
            catch (Exception ex)
            {
            }
        }


        private void _CodeTCP_OnRecv(IPEndPoint remote, byte[] data)
        {
            //if (DateTime.Now>Convert.ToDateTime("2023-06-23"))
            //{
            //    return;
            //}

            string com = System.Text.Encoding.UTF8.GetString(data).ToUpper().Trim();
            SYGlobal.AddLogFrom("来自扫码枪数据：" + com, BgColorGrade.Green);
            this.Invoke((EventHandler)delegate
            {
                ucTextBoxEx1.InputText = com;
            });

            string textBox_NG_COUNT = SYGlobal._SMTConfigFrom["textBox_NG_COUNT"];
            int iNgCount = Convert.ToInt32(textBox_NG_COUNT);


            if (com.Length < 10)
            {
                
                // 代表未识别到
                SYGlobal.AddLogFrom("扫码枪数据格式不正确：" + com, BgColorGrade.Red);
                _SN_NG_Count = _SN_NG_Count - 1;
                if (_SN_NG_Count <1)
                {
                    // 报警
                    string ERRORINDEX = SYGlobal._SMTConfigFrom["ERRORINDEX"].ToString().Split('-')[0];
                    SYGlobal.busTcpClient.Write(ERRORINDEX, true);
                    _SN_NG_Count = iNgCount;
                }
                return;
            }
            else 
            {

                _SN_NG_Count = iNgCount;

                _ThisSN = com;
                string QIEBU = SYGlobal._SMTConfigFrom["textBox_QIEBU"];
                string QIEStation = SYGlobal._SMTConfigFrom["textBox_QIEStation"];
                string QIEStep = SYGlobal._SMTConfigFrom["textBox_QIEStep"];
                string QIEOutPutStr = SYGlobal._SMTConfigFrom["textBox_QIEOutPutStr"];
                DataTable outputstr = new DataTable();
                string Inputstr = $"SN={_ThisSN};$;Line={SYGlobal._SMTConfigFrom["textBox_Line"]};$;Station={QIEStation};$;";
                SYGlobal.AddLogFrom($"SMT请求料号:QIEBU={QIEBU},QIEStation={QIEStation},QIEStep={QIEStep},Inputstr={Inputstr}", BgColorGrade.Blue);

                bool strs1 = SYGlobal.DBSPsevr.ExchangeDataViaSP(20, QIEBU, QIEStation, QIEStep, Inputstr, ref QIEOutPutStr, ref outputstr);
                SYGlobal.AddLogFrom("SMT料号返回数据：" + QIEOutPutStr, BgColorGrade.Yellow);

                if (!strs1)
                {
                    SYGlobal.AddLogFrom("SMT料号数据失败：" + QIEOutPutStr, BgColorGrade.Red);
                    return;
                }

                string strQEIDATA = QIEOutPutStr.Substring(QIEOutPutStr.IndexOf("CompPN") + 7, (QIEOutPutStr.Length - (QIEOutPutStr.IndexOf("CompPN") + 7)));

                if (strQEIDATA.Length < 5)
                {
                    strQEIDATA = "";
                }
            
                string ERRORINDEX = SYGlobal._SMTConfigFrom["ERRORINDEX"].ToString().Split('-')[0];
              
                OperateResult<Boolean> read = SYGlobal.busTcpClient.ReadBool("M111");
                
                if (strQEIDATA == "")
                {
                    SYGlobal.AddLogFrom("无料号-直通模式" , BgColorGrade.Green);
                    if (read.Content == false)
                    {
                        SYGlobal.AddLogFrom("BKT组装当前状态是加工模式：" + QIEOutPutStr, BgColorGrade.Red);
                        SYGlobal.busTcpClient.Write(ERRORINDEX, true);
                        SYGlobal.AddLogFrom($"触发PLC:{ERRORINDEX}报警 == True", BgColorGrade.Red);
                        return;
                    }
                }
                else {
                    SYGlobal.AddLogFrom("有料号-加工模式", BgColorGrade.Green);
                    if (read.Content)
                    {
                        SYGlobal.AddLogFrom("BKT组装当前状态是直通模式：" + QIEOutPutStr, BgColorGrade.Red);
                        SYGlobal.busTcpClient.Write(ERRORINDEX, true);
                        SYGlobal.AddLogFrom($"触发PLC:{ERRORINDEX}报警 == True", BgColorGrade.Red);
                        return;
                    }
                }

                Boolean SnCheckBol = true;

                if (strQEIDATA!="")
                {
                    string[] StrQieDataArry = strQEIDATA.Split(';');

                    for (int i = 0; i < StrQieDataArry.Length; i++)
                    {
                        if (StrQieDataArry[i].Length < 5)
                        {
                            break;
                        }
                        string Txt_LSN = "";
                        string Txt_RSN = "";
                        try { 
                         Txt_LSN = _FanAnJsondata["Txt_LSN"];
                        }
                        catch { }

                        try { 
                         Txt_RSN = _FanAnJsondata["Txt_RSN"];
                        }
                        catch { }


                        if (Txt_RSN == "" && Txt_RSN == "")
                        {
                            SYGlobal.AddLogFrom("请核对料号正确输入", BgColorGrade.Red);
                            SYGlobal.busTcpClient.Write(ERRORINDEX, true);
                            SYGlobal.AddLogFrom($"触发PLC:{ERRORINDEX}报警 == True", BgColorGrade.Red);
                            SnCheckBol = false;
                            break;
                        }

                        if (StrQieDataArry[i].IndexOf(Txt_LSN) !=-1 && Txt_LSN.Length>5)
                        {
                            SYGlobal.AddLogFrom("正确左侧料号：" + _FanAnJsondata["Txt_LSN"], BgColorGrade.Green);
                        }
                        else if (StrQieDataArry[i].IndexOf(Txt_RSN) !=-1 && Txt_RSN.Length>5)
                        {
                            SYGlobal.AddLogFrom("正确右侧料号：" + _FanAnJsondata["Txt_RSN"], BgColorGrade.Green);
                        }
                        else
                        {
                            SYGlobal.AddLogFrom("不是正确的料号：" + StrQieDataArry[i], BgColorGrade.Red);
                            // 报警
                            SYGlobal.busTcpClient.Write(ERRORINDEX, true);
                            SYGlobal.AddLogFrom($"触发PLC:{ERRORINDEX}报警 == True", BgColorGrade.Red);
                            SnCheckBol = false;
                            break;
                        }
                    }
                }

                string textBox_QIEGStep = SYGlobal._SMTConfigFrom["textBox_QIEGStep"];
                Inputstr = $"SN={_ThisSN};$;Line={SYGlobal._SMTConfigFrom["textBox_Line"]};$;Result={(SnCheckBol ? "PASS" : "FAIL")};$;ErrorCode=0;$;";
                SYGlobal.AddLogFrom($"SMT回传数据:QIEBU={QIEBU},QIEStation={QIEStation},QIEStep={textBox_QIEGStep},Inputstr={Inputstr}", BgColorGrade.Blue);
                outputstr = new DataTable();
                QIEOutPutStr = "";
                strs1 = SYGlobal.DBSPsevr.ExchangeDataViaSP(20, QIEBU, QIEStation, textBox_QIEGStep, Inputstr, ref QIEOutPutStr, ref outputstr);
                SYGlobal.AddLogFrom("SMT回传数据结果：" + QIEOutPutStr, BgColorGrade.Blue);
                if (!strs1)
                {
                    SYGlobal.AddLogFrom("SMT回传数据失败：" + QIEOutPutStr, BgColorGrade.Red);
                    return;
                }

                if (QIEOutPutStr.IndexOf("OK") == -1)
                {
                    SYGlobal.AddLogFrom("SMT回传校验失败：" + QIEOutPutStr, BgColorGrade.Red);
                }
                else
                {
                    SYGlobal.AddLogFrom("SMT回传校验成功OK", BgColorGrade.Green);
                }


            }
        }

        public void InitLeftPLCItemData()
        {
            flowLayoutPanel1.Controls.Clear();
            _LeftPLCItemList.Clear();
            Dictionary<string, string> _dic = SYGlobal._FromSysconf.GetValueAll();

            foreach (var item in _dic)
            {
                if (item.Key.IndexOf("SYS") != -1)
                {
                    try
                    {
                        SY.Common.SYJsonObject itemjson = new SY.Common.SYJsonObject(item.Value);
                        JavaScriptSerializer json = new JavaScriptSerializer();
                        PLCSigModel items = json.Deserialize<PLCSigModel>(itemjson);

                        object bOneInte = SYGlobal._PlcDataList.Find(s => s.Key == items.Key).Value;
                        HomePLCItem itemse = new HomePLCItem(items);
                        flowLayoutPanel1.Controls.Add(itemse);
                        _LeftPLCItemList.Add(itemse);
                        itemse.InitVal(bOneInte);
                    }
                    catch (Exception ex) { }

                }
            }
        }

        // 机器人通信触发信号
        private void _TcpSrv_OnRecv(IPEndPoint remote, byte[] data)
        {
            string com = System.Text.Encoding.UTF8.GetString(data).ToUpper().Trim();
            com = com.Replace("\r\n", "");
            SYGlobal.AddLogFrom(com, BgColorGrade.Green);
            string send = "";
            if (com.IndexOf("PZDOWN;") != -1 || com.IndexOf("PZUP;") != -1)
            {
                // 启用备用方案
                string[] comarry = com.Split(';');
                string BtData = comarry[1];

                Boolean BolUp = false;
                Boolean Bolse = false;
                if (com.IndexOf("PZUP;") != -1)
                {
                    int iCounts = ListData.FindIndex(s => s == Convert.ToInt32(BtData));
                    if (iCounts != -1)
                    {
                        BolUp = true;
                        send = "NG";
                    }
                    else {
                        Bolse = true;
                    }

                }

                if (!BolUp)
                {
                    send = VisualTrr(com, BtData);
                }

                if (send.IndexOf("NG")!=-1 && !Bolse)
                {
                    DirectoryInfo dir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "SYVisionModel", _strFanAn));
                    FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();

                    for (int i = 0; i < fileinfo.Length; i++)
                    {
                        if (fileinfo[i].Name.IndexOf("标头" + BtData) != -1 && fileinfo[i].Name != ("标头" + BtData))
                        {
                            SYGlobal.AddLogFrom("进行调用："+ fileinfo[i].Name);
                            send = VisualTrr(comarry[0] +";"+ fileinfo[i].Name.Substring(2, fileinfo[i].Name.Length - 2)+";", BtData);

                            if (send.IndexOf("NG") == -1)
                            {
                                ListData.Add(Convert.ToInt32( BtData));
                                break;
                            }
                        
                        }
                    }
                }
               
            }
            else
            {
                send = ProgramRobotTcp(com);
            }
            send += "\r\n";
            SYGlobal._TcpSrv.Send(System.Text.Encoding.UTF8.GetBytes(send));
            SYGlobal.AddLogFrom(send + "--Send--OK", BgColorGrade.Blue);
        }




        private string VisualTrr(string com,string VisualData = "") 
        {
            string send = "";

            string Camstr = com.IndexOf("PZDOWN;") != -1 ? "下视觉" : "上视觉";
            string[] comarry = com.Split(';');
            string BtData = comarry[1];
            string strkey = $"SSY{Camstr}{_strFanAn}{BtData}";
            string strintdata = SYGlobal._CamConfig[strkey];

            if (strintdata.Length > 1)
            {
                DH_CamDev _DH = DH_CamDev.VDatas.Find(s => s.DevName == SYGlobal._CameraConfig[Camstr]);
                SYJsonObject jsonitem = new SYJsonObject(strintdata);
                for (int j = 0; j < Convert.ToDouble(jsonitem["data0"]); j++)
                {
                    SYGlobal.AddLogFrom(Camstr + "触发第：" + j);
                    for (int i = 1; i < jsonitem.GetKeys().Count; i++)
                    {
                        string[] dataitem = jsonitem[("data" + i)].ToString().Split(',');

                        if (dataitem.Length > j)
                        {
                            _DH.SetCameraPram(i, Convert.ToDouble(dataitem[j]));
                        }
                    }
                    send = ProgramRobotTcp(com,true, VisualData);

                    if (send.IndexOf("OK") != -1)
                    {
                        break;
                    }
                }
            }
            else
            {
                send = ProgramRobotTcp(com, true, VisualData);
            }
            return send;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            ProgramRobotTcp("UPCHECKTRR;" + comboBox1.Text);
        }

        private bool _StartBol = false;
        public string ProgramRobotTcp(string Com, bool Bold = true, string VisualData="")
        {
            string com = Com.ToUpper().Trim();
            com = com.Replace("\r\n", "");
            string send = "";

            if (com.IndexOf("START;") != -1)
            {
                // 开始比较方案参数是否更换
                GC.Collect();
                ListData.Clear();
                if (!_StartBol)
                {
                    _StartBol = true;

                    bool boles = IfUpdatefangan();
                    send = "START;" + (boles ? "NG;" : "OK;");
                    UpDataStep(1);

                    _StartBol = false;
                }

            }
            // 方案昵称
            if (com.IndexOf("ROBOTSTATE") != -1)
            {
                if (Bold)
                {
                    _RobotTcpMess.Clear();
                    _RelsdataMess.Clear();
                    _RobotTcpMess.Add(com);
                }
                send = $"ROBOTSTATE;{_strFanAn};";
                // 获取当前机种全部点数据
                if (Bold)
                    _RelsdataMess.Add(send);
            }
            // 标头数量 以及是否启动
            if (com.IndexOf("HEAD;04") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                send = $"HEAD;01;{_FanAnJsondata["Head_01"]};02;{_FanAnJsondata["Head_02"]};03;{_FanAnJsondata["Head_03"]};04;{_FanAnJsondata["Head_04"]};";
                if (Bold)
                    _RelsdataMess.Add(send);
            }

            // 机器人发送吸料点数量
            if (com.IndexOf("HEADGRABCOUNT;") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                send = $"HEADGRABCOUNT;{_FanAnJsondata["Grab_Count"]};";
                if (Bold)
                    _RelsdataMess.Add(send);
            }
            // 发送坐标数据
            if (com.IndexOf("HEADGRAB;") != -1 || com.IndexOf("TRRDOWNDATA;") != -1 || com.IndexOf("TRRUPDATA;") != -1 || com.IndexOf("UPCHECKTRRINFO;") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                string[] comarry = com.Split(';');
                RobotData itemes = null;
                itemes = _RobotDataList.Find(s => s.key == $"{_strFanAn}{comarry[0]}" && s.Index == $"{comarry[1]}");
                if (itemes != null)
                {
                    send = $"{comarry[0]};{comarry[1]};{Convert.ToDecimal(itemes.x) + Convert.ToDecimal(itemes.bx)};{Convert.ToDecimal(itemes.y) + Convert.ToDecimal(itemes.by)};{Convert.ToDecimal(itemes.z) + Convert.ToDecimal(itemes.bz)};{Convert.ToDecimal(itemes.a) + Convert.ToDecimal(itemes.ba)};";
                }
                if (Bold)
                    _RelsdataMess.Add(send);
            }

            // 下视觉拍照次数
            if (com.IndexOf("TRRDOWNCOUNT;") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                send = $"TRRDOWNCOUNT;{_FanAnJsondata["Down_Trr_Count"]};";
                if (Bold)
                    _RelsdataMess.Add(send);
            }

            // 上视觉拍照次数
            if (com.IndexOf("TRRUPCOUNT;") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                send = $"TRRUPCOUNT;{_FanAnJsondata["Up_Trr_Count"]};";
                if (Bold)
                    _RelsdataMess.Add(send);
            }


            if (com.IndexOf("TFINDEX;") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                send = $"TFINDEX;{_FanAnJsondata["HeadTF_01"]};{_FanAnJsondata["HeadTF_02"]};{_FanAnJsondata["HeadTF_03"]};{_FanAnJsondata["HeadTF_04"]};";
                if (Bold)
                    _RelsdataMess.Add(send);
            }


            if (com.IndexOf("UPCHECKNUM;") != -1)
            {
                if (Bold)
                    _RobotTcpMess.Add(com);
                send = $"UPCHECKNUM;{_FanAnJsondata["UP_UPCHECKNUM"]};";
                if (Bold)
                    _RelsdataMess.Add(send);
            }

            if (com.IndexOf("PZDOWN;") != -1)
            {
                // 
                string[] comarry = com.Split(';');
                bool state = true;
                HObject Image = null;
                HObject ho_circle;
                HObject ho_cross;
                HObject cross1;
                HTuple hv_indices;
                HTuple hv_indices1;
                HTuple hv_angledown;
                HTuple hv_distance;
                HTuple hv_rowcenter;
                HTuple hv_colcenter;
                int inputareamin = 0;
                int inputareamax = 0;
                double inputlen = 0;
                double inputangle = 0;
                double Image_a = 0;
                string var = "";
                HTuple area = 0;
                HTuple len = 0;
                string Project = _strFanAn;
                int starttime = System.Environment.TickCount;
                SYJsonObject GKjssave = new SYJsonObject();

                RobotData itemes = _RobotDataList.Find(s => s.key == $"{_strFanAn}TRRDOWNDATA" && s.Index == $"{VisualData}");
                string BtData = itemes.btIndex;
                string GKpath = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + comarry[1], "Down", "GKdata.json");
                if (File.Exists(GKpath))
                {
                    GKjssave.FromFile(GKpath);
                    inputareamin = int.Parse((GKjssave["areamin"]));
                    inputareamax = int.Parse((GKjssave["areamax"]));
                    inputlen = Convert.ToDouble(((GKjssave["len"])));
                    Image_a = Convert.ToDouble(GKjssave["image_A"]);
                    inputangle = Convert.ToDouble(GKjssave["Angle"]);
                }
                else
                {
                    throw new Exception("无管控数据");
                }

                // 当前机种当前标头 当前视觉

                // HOperatorSet.ReadImage(out Image, @"C:\Users\Administrator\Desktop\copy——ng-11-11\BKTtmp\2022-11-01\1\Down\20221101133612.jpg");




                // 普通抓图

                DH_CamDev.GrabImage(SYGlobal._CameraConfig["下视觉"], out Image);

                //HK_ComHe.HKGrabImage(SYGlobal._CameraConfig["下视觉"],$"SY下视觉{Project}{BtData}", ref Image);
                //HK_ComHe.HKGrabImage(SYGlobal._CameraConfig["下视觉"], $"SY下视觉{Project}{BtData}", ref Image);

                alg._win.displayImage(Image);


                syHalconTool1.displayText($"图像旋转:{Image_a}", Color.Red, 500, 100);

                HOperatorSet.RotateImage(Image, out Image, Image_a, "constant");
                this.Invoke((EventHandler)delegate
                {
                    alg._win.clearWindow();
                    alg._win.displayImage(Image);
                });


                //SaveImage(Image, comarry[1], "Down");

                //Algorithmic.BKTAlgorithmicdown(alg._win, Image, out ho_cross,
                //  out cross1, 2500, 10000, 0.8, 1, 10, out hv_indices, out hv_indices1,
                //  out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter);
                //downPx1 = hv_rowcenter[hv_indices];
                //downPy1 = hv_colcenter[hv_indices];
                //downPa1 = hv_angledown;
                //SYJsonObject jsonitem = new SYJsonObject();
                //jsonitem["downPx1"] = hv_rowcenter[hv_indices].ToString();
                //jsonitem["downPy1"] = hv_colcenter[hv_indices].ToString();
                //jsonitem["downPa1"] = hv_angledown.ToString();

                //_DownDicData.Add(com, jsonitem.ToString());
                string ReduStr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + comarry[1], "Down", "ReduceRegion.hobj");
                HObject ReduImage = null;
                if (File.Exists(ReduStr))
                {
                    HOperatorSet.ReadRegion(out ReduImage, ReduStr);
                }


                #region
                #region 1标头
                if (BtData == "1")
                {

                    Algorithmic.BKTAlgorithmicdown(comarry[1], Project, alg._win, Image, 2, inputlen, out ho_cross,
                        out cross1, inputareamin, inputareamax, 0.6, 1, 10, ReduImage, out hv_indices, out hv_indices1,
                        out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter, out state, inputangle, out HTuple area1, out HTuple outlen);
                    downPx1 = hv_rowcenter[hv_indices];
                    downPy1 = hv_colcenter[hv_indices];
                    downPa1 = hv_angledown;
                }
                #endregion
                #region 2标头
                if (BtData == "2")
                {
                    Algorithmic.BKTAlgorithmicdown(comarry[1], Project, alg._win, Image, 2, inputlen, out ho_cross,
                        out cross1, inputareamin, inputareamax, 0.6, 1, 10, ReduImage, out hv_indices, out hv_indices1,
                        out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter, out state, inputangle, out HTuple area1, out HTuple outlen);
                    downPx2 = hv_rowcenter[hv_indices];
                    downPy2 = hv_colcenter[hv_indices];
                    downPa2 = hv_angledown;
                }
                #endregion
                #region 3标头
                if (BtData == "3")
                {
                    Algorithmic.BKTAlgorithmicdown(comarry[1], Project, alg._win, Image, 2, inputlen, out ho_cross,
                        out cross1, inputareamin, inputareamax, 0.6, 1, 10, ReduImage, out hv_indices, out hv_indices1,
                        out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter, out state, inputangle, out HTuple area1, out HTuple outlen);
                    downPx3 = hv_rowcenter[hv_indices];
                    downPy3 = hv_colcenter[hv_indices];
                    downPa3 = hv_angledown;
                }
                #endregion
                #region 4标头
                if (BtData == "4")

                {
                    Algorithmic.BKTAlgorithmicdown(comarry[1], Project, alg._win, Image, 2, inputlen, out ho_cross,
                        out cross1, inputareamin, inputareamax, 0.6, 1, 10, ReduImage, out hv_indices, out hv_indices1,
                        out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter, out state, inputangle, out HTuple area1, out HTuple outlen);
                    downPx4 = hv_rowcenter[hv_indices];
                    downPy4 = hv_colcenter[hv_indices];
                    downPa4 = hv_angledown;
                }
                #endregion

                #endregion

                string Bts = comarry[1].Substring(0, 1);
                if (state == true)
                {
                    send = $"{comarry[0]};{VisualData};OK;";
                    int Endtime = System.Environment.TickCount;
                    SYGlobal.AddLogFrom((Endtime - starttime).ToString(), BgColorGrade.Blue);

                    UpDataStep(Convert.ToInt32(Bts) + 1);
                }
                else
                {

                    SaveImage(Image, comarry[1], "DownNG");

                    UpDataStep(Convert.ToInt32(Bts) + 1, true);

                    //string pathimage = Path.Combine(@"D:\", "PDOWN", DateTime.Now.ToString("yyyyMMdd"), "标头" + comarry[1]);
                    //if (!File.Exists(pathimage))
                    //{
                    //    Directory.CreateDirectory(pathimage);
                    //}
                    //HOperatorSet.WriteImage(Image, "bmp", 0, pathimage + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bmp");
                    send = $"{comarry[0]};{VisualData};NG;";
                }

                allshow.Res(syHalconTool1, "Down", VisualData);


            }
            // 上视觉触发
            if (com.IndexOf("PZUP;") != -1)
            {
                string[] comarry = com.Split(';');
                HObject Image = null;
                // HOperatorSet.ReadImage(out Image, @"F:\shouyu\bkt图片\Image_20221005142142742.bmp");
                bool state = true;
                string Project = _strFanAn;
                double Eedz = 0;
                int starttime = System.Environment.TickCount;
                HObject ho_circle = null;
                HObject ho_cross = null;
                HObject cross1 = null;
                HTuple hv_indices = null;
                HTuple hv_indices1 = null;
                HTuple hv_angledown = null;
                HTuple hv_distance = null;
                HTuple hv_rowcenter = null;
                HTuple hv_colcenter = null;
                //下视觉拍照位
                double DownWx = 0;
                double DownWy = 0;
                double DownWa = 0;
                //上视觉拍照位
                double UpWx = 0;
                double UpWy = 0;
                double UpWa = 0;
                //示教放置位
                double FWx = 0;
                double FWy = 0;
                double FWz = 0;
                double FWa = 0;
                //下视觉示教mark点
                double startmarkx = 0;
                double startmarky = 0;
                //上下视觉矩阵所在文件夹
                string downhomat = "";
                string uphomat = "";
                string uphomat1 = "";
                //上下视觉示教  运行角度
                double downshijiaoangle = 89.6685;
                double upshijiaoangle = 40.3447;
                double downrunangle = 0;
                double uprunangle = 0;
                //下视觉示教图像mark点
                double downshijiaoPx = 0;
                double downshijiaoPy = 0;
                //下视觉运行mark

                //上视觉示教机械坐标
                double upshijiaoWx = 0;
                double upshijiaoWy = 0;
                //上视觉运行mark

                //最终贴合位
                string BT = "";
                string downcalb = "";

                int inputareamin = 0;
                int inputareamax = 0;
                double inputlen = 0;
                double inputangle = 0;
                double Image_a = 0;

                RobotData itemes = _RobotDataList.Find(s => s.key == $"{_strFanAn}TRRUPDATA" && s.Index == $"{VisualData}");
                SYGlobal.AddLogFrom(itemes.btIndex);
                string BtData = comarry[1];

                SYJsonObject GKjssave = new SYJsonObject();
                string GKpath = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Up", "GKdata.json");
                if (File.Exists(GKpath))
                {
                    GKjssave.FromFile(GKpath);
                    inputareamin = int.Parse((GKjssave["areamin"]));
                    inputareamax = int.Parse((GKjssave["areamax"]));
                    inputlen = Convert.ToDouble(((GKjssave["len"])));
                    Image_a = Convert.ToDouble(GKjssave["image_A"]);
                    inputangle = Convert.ToDouble(GKjssave["Angle"]);
                }
                else
                {
                    throw new Exception("无管控数据");
                }
                // 
                // HOperatorSet.ReadImage(out Image, @"C:\Users\Administrator\Desktop\copy——ng-11-11\BKTtmp\2022-11-01\1\Up\20221101113321.jpg");

                //HK_ComHe.HKGrabImage(SYGlobal._CameraConfig["上视觉"], $"SY上视觉{Project}{BtData}", ref Image);
                //HK_ComHe.HKGrabImage(SYGlobal._CameraConfig["上视觉"], $"SY上视觉{Project}{BtData}", ref Image);


                DH_CamDev.GrabImage(SYGlobal._CameraConfig["上视觉"], out Image);


                syHalconTool1.displayText($"图像旋转:{Image_a}", Color.Red, 500, 100);

                HOperatorSet.RotateImage(Image, out Image, Image_a, "constant");
                this.Invoke((EventHandler)delegate
                {
                    alg._win.clearWindow();
                    alg._win.displayImage(Image);
                });

                //SaveImage(Image, BtData, "Up");

                //SYJsonObject jsondata = SYGlobal._RobotSysconf[$"SY{_strFanAn}TRRDOWN{comarry[1]}"];
                //JavaScriptSerializer json = new JavaScriptSerializer();
                //RobotData itemes = json.Deserialize<RobotData>(jsondata);
                //DownWx = Convert.ToDouble(itemes.x);
                downcalb = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down");
                downhomat = Path.Combine(downcalb, "HoMat2DPixToM.tup");

                string upcalb = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Up");
                uphomat = Path.Combine(upcalb, "HomMat2DMToPix.tup");
                uphomat1 = Path.Combine(upcalb, "HoMat2DPixToM.tup");

                string ReduStr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Up", "ReduceRegion.hobj");
                HObject ReduImage = null;
                if (File.Exists(ReduStr))
                {
                    HOperatorSet.ReadRegion(out ReduImage, ReduStr);
                }
                try
                {


                    #region 标头1
                    if (VisualData == "1")
                    {

                        string path = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头"+ BtData, "Down", "data.json");
                        string path1 = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "SJdata标头1.json");



                        SYJsonObject jssave = new SYJsonObject();
                        if (File.Exists(path))
                        {
                            jssave.FromFile(path);

                            FWx = Convert.ToDouble(jssave["Down_01_RobotX"]);
                            FWy = Convert.ToDouble(jssave["Down_01_RobotY"]);
                            FWa = Convert.ToDouble(jssave["Down_01_RobotA"]);
                        }

                        if (File.Exists(path1))
                        {
                            jssave.FromFile(path1);
                            startmarkx = Convert.ToDouble(jssave["Down_01startmarkx"]);
                            startmarky = Convert.ToDouble(jssave["Down_01startmarky"]);
                            downshijiaoangle = Convert.ToDouble(jssave["Down_01downshijiaoangle"]);
                            downshijiaoPx = Convert.ToDouble(jssave["Down_01downshijiaoPx"]);
                            downshijiaoPy = Convert.ToDouble(jssave["Down_01downshijiaoPy"]);
                        }

                        SYJsonObject jssaveup = new SYJsonObject();
                        string upcalbpath = Path.Combine(upcalb, "SJdata标头1.json");
                        if (File.Exists(upcalbpath))
                        {
                            jssave.FromFile(upcalbpath);
                            upshijiaoWx = Convert.ToDouble(jssave["Up_01upshijiaoWx"]);
                            upshijiaoWy = Convert.ToDouble(jssave["Up_01upshijiaoWy"]);
                            upshijiaoangle = Convert.ToDouble(jssave["Up_01upshijiaoangle"]);
                        }
                        // 检测点位数据 需要动态读取
                        Datavar(Project, "1", out DownWx, out DownWy, out DownWa, out UpWx, out UpWy, out UpWa);
                        Outdata(comarry[1], Project, ReduImage, alg._win, Image, 2, inputlen, inputareamin, inputareamax, inputangle, ho_circle, ho_cross, cross1, hv_indices, hv_indices1, hv_angledown, hv_distance, hv_rowcenter, hv_colcenter,
                     downPx1, downPy1, downPa1, out state, DownWx, DownWy, DownWa, UpWx, UpWy, UpWa, FWx, FWy, FWa, startmarkx, startmarky, downhomat, uphomat, uphomat1, downshijiaoangle, upshijiaoangle,
                 downrunangle, uprunangle, downshijiaoPx, downshijiaoPy, upshijiaoWx, upshijiaoWy);


                    }
                    #endregion
                    #region 标头2
                    if (VisualData == "2")
                    {

                        string path = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "data.json");
                        string path1 = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "SJdata标头2.json");
                        SYJsonObject jssave = new SYJsonObject();
                        if (File.Exists(path))
                        {
                            jssave.FromFile(path);


                            FWx = Convert.ToDouble(jssave["Down_02_RobotX"]);
                            FWy = Convert.ToDouble(jssave["Down_02_RobotY"]);
                            FWa = Convert.ToDouble(jssave["Down_02_RobotA"]);
                        }

                        if (File.Exists(path1))
                        {
                            jssave.FromFile(path1);
                            startmarkx = Convert.ToDouble(jssave["Down_02startmarkx"]);
                            startmarky = Convert.ToDouble(jssave["Down_02startmarky"]);
                            downshijiaoangle = Convert.ToDouble(jssave["Down_02downshijiaoangle"]);
                            downshijiaoPx = Convert.ToDouble(jssave["Down_02downshijiaoPx"]);
                            downshijiaoPy = Convert.ToDouble(jssave["Down_02downshijiaoPy"]);
                        }

                        SYJsonObject jssaveup = new SYJsonObject();
                        string upcalbpath = Path.Combine(upcalb, "SJdata标头2.json");
                        if (File.Exists(upcalbpath))
                        {
                            jssave.FromFile(upcalbpath);
                            upshijiaoWx = Convert.ToDouble(jssave["Up_02upshijiaoWx"]);
                            upshijiaoWy = Convert.ToDouble(jssave["Up_02upshijiaoWy"]);
                            upshijiaoangle = Convert.ToDouble(jssave["Up_02upshijiaoangle"]);
                        }

                        Datavar(Project, "2", out DownWx, out DownWy, out DownWa, out UpWx, out UpWy, out UpWa);
                        Outdata(comarry[1], Project, ReduImage, alg._win, Image, 2, inputlen, inputareamin, inputareamax, inputangle, ho_circle, ho_cross, cross1, hv_indices, hv_indices1, hv_angledown, hv_distance, hv_rowcenter, hv_colcenter,
           downPx2, downPy2, downPa2, out state, DownWx, DownWy, DownWa, UpWx, UpWy, UpWa, FWx, FWy, FWa, startmarkx, startmarky, downhomat, uphomat, uphomat1, downshijiaoangle, upshijiaoangle,
         downrunangle, uprunangle, downshijiaoPx, downshijiaoPy, upshijiaoWx, upshijiaoWy
        );

                    }
                    #endregion
                    #region 标头3
                    if (VisualData == "3")
                    {
                        string path = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "data.json");
                        string path1 = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "SJdata标头3.json");
                        SYJsonObject jssave = new SYJsonObject();
                        if (File.Exists(path))
                        {
                            jssave.FromFile(path);


                            FWx = Convert.ToDouble(jssave["Down_03_RobotX"]);
                            FWy = Convert.ToDouble(jssave["Down_03_RobotY"]);
                            FWa = Convert.ToDouble(jssave["Down_03_RobotA"]);
                        }

                        if (File.Exists(path1))
                        {
                            jssave.FromFile(path1);
                            startmarkx = Convert.ToDouble(jssave["Down_03startmarkx"]);
                            startmarky = Convert.ToDouble(jssave["Down_03startmarky"]);
                            downshijiaoangle = Convert.ToDouble(jssave["Down_03downshijiaoangle"]);
                            downshijiaoPx = Convert.ToDouble(jssave["Down_03downshijiaoPx"]);
                            downshijiaoPy = Convert.ToDouble(jssave["Down_03downshijiaoPy"]);
                        }

                        SYJsonObject jssaveup = new SYJsonObject();
                        string upcalbpath = Path.Combine(upcalb, "SJdata标头3.json");
                        if (File.Exists(upcalbpath))
                        {
                            jssave.FromFile(upcalbpath);
                            upshijiaoWx = Convert.ToDouble(jssave["Up_03upshijiaoWx"]);
                            upshijiaoWy = Convert.ToDouble(jssave["Up_03upshijiaoWy"]);
                            upshijiaoangle = Convert.ToDouble(jssave["Up_03upshijiaoangle"]);
                        }


                        Datavar(Project, "3", out DownWx, out DownWy, out DownWa, out UpWx, out UpWy, out UpWa);
                        Outdata(comarry[1], Project,ReduImage, alg._win, Image, 2, inputlen, inputareamin, inputareamax, inputangle, ho_circle, ho_cross, cross1, hv_indices, hv_indices1, hv_angledown, hv_distance, hv_rowcenter, hv_colcenter,
                  downPx3, downPy3, downPa3, out state, DownWx, DownWy, DownWa, UpWx, UpWy, UpWa, FWx, FWy, FWa, startmarkx, startmarky, downhomat, uphomat, uphomat1, downshijiaoangle, upshijiaoangle,
                  downrunangle, uprunangle, downshijiaoPx, downshijiaoPy, upshijiaoWx, upshijiaoWy
                 );
                    }
                    #endregion
                    #region 标头4
                    if (VisualData == "4")
                    {
                        string path = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "data.json");
                        string path1 = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", Project, "标头" + BtData, "Down", "SJdata标头4.json");
                        SYJsonObject jssave = new SYJsonObject();
                        if (File.Exists(path))
                        {
                            jssave.FromFile(path);

                            FWx = Convert.ToDouble(jssave["Down_04_RobotX"]);
                            FWy = Convert.ToDouble(jssave["Down_04_RobotY"]);
                            FWa = Convert.ToDouble(jssave["Down_04_RobotA"]);
                        }

                        if (File.Exists(path1))
                        {
                            jssave.FromFile(path1);
                            startmarkx = Convert.ToDouble(jssave["Down_04startmarkx"]);//示教时mark点的机器人坐标
                            startmarky = Convert.ToDouble(jssave["Down_04startmarky"]);
                            downshijiaoangle = Convert.ToDouble(jssave["Down_04downshijiaoangle"]);//示教时图像角度 坐标
                            downshijiaoPx = Convert.ToDouble(jssave["Down_04downshijiaoPx"]);
                            downshijiaoPy = Convert.ToDouble(jssave["Down_04downshijiaoPy"]);
                        }

                        SYJsonObject jssaveup = new SYJsonObject();
                        string upcalbpath = Path.Combine(upcalb, "SJdata标头4.json");
                        if (File.Exists(upcalbpath))
                        {
                            jssave.FromFile(upcalbpath);
                            upshijiaoWx = Convert.ToDouble(jssave["Up_04upshijiaoWx"]);
                            upshijiaoWy = Convert.ToDouble(jssave["Up_04upshijiaoWy"]);
                            upshijiaoangle = Convert.ToDouble(jssave["Up_04upshijiaoangle"]);
                        }

                        Datavar(Project, "4", out DownWx, out DownWy, out DownWa, out UpWx, out UpWy, out UpWa);
                        Outdata(comarry[1], Project, ReduImage, alg._win, Image, 2, inputlen, inputareamin, inputareamax, inputangle, ho_circle, ho_cross, cross1, hv_indices, hv_indices1, hv_angledown, hv_distance, hv_rowcenter, hv_colcenter,
                downPx4, downPy4, downPa4, out state, DownWx, DownWy, DownWa, UpWx, UpWy, UpWa, FWx, FWy, FWa, startmarkx, startmarky, downhomat, uphomat, uphomat1, downshijiaoangle, upshijiaoangle,
              downrunangle, uprunangle, downshijiaoPx, downshijiaoPy, upshijiaoWx, upshijiaoWy
              );

                    }

                    if (state == true)
                    {
                        // 增加不藏
                        itemes = _RobotDataList.Find(s => s.key == $"{_strFanAn}TF" && s.btIndex == $"{comarry[1]}");

                        itemes.bx = itemes.bx == null ? "0" : itemes.bx;
                        itemes.by = itemes.by == null ? "0" : itemes.by;
                        itemes.bz = itemes.bz == null ? "0" : itemes.bz;
                        itemes.ba = itemes.ba == null ? "0" : itemes.ba;

                        send = comarry[0] + ";" + VisualData + ";" + _FanAnJsondata[$"HeadTF_0{VisualData}"] + ";" + "OK" + ";" + (EndFWx1 + Convert.ToDouble(itemes.bx)).ToString() + ";" + (EndFWy1 + Convert.ToDouble(itemes.by)).ToString() + ";" + (Convert.ToDouble(itemes.z) + Convert.ToDouble(itemes.bz)).ToString() + ";" + (EndFWa1 + Convert.ToDouble(itemes.ba)).ToString() + ";";
                        SYGlobal.AddLogFrom($"BT:{comarry[1]},贴敷增加补偿:X:{itemes.bx},Y:{itemes.by},Z:{itemes.bz},A:{itemes.ba}", BgColorGrade.Red);
                        int Endtime = System.Environment.TickCount;
                        SYGlobal.AddLogFrom((Endtime - starttime).ToString(), BgColorGrade.Blue);

                        UpDataStep(Convert.ToInt32(VisualData) + 5);
                    }
                    else
                    {
                        SaveImage(Image, comarry[1], "UpNG");
                        send = Com.ToString() + _FanAnJsondata[$"HeadTF_0{comarry[1]}"] + ";NG;0;0;0;0;";
                        UpDataStep(Convert.ToInt32(VisualData) + 5, true);
                    }
                    allshow.Res(syHalconTool1, "Up", VisualData);
                }
                catch (Exception ex)
                {
                    send = Com.ToString() + _FanAnJsondata[$"HeadTF_0{comarry[1]}"] + ";NG;0;0;0;0;";
                    SYGlobal.AddLogFrom(send, BgColorGrade.Blue);
                    SaveImage(Image, comarry[1], "UpExNG");
                }
                #endregion
            }

            if (com.IndexOf("UPCHECKTRR;") != -1)
            {
                string[] comarry = com.Split(';');

                bool CeckBol = true;
                bool CeckBol1 = true;
                SYJsonObject sYJsonObject = new SYJsonObject();
                String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", _strFanAn);
                string widthpat = Path.Combine(datapath, $"film{comarry[1].ToString()}.json");

                syHalconTool1.clearWindow();

                if (File.Exists(widthpat))
                {
                    string strkey = $"UpCheck上视觉{_strFanAn}{comarry[1]}";
                    string strintdata = SYGlobal._CamConfig[strkey];

                    if (strintdata.Length > 1)
                    {
                        DH_CamDev _DH = DH_CamDev.VDatas.Find(s => s.DevName == SYGlobal._CameraConfig["上视觉"]);
                        SYJsonObject jsonitem = new SYJsonObject(strintdata);

                        for (int i = 1; i < jsonitem.GetKeys().Count; i++)
                        {
                            _DH.SetCameraPram(i, Convert.ToDouble(jsonitem[("data" + (i - 1))].ToString()));
                        }
                    }

                    sYJsonObject.FromFile(widthpat);
                    if (Convert.ToBoolean(sYJsonObject["离心纸检测是否开启"]))
                    {
                        DH_CamDev.GrabImage(SYGlobal._CameraConfig["上视觉"], out HObject Image);
                        syHalconTool1.displayImage(Image);
                        Algorithmic.Filmalgo(Image, _strFanAn, comarry[1], out HTuple max, syHalconTool1);
                        if (max > int.Parse(sYJsonObject["离心纸管控面积"]) - int.Parse(sYJsonObject["离心纸宽松面积"]))
                        {
                            syHalconTool1.displayText("未撕膜", Color.Red, 2500, 1000);
                            CeckBol1 = false;
                        }
                        else
                        {
                            syHalconTool1.displayText("已撕膜", Color.Green, 2500, 1000);
                        }
                    }
                }

                sYJsonObject = new SYJsonObject();
                datapath = Path.Combine(AppContext.BaseDirectory, "DATA", _strFanAn);
                widthpat = Path.Combine(datapath, $"PCBCheck{comarry[1].ToString()}.json");

                if (File.Exists(widthpat))
                {
                    string strkey = $"PCBUpCheck上视觉{_strFanAn}{comarry[1]}";
                    string strintdata = SYGlobal._CamConfig[strkey];

                    if (strintdata.Length > 1)
                    {
                        DH_CamDev _DH = DH_CamDev.VDatas.Find(s => s.DevName == SYGlobal._CameraConfig["上视觉"]);
                        SYJsonObject jsonitem = new SYJsonObject(strintdata);
                        for (int i = 1; i < jsonitem.GetKeys().Count; i++)
                        {
                            _DH.SetCameraPram(i, Convert.ToDouble(jsonitem[("data" + (i - 1))].ToString()));
                        }
                    }


                    sYJsonObject.FromFile(widthpat);

                    if (Convert.ToBoolean(sYJsonObject["组装检测是否开启"]))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            DH_CamDev.GrabImage(SYGlobal._CameraConfig["上视觉"], out HObject Image);



                            syHalconTool1.displayImage(Image);

                            Algorithmic.Filmalgo_PCBTest(Image, _strFanAn, comarry[1], out HTuple max1, syHalconTool1);

                            double index11 = Convert.ToDouble(sYJsonObject["组装面积管控面积"]) - Convert.ToDouble(max1.D);


                            if (int.Parse(sYJsonObject["组装面积最大"]) > index11 && int.Parse(sYJsonObject["组装面积最小"]) < index11)
                            {
                                syHalconTool1.displayText("组装OK", Color.Green, 2500, 1800);
                                CeckBol = true;
                                break;
                            }
                            else
                            {
                                if (i == 2)
                                {
                                    syHalconTool1.displayText("组装NG", Color.Red, 2500, 1800);
                                }
                                CeckBol = false;
                                Thread.Sleep(300);
                                SaveImage(Image, comarry[1], "Check");
                            }



                        }
                    }
                }
                allshow.Res(syHalconTool1, "Check", comarry[1]);
                // 组装到位
                if (CeckBol == true && CeckBol1 == true)
                {
                    CeckBol = true;
                }
                else
                {
                    CeckBol = false;
                }
                send = com += (CeckBol ? "OK;" : "NG;");
            }
            return send;
        }



        public void SaveImage(HObject image, string bt, string Type)
        {

            string SaveFile = "";

            try
            {

                SaveFile = SYGlobal._ConfingFrom["SaveFile"];
            }
            catch
            {
                SaveFile = AppContext.BaseDirectory;
            }

            string ImageFile = Path.Combine(SaveFile, DateTime.Now.ToString("yyyy-MM-dd"), "BKTtmp", bt, Type);
            if (!Directory.Exists(ImageFile))
            {
                Directory.CreateDirectory(ImageFile);
            }
            HalImageHelp.SaveImageData(image, syHalconTool1.GetTextDic(), Path.Combine(ImageFile, DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg"), 100, 0, 0, 0, 200);
        }

        public void Outdata(string bt,string jz,HObject ReduImage, SYHalconTool win, HObject Image, int num, double len, int aertmin, int aertmax, double inputangle, HObject ho_circle, HObject ho_cross, HObject cross1,
            HTuple hv_indices, HTuple hv_indices1, HTuple hv_angledown, HTuple hv_distance, HTuple hv_rowcenter, HTuple hv_colcenter,
           double downPx1, double downPy1, double downPa1, out bool state,
            double DownWx = 0, double DownWy = 0, double DownWa = 0, double UpWx = 0, double UpWy = 0, double UpWa = 0, double FWx = 0, double FWy = 0, double FWa = 0,
            double startmarkx = 0, double startmarky = 0, string downhomat = "", string uphomat = "", string uphomat1 = "", double downshijiaoangle = 0, double upshijiaoangle = 0,
            double downrunangle = 0, double uprunangle = 0, double downshijiaoPx = 0, double downshijiaoPy = 0, double upshijiaoWx = 0, double upshijiaoWy = 0
        )
        {

            Algorithmic.BKTAlgorithmicUp(bt,jz, win, Image, num, len, out ho_cross,
                      out cross1, aertmin, aertmax, 0.6, 1, 10, ReduImage, out hv_indices, out hv_indices1,
                      out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter, out state, inputangle, out HTuple area, out HTuple Dislen);

            UpPx1 = hv_rowcenter[hv_indices];//上视觉图像坐标
            UpPy1 = hv_colcenter[hv_indices];
            UpPa1 = hv_angledown;


            if (hv_angledown != null)
            {
                Algorithmic.Contraposition(win, DownWx, DownWy, DownWa, UpWx,
        UpWy, UpWa, FWx, FWy, FWa, startmarkx, startmarky, downhomat, uphomat, uphomat1
    , downshijiaoangle, upshijiaoangle, downPa1, UpPa1, downshijiaoPx, downshijiaoPy,
        downPx1, downPy1, upshijiaoWx, upshijiaoWy, UpPx1, UpPy1, out EndFWx1, out EndFWy1, out EndFWa1);
            }
            //if (winEx != null)
            //{
            //    Algorithmic.BKTAlgorithmicUp(winEx, Image, num, len, out ho_cross,
            //   out cross1, aertmin, aertmax, 0.6, 1, 10, ReduImage, out hv_indices, out hv_indices1,
            //   out hv_angledown, out hv_distance, out hv_rowcenter, out hv_colcenter, out state, inputangle, out area, out Dislen);
            //    AllshowWindow.algo[4]._win.displayText("贴合位：" + "X:" + EndFWx1.ToString()
            //        + ";" + "Y:" + EndFWy1.ToString() + ";" + "A:" + EndFWa1.ToString(), Color.Lime, 1000, 1000);
            //}

            SYGlobal.AddLogFrom("贴合位：" + "X:" + Convert.ToDouble(EndFWx1.ToString()).ToString("0.000")
                        + ";" + "Y:" + Convert.ToDouble(EndFWy1.ToString()).ToString("0.000") + ";" + "A:" + Convert.ToDouble(EndFWa1.ToString()).ToString("0.000"), BgColorGrade.Green);
        }

        public void Datavar(string Project, string num, out double DownWx, out double DownWy, out double DownWa, out double UpWx, out double UpWy,
         out double UpWa)
        {
            DownWx = 0;
            DownWy = 0;
            DownWa = 0;
            //上视觉拍照位
            UpWx = 0;
            UpWy = 0;
            UpWa = 0;
            //示教放置位

            Dictionary<String, String> datavar = SYGlobal._RobotSysconf.GetValueAll();
            foreach (var item in datavar)
            {
                SYJsonObject sYJsonObject = new SYJsonObject(datavar[item.Key]);
                JavaScriptSerializer json = new JavaScriptSerializer();
                RobotData items = json.Deserialize<RobotData>(sYJsonObject);
                // 修改待定
                if (items.JZ == Project && items.btIndex == num)
                {
                    if (items.Type == "下视觉补偿")
                    {
                        DownWx = double.Parse(items.x);
                        DownWy = double.Parse(items.y);
                        DownWa = double.Parse(items.a);
                    }
                    if (items.Type == "上视觉补偿")
                    {
                        UpWx = double.Parse(items.x);
                        UpWy = double.Parse(items.y);
                        UpWa = double.Parse(items.a);
                    }


                }
            }
            if (DownWx == 0 && UpWx == 0)
            {
                throw new Exception("未获获取拍照坐标");
            }
        }

        /// <summary>
        /// 判断是否更换机器人数据信息
        /// </summary>
        private bool IfUpdatefangan()
        {
            // 获取当前方案
            SYJsonObject jsonitem = new SY.Common.SYJsonObject(SYGlobal._FromSysconf["Com_Project"]);

            string strFanAn = jsonitem["FanAn"];

            if (strFanAn != _strFanAn)
            {
                return true;
            }
            List<string> RelsdataMessRun = new List<string>();

            if (_RobotTcpMess.Count > 0)
            {
                InitBtnData();

                foreach (var item in _RobotTcpMess)
                {
                    RelsdataMessRun.Add(ProgramRobotTcp(item, false));
                }
                string data1 = string.Join("", RelsdataMessRun);
                string data2 = string.Join("", _RelsdataMess);
                if (data1 != data2)
                {
                    return true;
                }
            }
            return false;

        }

        // PLC监听数据
        private void SYGlobal__PlcTcpClickFun(object args)
        {
            try
            {
                PLCSigModel item = (PLCSigModel)args;
                if (SYGlobal._ErrorSysconf[item.Key] != "")
                {
                    ShowerrorMes(item.IoInfoName, Color.Red);
                    int iCount = SYGlobal._ErrorSysconf.GetValueAll().Count;
                    SYGlobal._ErrorDataSysconf["SY" + iCount] = new SY.Common.SYJsonObject(new erroritem() { ID = iCount + "", info = item.IoInfoName, key = item.Key, time = DateTime.Now.ToString("yyyy-MM-dd HH:mm.ss") }).ToString();
                    SYGlobal._ErrorDataSysconf.Save();
                }

                HomePLCItem itemes = _LeftPLCItemList.Find(s => s._item.Key == item.Key) as HomePLCItem;

                if (itemes != null)
                {
                    itemes.InitVal(item.Value);
                }


                string TRRIF = SYGlobal._QIEConfigFrom["TRRIF"].ToString().Split('-')[0];
                string PASSCOUNT = SYGlobal._QIEConfigFrom["PASSCOUNT"].ToString().Split('-')[0];
                string QBERROR = SYGlobal._QIEConfigFrom["QBERROR"].ToString().Split('-')[0];
                string VISERROR = SYGlobal._QIEConfigFrom["VISERROR"].ToString().Split('-')[0];
                string YLERROR = SYGlobal._QIEConfigFrom["YLERROR"].ToString().Split('-')[0];
                string ERRORTIME = SYGlobal._QIEConfigFrom["ERRORTIME"].ToString().Split('-')[0];


                if (item.IoKeyData == TRRIF && Convert.ToInt32(item.Value) == 1)
                {

                    SYGlobal.AddLogFrom($"QIE上传信号:{item.Key} Value={item.Value}");
                    string QIEBU = SYGlobal._QIEConfigFrom["textBox_QIEBU"];
                    string QIEStation = SYGlobal._QIEConfigFrom["textBox_QIEStation"];
                    string QIEStep = SYGlobal._QIEConfigFrom["textBox_QIEStep"];
                    string QIEOutPutStr = SYGlobal._QIEConfigFrom["textBox_QIEOutPutStr"];
                    DataTable outputstr = new DataTable();


                    int iPASSCOUNT = Convert.ToInt32(SYGlobal._PlcDataList.Find(s => s.Key == PASSCOUNT).Value);
                    int iQBERROR = Convert.ToInt32(SYGlobal._PlcDataList.Find(s => s.Key == QBERROR).Value);
                    int iVISERROR = Convert.ToInt32(SYGlobal._PlcDataList.Find(s => s.Key == VISERROR).Value);
                    int iYLERROR = Convert.ToInt32(SYGlobal._PlcDataList.Find(s => s.Key == YLERROR).Value);
                    int iERRORTIME = Convert.ToInt32(SYGlobal._PlcDataList.Find(s => s.Key == ERRORTIME).Value);



                    string Inputstr = $"SN={(_ThisSN == "" ? "NULL" : _ThisSN)};$;Line={SYGlobal._QIEConfigFrom["textBox_Line"]};$;Station={QIEStation};$;TCmbPro={_strFanAn};$;PassCount={iPASSCOUNT};$;PCount={iQBERROR};$;NGCount={iVISERROR};$;YCount={iYLERROR};$;NGTime={iERRORTIME};$;";
                    SYGlobal.AddLogFrom($"QIE上传:QIEBU={QIEBU},QIEStation={QIEStation},QIEStep={QIEStep},Inputstr={Inputstr}", BgColorGrade.Blue);

                    bool strs1 = SYGlobal.DBSPsevr.ExchangeDataViaSP(20, QIEBU, QIEStation, QIEStep, Inputstr, ref QIEOutPutStr, ref outputstr);
                    string fest = SYGlobal.DBSPsevr.GetErrMsg();
                    SYGlobal.AddLogFrom("QIE返回" + QIEOutPutStr, BgColorGrade.Blue);
                }
            }
            catch (Exception ex)
            {


            }
        }

        #region 界面PLC操作按钮控制
        /// <summary>
        /// PLC启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Start_BtnClick(object sender, EventArgs e)
        {
            try
            {

                string strStartkey = SYGlobal._HomeConfigFromData["Com_Start"].ToString().Split('-')[0];

                SYGlobal.busTcpClient.Write(strStartkey, true);
            }
            catch
            {
                throw new Exception("无效配置");

            }
        }

        private void ucBtnExt2_BtnClick(object sender, EventArgs e)
        {
            try
            {
                string strStartkey = SYGlobal._HomeConfigFromData["Com_Stop"].ToString().Split('-')[0];
                SYGlobal.busTcpClient.Write(strStartkey, true);
            }
            catch
            {
                throw new Exception("无效配置");
            }
        }

        private void ucBtnExt3_BtnClick(object sender, EventArgs e)
        {
            try
            {
                string strStartkey = SYGlobal._HomeConfigFromData["Com_Reset"].ToString().Split('-')[0];
                SYGlobal.busTcpClient.Write(strStartkey, true);

            }
            catch
            {

                throw new Exception("无效配置");
            }

        }

        #endregion

        #region 日志相关数据

        /// <summary>
        /// 添加一段日志
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public void LogAppend(Color color, string text)
        {
            ListText_Log.SelectionColor = color;
            ListText_Log.AppendText(text + "\r\n");
        }


        /// <summary>
        /// 日志 线程实时抓取
        /// </summary>
        /// <param name="args"></param>
        public void initFromLog(object args)
        {
            this.Invoke(new Action(() =>
            {
                lock (SYGlobal._LogFromDataList)
                {
                    if (SYGlobal._LogFromDataList.Count > iLogCount)
                    {
                        if (iLogCount > 500)
                        {
                            ListText_Log.Text = "";
                        }
                        for (int i = iLogCount; i < SYGlobal._LogFromDataList.Count; i++)
                        {
                            FromLog_Model item = SYGlobal._LogFromDataList[i];
                            LogAppend(item.bgColor, item.text);
                        }

                        iLogCount = SYGlobal._LogFromDataList.Count;
                        ListText_Log.SelectionStart = ListText_Log.Text.Length;
                        ListText_Log.SelectionLength = 0;
                        ListText_Log.ScrollToCaret();
                    }
                }
            }));
            SYGlobal._ThreadTask.QueueWorkItem(new WaitCallback(initFromLog), 1000, null);
        }

        /// <summary>
        /// 上位机连接通信 显示LED
        /// </summary>
        /// <param name="led"></param>
        /// <param name="lab"></param>
        /// <param name="bol"></param>
        private void UpdateConnectState(UCSignalLamp led, Label lab, Boolean bol)
        {
            this.Invoke(new Action(() =>
            {
                try
                {
                    System.Drawing.Color col = System.Drawing.Color.Red;

                    string strtext = lab.Text;

                    if (bol)
                    {
                        col = System.Drawing.Color.Green;
                        strtext = strtext.Substring(0, strtext.Length - 2) + "正常";
                    }
                    else
                    {
                        strtext = strtext.Substring(0, strtext.Length - 2) + "异常";
                    }
                    lab.Text = strtext;
                    led.LampColor = new System.Drawing.Color[] { col };

                }
                catch { }
            }));
        }




        #endregion

        #region 界面UI复制渲染


        /// <summary>
        /// 页面步骤条
        /// </summary>
        /// <param name="idex"></param>
        /// <param name="Bol"></param>
        public void UpDataStep(int idex, bool Bol = false)
        {
            this.Invoke((EventHandler)delegate
            {
                Dev_Step3.ClearSterForeColor();

                Dev_Step3.StepIndex = idex;
                if (Bol)
                {
                    Dev_Step3.AddSterForeColor(idex - 1, System.Drawing.Color.Red);
                }
            });
        }


        public void LoadData()
        {
            this.Invoke((EventHandler)delegate
            {
                try
                {
                    //label6.Text = $"今日已检：{SYGlobal._NowData.iOKCount + SYGlobal._NowData.iNGCount}";
                    //label5.Text = $"合格：{SYGlobal._NowData.iOKCount}";
                    //label3.Text = $"不合格：{SYGlobal._NowData.iNGCount}";
                    //double dos = Convert.ToDouble(SYGlobal._NowData.iOKCount) / Convert.ToDouble(SYGlobal._NowData.iOKCount + SYGlobal._NowData.iNGCount);
                    //label4.Text = $"合格率：{Convert.ToDouble((dos * 100)).ToString("0.00")}%";
                }
                catch { }
                ;
            });
        }
        public void ShowerrorMes(string msg, Color col)
        {
            this.Invoke((EventHandler)delegate
            {
                ucRollText1.Text = msg;
                ucRollText1.ForeColor = col;
            });
        }

        private void ucRollText1_Click(object sender, EventArgs e)
        {
            ErrorCon ec = new ErrorCon();
            ec.ShowDialog();
        }


        private void ucBtnExt6_BtnClick(object sender, EventArgs e)
        {
            try
            {
                bool bOneInte = (bool)SYGlobal._PlcDataList.Find(s => s.Key == _strOneInte).Value;
                SYGlobal.busTcpClient.Write(_strOneInte, !bOneInte);
                ShowBtnData(ucBtnExt6, bOneInte ? "联机模式" : "单机模式", bOneInte ? Color.Green : Color.Red);
            }
            catch
            {
                throw new Exception("无效配置");
            }
        }

        private void ucBtnExt1_BtnClick(object sender, EventArgs e)
        {
            try
            {
                bool bJGMS = (bool)SYGlobal._PlcDataList.Find(s => s.Key == _strJGMS).Value;
                SYGlobal.busTcpClient.Write(_strJGMS, !bJGMS);
                ShowBtnData(ucBtnExt1, bJGMS ? "加工模式" : "直通模式", bJGMS ? Color.Green : Color.Red);
            }
            catch
            {
                throw new Exception("无效配置");
            }
        }

        private void ucBtnExt5_BtnClick(object sender, EventArgs e)
        {
            try
            {
                bool bMats = (bool)SYGlobal._PlcDataList.Find(s => s.Key == _strMats).Value;
                SYGlobal.busTcpClient.Write(_strMats, !bMats);
                ShowBtnData(ucBtnExt5, bMats ? "手动模式" : "自动模式", bMats ? Color.Green : Color.Red);
            }
            catch
            {
                throw new Exception("无效配置");
            }
        }

        private void ucBtnExt4_BtnClick(object sender, EventArgs e)
        {
            try
            {
                string strStartkey = SYGlobal._HomeConfigFromData["Com_ErrorCli"].ToString().Split('-')[0];
                SYGlobal.busTcpClient.Write(strStartkey, true);
                ShowerrorMes("正常", Color.Lime);
            }
            catch
            {
                throw new Exception("无效配置");
            }
        }



        public void ShowBtnData(UCBtnExt btn, string TxT, Color col)
        {
            this.Invoke((EventHandler)delegate
            {
                btn.BtnText = TxT;
                btn.FillColor = col;
            });
        }
        #endregion

        #region 其他方法

        /// <summary>
        /// 打开图片文件夹
        /// </summary>
        /// <returns></returns>
        private string GetOpenFileDialog()
        {
            string RefString = "";

            OpenFileDialog opnDlg = new OpenFileDialog();
            opnDlg.Filter = "所有图像文件 | *.bmp; *.pcx; *.png; *.jpg; *.gif;" +
                "*.tif; *.ico; *.dxf; *.cgm; *.cdr; *.wmf; *.eps; *.emf";
            opnDlg.Title = "打开图像文件";
            opnDlg.ShowHelp = true;
            opnDlg.Multiselect = false; // Multiple select  
            if (opnDlg.ShowDialog() == DialogResult.OK)
            {
                RefString = opnDlg.FileName;
            }
            return RefString;
        }
        #endregion


        private void 打開窗口監視ToolStripMenuItem_Click(object sender, EventArgs e)
        {



        }

        private void button2_Click(object sender, EventArgs e)
        {
            _TcpSrv_OnRecv(null, System.Text.Encoding.UTF8.GetBytes("PZDOWN;" + comboBox1.Text));

        }
        private void button3_Click(object sender, EventArgs e)
        {
            _TcpSrv_OnRecv(null, System.Text.Encoding.UTF8.GetBytes("PZUP;" + comboBox1.Text));

        }



    }
}
