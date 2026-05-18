using Microsoft.AspNetCore.Mvc;

namespace SecureAuth.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateApiSignatureAttribute : TypeFilterAttribute
{
    public ValidateApiSignatureAttribute()
        : base(typeof(ValidateApiSignatureFilter))
    {
        Order = int.MinValue;
    }
}
