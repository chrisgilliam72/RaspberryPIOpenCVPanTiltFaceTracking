public class GStreamerVideoCaptureFactory : IVideoCaptureFactory
{
    public IVideoCapture CreateVideoCapture( int frameWidth, int frameHeight)
    {
        return new GStreamerVideoCapture(frameWidth, frameHeight);
    }
}