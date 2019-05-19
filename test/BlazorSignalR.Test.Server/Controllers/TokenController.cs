using System.Threading.Tasks;
using BlazorSignalR.Test.Client.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlazorSignalR.Test.Server.Controllers
{
    [Route("generatetoken")]
    public class TokenController : Controller
    {
        [HttpGet]
        public Task<string> GenerateToken([FromServices]IJwtTokenResolver jwtTokenResolver)
        {
            return jwtTokenResolver.GetJwtTokenAsync(Request.Query["user"]);
        }
    }
}
