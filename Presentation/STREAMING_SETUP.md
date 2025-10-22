# 🎥 Canon Camera Live Streaming API Guide

This guide shows you how to use your .NET API to stream live video from your Canon camera to external applications (like React Native desktop apps).

## 🚀 **Quick Start**

### **Step 1: Build and Run Your .NET API**
1. **Build the project**:
   ```bash
   dotnet build Application.csproj
   ```

2. **Run the API**:
   ```bash
   dotnet run --project Application.csproj
   ```

3. **Verify API is running**:
   - Visit `https://localhost:7000/swagger`
   - You should see all available endpoints

### **Step 2: Connect Your Canon Camera**
1. **Connect camera via USB**
2. **Set camera to PTP mode**
3. **Power on the camera**
4. **Open session via API**:
   - Go to `https://localhost:7000/swagger`
   - GET `/api/cameras` to see connected cameras
   - POST `/api/cameras/{cameraRef}/session` to open session

### **Step 3: Test Streaming Endpoints**
1. **Test MJPEG stream**:
   - Open browser: `https://localhost:7000/api/stream/liveview`
   - You should see live video stream

2. **Test single frame**:
   - GET `https://localhost:7000/api/stream/liveview/single`
   - Returns current live view frame as JPEG

## 📡 **Streaming Options**

### **Option 1: SignalR (Recommended)**
- **Real-time bidirectional communication**
- **Automatic reconnection**
- **Best for mobile apps**

**Endpoints:**
- `wss://localhost:7000/cameraHub` - SignalR hub
- Methods: `StartLiveViewStream`, `StopLiveViewStream`
- Events: `LiveViewFrame`, `LiveViewStarted`, `LiveViewStopped`, `Error`

### **Option 2: MJPEG HTTP Stream**
- **Simple HTTP streaming**
- **Works in any web browser**
- **Good for web applications**

**Endpoints:**
- `GET /api/stream/liveview` - MJPEG stream
- `GET /api/stream/liveview/single` - Single frame

### **Option 3: WebSocket**
- **Low-level WebSocket communication**
- **Custom protocol**
- **Maximum control**

**Endpoints:**
- `ws://localhost:7000/ws` - WebSocket connection
- Commands: `start_liveview`, `stop_liveview`, `get_frame`

## 🔧 **Configuration**

### **Frame Rate Control**
In `CameraHub.cs`, adjust the delay:
```csharp
await Task.Delay(100); // 10 FPS
await Task.Delay(50);  // 20 FPS
await Task.Delay(200); // 5 FPS
```

### **Image Quality**
In `CameraHub.cs`, adjust JPEG quality:
```csharp
bitmap.Save(ms, ImageFormat.Jpeg, new EncoderParameters {
    Param = { new EncoderParameter(Encoder.Quality, 80L) }
});
```

### **Network Configuration**
1. **Find your computer's IP address**:
   ```bash
   ipconfig  # Windows
   ifconfig  # macOS/Linux
   ```

2. **Update React Native app** with your IP:
   ```javascript
   .withUrl('https://192.168.1.100:7000/cameraHub', {
   ```

3. **Configure firewall** to allow port 7000

## 📱 **External Application Integration**

### **For React Native Desktop Apps**
Create a separate React Native desktop project and connect to this API:

```javascript
import SignalR from '@microsoft/signalr';

const connection = new SignalR.HubConnectionBuilder()
  .withUrl('https://YOUR_IP:7000/cameraHub')
  .build();

// Start live view
await connection.invoke('StartLiveViewStream');

// Listen for frames
connection.on('LiveViewFrame', (base64Image) => {
  const imageUri = `data:image/jpeg;base64,${base64Image}`;
  // Display image in your component
});
```

### **For Web Applications**
```javascript
// MJPEG Stream
const img = document.createElement('img');
img.src = 'https://YOUR_IP:7000/api/stream/liveview';

// Single Frame
fetch('https://YOUR_IP:7000/api/stream/liveview/single')
  .then(response => response.blob())
  .then(blob => {
    const imageUrl = URL.createObjectURL(blob);
    // Display image
  });
```

### **For Desktop Applications (C#)**
```csharp
// SignalR Client
var connection = new HubConnectionBuilder()
    .WithUrl("https://YOUR_IP:7000/cameraHub")
    .Build();

connection.On<string>("LiveViewFrame", (base64Image) => {
    var imageBytes = Convert.FromBase64String(base64Image);
    // Display image in your desktop app
});

await connection.StartAsync();
await connection.InvokeAsync("StartLiveViewStream");
```

## 🛠 **Troubleshooting**

### **Connection Issues**
1. **Check API is running**: Visit `https://localhost:7000/swagger`
2. **Verify camera connection**: Test with `/api/cameras` endpoint
3. **Check network**: Ensure devices are on same network
4. **Firewall**: Allow port 7000 through firewall

### **Performance Issues**
1. **Reduce frame rate**: Increase delay in streaming loop
2. **Lower image quality**: Reduce JPEG quality
3. **Smaller images**: Resize images before sending
4. **Network optimization**: Use wired connection

### **Camera Issues**
1. **Camera not detected**: Check USB connection and drivers
2. **Session errors**: Ensure camera is in PTP mode
3. **Live view fails**: Check camera supports live view
4. **Permission errors**: Run API as administrator

## 📊 **Performance Tips**

### **Optimize for Mobile**
```javascript
// Reduce frame rate for mobile
await Task.Delay(200); // 5 FPS for mobile

// Compress images
var compressedImage = CompressImage(liveViewImage, 0.7f);
```

### **Memory Management**
```javascript
// Dispose images properly
using (var image = GetLiveViewImage())
{
    // Process image
} // Automatically disposed
```

### **Network Optimization**
- Use WiFi instead of cellular
- Reduce image resolution for live view
- Implement frame skipping for slow connections

## 🔐 **Security Considerations**

1. **HTTPS/WSS**: Use secure connections in production
2. **Authentication**: Add user authentication
3. **Rate limiting**: Prevent abuse
4. **Input validation**: Validate all inputs

## 📈 **Scaling**

For multiple cameras or high load:
1. **Load balancing**: Use multiple API instances
2. **Caching**: Cache camera settings
3. **Database**: Store camera configurations
4. **Monitoring**: Add logging and metrics

## 🎯 **Next Steps**

1. **Create separate React Native desktop project**:
   ```bash
   npx react-native init CanonCameraApp --template react-native-template-typescript
   cd CanonCameraApp
   npm install @microsoft/signalr
   ```

2. **Add camera controls**: Focus, zoom, settings
3. **Recording**: Save video streams
4. **Multiple cameras**: Support multiple simultaneous streams
5. **Cloud integration**: Stream to cloud services
6. **AI processing**: Add computer vision features

## 📋 **API Endpoints Summary**

### **Camera Management**
- `GET /api/cameras` - List connected cameras
- `POST /api/cameras/{cameraRef}/session` - Open camera session
- `DELETE /api/cameras/session` - Close camera session
- `GET /api/cameras/status` - Get camera status

### **Live Streaming**
- `GET /api/stream/liveview` - MJPEG live stream
- `GET /api/stream/liveview/single` - Single frame
- `wss://localhost:7000/cameraHub` - SignalR hub
- `ws://localhost:7000/ws` - WebSocket endpoint

### **Camera Controls**
- `POST /api/cameras/photo` - Take photo
- `POST /api/cameras/liveview/start` - Start live view
- `POST /api/cameras/liveview/stop` - Stop live view
- `GET /api/cameras/settings/{propertyId}` - Get camera setting
- `POST /api/cameras/settings/{propertyId}` - Set camera setting
