using System;

namespace SCQueryConnect.Common.Models
{
    public class InvalidCredentialsException : Exception
    {
        public const string LoginFailed = "Login failed. Please note passwords are case sensitive and ensure you are using the correct username";

        public InvalidCredentialsException() : base(LoginFailed)
        {
        }
    }
}
