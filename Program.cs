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

const int FRAME_W = 320;
const int FRAME_H = 200;
int cam_pan = 130;
int cam_tilt= 65;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostcontext, services) =>
    {
        services.AddSingleton<IPanTiltService,PanTiltService>();   
        services.AddLogging(configure => configure.AddConsole()); // Add console logging
    }).Build();


//test commit

var panTiltService= host.Services.GetService<IPanTiltService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>(); // Get logger instance
logger.LogInformation("Initialising...");
if (panTiltService!=null)
{
    panTiltService.Init(0x40,60);
    panTiltService.MoveTo(cam_pan,cam_tilt);

    var cascade = new CascadeClassifier(@"./Data/lbpcascade_frontalface.xml");


    // To use libcamera install gstreamer and use this.
    using var capture = new VideoCapture(
        "libcamerasrc ! video/x-raw,format=RGB,width=320,height=200 ! videoconvert ! appsink", 
        VideoCaptureAPIs.GSTREAMER
    );
    // using(VideoCapture capture = new VideoCapture(0))
   
    using(var frame = new Mat())
    using(var gray = new Mat())
    {

        capture.Set(VideoCaptureProperties.FrameWidth, 320);
        capture.Set(VideoCaptureProperties.FrameHeight, 200);
        Thread.Sleep(2000);
        while (capture.IsOpened())

        {

            if (capture.Read(frame))
            {
                
                Cv2.Flip(frame, frame, FlipMode.XY);
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGRA2GRAY);
                Cv2.EqualizeHist(gray, gray);

                var faces = cascade.DetectMultiScale(
                    image: frame,
                    scaleFactor: 1.1,   // Slightly reduce image size each pass
                    minNeighbors: 3,    // Filter out false positives
                    minSize: new Size(10, 10)  // Ignore very small faces
                );
                
                if (faces.Count()>0)
                {
 
                    int x=faces[0].X;
                    int y=faces[0].Y;
                    int w=faces[0].Width;
                    int h=faces[0].Height;
                    logger.LogInformation($"Face found: x:{x} y:{y} w:{w} h:{h}");
                    Cv2.Rectangle(frame, new Point(x, y), new Point(x+w, y+h),Scalar.FromRgb(0, 255, 0), 4);

                    //Get the centre of the face
                    x = x+(w/2);
                    y = y+(h/2);
                    logger.LogInformation($"Center Pos x:{x} y:{y}");
                    //Correct relative to centre of image
                    var turn_x  = (float)(x - (FRAME_W/2));
                    var turn_y  = (float)(y - (FRAME_H/2));
                    logger.LogInformation($"Turn  x:{turn_x} y:{turn_y}");

                    //Convert to percentage offset
                    turn_x  /= (float)(FRAME_W/2);
                    turn_y  /= (float)(FRAME_H/2);
                    turn_x   *= 4;
                    turn_y   *= 4 ;
                    logger.LogInformation($"Turn %  x:{turn_x} y:{turn_y}");
                    cam_pan= cam_pan+ Convert.ToInt32((turn_x)*-1);
                    cam_tilt=cam_tilt+Convert.ToInt32(turn_y);

                    logger.LogInformation($"Pan: {cam_pan} Tilt: {cam_tilt}");
                    panTiltService.MoveTo(cam_pan,cam_tilt);
  
  
                }

                Cv2.ImShow("Video",frame);
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

    Cv2.DestroyAllWindows();
}



