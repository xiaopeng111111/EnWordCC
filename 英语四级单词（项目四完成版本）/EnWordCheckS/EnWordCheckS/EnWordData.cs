using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnWordCheckS
{
    public static  class EnWordData
    {
        public static bool FetchFromVoc(ref ArrayList arryVoc, string vFile)
        {
            FileInfo fiVoc = new FileInfo(vFile);
            FileStream fsVoc = new FileStream(vFile, FileMode.Open, FileAccess.Read);
            long curPos = 0;
            int totalLen = 0;
            byte[] charBuf = new byte[1000];
            EnWord oneWord;
            int reCode = 1;
            reCode = fsVoc.Read(charBuf, 0, 12);
            while (reCode > 0)
            {
                oneWord = new EnWord();
                oneWord.lenEn = BitConverter.ToInt32(charBuf, 0);
                oneWord.lenPhnSym = BitConverter.ToInt32(charBuf, 4);
                oneWord.lenChChar = BitConverter.ToInt32(charBuf, 8);
                totalLen = oneWord.lenEn + oneWord.lenPhnSym + oneWord.lenChChar;
                curPos += totalLen;
                fsVoc.Read(charBuf, 0, totalLen);
                oneWord.eWord = Encoding.UTF8.GetString(charBuf, 0, oneWord.lenEn);
                oneWord.PhoneticSymbol = Encoding.UTF8.GetString(charBuf,
                 oneWord.lenEn, oneWord.lenPhnSym);
                oneWord.ChineseChar = Encoding.UTF8.GetString(charBuf,
                    oneWord.lenEn + oneWord.lenPhnSym, oneWord.lenChChar);
                arryVoc.Add(oneWord);
                reCode = fsVoc.Read(charBuf, 0, 12);
            }
            fsVoc.Close();
            fsVoc.Dispose();
            return true;
        }


        //实现英文单词由英文到序列化数据转化与数据读取操作
        public static bool StringToEnWord(string fileName)
        {
            if (!File.Exists(fileName))
            {
                MessageBox.Show(string.Format("{0}文件不存在！", fileName));
                return false;
            }
            StreamReader srTxt = new StreamReader(fileName);
            FileStream fsData = new FileStream("ban4.voc", FileMode.Create);
            byte[] charByetes;
            byte[] charBuf = new byte[1000];
            int totalLen = 0;
            string oneLine;
            oneLine = srTxt.ReadLine();
            while (oneLine != null)
            {
                if (oneLine.Length < 2)
                {
                    oneLine = srTxt.ReadLine();
                    continue;
                }
                //用正则表达式对单词进行匹配
                Regex r = new Regex("^(?<enWord>\\D+)/(?<PhoneticSymbol>\\D+)/(?<exPlain>\\D+)");
                Match m = r.Match(oneLine);
                EnWord oneEWord = new EnWord();
                oneEWord.eWord = m.Groups["enWord"].Value.Trim();
                oneEWord.PhoneticSymbol = m.Groups["PhoneticSymbol"].Value.Trim();
                oneEWord.ChineseChar = m.Groups["exPlain"].Value.Trim();
                //英语单词序列化
                charByetes = Encoding.UTF8.GetBytes(oneEWord.eWord);
                oneEWord.lenEn = charByetes.Length;
                Array.Copy(charByetes, 0, charBuf, 12, oneEWord.lenEn);
                charByetes = BitConverter.GetBytes(oneEWord.lenEn);
                Array.Copy(charByetes, 0, charBuf, 0, 4);
                //音标序列化
                charByetes = Encoding.UTF8.GetBytes(oneEWord.PhoneticSymbol);
                oneEWord.lenPhnSym = charByetes.Length;
                Array.Copy(charByetes, 0, charBuf, oneEWord.lenEn + 12, oneEWord.lenPhnSym);
                charByetes = BitConverter.GetBytes(oneEWord.lenPhnSym);
                Array.Copy(charByetes, 0, charBuf, 4, 4);
                //中文释义序列化
                charByetes = Encoding.UTF8.GetBytes(oneEWord.ChineseChar);
                oneEWord.lenChChar = charByetes.Length;
                Array.Copy(charByetes, 0, charBuf, oneEWord.lenEn + oneEWord.lenPhnSym + 12, oneEWord.lenChChar);
                charByetes = BitConverter.GetBytes(oneEWord.lenChChar);
                Array.Copy(charByetes, 0, charBuf, 8, 4);
                //totalLen是单个数据总长度
                totalLen = oneEWord.lenEn + oneEWord.lenPhnSym + oneEWord.lenChChar + 12;
                fsData.Write(charBuf, 0, totalLen);
                oneLine = srTxt.ReadLine();
            }
            fsData.Flush();
            fsData.Close();
            fsData.Dispose();
            return true;
        }

    }
}
