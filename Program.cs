using System;
using System.Diagnostics;
using System.IO;
using OpenCvSharp;
using PanTiltHatLib;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        services.AddLogging(configure => configure.AddConsole()); // Add console logging
    }).Build();


//test commit

double hWidth=640;
double vHeight=480;
int initX=130;
int initY=65;
var panTiltService= host.Services.GetService<IPanTiltService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>(); // Get logger instance
logger.LogInformation("Initialising...");
if (panTiltService!=null)
{
    panTiltService.Init(0x40,60);
    panTiltService.MoveTo(initX,initY);
    double panPos=panTiltService.CurrentHPosition();
    double tiltPos=panTiltService.CurrentVPosition();
    var cascade = new CascadeClassifier(@"./Data/haarcascade_frontalface_alt.xml");

    var color = Scalar.FromRgb(0, 255, 0);
    // To use libcamera install gstreamer and use this.
        using var capture = new VideoCapture(
        "libcamerasrc ! video/x-raw, width=(int)640,height=(int)480, ! videoconvert ! appsink", 
        VideoCaptureAPIs.GSTREAMER
    );
    // using(VideoCapture capture = new VideoCapture(0))
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
        int framesSinceLastDetection=0;
        capture.Set(VideoCaptureProperties.FrameWidth, hWidth);
        capture.Set(VideoCaptureProperties.FrameHeight, vHeight);
        Thread.Sleep(1000);
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
                    scaleFactor: 1.1,   // Slightly reduce image size each pass
                    minNeighbors: 5,    // Filter out false positives
                    minSize: new Size(30, 30)  // Ignore very small faces
                );
                
                if (faces.Count()>0)
                {
                    framesSinceLastDetection=0;
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
                else
                {
                    if (framesSinceLastDetection>50)
                    {
                        panTiltService.MoveTo(initX,initY);
                        panPos=panTiltService.CurrentHPosition();
                        tiltPos=panTiltService.CurrentVPosition();
                        lastX=0;
                        lastY=0;
                        lastRelXPos=0;
                        lastRelYPos=0;
                        framesSinceLastDetection=0;
                    }
                    else
                        framesSinceLastDetection++;
                }
                // Cv2.Resize(srcImage,srcImage, new Size(640,480));
                window.ShowImage(srcImage);
                int key = Cv2.WaitKey(1);
                if (key == 27)
                {
                    break;
                }                                                                       
            }
            else
                logger.LogError("Error reading frame");
        }
    }
}



