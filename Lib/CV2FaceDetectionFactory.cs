public class Cv2FaceDetectionFactory : IObjectDetectionFactory
{
    public IObjectDetection CreateObjectDetection()
    {
        return new CV2FaceDetection();
    }
}