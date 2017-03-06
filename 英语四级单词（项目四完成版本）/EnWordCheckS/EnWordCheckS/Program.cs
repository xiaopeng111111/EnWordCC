using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace EnWordCheckS
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
           // Application.Run(new FrmMain());
            //创建需要的窗体
            DataControl.frmMain = new FrmMain();
            DataControl.arStudents = new ArrayList();
            DataControl.arVocBand4 = new ArrayList();
            EnWordData.StringToEnWord("ban4vocab.txt");
            EnWordData.FetchFromVoc(ref DataControl.arVocBand4, "ban4.voc");
            DataControl.hasVocBand4 = true;
            Stu2Data.LoadStuFromEls(Application.StartupPath + "\\stu.xls",ref DataControl .arStudents );
            DataControl.hasStudent = true;
            //生成单词词组，100为单位
            ((FrmMain)DataControl.frmMain).GenerateWordsGroup();
            //获取学生信息
            ((FrmMain)DataControl.frmMain).FreshStuInfo();
            ((FrmMain)DataControl.frmMain).Update();

            Application.Run(DataControl.frmMain);

        }
    }
}
