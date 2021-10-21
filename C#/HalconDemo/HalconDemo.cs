using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing.Imaging;
using CameraHandle = System.IntPtr;
using CKSDK;
using CKAPI = CKSDK.CKSDKAPI;

namespace HalconDemo
{
    public partial class HalconDemo : Form
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

        public HalconDemo()
        {
            InitializeComponent();

            CameraRefresh();

            HD = new HDevelopExport();
            HD.SetWindow(hWindowControl1.HalconWindow);
        }

        public void CaptureThreadFunction()
        {
            stImageInfo ImageInfo = new stImageInfo();
            CameraSdkStatus status = CameraSdkStatus.CAMERA_STATUS_SUCCESS;
            IntPtr pRGBFrame = IntPtr.Zero;
            uint DispFrameNum = 0;
            int FrameTimeCur = 0;
            int FrameTimeLast = 0;
            FrameStatistic curFS = new FrameStatistic();
            FrameStatistic lastFS = new FrameStatistic();

            IntPtr pbyBuffer = IntPtr.Zero;
            uint dRGBBufLen = 0;
            CameraHandle hBuf = IntPtr.Zero;

            // m_hCamera 代表著當前相機的訊息
            // 而顯示相機畫面的CameraPlay，需要知道哪部相機的畫面需要被顯示，故將 m_hCamera 告知給 CameraPlay
            // 開始接收相機的訊息，並顯示畫面
            CKAPI.CameraPlay(m_hCamera);
            while (!m_bExit)
            {
                // 獲取畫面的統計資訊
                // 而獲取這些資訊需要知道是哪一台相機(為了能夠識別)，故將 m_hCamera 也就是現在的相機資訊告知給 CameraGetFrameStatistic
                // 就可以透過 CameraGetFrameStatistic 拿到剛剛給定的相機的禎數資訊(畫面每秒更新數)，並將其記錄在 curFS 之中。
                // 而其中 curFS 又包括1.每秒的總畫面刷新次數 2.每秒丟失的畫面數量 3.每秒有效用的禎數
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

                // 1.先取得相機畫面
                // 取得由黑、白、及不同程度的灰色，所組成的相機灰階畫面
                status = CKAPI.CameraGetRawImageBuffer(m_hCamera, out hBuf, 1000);
                if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    Thread.Sleep(5);
                    continue;
                }

                // 2.取得存放畫面處的位址  (取得其緩衝區地址)
                pbyBuffer = CKAPI.CameraGetImageInfo(m_hCamera, hBuf, out ImageInfo);
                //获得经 ISP 处理的 RGB 数据
                //////////////////////////////////
                if (dRGBBufLen < (ImageInfo.iWidth * ImageInfo.iHeight * 4))
                {
                    dRGBBufLen = (ImageInfo.iWidth * ImageInfo.iHeight * 4);
                    pRGBFrame = Marshal.AllocHGlobal(Convert.ToInt32(dRGBBufLen));
                }
                m_uWidth = ImageInfo.iWidth;
                m_uHeight = ImageInfo.iHeight;

                // 3.優化畫面。
                // 處理獲取的原始圖像，
                // 並輸出飽和度更高(綠者更綠、紅者更紅)
                // 顏色校正(讓畫面顏色更貼近真實)
                // 降噪處理後的圖像
                // 
                CKAPI.CameraGetOutImageBuffer(m_hCamera, ref ImageInfo, pbyBuffer, pRGBFrame);
                DispFrameNum++;
                if (m_isNeedSave)
                {
                    Bitmap bitmap = new Bitmap((int)ImageInfo.iWidth, (int)ImageInfo.iHeight, (int)ImageInfo.iWidth * 3, PixelFormat.Format24bppRgb, pRGBFrame);
                    bitmap.Save(m_savePath);
                    m_isNeedSave = false;
                }
                //////////////////////////////////显示
                //CKAPI.CameraDisplay(m_hCamera, pRGBFrame, ref ImageInfo);
                ///////////////////////////////////

                //使用halcon窗口进行显示
                HD.display(pRGBFrame, (int)ImageInfo.iWidth, (int)ImageInfo.iHeight, 2592, 1944);

                // 4.優化儲存空間，釋放在[2.]時候存放之畫面所佔有的空間
                // 釋放由 CameraGetRawImageBuffer 獲得的緩衝區
                CKAPI.CameraReleaseFrameHandle(m_hCamera, hBuf);
            }
            // 暫停顯示裝置
            CKAPI.CameraPause(m_hCamera);
        }

        private void CameraRefresh()
        {
            // 先清空等等會用到的下拉式選單
            this.comboBox_DeviceList.Items.Clear();

            CameraSdkStatus status;
            //tSdkCameraDevInfo[] tCameraDevInfoList;
            int devNum = 0;

            // 1.取得當前探測到的設備數目，並將數目記錄到 devNum，將狀態(成功與否)記錄到 status
            status = CKAPI.CameraEnumerateDevice(out devNum);
            // 若此時狀態是失敗的，則不繼續執行後續的指令。
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                return;

            // 2.將各個探測到設備的名稱，添加到下拉式選單裡面
            for (int i = 0; i < devNum; i++)
            {
                tDevEnumInfo devAllInfo;
                // 獲得設備的狀態資料，如：設備是否開啟、設備的版本、設備的系列、名稱等等......
                // 並將查詢設備時的狀態(成功於否)儲存在 status
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
            // Sets the activation page of the camera configuration window
            // 1. 設定 相機校調視窗 的啟用面板

            // 1. 在創建啟動畫面前，先設定"相機校調視窗"的啟動畫面。(但若此前置作業發生異常，則不繼續執行後續指令。)
            // 2. 設定完"相機校調視窗"的啟動畫面後，創建一個新的相機設置視窗。(但若創建設置視窗時發生異常，則不繼續執行後續指令。)
            // 最後，將創建好的"相機校調視窗"顯示出來。
            if (m_hCamera != IntPtr.Zero)
            {
                if (CKAPI.CameraSetActivePage(m_hCamera, emSettingPage.SETTING_PAGE_ALL, 0) != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return;
                if (CKAPI.CameraCreateSettingPageEx(m_hCamera) != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return;
                // 否則，若創建新的 相機校調視窗 時發生異常，
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
            // 初始化選單中所勾選的裝置，並獲取剛剛初始化的裝置的位址訊息
            // 並同時獲取選單中所勾選相機的資訊(並存入 m_hCamera 之中)。
            // 若在獲取過程中有異常，則不繼續執行後續指令。
            CameraSdkStatus status = CKAPI.CameraInit(out m_hCamera, index);
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                m_hCamera = IntPtr.Zero;
                return false;
            }
            // 設定將相機畫面做影像處理時的模式
            // 告知 CameraSetIspOutFormat 所要設定的相機(其資訊儲存在 m_hCamera 之中)、所設定的模式(這裡是 emCameraMediaType.CAMERA_MEDIA_TYPE_RGB8 模式)
            CKAPI.CameraSetIspOutFormat(m_hCamera, emCameraMediaType.CAMERA_MEDIA_TYPE_RGB8);

            // 新建视频播放线程
            m_bExit = false;
            m_CapThread = new Thread(new ThreadStart(CaptureThreadFunction));
            m_CapThread.Start();

            return true;
        }

        private void CameraStop()
        {
            // 先停止視窗中相機畫面的顯示
            if (m_CapThread != null)
            {
                m_bExit = true;
                while (m_CapThread.IsAlive)
                    Thread.Sleep(10);
                m_CapThread = null;
            }
            // 再關閉相機、並釋放電腦中儲存相機相關資訊所花費的空間、讓空間可以重新利用。
            if (m_hCamera != IntPtr.Zero)
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

        private void HalconDemo_FormClosing(object sender, FormClosingEventArgs e)
        {
            CameraStop();
        }

        private void timer_DevInfo_Tick(object sender, EventArgs e)
        {
            this.label_DevInfo.Text = string.Format("resolution: {0} x {1} | capture frame rate: {2:###.##} | display frame rate: {3:###.##}",
                m_uWidth, m_uHeight, m_CapFrameRate, m_DisFrameRate);
        }

        HDevelopExport HD;
        private string m_savePath;
        private bool m_isNeedSave;
    }
}
