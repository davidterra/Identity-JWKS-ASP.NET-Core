using System;
using System.Collections.Generic;

namespace User.Api.Models
{
    public class Signup
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PasswordConfirmed { get; set; }
    }

    public class UserLogin
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UserClaim
    {
        public string Value { get; set; }
        public string Type { get; set; }
    }

    public class UserToken
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public IEnumerable<UserClaim> Claims { get; set; }
    }

    public class UserLoginResponse
    {
        public string AccessToken { get; set; }
        public Guid RefreshToken { get; set; }
        public double ExpiresIn { get; set; }
        public UserToken UserToken { get; set; }
    }


}