using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SecureAuth.Contracts;
using SecureAuth.Services;

namespace SecureAuth.Filters;

public sealed class ValidateApiSignatureFilter : IAsyncActionFilter
{
    private readonly ApiSignatureValidator _validator;

    public ValidateApiSignatureFilter(ApiSignatureValidator validator)
    {
        _validator = validator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var signedRequest = context.ActionArguments.Values.OfType<ISignedRequest>().SingleOrDefault();

        if (signedRequest is null)
        {
            context.Result = new BadRequestObjectResult(ErrorResponse.InvalidRequest());
            return;
        }

        if (signedRequest.RequestDate is null && context.ModelState.ErrorCount > 0)
        {
            context.Result = new BadRequestObjectResult(ErrorResponse.InvalidRequest());
            return;
        }

        var result = _validator.Validate(signedRequest.ApiSignature, signedRequest.RequestDate);

        if (result == ApiSignatureValidationResult.InvalidSignature)
        {
            context.Result = new UnauthorizedObjectResult(ErrorResponse.InvalidSignature());
            return;
        }

        if (result == ApiSignatureValidationResult.StaleRequest)
        {
            context.Result = new UnauthorizedObjectResult(ErrorResponse.StaleRequest());
            return;
        }

        await next();
    }
}
