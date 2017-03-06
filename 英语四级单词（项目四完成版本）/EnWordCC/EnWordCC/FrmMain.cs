using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EnWordCC
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }
        public static IntPtr mMainWndhandle;
        public static byte[] udpSearchRecvDataBuf;
        public static Socket sockUdpSearchRecv;
        public static Socket sockUdpSearchSend;
        public static EndPoint remoteUdpEp;
        public static int iUdprecvDataLen;
        public static IPEndPoint ipeRemoteClient;
        public static string strInfo;
        //UDP数据发送与接收端口值-客户端
        public const int SEARCH_SEND_PORT = 9095;
        public const int SEARCH_RECV_PORT = 9096;
        public const int UDPDATA_IN = 0x521;
        public const int WORD_IN = 0x522;
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(
            IntPtr hWnd,//handle to destination window
            int Msg,//message
            int wParam,//first message parameter
            int lParam//second message parameter
            );

        private void FrmMain_Load(object sender, EventArgs e)
        {
            mMainWndhandle = this.Handle;
            mrEventGotServer = new ManualResetEvent(false);
            mrEventTermiThread = new ManualResetEvent(false);
            ThreadStart thWorkStart = new ThreadStart(workThread);
            Thread theWorkTh = new Thread(thWorkStart);
            theWorkTh.Start();
        }
        #region 窗体消息重载函数
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case UDPDATA_IN:
                    label1.Text = strInfo;break;
                case WORD_IN:
                    label2.Text = ((EnWord)DataControl.arVocBand4[iCurTestWordIndex]).eWord;
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion
        #region 查找服务UDP接收数据回调函数
        public static void UdpSearchReceiveCallback(IAsyncResult ar)
        {
            try
            {
                ipeRemoteClient = new IPEndPoint(IPAddress.Any, 9095);
                EndPoint tempRemoteEP = (EndPoint)ipeRemoteClient;
                iUdprecvDataLen = sockUdpSearchRecv.EndReceiveFrom(ar, ref tempRemoteEP);
                strInfo = Encoding.UTF8.GetString(udpSearchRecvDataBuf, 0, iUdprecvDataLen);

                if (strInfo.CompareTo("hello from server") == 0)
                {//服务端发来数据
                    SendMessage(mMainWndhandle, UDPDATA_IN, 100, 100);
                    mrEventGotServer.Set();
                }
                else
                {
                    if (mrEventGotServer.WaitOne(1))
                    {
                        iCurTestWordIndex = BitConverter.ToInt32(udpSearchRecvDataBuf, 0);
                        SendMessage(mMainWndhandle, WORD_IN, 100, 100);
                    }
                }
                sockUdpSearchRecv.BeginReceiveFrom(udpSearchRecvDataBuf, 0, 1024, SocketFlags.None, ref tempRemoteEP, UdpSearchReceiveCallback, new object());
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }
        #endregion
        #region 主工作线程
        //当前要测试的单词数组下标
        public static int iCurTestWordIndex;
        public static string strLocalHAddr;
        public static IPEndPoint SearchServerIpEP;
        public static int sendDataLen;
        public static byte[] udpRecvDataBuf;
        public static byte[] udpDataSendBuf;
        public static ManualResetEvent mrEventGotServer;
        public static ManualResetEvent mrEventTermiThread;
        public static void workThread()
        {
            #region 获取本地可用IP地址
            strLocalHAddr = null;
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //检测可用网卡网关值，确定是否可用
            NetworkInterface[] NetWorkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface NetworkIntf in NetWorkInterfaces)
            {
                IPInterfaceProperties IpInterPro = NetworkIntf.GetIPProperties();
                UnicastIPAddressInformationCollection uniIPAInfoCol = IpInterPro.UnicastAddresses;
                foreach (UnicastIPAddressInformation UniCIPAInfo in uniIPAInfoCol)
                {
                    if ((UniCIPAInfo.Address.AddressFamily == AddressFamily.InterNetwork) && (UniCIPAInfo.IPv4Mask != null))
                    {
                        if (IpInterPro.GatewayAddresses.Count != 0)
                        {
                            //IpInterPro.GatewayAddresses的count为0,所以[0]也超出索引范围
                            //所以先将网关地址做长度判断
                            if (IpInterPro.GatewayAddresses[0].Address.ToString().CompareTo("0.0.0.0") != 0)
                            {
                                strLocalHAddr = UniCIPAInfo.Address.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            if (strLocalHAddr == null)
            {
                //无可用网络
                MessageBox.Show("无可用网络连接，请检查网络");
            }
            else
            {
                strLocalHAddr = strLocalHAddr.Substring(0, strLocalHAddr.LastIndexOf('.') + 1) + "255";
            }
            #endregion 获取本地可用IP地址
            #region 绑定查找服务的UDP对象
            udpSearchRecvDataBuf = new byte[1024];
            sockUdpSearchRecv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //绑定接收服务器UDP数据的RECV_PORT端口
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 9096);
            sockUdpSearchRecv.Bind(iep);
            remoteUdpEp = new IPEndPoint(IPAddress.Any, 0);
            sockUdpSearchRecv.BeginReceiveFrom(udpSearchRecvDataBuf, 0, 1024, SocketFlags.None, ref remoteUdpEp, UdpSearchReceiveCallback, new object());
            #endregion
            #region 发送广播包查找服务器
            //创建使用UDP发送数据的Socket对象
            sockUdpSearchSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //设置该socket实例的发送形式
            sockUdpSearchSend.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            SearchServerIpEP = new IPEndPoint(IPAddress.Parse(strLocalHAddr), SEARCH_SEND_PORT);
            udpDataSendBuf = Encoding.UTF8.GetBytes("are you online?");
            sendDataLen = udpDataSendBuf.Length;
            //向服务器发送探测包
            sockUdpSearchSend.SendTo(udpDataSendBuf, sendDataLen, SocketFlags.None, SearchServerIpEP);
            //等待服务器响应
            ManualResetEvent[] mrEventAl = new ManualResetEvent[2];
            mrEventAl[0] = mrEventGotServer;
            mrEventAl[1] = mrEventTermiThread;
            int eventIndex = WaitHandle.WaitAny(mrEventAl, 500);
            while (eventIndex != 1)
            {
                eventIndex = WaitHandle.WaitAny(mrEventAl, 500);
            }
            #endregion
        }
        #endregion
    }
}
