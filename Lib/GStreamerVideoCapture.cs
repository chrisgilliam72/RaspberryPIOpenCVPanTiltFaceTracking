using OpenCvSharp;
public class GStreamerVideoCapture : IVideoCapture
{
    private readonly VideoCapture capture;
  
    public bool IsOpened
    {
        get { return capture.IsOpened(); }
    }

    public GStreamerVideoCapture(int frameWidth , int frameHeight )
    {
        var pipelineString = $"libcamerasrc ! video/x-raw,format=RGB,width={frameWidth},height={frameHeight} ! videoconvert ! appsink";
     
        capture = new VideoCapture(pipelineString, VideoCaptureAPIs.GSTREAMER);
        capture.Set(VideoCaptureProperties.FrameWidth, frameWidth);
        capture.Set(VideoCaptureProperties.FrameHeight, frameHeight);
    }

    public bool Read(Mat frame)
    {
        return capture.Read(frame);
    }

}
