namespace Presentation.Constants
{
    public static class LogConstants
    {
        // API Response Messages
        public const string SESSION_OPENED_SUCCESSFULLY = "Session opened successfully";
        public const string SESSION_CLOSED_SUCCESSFULLY = "Session closed successfully";
        public const string SETTING_UPDATED_SUCCESSFULLY = "Setting updated successfully";
        public const string PHOTO_COMMAND_SENT_SUCCESSFULLY = "Photo command sent successfully";
        public const string BULB_PHOTO_COMMAND_SENT_SUCCESSFULLY = "Bulb photo command sent successfully";
        public const string LIVE_VIEW_STARTED_SUCCESSFULLY = "Live view started successfully";
        public const string LIVE_VIEW_STOPPED_SUCCESSFULLY = "Live view stopped successfully";
        public const string FILMING_STARTED_SUCCESSFULLY = "Filming started successfully";
        public const string FILMING_STOPPED_SUCCESSFULLY = "Filming stopped successfully";
        public const string FOCUS_COMMAND_SENT_SUCCESSFULLY = "Focus command sent successfully";
        public const string CAPACITY_SET_SUCCESSFULLY = "Capacity set successfully";
        public const string CAMERA_UI_LOCKED_SUCCESSFULLY = "Camera UI locked successfully";
        public const string CAMERA_UI_UNLOCKED_SUCCESSFULLY = "Camera UI unlocked successfully";

        // Error Messages
        public const string NO_CAMERA_SESSION_OPEN = "No camera session is open";
        public const string CAMERA_NOT_FOUND = "Camera not found";
        public const string FAILED_TO_GET_CAMERA_LIST = "Failed to get camera list";
        public const string FAILED_TO_OPEN_SESSION = "Failed to open session";
        public const string FAILED_TO_CLOSE_SESSION = "Failed to close session";
        public const string FAILED_TO_GET_SETTING = "Failed to get setting";
        public const string FAILED_TO_SET_SETTING = "Failed to set setting";
        public const string FAILED_TO_GET_SETTINGS_LIST = "Failed to get settings list";
        public const string FAILED_TO_TAKE_PHOTO = "Failed to take photo";
        public const string FAILED_TO_TAKE_BULB_PHOTO = "Failed to take bulb photo";
        public const string FAILED_TO_START_LIVE_VIEW = "Failed to start live view";
        public const string FAILED_TO_STOP_LIVE_VIEW = "Failed to stop live view";
        public const string FAILED_TO_START_FILMING = "Failed to start filming";
        public const string FAILED_TO_STOP_FILMING = "Failed to stop filming";
        public const string FAILED_TO_SET_FOCUS = "Failed to set focus";
        public const string FAILED_TO_SET_UI_LOCK_STATE = "Failed to set UI lock state";
        public const string FAILED_TO_SET_CAPACITY = "Failed to set capacity";
        public const string FAILED_TO_GET_CAMERA_STATUS = "Failed to get camera status";
        public const string FAILED_TO_GET_CAMERA_ENTRIES = "Failed to get camera entries";

        // SDK Error Messages
        public const string SDK_ERROR_PREFIX = "SDK Error: 0x";
        public const string CAMERA_OR_REFERENCE_NULL = "Camera or camera reference is null/zero";
        public const string STRING_MUST_NOT_BE_NULL = "String must not be null";
        public const string VALUE_TOO_LARGE = "Value must be smaller than 32 bytes";
        public const string METHOD_CANNOT_BE_USED_WITH_PROPERTY_ID = "Method cannot be used with this Property ID";
        public const string CAMERA_NOT_IN_FILM_MODE = "Camera is not in film mode";
        public const string BULBTIME_TOO_SMALL = "Bulbtime has to be bigger than 1000ms";

        // Test Console Messages
        public const string TEST_CONSOLE_TITLE = "Canon SDK API Test Console";
        public const string TEST_CONSOLE_SEPARATOR = "==========================";
        public const string GETTING_CONNECTED_CAMERAS = "1. Getting connected cameras...";
        public const string NO_CAMERAS_FOUND = "   No cameras found. Please connect a Canon camera and try again.";
        public const string FOUND_CAMERAS_COUNT = "   Found {0} camera(s):";
        public const string CAMERA_INFO_FORMAT = "   - {0} on {1} (Ref: {2})";
        public const string GETTING_CAMERA_STATUS = "2. Getting camera status...";
        public const string SESSION_OPEN_STATUS = "   Session Open: {0}";
        public const string LIVE_VIEW_STATUS = "   Live View On: {0}";
        public const string IS_FILMING_STATUS = "   Is Filming: {0}";
        public const string MAIN_CAMERA_STATUS = "   Main Camera: {0}";
        public const string OPENING_SESSION = "3. Opening session with first camera...";
        public const string SESSION_OPENED_SUCCESS = "   Session opened successfully!";
        public const string GETTING_UPDATED_STATUS = "4. Getting updated camera status...";
        public const string GETTING_CAMERA_SETTINGS = "5. Getting camera settings...";
        public const string ISO_SPEED_VALUE = "   ISO Speed: {0}";
        public const string APERTURE_VALUE = "   Aperture Value: {0}";
        public const string SHUTTER_SPEED_VALUE = "   Shutter Speed: {0}";
        public const string ERROR_GETTING_SETTINGS = "   Error getting settings: {0}";
        public const string GETTING_ISO_VALUES = "6. Getting available ISO values...";
        public const string AVAILABLE_ISO_VALUES = "   Available ISO values: {0}";
        public const string ERROR_GETTING_ISO_VALUES = "   Error getting ISO values: {0}";
        public const string SETTING_CAMERA_CAPACITY = "7. Setting camera capacity...";
        public const string CAPACITY_SET_SUCCESS = "   Capacity set successfully!";
        public const string FAILED_TO_SET_CAPACITY_STATUS = "   Failed to set capacity: {0}";
        public const string ERROR_SETTING_CAPACITY = "   Error setting capacity: {0}";
        public const string TAKING_PHOTO = "8. Taking a photo...";
        public const string PHOTO_COMMENTED_OUT = "   (This is commented out for safety - uncomment to test)";
        public const string PHOTO_COMMAND_SENT_SUCCESS = "   Photo command sent successfully!";
        public const string FAILED_TO_TAKE_PHOTO_STATUS = "   Failed to take photo";
        public const string ERROR_TAKING_PHOTO = "   Error taking photo: {0}";
        public const string CLOSING_SESSION = "9. Closing session...";
        public const string SESSION_CLOSED_SUCCESS = "   Session closed successfully!";
        public const string FAILED_TO_CLOSE_SESSION_STATUS = "   Failed to close session";
        public const string FAILED_TO_OPEN_SESSION_STATUS = "   Failed to open session";
        public const string GENERAL_ERROR = "Error: {0}";
        public const string API_SERVER_NOT_RUNNING = "Make sure the API server is running on https://localhost:7000";
        public const string TEST_COMPLETED = "Test completed. Press any key to exit...";

        // Default Values
        public const string NONE = "None";
        public const string CAMERA = "Camera";
        public const string HDD = "HDD";
        public const string VOLUME_PREFIX = "Volume";
        public const string VOLUME_FORMAT = "Volume{0}({1})";

        // File Extensions
        public const string JPG_EXTENSION = ".jpg";
        public const string JPEG_EXTENSION = ".jpeg";

        // HTTP Error Messages
        public const string HTTP_ERROR_PREFIX = "Error: {0}";
    }
}
