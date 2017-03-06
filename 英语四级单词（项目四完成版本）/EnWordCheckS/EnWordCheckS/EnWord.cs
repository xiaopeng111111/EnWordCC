using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace EnWordCheckS
{
    public class EnWord
    {
        //序号，英文单词，词性，音标，中文，本类封装一个单词
        public int lenEn;   //英文占据的字节长度
        public string eWord;   //英文单词
        public int lenPhnSym;   //音标占据的字节长度
        public string PhoneticSymbol;  //音标
        public int lenChChar;  //中文占据的字节长度
        public string ChineseChar;  //中文解释

    }
}
