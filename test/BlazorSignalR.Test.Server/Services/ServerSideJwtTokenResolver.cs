using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using BlazorSignalR.Test.Client.Services;
using Microsoft.IdentityModel.Tokens;

namespace BlazorSignalR.Test.Server.Services
{
    public sealed class ServerSideJwtTokenResolver : IJwtTokenResolver
    {
        public Task<string> GetJwtTokenAsync(string userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var credentials = new SigningCredentials(Startup.SecurityKey, SecurityAlgorithms.HmacSha256); // Too lazy to inject the key as a service
            var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.UtcNow.AddSeconds(30), signingCredentials: credentials);
            return Task.FromResult(Startup.JwtTokenHandler.WriteToken(token)); // Even more lazy here
        }
    }
}
