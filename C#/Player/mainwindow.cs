using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing.Imaging;
using CameraHandle = System.IntPtr;
using CKSDK;
using CKAPI = CKSDK.CKSDKAPI;


namespace Player
{
    public partial class mainwindow : Form
    {
        #region variable
        protected CameraHandle m_hCamera = IntPtr.Zero;             // 句柄
        protected Thread m_CapThread;
        protected bool m_bExit = false;
        protected uint m_uWidth = 0;
        protected uint m_uHeight = 0;
        double m_CapFrameRate = 0;
        double m_DisFrameRate = 0;
        #endregion

        public mainwindow()
        {
            InitializeComponent();

            CameraRefresh();
        }

        public void CaptureThreadFunction()
        {
            stImageInfo ImageInfo;
            CameraSdkStatus status = CameraSdkStatus.CAMERA_STATUS_SUCCESS;
            IntPtr pRGBFrame = IntPtr.Zero;
            uint DispFrameNum = 0;
            int FrameTimeCur = 0;
            int FrameTimeLast = 0;
            FrameStatistic curFS;
            FrameStatistic lastFS;

            lastFS.iCapture = 0;
            lastFS.iLost = 0;
            lastFS.iTotal = 0;

            IntPtr pbyBuffer = IntPtr.Zero;
            uint dRGBBufLen = 0;
            CameraHandle hBuf = IntPtr.Zero;

            CKAPI.CameraPlay(m_hCamera);
            while (!m_bExit)
            {
                // 獲取畫面的統計資訊
                // 其中包括每秒的畫面刷新次數、可能因網速不穩而引發畫面丟失的數量、或雜訊太多的畫面數量
                CKAPI.CameraGetFrameStatistic(m_hCamera, out curFS);
                if (FrameTimeCur != 0)
                {
                    FrameTimeCur = Environment.TickCount;
                    int deltime = FrameTimeCur - FrameTimeLast;
                    if (1000 <= deltime)
                    {
                        m_CapFrameRate = (double)(((double)curFS.iCapture - (double)lastFS.iCapture) * 1000.0) / deltime;
                        m_DisFrameRate = (double)DispFrameNum * 1000.0 / deltime;

                        lastFS = curFS;
                        FrameTimeLast = FrameTimeCur;

                        DispFrameNum = 0;
                    }
                }
                else
                {
                    FrameTimeCur = Environment.TickCount;
                    FrameTimeLast = FrameTimeCur;
                    lastFS = curFS;
                }

                //获取 RAW 数据
                status = CKAPI.CameraGetRawImageBuffer(m_hCamera, out hBuf, 1000);
                if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    Thread.Sleep(5);
                    continue;
                }

                //获得图像缓冲区地址
                pbyBuffer = CKAPI.CameraGetImageInfo(m_hCamera, hBuf, out ImageInfo);
                //获得经 ISP 处理的 RGB 数据
                //////////////////////////////////
                if (dRGBBufLen < (ImageInfo.iWidth * ImageInfo.iHeight * 4))
                {
                    if (pRGBFrame != IntPtr.Zero)
                        Marshal.FreeHGlobal(pRGBFrame);
                    dRGBBufLen = (ImageInfo.iWidth * ImageInfo.iHeight * 4);
                    pRGBFrame = Marshal.AllocHGlobal(Convert.ToInt32(dRGBBufLen));
                }
                m_uWidth = ImageInfo.iWidth;
                m_uHeight = ImageInfo.iHeight;
                CKAPI.CameraGetOutImageBuffer(m_hCamera, ref ImageInfo, pbyBuffer, pRGBFrame);
                DispFrameNum++;
                if (m_isNeedSave)
                {
                    Bitmap bitmap = new Bitmap((int)ImageInfo.iWidth, (int)ImageInfo.iHeight, (int)ImageInfo.iWidth * 3, PixelFormat.Format24bppRgb, pRGBFrame);
                    bitmap.Save("d:\\camera_test.bmp");
                    m_isNeedSave = false;
                }
                //////////////////////////////////显示
                CKAPI.CameraDisplay(m_hCamera, pRGBFrame, ref ImageInfo);
                //释放由 CameraGetRawImageBuffer 获得的缓冲区
                CKAPI.CameraReleaseFrameHandle(m_hCamera, hBuf);
            }
            CKAPI.CameraPause(m_hCamera);

            if(pRGBFrame != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pRGBFrame);
            }
        }

        private void CameraRefresh()
        {
            // 清空设备combobox
            this.comboBox_DeviceList.Items.Clear();

            CameraSdkStatus status;
            //tSdkCameraDevInfo[] tCameraDevInfoList;
            int devNum = 0;

            // 1.取得當前探測到的設備數目
            status = CKAPI.CameraEnumerateDevice(out devNum);
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                return;
            // 2.將各個探測到設備的名稱，添加到下拉式選單裡面
            for(int i = 0; i < devNum; i++)
            {
                tDevEnumInfo devAllInfo;
                // 獲得設備的狀態，如：有無找到設備、設備是否正常開啟，是否關閉等等。
                status = CKAPI.CameraGetEnumIndexInfo(i, out devAllInfo);
                if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    continue;
                // 若設備狀態沒有問題，則將設備名稱添加到下拉式選單裡
                this.comboBox_DeviceList.Items.Add(new string(devAllInfo.DevAttribute.acFriendlyName));
            }
            if (devNum > 0)
                this.comboBox_DeviceList.SelectedIndex = 0;
        }

        private void CameraSetting()
        {
            // 1. 在創建啟動畫面前，先設定"相機設置視窗"的啟動畫面。(但若此前置作業發生異常，則不繼續執行後續指令。)
            // 2. 設定完"相機設置視窗"的啟動畫面後，創建一個新的相機設置視窗。(但若創建設置視窗時發生異常，則不繼續執行後續指令。)
            // 最後，將創建好的"相機設置視窗"顯示出來。
            if(m_hCamera != IntPtr.Zero)
            {
                if (CKAPI.CameraSetActivePage(m_hCamera, emSettingPage.SETTING_PAGE_ALL, 0) != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return;
                if (CKAPI.CameraCreateSettingPageEx(m_hCamera) != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return;
                CKAPI.CameraShowSettingPage(m_hCamera, 1);
            }
        }

        private bool CameraPlay()
        {
            // 先停止当前的播放，然后再打开新的播放设备
            CameraStop();

            int index = this.comboBox_DeviceList.SelectedIndex;
            if (index < 0)
                return false;
            // 初始化设备，返回设备句柄
            CameraSdkStatus status = CKAPI.CameraInit(out m_hCamera, index);
            if(status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                m_hCamera = IntPtr.Zero;
                return false;
            }
            // 设置ISP输出格式
            status = CKAPI.CameraSetIspOutFormat(m_hCamera, emCameraMediaType.CAMERA_MEDIA_TYPE_BGR8);
            // 初始化播放显示，设置显示图像的控件句柄
            status = CKAPI.CameraDisplayInit(m_hCamera, this.pictureBox.Handle);
            if(status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                // 將句柄反初始化，釋放資源
                CKAPI.CameraUnInit(m_hCamera);
                m_hCamera = IntPtr.Zero;
                return false;
            }
            // 設定顯示的句柄及模式
            CKAPI.CameraSetDisplayMode(m_hCamera, 0);

            // 新建视频播放线程
            m_bExit = false;
            m_CapThread = new Thread(new ThreadStart(CaptureThreadFunction));
            m_CapThread.Start();

            return true;
        }

        private void CameraStop()
        {
            // 先关闭线程
            if(m_CapThread != null)
            {
                m_bExit = true;
                while(m_CapThread.IsAlive)
                    Thread.Sleep(10);
                m_CapThread = null;
            }
            // 关闭相机句柄
            if(m_hCamera != IntPtr.Zero)
            {
                CKAPI.CameraUnInit(m_hCamera);
                m_hCamera = IntPtr.Zero;
            }
        }

        private void button_Refresh_Click(object sender, EventArgs e)
        {
            CameraRefresh();
        }

        private void button_PlayOrStop_Click(object sender, EventArgs e)
        {
            if (this.button_PlayOrStop.Text == "Play")
            {
                if (CameraPlay())
                {
                    this.button_PlayOrStop.Text = "Stop";
                    this.button_Setting.Enabled = true;
                    this.timer_DevInfo.Start();
                }
            }
            else
            {
                CameraStop();
                this.button_PlayOrStop.Text = "Play";
                this.button_Setting.Enabled = false;
                this.timer_DevInfo.Stop();
            }
        }

        private void button_Setting_Click(object sender, EventArgs e)
        {
            CameraSetting();
        }

        private void timer_DevInfo_Tick(object sender, EventArgs e)
        {
            this.label_DevInfo.Text = string.Format("resolution: {0} x {1} | capture frame rate: {2:###.##} | display frame rate: {3:###.##}",
                m_uWidth, m_uHeight, m_CapFrameRate, m_DisFrameRate);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CameraStop();
        }

        private void SaveImageBtn_Click(object sender, EventArgs e)
        {
            if (this.button_PlayOrStop.Text == "Stop")
            {
                m_savePath = "d:\\camera_test.bmp";
                m_isNeedSave = true;
            }
            else
            {
                MessageBox.Show("Please play camera first!");
            }
        }

        private string m_savePath;
        private bool m_isNeedSave;
    }

}
