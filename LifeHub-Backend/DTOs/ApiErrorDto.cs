namespace LifeHub.DTOs
{
    public class ApiErrorDto
    {
        public bool Success { get; set; } = false;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}