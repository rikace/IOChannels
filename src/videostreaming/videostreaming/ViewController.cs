using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AppKit;
using AVFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using ImageIO;
using Microsoft.AspNetCore.SignalR.Client;
using MobileCoreServices;

namespace avcapturesessiontest
{
    public partial class ViewController : NSViewController, IAVCaptureVideoDataOutputSampleBufferDelegate, IAVCaptureAudioDataOutputSampleBufferDelegate
    {
        public AVCaptureSession captureSession;
        public AVCaptureDeviceInput captureDeviceInput;
        public AVCaptureOutput captureOutput;

        public bool VideoInputPermissionGranted { get; set; }

        private HubConnection connection;

        public ViewController(IntPtr handle) : base(handle)
        {
            VideoInputPermissionGranted = false;

            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/video-hub")
                .Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
        }


        public override async void ViewDidLoad()
        {
            try
            {   // Open SignalR connection to server
                await connection.StartAsync();
            }
            catch (Exception e)
            {
                var alert = new NSAlert
                {
                    MessageText = "Error",
                    InformativeText = e.Message
                };
                alert.RunModal();
                throw;
            }

            await AuthorizeVideoInputUse();
            while (VideoInputPermissionGranted == false)
            {
                await Task.Delay(50);
            }

            base.ViewDidLoad();

            if (SetupCapture() == true)
            {
                if (captureSession.Running == false)
                    Console.WriteLine("AVCaptureSession failed to start running");
            }
        }


        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
            }
        }


        async Task AuthorizeVideoInputUse()
        {
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);

            if (authorizationStatus != AVAuthorizationStatus.Authorized)
            {
                await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
            VideoInputPermissionGranted = true;
        }


        public bool SetupCapture()
        {
            // configure the capture session for low resolution, change this if your code
            // can cope with more data or volume
            captureSession = new AVCaptureSession()
            {
                //SessionPreset = AVCaptureSession.PresetPhoto  
                SessionPreset = AVCaptureSession.Preset1280x720  
            };

            // create a device input and attach it to the session
            var captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);
            if (captureDeviceInput == null)
            {
                Console.WriteLine("No video input device");
                return false;
            }

            if (captureSession.CanAddInput(captureDeviceInput))
                captureSession.AddInput(captureDeviceInput);
            else
            {
                Console.WriteLine("Could not add input capture device to AVACaptureSession");
                return false;
            }


            // create a VideoDataOutput and add it to the sesion
            AVCaptureVideoDataOutput output = new AVCaptureVideoDataOutput
            {
                AlwaysDiscardsLateVideoFrames = false, // true,
                WeakVideoSettings = new CVPixelBufferAttributes()
                {
                    PixelFormatType = CVPixelFormatType.CV24RGB
                }.Dictionary //,

                // If you want to cap the frame rate at a given speed, in this sample: 30 frames per second
                //MinFrameDuration = new CMTime(1, 30)
            };


            CoreFoundation.DispatchQueue videoCaptureQueue = new CoreFoundation.DispatchQueue("Video Capture Queue");
            output.SetSampleBufferDelegateQueue(this, videoCaptureQueue);

            if (captureSession.CanAddOutput(output))
                captureSession.AddOutput(output);
            else
                return false;

            // add preview layer to this view controller's NSView
            AVCaptureVideoPreviewLayer previewLayer = new AVCaptureVideoPreviewLayer(captureSession);

            previewLayer.Frame = this.View.Bounds;
            previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;

            if (this.View.Layer == null)
            {
                this.View.WantsLayer = true;
                this.View.Layer = previewLayer;
            }
            else
            {
                this.View.WantsLayer = true;
                this.View.Layer.AddSublayer(previewLayer);
            }

            captureSession.StartRunning();

            return true;
        }

        private byte[] bytes = new byte[0];

        [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
        public virtual void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection avConnection)
        {
            CVImageBuffer imageBuffer = sampleBuffer.GetImageBuffer();
            var pixelBuffer = imageBuffer as CVPixelBuffer;

            var bufferSize = pixelBuffer.Height * pixelBuffer.BytesPerRow;
            if (bytes.Length != bufferSize)
                bytes = new byte[bufferSize];

            pixelBuffer.Lock(CVPixelBufferLock.None);
            Marshal.Copy(pixelBuffer.BaseAddress, bytes, 0, bytes.Length);
            pixelBuffer.Unlock(CVPixelBufferLock.None);

            var image = SixLabors.ImageSharp.Image
                .LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgb24>(
                    SixLabors.ImageSharp.Configuration.Default,
                    bytes, (int)pixelBuffer.Width, (int)pixelBuffer.Height);

            string asciiImage = ImageConverter.ImageToAsciiArt(image);

            connection.InvokeAsync("SendFrame", asciiImage);
        }
    }
}