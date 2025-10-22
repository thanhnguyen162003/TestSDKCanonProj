using Presentation;
using Presentation.Constants;

namespace Presentation
{
    public class TestConsole
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine(LogConstants.TEST_CONSOLE_TITLE);
            Console.WriteLine(LogConstants.TEST_CONSOLE_SEPARATOR);
            Console.WriteLine();

            using var client = new TestClient();

            try
            {
                // Test 1: Get connected cameras
                Console.WriteLine(LogConstants.GETTING_CONNECTED_CAMERAS);
                var cameras = await client.GetCamerasAsync();
                
                if (cameras.Count == 0)
                {
                    Console.WriteLine(LogConstants.NO_CAMERAS_FOUND);
                    return;
                }

                Console.WriteLine(string.Format(LogConstants.FOUND_CAMERAS_COUNT, cameras.Count));
                foreach (var camera in cameras)
                {
                    Console.WriteLine(string.Format(LogConstants.CAMERA_INFO_FORMAT, camera.ProductName, camera.PortName, camera.Ref));
                }
                Console.WriteLine();

                // Test 2: Get camera status
                Console.WriteLine(LogConstants.GETTING_CAMERA_STATUS);
                var status = await client.GetStatusAsync();
                Console.WriteLine(string.Format(LogConstants.SESSION_OPEN_STATUS, status.SessionOpen));
                Console.WriteLine(string.Format(LogConstants.LIVE_VIEW_STATUS, status.LiveViewOn));
                Console.WriteLine(string.Format(LogConstants.IS_FILMING_STATUS, status.IsFilming));
                Console.WriteLine(string.Format(LogConstants.MAIN_CAMERA_STATUS, status.MainCamera));
                Console.WriteLine();

                // Test 3: Open session with first camera
                if (cameras.Count > 0)
                {
                    Console.WriteLine("3. Opening session with first camera...");
                    var success = await client.OpenSessionAsync(cameras[0].Ref);
                    if (success)
                    {
                        Console.WriteLine("   Session opened successfully!");
                        
                        // Test 4: Get updated status
                        Console.WriteLine("4. Getting updated camera status...");
                        status = await client.GetStatusAsync();
                        Console.WriteLine($"   Session Open: {status.SessionOpen}");
                        Console.WriteLine($"   Main Camera: {status.MainCamera}");
                        Console.WriteLine();

                        // Test 5: Get some camera settings (using common property IDs)
                        Console.WriteLine("5. Getting camera settings...");
                        try
                        {
                            // ISO Speed (Property ID: 0x00000004)
                            var iso = await client.GetSettingAsync(0x00000004);
                            Console.WriteLine($"   ISO Speed: {iso}");
                            
                            // Aperture Value (Property ID: 0x00000008)
                            var aperture = await client.GetSettingAsync(0x00000008);
                            Console.WriteLine($"   Aperture Value: {aperture}");
                            
                            // Shutter Speed (Property ID: 0x00000009)
                            var shutter = await client.GetSettingAsync(0x00000009);
                            Console.WriteLine($"   Shutter Speed: {shutter}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   Error getting settings: {ex.Message}");
                        }
                        Console.WriteLine();

                        // Test 6: Get available ISO values
                        Console.WriteLine("6. Getting available ISO values...");
                        try
                        {
                            var isoValues = await client.GetSettingsListAsync(0x00000004);
                            Console.WriteLine($"   Available ISO values: {string.Join(", ", isoValues)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   Error getting ISO values: {ex.Message}");
                        }
                        Console.WriteLine();

                        // Test 7: Set capacity (required for some operations)
                        Console.WriteLine("7. Setting camera capacity...");
                        try
                        {
                            // This is a simplified test - in real usage you'd get actual disk space
                            var response = await client.HttpClient.PostAsync($"{client.BaseUrl}/api/cameras/capacity?bytesPerSector=4096&numberOfFreeClusters=1000000", null);
                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine("   Capacity set successfully!");
                            }
                            else
                            {
                                Console.WriteLine($"   Failed to set capacity: {response.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   Error setting capacity: {ex.Message}");
                        }
                        Console.WriteLine();

                        // Test 8: Take a photo (commented out for safety)
                        Console.WriteLine("8. Taking a photo...");
                        Console.WriteLine("   (This is commented out for safety - uncomment to test)");
                        /*
                        try
                        {
                            var photoSuccess = await client.TakePhotoAsync();
                            if (photoSuccess)
                            {
                                Console.WriteLine("   Photo command sent successfully!");
                            }
                            else
                            {
                                Console.WriteLine("   Failed to take photo");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   Error taking photo: {ex.Message}");
                        }
                        */
                        Console.WriteLine();

                        // Test 9: Close session
                        Console.WriteLine("9. Closing session...");
                        success = await client.CloseSessionAsync();
                        if (success)
                        {
                            Console.WriteLine("   Session closed successfully!");
                        }
                        else
                        {
                            Console.WriteLine("   Failed to close session");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   Failed to open session");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Make sure the API server is running on https://localhost:7000");
            }

            Console.WriteLine();
            Console.WriteLine("Test completed. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
