using Application.DTOs;

namespace ETicaret.Models
{
    public class UserModel
    {
        public LoginDto? Login { get; set; }
        public RegisterDto? Register { get; set; }
        public bool IsRegister { get; set; } = false;
        private string? _returnUrl;
        public string? ReturnUrl
        {
            get
            {
                if (_returnUrl == null)
                {
                    return "/";
                }
                else
                {
                    return _returnUrl;
                }
            }
            set
            {
                _returnUrl = value;
            }
        }
    }
}
