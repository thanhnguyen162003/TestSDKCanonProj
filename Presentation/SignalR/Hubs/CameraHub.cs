using Microsoft.AspNetCore.SignalR;
using Presentation.Kernels;
using System.Drawing;
using System.Drawing.Imaging;

namespace Presentation.SignalR.Hubs
{
    public class CameraHub : Hub
    {
        private readonly SDKHandler _sdkHandler;

        public CameraHub(SDKHandler sdkHandler)
        {
            _sdkHandler = sdkHandler;
        }

        public async Task StartLiveViewStream(int targetFps = 30, int quality = 80)
        {
            if (!_sdkHandler.CameraSessionOpen)
            {
                await Clients.Caller.SendAsync("Error", "No camera session is open");
                return;
            }

            try
            {
                _sdkHandler.StartLiveView();
                
                // Calculate delay for target FPS
                var frameDelay = 1000 / targetFps; // milliseconds per frame
                
                // Start high-performance streaming loop
                _ = Task.Run(async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var frameCount = 0;
                    
                    while (_sdkHandler.IsLiveViewOn)
                    {
                        try
                        {
                            var frameStartTime = stopwatch.ElapsedMilliseconds;
                            
                            var liveViewImage = _sdkHandler.GetLiveViewImage();
                            if (liveViewImage != null)
                            {
                                // Optimized conversion with quality control
                                var base64Image = ConvertBitmapToBase64Optimized(liveViewImage, quality);
                                
                                // Send to all connected clients
                                await Clients.All.SendAsync("LiveViewFrame", base64Image);
                                
                                frameCount++;
                                
                                // Calculate processing time and adjust delay
                                var processingTime = stopwatch.ElapsedMilliseconds - frameStartTime;
                                var adjustedDelay = Math.Max(1, frameDelay - (int)processingTime);
                                
                                await Task.Delay(adjustedDelay);
                            }
                            else
                            {
                                await Task.Delay(frameDelay);
                            }
                        }
                        catch (Exception ex)
                        {
                            await Clients.All.SendAsync("Error", ex.Message);
                            break;
                        }
                    }
                    
                    stopwatch.Stop();
                });
                
                await Clients.Caller.SendAsync("LiveViewStarted", new { targetFps, quality });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task StopLiveViewStream()
        {
            try
            {
                _sdkHandler.StopLiveView();
                await Clients.All.SendAsync("LiveViewStopped");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        private string ConvertBitmapToBase64(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Jpeg);
            var imageBytes = ms.ToArray();
            return Convert.ToBase64String(imageBytes);
        }

        private string ConvertBitmapToBase64Optimized(Bitmap bitmap, int quality)
        {
            using var ms = new MemoryStream();
            
            var jpegCodec = ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            
            if (jpegCodec != null)
            {
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                bitmap.Save(ms, jpegCodec, encoderParams);
            }
            else
            {
                bitmap.Save(ms, ImageFormat.Jpeg);
            }
            
            var imageBytes = ms.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }
}
