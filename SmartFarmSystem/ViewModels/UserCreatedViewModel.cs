namespace SmartFarmSystem.ViewModels
{
    public class UserCreatedViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string SetupLink { get; set; } = string.Empty;
        public bool EmailFailed { get; set; }
    }
}