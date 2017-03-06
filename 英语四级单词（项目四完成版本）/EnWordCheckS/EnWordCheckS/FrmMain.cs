using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnWordCheckS
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }
        #region 变量声明
        public static string strLocalHAddr;
        public static string strInfo;
        public static EnWord enwData;
        public static byte[] udpRecvDataBuf;
        public static byte[] udpSendDataBuf;
        public static IntPtr mWndhandle;
        public static IPAddress LocalHostIPAddress;
        public static EndPoint epRemoteClient;
        public static IPEndPoint ipepRemoteClient;
        public static Socket sockUdpRecv;
        public static Socket sockUdpSend;
        public static bool threadNoExist;
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(
            IntPtr hWnd,//handle to destination window
            int Msg,   //message
            int wParam,   //first message parameter
            int IParam    //second message parameter

            );
        public static bool udpSockClosed;
        public const int NO_SERVER = 0x520;
        public const int FOUND_CLIENT = 0x521;
        //UDP数据发送端口与接收端口值-服务端
        public const int SEND_PORT = 9096;
        public const int RECV_PORT = 9095;
        public static int iUdprecvDataLen;
        public static int iUdpsendDataLen;
        public static ManualResetEvent mrEventGotServer;
        public static ManualResetEvent mrEventWordToSend;
        public static ManualResetEvent mrEventTremiThr;
        public static ManualResetEvent mrEventClinetCon;
        #endregion
        [DllImport("kernel32")]
        static extern uint GetTickCount();
        //生成新单词组
        public static int curWordGroupIndex = 0;
        //当前单词在数组中的编号
        public static int iCurTestWordIndex;
        public ArrayList arAllWordsUnits;




        #region 生成随机数组
        public void GenerateWordsGroup()
        {
            if (DataControl.hasStudent && DataControl.hasVocBand4)
            {
                Random ran1 = new Random((int)GetTickCount());
                int totalVoc = DataControl.arVocBand4.Count;
                ArrayList arIniCount = new ArrayList();
                for (int i = 0; i < totalVoc; i++)
                {
                    arIniCount.Add(i);
                }
                int curNum = 0;
                arAllWordsUnits = new ArrayList();
                for (int j = 0; j < 30; j++)
                {
                    ArrayList arOneGroup = new ArrayList();
                    for (int i = 0; i < 100; i++)
                    {
                        curNum = ran1.Next(totalVoc);
                        arOneGroup.Add(arIniCount[curNum]);
                        arIniCount.RemoveAt(curNum);
                        totalVoc--;
                    }
                    arAllWordsUnits.Add(arOneGroup);
                }
            }
        }//end public void GenerateWordsGroup
        #endregion
        #region freshWordList显示一组单词表
        public void freshWordList()
        {
            listBox1.Items.Clear();
            for (int i = 0; i < 100; i++)
            {
                int curIndex = (int)((ArrayList)arAllWordsUnits[curWordGroupIndex])[i];
                EnWord oneWord = (EnWord)DataControl.arVocBand4[curIndex];
                listBox1.Items.Add(oneWord.eWord + "/" + oneWord.PhoneticSymbol + "/" + oneWord.ChineseChar);
            }
            listBox1.TopIndex = 30;
            listBox1.SelectedIndex = 50;
            curWordGroupIndex = (curWordGroupIndex + 1) % 30;
        }
        #endregion


        //及时发送英文单词的线程
        static void thrServerSendEnWord()   //关了的
        {
      

        //控制仅有一个线程实例
        threadNoExist = false;
            #region 等待用户连接成功
            while (!mrEventClinetCon.WaitOne(500)) ;
            #endregion

            #region 等待用户选择单词
            mrEventWordToSend = new ManualResetEvent(false);
            ManualResetEvent[] mrA = new ManualResetEvent[2];
            mrA[0] = mrEventWordToSend;
            mrA[1] = mrEventTremiThr;
            int retVal;
            retVal = WaitHandle.WaitAny(mrA, 500);
            //值为1表示要结束当前工作线程
            while (retVal != 1)
            {
                //值为0表示有单词要发送
                if (retVal == 0)
                {
                    //有单词要发送
                #region 发送单词
                    //设置要发送的单词的下标值即可，而不发送完整
                    byte[] bUdpData = BitConverter.GetBytes(iCurTestWordIndex);
                    iUdpsendDataLen = 4;
                    Buffer.BlockCopy(bUdpData, 0, udpSendDataBuf, 0, iUdpsendDataLen);
                    //ipepRemoteClient
                    ipepRemoteClient.Port = SEND_PORT;
                    sockUdpSend.SendTo(udpSendDataBuf, iUdpsendDataLen, SocketFlags.None, ipepRemoteClient);
                    //事件重置
                    mrEventWordToSend.Reset();
                    #endregion

                }
                retVal = WaitHandle.WaitAny(mrA, 500);
            }
            #endregion
            mrEventWorkThreadEnd.Set();
            threadNoExist = true;
            while (!mrEventWorkThreadEnd.WaitOne(2000)) ;
            SendMessage(mWndhandle, WM_CLOSE, 0, 0);
        }



        public class StateObject
        { }
        public static StateObject stobUdp;
        public static void ReceiveUdpCallback(IAsyncResult ar)
        {
            try
            {
                stobUdp = (StateObject)ar.AsyncState;
                EndPoint tempRemoteEP = (EndPoint)ipepRemoteClient;
                iUdprecvDataLen = sockUdpRecv.EndReceiveFrom(ar, ref tempRemoteEP);
                strInfo = Encoding.UTF8.GetString(udpRecvDataBuf, 0, iUdprecvDataLen);
                udpSendDataBuf = new byte[1024];
                ipepRemoteClient = (IPEndPoint)tempRemoteEP;
                IPEndPoint iep = new IPEndPoint(ipepRemoteClient.Address, SEND_PORT);
                udpSendDataBuf = Encoding.UTF8.GetBytes("hello from server");
                if (strInfo.CompareTo("are you online?") == 0)
                {
                    //检测用户发来数据，确定客户端是否连接成功
                    //回复客户端
                    sockUdpSend.SendTo(udpSendDataBuf, udpSendDataBuf.Length, SocketFlags.None, iep);
                    mrEventClinetCon.Set();
                }
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        public static IPEndPoint ipepClient;
        private void FrmMain_Load(object sender, EventArgs e)
        {
            mWndhandle = this.Handle;    //指向窗体
            mrEventTremiThr = new ManualResetEvent(false);
            mrEventWorkThreadEnd = new ManualResetEvent(false);   //工作线程结束信号
            mrEventClinetCon = new ManualResetEvent(false);
            mrEventWordToSend = new ManualResetEvent(false);   //单词发送信号
            udpRecvDataBuf = new byte[1024];
            //启动管理结束线程
            ThreadStart theTStart = new ThreadStart(TerminateAllThread);   //theTStart委托TerminateAllThread（结束线程）
            Thread thrT = new Thread(theTStart);
            thrT.Start();
            #region 服务在线响应，仅需绑定回调函数即可
            sockUdpSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); //初始化一个Socket协议，用于发送数据
            //开始进行UDP数据包接收，接收到广播包就进行回复，表示服务器存在
            sockUdpRecv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);   //初始化一个Scoket协议，用于接收数据
            //初始化指定端口的网络端口实例
            ipepRemoteClient = new IPEndPoint(IPAddress.Any, 9095);
            //绑定接收端口的网络端口实例
            sockUdpRecv.Bind(ipepRemoteClient);  //绑定这个实例      //Bind用于提交服务端（Recv）的值
            stobUdp = new StateObject();
            epRemoteClient = new IPEndPoint(IPAddress.Any, 0);
            sockUdpRecv.BeginReceiveFrom(udpRecvDataBuf, 0, 1024,
                SocketFlags.None, ref epRemoteClient, ReceiveUdpCallback, stobUdp);
            #endregion
            //启动主工作线程
            ThreadStart theStart = new ThreadStart(thrServerSendEnWord);
            Thread thr = new Thread(theStart);
            thr.Start();
        }
        #region 全局线程结束管理
        public const int WM_CLOSE = 0x10;
        public static ManualResetEvent[] mrTerAll;
        public static ManualResetEvent mrEventWorkThreadEnd;
        public static void TerminateAllThread()
        {
            while (!mrEventWorkThreadEnd.WaitOne(2000)) ;
            SendMessage(mWndhandle, WM_CLOSE, 0, 0);
        }
        #endregion
        #region  FreshStuInfo刷新学生信息
        public void FreshStuInfo()
        {
            //listBox2.Items.Clear();
            if (DataControl.arStudents.Count > 0)
            {
                for (int i = 0; i < DataControl.arStudents.Count; i++)
                {
                    listBox2.Items.Add(((Student)DataControl.arStudents[i]).StuName);
                }
            }
        }
        #endregion



        private void button1_Click(object sender, EventArgs e)
        {
            ipepClient = new IPEndPoint(IPAddress.Parse("192.168.1.106"), SEND_PORT);
            udpSendDataBuf = Encoding.UTF8.GetBytes("now now now");
            sockUdpSend.SendTo(udpSendDataBuf, udpSendDataBuf.Length, SocketFlags.None, ipepClient);
        }

        #region 窗体消息重载
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case FOUND_CLIENT:
                    label7.Text = string.Format("客户端{0}");
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;

            }
        }
        #endregion



       

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mrEventWorkThreadEnd.WaitOne(1))
            { mrEventTremiThr.Set(); }
            else
            {
                e.Cancel =true ;
             
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mrEventWorkThreadEnd.Set();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //刷新显示单词
            freshWordList();
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            ArrayList curWordList = (ArrayList)arAllWordsUnits[(curWordGroupIndex + 29) % 30];
            int curWordIndex = (int)curWordList[listBox1.SelectedIndex];
            DataControl.curCheckWordIndex = curWordIndex;
            EnWord oneWord = (EnWord)DataControl.arVocBand4[curWordIndex];
            label4.Text = oneWord.eWord;
            label5.Text = oneWord.ChineseChar;
            label6.Text = "\\" + oneWord.PhoneticSymbol + "\\";
            iCurTestWordIndex = curWordIndex;
            mrEventWordToSend.Set();
        }

    }
}
