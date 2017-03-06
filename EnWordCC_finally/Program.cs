using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnWordCC
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
            //创建需要的窗体
            DataControl.frmMain = new FrmMain();
            DataControl.arStudents = new ArrayList();
            DataControl.arVocBand4 = new ArrayList();
            EnWordData.FetchFromVoc(ref DataControl.arVocBand4, "band4.voc");
            DataControl.hasVocBand4 = true;
            Application.Run(DataControl.frmMain);

      
        }
    }
}
