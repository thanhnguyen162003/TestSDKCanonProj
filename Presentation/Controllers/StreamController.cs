using Microsoft.AspNetCore.Mvc;
using Presentation.Kernels;
using System.Drawing;
using System.Drawing.Imaging;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly SDKHandler _sdkHandler;

        public StreamController(SDKHandler sdkHandler)
        {
            _sdkHandler = sdkHandler;
        }

        [HttpGet("liveview")]
        public async Task<IActionResult> GetLiveViewStream()
        {
            if (!_sdkHandler.CameraSessionOpen)
            {
                return BadRequest("No camera session is open");
            }

            try
            {
                _sdkHandler.StartLiveView();
                
                Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                while (_sdkHandler.IsLiveViewOn)
                {
                    try
                    {
                        var liveViewImage = _sdkHandler.GetLiveViewImage();
                        if (liveViewImage != null)
                        {
                            using var ms = new MemoryStream();
                            liveViewImage.Save(ms, ImageFormat.Jpeg);
                            var imageBytes = ms.ToArray();

                            await Response.WriteAsync("--frame\r\n");
                            await Response.WriteAsync("Content-Type: image/jpeg\r\n");
                            await Response.WriteAsync($"Content-Length: {imageBytes.Length}\r\n\r\n");
                            await Response.Body.WriteAsync(imageBytes, 0, imageBytes.Length);
                            await Response.WriteAsync("\r\n");
                            await Response.Body.FlushAsync();
                        }
                        
                        await Task.Delay(100); // ~10 FPS
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to start live view stream: {ex.Message}");
            }
        }

        [HttpGet("liveview/single")]
        public IActionResult GetSingleFrame()
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

                using var ms = new MemoryStream();
                liveViewImage.Save(ms, ImageFormat.Jpeg);
                var imageBytes = ms.ToArray();

                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get live view frame: {ex.Message}");
            }
        }
    }
}
