using Microsoft.AspNetCore.Mvc;
using Presentation.Kernels;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoBoothController : ControllerBase
    {
        private readonly SDKHandler _sdkHandler;
        private static readonly object _streamLock = new object();
        private static bool _isStreaming = false;
        private static CancellationTokenSource _streamingCancellation = new();

        public PhotoBoothController(SDKHandler sdkHandler)
        {
            _sdkHandler = sdkHandler;
        }

        [HttpGet("liveview/highfps")]
        public async Task<IActionResult> GetHighFpsLiveViewStream(
            [FromQuery] int fps = 30, 
            [FromQuery] int quality = 70,
            [FromQuery] int width = 640,
            [FromQuery] int height = 480)
        {
            if (!_sdkHandler.CameraSessionOpen)
            {
                return BadRequest("No camera session is open");
            }

            if (_isStreaming)
            {
                return BadRequest("Stream is already active");
            }

            try
            {
                _sdkHandler.StartLiveView();
                _isStreaming = true;
                _streamingCancellation = new CancellationTokenSource();

                Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                Response.Headers.Add("X-FPS", fps.ToString());
                Response.Headers.Add("X-Quality", quality.ToString());

                var frameDelay = 1000 / fps; // milliseconds per frame
                var stopwatch = Stopwatch.StartNew();
                var frameCount = 0;

                while (!_streamingCancellation.Token.IsCancellationRequested && _sdkHandler.IsLiveViewOn)
                {
                    try
                    {
                        var frameStartTime = stopwatch.ElapsedMilliseconds;
                        
                        var liveViewImage = _sdkHandler.GetLiveViewImage();
                        if (liveViewImage != null)
                        {
                            // Resize and optimize image for performance
                            var optimizedImage = OptimizeImageForStreaming(liveViewImage, width, height, quality);
                            
                            using var ms = new MemoryStream();
                            optimizedImage.Save(ms, ImageFormat.Jpeg);
                            var imageBytes = ms.ToArray();

                            await Response.WriteAsync("--frame\r\n");
                            await Response.WriteAsync("Content-Type: image/jpeg\r\n");
                            await Response.WriteAsync($"Content-Length: {imageBytes.Length}\r\n\r\n");
                            await Response.Body.WriteAsync(imageBytes, 0, imageBytes.Length);
                            await Response.WriteAsync("\r\n");
                            await Response.Body.FlushAsync();

                            frameCount++;
                            
                            // Calculate processing time and adjust delay for consistent FPS
                            var processingTime = stopwatch.ElapsedMilliseconds - frameStartTime;
                            var adjustedDelay = Math.Max(1, frameDelay - (int)processingTime);
                            
                            await Task.Delay(adjustedDelay, _streamingCancellation.Token);
                        }
                        else
                        {
                            await Task.Delay(frameDelay, _streamingCancellation.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue streaming
                        Console.WriteLine($"Streaming error: {ex.Message}");
                        await Task.Delay(frameDelay, _streamingCancellation.Token);
                    }
                }

                stopwatch.Stop();
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _isStreaming = false;
                return BadRequest($"Failed to start high FPS stream: {ex.Message}");
            }
        }

        [HttpPost("liveview/stop")]
        public IActionResult StopLiveViewStream()
        {
            try
            {
                _streamingCancellation.Cancel();
                _isStreaming = false;
                _sdkHandler.StopLiveView();
                return Ok(new { message = "Live view stream stopped" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to stop stream: {ex.Message}");
            }
        }

        [HttpGet("liveview/frame")]
        public IActionResult GetOptimizedFrame(
            [FromQuery] int quality = 80,
            [FromQuery] int width = 800,
            [FromQuery] int height = 600)
        {
            if (!_sdkHandler.CameraSessionOpen)
            {
                return BadRequest("No camera session is open");
            }

            try
            {
                var liveViewImage = _sdkHandler.GetLiveViewImage();
                if (liveViewImage == null)
                {
                    return NotFound("No live view image available");
                }

                var optimizedImage = OptimizeImageForStreaming(liveViewImage, width, height, quality);
                
                using var ms = new MemoryStream();
                optimizedImage.Save(ms, ImageFormat.Jpeg);
                var imageBytes = ms.ToArray();

                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get optimized frame: {ex.Message}");
            }
        }

        [HttpPost("capture")]
        public async Task<IActionResult> CapturePhoto(
            [FromQuery] bool highQuality = true,
            [FromQuery] string? filename = null)
        {
            if (!_sdkHandler.CameraSessionOpen)
            {
                return BadRequest("No camera session is open");
            }

            try
            {
                // Take photo
                _sdkHandler.TakePhoto();
                
                // Wait a moment for photo to be processed
                await Task.Delay(1000);
                
                // Get the latest photo from camera
                var photoData = await GetLatestPhotoFromCamera();
                
                if (photoData != null)
                {
                    var result = new
                    {
                        success = true,
                        message = "Photo captured successfully",
                        filename = filename ?? $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg",
                        size = photoData.Length,
                        timestamp = DateTime.UtcNow
                    };

                    return Ok(result);
                }
                else
                {
                    return BadRequest("Failed to retrieve captured photo");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to capture photo: {ex.Message}");
            }
        }

        [HttpGet("status")]
        public IActionResult GetStreamingStatus()
        {
            return Ok(new
            {
                isStreaming = _isStreaming,
                cameraSessionOpen = _sdkHandler.CameraSessionOpen,
                liveViewActive = _sdkHandler.IsLiveViewOn,
                timestamp = DateTime.UtcNow
            });
        }

        private Bitmap OptimizeImageForStreaming(Bitmap originalImage, int targetWidth, int targetHeight, int quality)
        {
            // Calculate aspect ratio preserving dimensions
            var aspectRatio = (double)originalImage.Width / originalImage.Height;
            int newWidth, newHeight;

            if (targetWidth / (double)targetHeight > aspectRatio)
            {
                newHeight = targetHeight;
                newWidth = (int)(targetHeight * aspectRatio);
            }
            else
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / aspectRatio);
            }

            // Create optimized bitmap
            var optimizedBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            
            using (var graphics = Graphics.FromImage(optimizedBitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                
                graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            return optimizedBitmap;
        }

        private async Task<byte[]?> GetLatestPhotoFromCamera()
        {
            try
            {
                // This would need to be implemented based on your camera's photo storage
                // For now, return null as placeholder
                await Task.Delay(100); // Simulate async operation
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
