using QuantaApply.Algorithm;
using SY.Common;
using SYGuideLocat.From.EpsonCom;
using SYGuideLocat.From.PLCCom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HalconDotNet;
using SYGuideLocat.Model;
using static System.Windows.Forms.Control;
using System.Text.RegularExpressions;
using System.Text;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Threading;
using SYGuideLocat;

namespace SYGuideLocat
{
    static class Program
    {
        private static SYGlobal _SG = null;
        /// <summary> 
        /// 是否开启Debug 调试 不会进入 ApplicationStart
        /// </summary>
        private const Boolean _IsDebug = false;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            string p = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(p);
            if (processes.Length > 4)
            {
                MessageBox.Show("程序已经在运行中", "系统提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            else
            {
                _SG = new SYGlobal();
                Application.ApplicationExit += _SG.ApplicationExit;
                Application.ThreadException += _SG.ThreadExceptionUI;
                AppDomain.CurrentDomain.UnhandledException += _SG.ThreadException;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //  窗体Load函数之后
                if (!_IsDebug)
                {
                    _SG.ApplicationStart();
                }
                MainFrom MainForm = new MainFrom();
                _SG.G_Form = MainForm;
                Application.Run(MainForm);
                _SG.Dispose();
                _SG = null;
            }

        }

    }
}
