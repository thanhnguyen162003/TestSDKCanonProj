namespace Presentation.Models.ResponseModels
{
    public class SettingsListResult
    {
        public uint PropertyId { get; set; }
        public List<uint> AvailableValues { get; set; } = new List<uint>();
    }
}
