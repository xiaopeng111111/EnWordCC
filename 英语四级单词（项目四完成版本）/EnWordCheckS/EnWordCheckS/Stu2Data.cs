using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MsExcel = Microsoft.Office.Interop.Excel;

namespace EnWordCheckS
{
    public static class Stu2Data
    {
        public static void LoadStuFromEls(string xlsFileName, ref ArrayList arryStu)
        {
            if (!File.Exists(xlsFileName))
            {
                MessageBox.Show("指定的Excel" + xlsFileName + "文档不存在！");
                return;
            }
            MsExcel.Application oExcApp;   //Excel Application;
            MsExcel.Workbook oExcBook;   //
            try
            {
                oExcApp = new MsExcel.ApplicationClass();
                oExcApp.Visible = false;
                oExcBook = oExcApp.Workbooks.Open(xlsFileName);
                MsExcel.Worksheet worksheet1 = (MsExcel.Worksheet)oExcBook.Worksheets["sheet1"];
                worksheet1.Activate();
                oExcApp.DisplayAlerts  = false;//不提示警告信息
                Student oneStu;
                MsExcel.Range range1;
                int i = 2;
                range1 = worksheet1.get_Range(string.Format("A{0}", i));
                while (range1.Text.ToString().Length > 0)
                {
                    oneStu = new Student();
                    range1 = worksheet1.get_Range(string.Format("A{0}", i));
                    oneStu.StuName = range1.Text.ToString();
                    range1 = worksheet1.get_Range(string.Format("D{0}", i));
                    oneStu.roomNum = range1.Text.ToString();
                    range1 = worksheet1.get_Range(string.Format("E{0}", i));
                    oneStu.PhoneNum = range1.Text.ToString();
                    arryStu.Add(oneStu);
                    i++;
                    range1 = worksheet1.get_Range(string.Format("A{0}", i));
                }
                worksheet1 = null;
                oExcBook.Close(false);
                oExcApp.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oExcApp);
                oExcApp = null;

            }
            catch (Exception e2)
            {
                MessageBox.Show(e2.Message);
            }
        }
    }
}
