
using System;
using HalconDotNet;

public partial class HDevelopExport
{
    public HTuple hv_ExpDefaultWinHandle;

    // Main procedure
    public void display(IntPtr pRgbData, int width, int height, int outWidth, int outHeight)
    {
        HObject cameraImage;
        HOperatorSet.GenEmptyObj(out cameraImage);
        cameraImage.Dispose();
        HOperatorSet.GenImageInterleaved(out cameraImage, pRgbData, "rgb", width, height, -1, "byte", outWidth, outHeight, 0, 0, -1, 0);       
        HOperatorSet.DispObj(cameraImage, hv_ExpDefaultWinHandle);
        cameraImage.Dispose();
    }

    public void InitHalcon()
    {
        // Default settings used in HDevelop 
        HOperatorSet.SetSystem("width", 512);
        HOperatorSet.SetSystem("height", 512);
    }

    public void SetWindow(HTuple Window)
    {
        hv_ExpDefaultWinHandle = Window;
    }

}

