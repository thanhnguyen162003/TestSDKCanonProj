using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Presentation.Kernels;
using System.Drawing;
using System.Drawing.Imaging;

namespace Presentation.WebSockets
{
    public class CameraWebSocketHandler
    {
        private readonly SDKHandler _sdkHandler;
        private readonly List<WebSocket> _connectedClients = new();

        public CameraWebSocketHandler(SDKHandler sdkHandler)
        {
            _sdkHandler = sdkHandler;
        }

        public async Task HandleWebSocketAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                _connectedClients.Add(webSocket);

                try
                {
                    await HandleWebSocketConnection(webSocket);
                }
                finally
                {
                    _connectedClients.Remove(webSocket);
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var command = JsonSerializer.Deserialize<WebSocketCommand>(message);

                switch (command?.Action)
                {
                    case "start_liveview":
                        await StartLiveViewStream();
                        break;
                    case "stop_liveview":
                        await StopLiveViewStream();
                        break;
                    case "get_frame":
                        await SendSingleFrame(webSocket);
                        break;
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private async Task StartLiveViewStream()
        {
            if (!_sdkHandler.CameraSessionOpen)
            {
                await BroadcastToAllClients(new { type = "error", message = "No camera session is open" });
                return;
            }

            try
            {
                _sdkHandler.StartLiveView();
                
                _ = Task.Run(async () =>
                {
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
                                var base64Image = Convert.ToBase64String(imageBytes);

                                await BroadcastToAllClients(new { 
                                    type = "frame", 
                                    data = base64Image,
                                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                });
                            }
                            
                            await Task.Delay(100); // ~10 FPS
                        }
                        catch (Exception ex)
                        {
                            await BroadcastToAllClients(new { type = "error", message = ex.Message });
                            break;
                        }
                    }
                });

                await BroadcastToAllClients(new { type = "liveview_started" });
            }
            catch (Exception ex)
            {
                await BroadcastToAllClients(new { type = "error", message = ex.Message });
            }
        }

        private async Task StopLiveViewStream()
        {
            try
            {
                _sdkHandler.StopLiveView();
                await BroadcastToAllClients(new { type = "liveview_stopped" });
            }
            catch (Exception ex)
            {
                await BroadcastToAllClients(new { type = "error", message = ex.Message });
            }
        }

        private async Task SendSingleFrame(WebSocket webSocket)
        {
            try
            {
                var liveViewImage = _sdkHandler.GetLiveViewImage();
                if (liveViewImage != null)
                {
                    using var ms = new MemoryStream();
                    liveViewImage.Save(ms, ImageFormat.Jpeg);
                    var imageBytes = ms.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    var response = JsonSerializer.Serialize(new { 
                        type = "frame", 
                        data = base64Image,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });

                    var buffer = Encoding.UTF8.GetBytes(response);
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = JsonSerializer.Serialize(new { type = "error", message = ex.Message });
                var buffer = Encoding.UTF8.GetBytes(errorResponse);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task BroadcastToAllClients(object message)
        {
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);

            var tasks = _connectedClients.Where(client => client.State == WebSocketState.Open)
                .Select(client => client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None));

            await Task.WhenAll(tasks);
        }
    }

    public class WebSocketCommand
    {
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
