using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnWordCC
{
    class Txt2Data
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
                //oneWord.PhoneticSymbol = Encoding.UTF8.GetString(charBuf, oneWord.lenEn, oneWord.lenPhnSym);
                //oneWord.eWord = Encoding.UTF8.GetString(charBuf, 0, oneWord.lenEn);
                oneWord.PhoneticSymbol = Encoding.UTF8.GetString(charBuf, oneWord.lenEn, oneWord.lenPhnSym);
                oneWord.ChineseChar = Encoding.UTF8.GetString(charBuf, oneWord.lenEn + oneWord.lenPhnSym, oneWord.lenChChar);
                arryVoc.Add(oneWord);
                reCode = fsVoc.Read(charBuf, 0, 12);
            }
            fsVoc.Close();
            fsVoc.Dispose();
            return true;
        }
    }
}
