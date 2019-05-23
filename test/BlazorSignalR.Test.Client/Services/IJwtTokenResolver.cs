using System.Threading.Tasks;

namespace BlazorSignalR.Test.Client.Services
{
    public interface IJwtTokenResolver
    {
        Task<string> GetJwtTokenAsync(string userId);
    }
}
