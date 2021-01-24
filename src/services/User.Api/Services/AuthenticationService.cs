using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using User.Api.Data;
using User.Api.Models;

namespace User.Api.Services
{
    public interface IAuthenticationService
    {
        Task<UserLoginResponse> CreateJwtAsync(string email);
        Task<(bool Succeeded, string[] Errors)> CreateNewAsync(Signup user);
        Task<bool> AscendAsync(UserLogin login);
        Task<RefreshToken> RenewTokenAsync(Guid refreshToken);
    }

    public class AuthenticationService : IAuthenticationService
    {

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppTokenSettings _appTokenSettingsSettings;
        private readonly UserDbContext _context;

        private readonly IJsonWebKeySetService _jwksService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #region ctor
        public AuthenticationService(SignInManager<IdentityUser> signInManager,
                                     UserManager<IdentityUser> userManager,
                                     IOptions<AppTokenSettings> appTokenSettingsSettings,
                                     UserDbContext context,
                                     IJsonWebKeySetService jwksService,
                                     IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appTokenSettingsSettings = appTokenSettingsSettings.Value;
            _jwksService = jwksService;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }
        #endregion        

        public async Task<(bool Succeeded, string[] Errors)> CreateNewAsync(Signup user)
        {

            var identityUser = new IdentityUser
            {
                UserName = user.Email,
                Email = user.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(identityUser, user.Password);

            if (result.Succeeded)
                return (true, null);

            return (false, result.Errors.Select(err => err.Description).ToArray());
        }

        public async Task<UserLoginResponse> CreateJwtAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var claims = await _userManager.GetClaimsAsync(user);

            var identityClaims = await GetClaimsUser(claims, user);
            var encodedToken = GetEncodedToken(identityClaims);

            var refreshToken = await CreateRefreshTokenAsync(email);

            return GetAutentication(encodedToken, user, claims, refreshToken);
        }

        private UserLoginResponse GetAutentication(string encodedToken, IdentityUser user, IList<Claim> claims, RefreshToken refreshToken)
        {
            return new UserLoginResponse
            {
                AccessToken = encodedToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = TimeSpan.FromHours(1).TotalSeconds,
                UserToken = new UserToken
                {
                    Id = user.Id,
                    Email = user.Email,
                    Claims = claims.Select(c => new UserClaim { Type = c.Type, Value = c.Value })
                }
            };
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(string email)
        {
            var refreshToken = new RefreshToken
            {
                Username = email,
                ExpirationDate = DateTime.UtcNow.AddHours(_appTokenSettingsSettings.RefreshTokenExpiration)
            };

            _context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(u => u.Username == email));
            await _context.RefreshTokens.AddAsync(refreshToken);

            await _context.SaveChangesAsync();

            return refreshToken;
        }

        private string GetEncodedToken(ClaimsIdentity identityClaims)
        {

            var tokenHandler = new JwtSecurityTokenHandler();
            var currentIssuer =
                $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
            var key = _jwksService.GetCurrent();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = currentIssuer,
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = key
            });

            return tokenHandler.WriteToken(token);
        }

        private async Task<ClaimsIdentity> GetClaimsUser(IList<Claim> claims, IdentityUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(),
                ClaimValueTypes.Integer64));

            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            return new ClaimsIdentity(claims);

        }

        private static long ToUnixEpochDate(DateTime date)
            => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .TotalSeconds);

        public async Task<bool> AscendAsync(UserLogin login)
        {
            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, false, true);

            return result.Succeeded;

        }

        public async Task<RefreshToken> RenewTokenAsync(Guid refreshToken)
        {
            var token = await _context.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(u => u.Token == refreshToken);

            if(token == null) return null;

            bool isExpired = token.ExpirationDate.ToLocalTime() < DateTime.Now;
            
            if(isExpired) return null;

            return await CreateRefreshTokenAsync(token.Username);

        }
    }


}