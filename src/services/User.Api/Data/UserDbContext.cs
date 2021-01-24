using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetDevPack.Security.JwtSigningCredentials;
using NetDevPack.Security.JwtSigningCredentials.Store.EntityFrameworkCore;
using User.Api.Models;

namespace User.Api.Data
{
    public class UserDbContext : IdentityDbContext, ISecurityKeyContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options)
            :base(options)
        {
            
        }

        public DbSet<SecurityKeyWithPrivate> SecurityKeys { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }

    
}