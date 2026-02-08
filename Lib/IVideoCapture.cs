using OpenCvSharp;

public interface  IVideoCapture
{
    public bool IsOpened { get; }
    public bool Read(Mat frame);
}