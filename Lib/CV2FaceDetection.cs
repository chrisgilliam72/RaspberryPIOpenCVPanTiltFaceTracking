using OpenCvSharp;
public class CV2FaceDetection : IObjectDetection
{
     private  Mat frame = new Mat();
     private  Mat gray = new Mat();
    private CascadeClassifier?  cascade;
    public Mat CreateDetector()
    {
        cascade = new CascadeClassifier(@"./Data/lbpcascade_frontalface.xml");
        return frame;
    }
    public DetectionRectangle[] DetectObjects()
    {
        var rects = new List<DetectionRectangle>();
        if (frame is not null)
        {                 
            Cv2.Flip(frame, frame, FlipMode.XY);
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGRA2GRAY);
            Cv2.EqualizeHist(gray, gray);

            var objects= cascade.DetectMultiScale(
                image: frame,
                scaleFactor: 1.1,   // Slightly reduce image size each pass
                minNeighbors: 3,    // Filter out false positives
                minSize: new Size(10, 10)  // Ignore very small objects
            );  

            foreach (var obj in objects)
            {
                rects.Add(new DetectionRectangle(obj.X, obj.Y, obj.Width, obj.Height));
            }
        }

        return rects.ToArray();
    }
}