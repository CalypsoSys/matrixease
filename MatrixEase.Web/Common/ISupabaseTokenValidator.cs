using System.Threading.Tasks;

namespace MatrixEase.Web.Common
{
    public interface ISupabaseTokenValidator
    {
        Task<SupabaseIdentity> ValidateTokenAsync(string token);
    }
}
