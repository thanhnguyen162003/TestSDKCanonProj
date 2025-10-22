# Canon EDSDK API

This project provides a REST API for controlling Canon cameras using the Canon EDSDK (EOS Digital SDK). The API exposes camera functionality through HTTP endpoints, making it easy to integrate Canon camera control into web applications or other systems.

## Prerequisites

1. **Canon EDSDK**: You need to obtain the Canon EDSDK from Canon's developer program
2. **Canon Camera**: A compatible Canon EOS camera connected via USB
3. **.NET 8.0**: The project targets .NET 8.0

## Setup

1. Place the Canon EDSDK DLLs (`EDSDK.dll`, `EdsImage.dll`) in the project root directory
2. Ensure your camera is connected and in the correct mode (usually Manual or Aperture Priority)
3. Run the application: `dotnet run`

The API will be available at `https://localhost:7000` with Swagger documentation at `https://localhost:7000/swagger`

## API Endpoints

### Camera Management

#### GET `/api/cameras`
Get list of connected cameras.

**Response:**
```json
[
  {
    "productName": "Canon EOS 5D Mark III",
    "portName": "USB",
    "deviceSubType": 0,
    "ref": "123456789"
  }
]
```

#### POST `/api/cameras/{cameraRef}/session`
Open a session with a specific camera.

**Parameters:**
- `cameraRef`: Camera reference from the cameras list

#### DELETE `/api/cameras/session`
Close the current camera session.

#### GET `/api/cameras/status`
Get current camera status.

**Response:**
```json
{
  "sessionOpen": true,
  "liveViewOn": false,
  "isFilming": false,
  "mainCamera": "Canon EOS 5D Mark III"
}
```

### Camera Settings

#### GET `/api/cameras/settings/{propertyId}`
Get current value of a camera setting.

**Parameters:**
- `propertyId`: Property ID (hex value, e.g., `0x00000004` for ISO)

#### POST `/api/cameras/settings/{propertyId}`
Set a camera setting value.

**Parameters:**
- `propertyId`: Property ID
- `value`: New value to set

#### GET `/api/cameras/settings/{propertyId}/list`
Get list of available values for a setting.

### Photography

#### POST `/api/cameras/photo`
Take a photo with current settings.

#### POST `/api/cameras/photo/bulb`
Take a photo in bulb mode.

**Parameters:**
- `bulbTime`: Exposure time in milliseconds (minimum 1000ms)

### Live View

#### POST `/api/cameras/liveview/start`
Start live view.

#### POST `/api/cameras/liveview/stop`
Stop live view.

**Parameters:**
- `lvOff`: Whether to turn off live view completely (default: true)

### Video Recording

#### POST `/api/cameras/filming/start`
Start video recording.

#### POST `/api/cameras/filming/stop`
Stop video recording.

### Focus Control

#### POST `/api/cameras/focus`
Control focus (only works during live view).

**Parameters:**
- `speed`: Focus speed and direction

### Utility

#### POST `/api/cameras/ui/lock`
Lock or unlock camera UI.

**Parameters:**
- `lockState`: true to lock, false to unlock

#### POST `/api/cameras/capacity`
Set available disk space (required for some operations).

**Parameters:**
- `bytesPerSector`: Bytes per sector
- `numberOfFreeClusters`: Number of free clusters

#### GET `/api/cameras/entries`
Get camera file system entries (files and folders).

## Common Property IDs

| Property | ID (Hex) | Description |
|----------|----------|-------------|
| ISO Speed | 0x00000004 | ISO sensitivity |
| Aperture Value | 0x00000008 | Aperture setting |
| Shutter Speed | 0x00000009 | Shutter speed |
| AE Mode | 0x0000000A | Auto exposure mode |
| Metering Mode | 0x0000000B | Metering mode |
| White Balance | 0x0000000C | White balance setting |
| Drive Mode | 0x0000000D | Drive mode |
| Image Quality | 0x0000000E | Image quality setting |

## Usage Examples

### Using curl

```bash
# Get connected cameras
curl -X GET "https://localhost:7000/api/cameras"

# Open session (replace with actual camera ref)
curl -X POST "https://localhost:7000/api/cameras/123456789/session"

# Get ISO setting
curl -X GET "https://localhost:7000/api/cameras/settings/4"

# Set ISO to 400
curl -X POST "https://localhost:7000/api/cameras/settings/4?value=400"

# Take a photo
curl -X POST "https://localhost:7000/api/cameras/photo"

# Start live view
curl -X POST "https://localhost:7000/api/cameras/liveview/start"

# Close session
curl -X DELETE "https://localhost:7000/api/cameras/session"
```

### Using the Test Client

The project includes a `TestClient` class for programmatic access:

```csharp
using var client = new TestClient();

// Get cameras
var cameras = await client.GetCamerasAsync();

// Open session
await client.OpenSessionAsync(cameras[0].Ref);

// Take a photo
await client.TakePhotoAsync();

// Close session
await client.CloseSessionAsync();
```

## Error Handling

All endpoints return appropriate HTTP status codes:
- `200 OK`: Success
- `400 Bad Request`: Invalid request or no session open
- `404 Not Found`: Camera not found
- `500 Internal Server Error`: SDK or camera error

Error responses include a message describing the issue.

## Important Notes

1. **Session Management**: Always open a session before performing camera operations
2. **Threading**: The Canon SDK requires STA threading, which is handled internally
3. **Camera Mode**: Ensure your camera is in Manual or Aperture Priority mode for full control
4. **Capacity Setting**: Some operations require setting available disk space first
5. **Live View**: Focus control only works during live view
6. **Safety**: Photo and video operations are real - use with caution

## Troubleshooting

### Common Issues

1. **"No cameras found"**: Ensure camera is connected and powered on
2. **"SDK Error"**: Check camera mode and connection
3. **"Session not open"**: Call the open session endpoint first
4. **"Camera not ready"**: Ensure camera is in the correct mode

### Debug Mode

Enable detailed logging by setting the log level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## License

This project is for educational and development purposes. Canon EDSDK usage is subject to Canon's licensing terms.
