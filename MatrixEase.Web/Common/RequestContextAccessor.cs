using Microsoft.AspNetCore.Http;

namespace MatrixEase.Web.Common
{
    public class RequestContextAccessor
    {
        private static readonly object SupabaseIdentityKey = new object();

        public SupabaseIdentity GetSupabaseIdentity(HttpContext httpContext)
        {
            if (httpContext?.Items.TryGetValue(SupabaseIdentityKey, out object value) == true &&
                value is SupabaseIdentity identity)
            {
                return identity;
            }

            return new SupabaseIdentity();
        }

        public void SetSupabaseIdentity(HttpContext httpContext, SupabaseIdentity identity)
        {
            httpContext.Items[SupabaseIdentityKey] = identity;
        }
    }
}
