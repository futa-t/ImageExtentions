using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImageExtentions;

[SupportedOSPlatform("windows")]
public class SuperSampling: IDisposable
{
    public Mat Mat { get; } = new();
    public int Width { get; private set; }
    public int Height { get; private set; }
    public double AspectRatio => (double)this.Width / (double)this.Height;
    private OpenCvSharp.Size orgSize;

    private void Init(Mat srcMat, double scaleFactor)
    {
        this.orgSize = new(srcMat.Width, srcMat.Height);
        this.Width = (int)(srcMat.Width * scaleFactor);
        this.Height = (int)(srcMat.Height * scaleFactor);
        var samplingSize = new OpenCvSharp.Size(this.Width, this.Height);
        Cv2.Resize(srcMat, this.Mat, samplingSize, 0, 0, InterpolationFlags.Area);
    }

    public SuperSampling(byte[] data, ImreadModes mode, double scaleFactor = 2)
    {
        using var srcMat = Cv2.ImDecode(data, mode);
        this.Init(srcMat, scaleFactor);
    }

    public SuperSampling(Mat srcMat, double scaleFactor)
        => this.Init(srcMat, scaleFactor);

    public OpenCvSharp.Size Size()
        => this.Mat.Size();

    public void Resize(int width, int height)
        => Cv2.Resize(this.Mat, this.Mat, new OpenCvSharp.Size(width, height), 0, 0, InterpolationFlags.Area);

    public void Resize(OpenCvSharp.Size size)
        => Cv2.Resize(this.Mat, this.Mat, size, 0, 0, InterpolationFlags.Area);

    public void Resize(int size)
    {
        int newWidth, newHeight;

        if (this.Width > this.Height)
        {
            newWidth = size;
            newHeight = (int)(size / this.AspectRatio);
        }
        else
        {
            newHeight = size;
            newWidth = (int)(size * this.AspectRatio);
        }

        this.Resize(newWidth, newHeight);
    }

    public void Dispose()
    {
        this.Mat?.Dispose();
        GC.SuppressFinalize(this);
    }

    public Bitmap ToBitmap() => this.Mat.ToBitmap();
}

[SupportedOSPlatform("windows")]
public static class ByteExtentions
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
        superSampling.Resize(size);
        return superSampling.ToBitmap();
    }

    public static Image? ToImage(this byte[] data)
    {
        using var srcMat = Cv2.ImDecode(data, ImreadModes.Color);
        if (srcMat == null || srcMat.Empty()) return null;
        return srcMat.ToBitmap();
    }

    public static Image ToImage(this byte[] data, int width, int height) => ImageFromByte(data, width, height);
    public static Image ToImage(this byte[] data, int size) => ImageFromByte(data, size);

}

[SupportedOSPlatform("windows")]
public static class ImageExtentions
{
    public static Mat ToMat(this Image image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Png);
        byte[] bytes = ms.ToArray();
        return Cv2.ImDecode(bytes, ImreadModes.Unchanged);
    }

    public static Image Resize(this Image image, int size)
    {
        using var superSampling = new SuperSampling(image.ToMat(), 2.0);
        superSampling.Resize(size);
        return superSampling.ToBitmap();
    }
    public static Image Resize(this Image image, int width, int height)
    {
        using var superSampling = new SuperSampling(image.ToMat(), 2.0);
        superSampling.Resize(width, height);
        return superSampling.ToBitmap();
    }

    public static Image? ToRoundedImage(this byte[] data, int size)
        => data.ToImage()?.Rounded(size);

    public static Image Rounded(this Image image, int size)
    {
        using var superSampling = new SuperSampling(image.ToMat(), 2.0);
        using var mask = CreateCircleMask(superSampling.Mat.Size());
        superSampling.Mat.SetAlphaFromMask(mask);

        double aspectRatio = superSampling.AspectRatio;
        int rw, rh;

        if (aspectRatio >= 1.0)
        {
            rh = size;
            rw = (int)Math.Round(size * aspectRatio);
        }
        else
        {
            rw = size;
            rh = (int)Math.Round(size / aspectRatio);
        }
        superSampling.Resize(rw, rh);

        return superSampling.ToBitmap();
    }

    private static Mat CreateCircleMask(OpenCvSharp.Size size)
    {
        Mat mask = new(size, MatType.CV_8UC1, Scalar.Black);
        int centerX = size.Width / 2;
        int centerY = size.Height / 2;
        int radius = Math.Min(centerX, centerY);
        Cv2.Circle(mask, centerX, centerY, radius, Scalar.White, -1, LineTypes.AntiAlias);
        return mask;
    }

    private static void SetAlphaFromMask(this Mat src, Mat mask)
    {
        Cv2.CvtColor(src, src, ColorConversionCodes.BGR2BGRA);
        var channel = src.Split();
        channel[3] = mask;
        Cv2.Merge(channel, src);
    }

}

