namespace ImageCvt
{
    public enum FileFmt
    {
        JPG,
        PNG,
        WEBP,
        GIF,
    }

    public enum CompMode
    {
        Sized = -2,
        Quality = -1,
        LosslessFast = 0,
        LosslessTypical = 1,
        LosslessSlow = 2,
    }
}
