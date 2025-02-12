using System;
using System.Diagnostics;
using System.IO;
using OpenCvSharp;
using PanTiltHatLib;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
// For legacy stack do this .
// edit /boot/firmware/config.txt
// comment out camera_auto_detect=1
//At the end in the section under all add the following
//gpu_mem=128
//start_x=1
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostcontext, services) =>
    {
        services.AddSingleton<IPanTiltService,PanTiltService>();   
    }).Build();


//test commit
Console.WriteLine("Initialising...");
double hWidth=320;
double vHeight=320;

var panTiltService= host.Services.GetService<IPanTiltService>();
if (panTiltService!=null)
{
    panTiltService.Init(0x40,60);
    panTiltService.Reset();
    double panPos=panTiltService.CurrentHPosition();
    double tiltPos=panTiltService.CurrentVPosition();
    var cascade = new CascadeClassifier(@"./Data/haarcascade_frontalface_alt.xml");

    var color = Scalar.FromRgb(0, 255, 0);
    // To use libcamera install gstreamer and use this.
    /*   using var capture = new VideoCapture(
        "libcamerasrc ! video/x-raw, width=(int)1080,height=(int)720, framerate=(fraction10/1) ! videoconvert ! appsink", 
        VideoCaptureAPIs.GSTREAMER
    );*/
    using(VideoCapture capture = new VideoCapture(0))
    using(Window window = new Window("Webcam"))
    using(Mat srcImage = new Mat())
    using(var grayImage = new Mat())
    using(var detectedFaceGrayImage = new Mat())
    {
        int count = 0;
        double lastX=0;
        double lastY=0;
        double lastRelXPos=0;
        double lastRelYPos=0;
        capture.Set(VideoCaptureProperties.FrameWidth, hWidth);
        capture.Set(VideoCaptureProperties.FrameHeight, vHeight);
        Thread.Sleep(2000);
        while (capture.IsOpened())
        {
            int faceMidPointX=0;
            int faceMidPointY=0;
            

            
            count++;

            if (capture.Read(srcImage))
            {
                
                Cv2.Flip(srcImage, srcImage, FlipMode.XY);
                Cv2.CvtColor(srcImage, grayImage, ColorConversionCodes.BGRA2GRAY);
                Cv2.EqualizeHist(grayImage, grayImage);

                var faces = cascade.DetectMultiScale(
                    image: grayImage,
                    minSize: new Size(10, 10)
                    );
                
                if (faces.Count()>0)
                {
                    var faceRect =faces.FirstOrDefault();
                    faceMidPointX=faceRect.X+(faceRect.Width/2);
                    faceMidPointY=faceRect.Y+(faceRect.Height/2);

                    double xRelPos=((double)faceMidPointX-(hWidth/2));
                    double yRelPos=((double)faceMidPointY-(vHeight/2));
                    xRelPos /=(double)hWidth/2.0;
                    yRelPos /=(double)vHeight/2.0;
                    // Console.WriteLine($"Rel X: {xRelPos} Rel Y: {yRelPos}");
                    if (Math.Abs(xRelPos-lastRelXPos)>0.05)
                    {
                        xRelPos*=5;
                        panPos-=xRelPos;                                      
                        panTiltService.HPos(panPos);
                    }  

                    if (Math.Abs(yRelPos-lastRelYPos)>0.05)
                    {
                        yRelPos*=5;
                        tiltPos+=yRelPos;
                        panTiltService.VPos(tiltPos);
                        Console.WriteLine($"Tilt change: {tiltPos}");
                    }     

                    // Console.WriteLine($"Pan Pos: {panPos} Tilt Pos {120+tiltPos}");
                    using(var detectedFaceImage = new Mat(srcImage, faceRect))
                    {
                        Cv2.Rectangle(srcImage, faceRect, color, 3);

                    }
                    
                    lastX=panPos;
                    lastY=tiltPos;
                    lastRelXPos=xRelPos;
                    lastRelYPos=yRelPos;
                    // Console.WriteLine($"Pan: {camPan} Tilt: {camTilt}");
                }

                Cv2.Resize(srcImage,srcImage, new Size(540,300));
                window.ShowImage(srcImage);
                int key = Cv2.WaitKey(1);
                if (key == 27)
                {
                    break;
                }                                                                       
            }
            else
            Console.WriteLine("Error reading frame");
        }
    }
}



