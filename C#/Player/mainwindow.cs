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
                // 獲取畫面刷新的統計資訊
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

                //获取 RAW 数据
                // 1.先取得相機影像的資訊
                // 可以設置逾時時間，這裡是1秒鐘內沒有獲得畫面資訊，則會產生逾時錯誤。
                // 隨後獲得拜耳轉換後的畫面，也就是將灰階畫面演算成彩色畫面。
                // 之後將影像以"地址"的形式輸出，意即，輸出電腦中某處存有該影像的位址，並存入 hBuf。
                status = CKAPI.CameraGetRawImageBuffer(m_hCamera, out hBuf, 1000);
                if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    Thread.Sleep(5);
                    continue;
                }

                //获得图像缓冲区地址
                // 2.取得相機的畫面
                // 而這個過程需要傳入用來區分相機的資訊，也就是 m_hCamera，
                // 以及影像存放哪裡，也就是 hBuf
                // 然後獲得影像資訊，如：畫面長度寬度、曝光時長、檔案大小。存於 ImageInfo
                pbyBuffer = CKAPI.CameraGetImageInfo(m_hCamera, hBuf, out ImageInfo);
                
                //获得经 ISP 处理的 RGB 数据
                // 將影像經過 Image Signal Processor 處理後
                // 如：
                // 儲存於 ImageInfo 
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

                // 3.優化畫面。
                // 傳入相機(m_hCamera)、待處理影像的檔案格式(ImageInfo)、影像處理後所要輸出至何處的資訊
                // 也就是處理獲取的原始圖像，
                // 並輸出飽和度更高(綠者更綠、紅者更紅)
                // 顏色校正(讓畫面顏色更貼近真實)
                // 降噪(雜訊)處理後的圖像
                CKAPI.CameraGetOutImageBuffer(m_hCamera, ref ImageInfo, pbyBuffer, pRGBFrame);
                DispFrameNum++;
                if (m_isNeedSave)
                {
                    Bitmap bitmap = new Bitmap((int)ImageInfo.iWidth, (int)ImageInfo.iHeight, (int)ImageInfo.iWidth * 3, PixelFormat.Format24bppRgb, pRGBFrame);
                    bitmap.Save("d:\\camera_test.bmp");
                    m_isNeedSave = false;
                }
                //////////////////////////////////显示
                // 顯示影像
                // 傳入相機的資訊(m_hCamera)、
                // 電腦中儲存影像的位址(pRGBFrame)、
                // 影像的相關資訊(ImageInfo)如資料大小，解析度等等......
                CKAPI.CameraDisplay(m_hCamera, pRGBFrame, ref ImageInfo);

                // 由於剛才已經獲得優化過的影像了，
                // 故將處理前之影像獨佔在電腦裡空間的位址釋放掉，以便空間在未來重複利用
                // (也就是釋放一開始從 CameraGetRawImageBuffer 拿到的原始影像，即儲存其資料的地址。
                CKAPI.CameraReleaseFrameHandle(m_hCamera, hBuf);
            }
            // 將待暫停相機的資訊傳入，暫停相機影像顯示系統的運作。
            // 執行後會得到一個數值，用以代表執行期間的狀態(如：是否遇到異常)。
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

            // 1.取得當前探測到的設備數量，並將結果存到 devNum 之中
            // 探測結束後回傳一個數值，以代表探測過程中的狀態，並儲存到 status 之中
            status = CKAPI.CameraEnumerateDevice(out devNum);
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                return;
            // 2.將各個探測到設備的名稱，添加到下拉式選單裡面
            for(int i = 0; i < devNum; i++)
            {
                tDevEnumInfo devAllInfo;
                // 查詢設備的狀態，如：有無找到設備、設備是否正常開啟，是否關閉、產品名稱、暱稱，驅動名稱、版本等等。
                // 並將獲得的訊息存入 devAllInfo 之中
                // 隨後回傳一個代表查詢過程狀態的狀態碼，儲存在 status 之中
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
            // 1. 在創建啟動畫面前，先設定"相機訊息視窗"，如這裡是設定讓視窗呈現所有的訊息(包括設備訊息、曝光的頁面、解析度的頁面......)。(但若此前置作業發生異常，則不繼續執行後續指令。)
            // 2. 設定完"相機訊息視窗"的畫面後，創建一個新的"相機訊息視窗"。(但若創建設置視窗時發生異常，則不繼續執行後續指令。)
            // 最後，將創建好的"相機訊息視窗"顯示出來。
            if(m_hCamera != IntPtr.Zero)
            {
                // 傳入將要被設定的相機(m_hCamera)，以及想要呈現哪些訊息(emSettingPage.SETTING_PAGE_ALL)，還有呈現訊息的視窗在其他視窗間的顯示順序編號(如此處為第一順位)
                if (CKAPI.CameraSetActivePage(m_hCamera, emSettingPage.SETTING_PAGE_ALL, 0) != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return;
                // 傳入相機的相關資訊，並創建該相機的"相機訊息視窗"，且會回傳在創建過程中的狀態(若狀態為異常，則不繼續執行後續指令)
                if (CKAPI.CameraCreateSettingPageEx(m_hCamera) != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    return;
                // 傳入相機的資訊，以及是否顯示該視窗，此處是選擇顯示(傳入1則顯示)
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
            // 傳入即將初始化相機在其他相機之間的順位編號，初始化選單中所勾選的裝置，並獲取剛剛初始化的裝置的訊息(存放在電腦中的位址)
            // 獲取選單中所勾選相機資訊的同時，將訊息存入 m_hCamera 之中
            // 若在獲取過程中有異常，則不繼續執行後續指令。
            CameraSdkStatus status = CKAPI.CameraInit(out m_hCamera, index);
            if(status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                m_hCamera = IntPtr.Zero;
                return false;
            }
            // 設定影像處理的輸出格式
            // 這裡需要傳入"哪個相機"需要被設定(m_hCamera)，以及要設定成何種輸出模式，如普通彩色影像或具透明的彩色影像等等......
            // 此處是設定成BGR三原色所組成的彩色影像
            status = CKAPI.CameraSetIspOutFormat(m_hCamera, emCameraMediaType.CAMERA_MEDIA_TYPE_BGR8);
            // 初始化顯示系統
            // 傳入待顯示相機之資訊，以及要顯示於哪個視窗中(此處為顯示在this.pictureBox.Handle)。
            // 並將剛剛初始化的狀態記錄在 status 之中
            status = CKAPI.CameraDisplayInit(m_hCamera, this.pictureBox.Handle);
            
            // 若初始化的狀態為異常，則關閉相機、並釋放電腦中儲存相機相關資訊所花費的空間、讓空間可以重新利用。
            // 且不執行後續指令
            if(status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                CKAPI.CameraUnInit(m_hCamera);
                m_hCamera = IntPtr.Zero;
                return false;
            }
            // 設定傳入相機顯示的模式，此處傳入剛剛初始化過後的 m_hCamera
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
            // 再關閉相機、並釋放電腦中儲存相機相關資訊所花費的空間、讓空間可以重新利用。
            // 再將注視著的相機不注視
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
