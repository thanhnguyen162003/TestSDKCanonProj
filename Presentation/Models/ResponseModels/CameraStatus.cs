namespace Presentation.Models.ResponseModels
{
    public class CameraStatus
    {
        public bool SessionOpen { get; set; }
        public bool LiveViewOn { get; set; }
        public bool IsFilming { get; set; }
        public string? MainCamera { get; set; }
    }
}
