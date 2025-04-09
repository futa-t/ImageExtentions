using System.Drawing;
using System.Runtime.Versioning;

using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImageExtentions;

[SupportedOSPlatform("windows")]
public class SuperSampling : IDisposable
{
    public Mat Mat { get; } = new();
    public int Width { get; private set; }
    public int Height { get; private set; }
    public double AspectRatio => (double)Width / (double)Height;

    private void Init(Mat srcMat, double scaleFactor)
    {
        Width= (int)(srcMat.Width * scaleFactor);
        Height = (int)(srcMat.Height * scaleFactor);
        var samplingSize = new OpenCvSharp.Size(Width, Height);
        Cv2.Resize(srcMat, Mat, samplingSize, 0, 0, InterpolationFlags.Area);
    }

    public SuperSampling(byte[] data, ImreadModes mode, double scaleFactor = 2)
    {
        using var srcMat = Cv2.ImDecode(data, mode);
        Init(srcMat, scaleFactor);
    }

    public SuperSampling(Mat srcMat, double scaleFactor)
        => Init(srcMat, scaleFactor);

    public OpenCvSharp.Size Size()
        => Mat.Size();

    public void Resize(int width, int height)
        => Cv2.Resize(Mat, Mat, new OpenCvSharp.Size(width, height),0,0, InterpolationFlags.Area);

    public void Resize(OpenCvSharp.Size size)
        => Cv2.Resize(Mat, Mat, size, 0, 0, InterpolationFlags.Area);

    public void Dispose()
    {
        Mat?.Dispose();
        GC.SuppressFinalize(this);
    }

    public Bitmap ToBitmap()
        => Mat.ToBitmap();
}

[SupportedOSPlatform("windows")]
public static class ImageExtentions
{

    public static Image ImageFromByte(byte[] data, int width, int height)
    {
        using var superSampling = new SuperSampling(data, ImreadModes.Color, 2.0);
        superSampling.Resize(width, height);

        return superSampling.ToBitmap();
    }

    public static Image ImageFromByte(byte[] data, int size)
    {
        using var superSampling = new SuperSampling(data, ImreadModes.Color, 2.0);
        int newWidth, newHeight;

        if (superSampling.Width > superSampling.Height)
        {
            newWidth = size;
            newHeight = (int)(size / superSampling.AspectRatio);
        }
        else
        {
            newHeight = size;
            newWidth = (int)(size * superSampling.AspectRatio);
        }

        superSampling.Resize(newWidth, newHeight);

        return superSampling.ToBitmap();
    }

    public static Image? ToImage(this byte[] data)
    {
        using var srcMat = Cv2.ImDecode(data, ImreadModes.Color);
        if (srcMat == null || srcMat.Empty()) return null;
        return srcMat.ToBitmap();
    }
}

