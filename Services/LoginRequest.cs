namespace Financial_management_backend.Services
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class LoginReponse
    {
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }

        public string RefreshToken { get; set; }
        public string Role { get; set; }
    }
}
