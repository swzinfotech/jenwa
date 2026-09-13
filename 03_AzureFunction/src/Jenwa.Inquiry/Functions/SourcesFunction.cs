using Jenwa.Inquiry.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jenwa.Inquiry.Functions;

/// <summary>
/// GET /api/line/sources?code=&lt;function key&gt; — lists the ids the webhook has seen, so
/// LINE_TO_IDS can be filled in without digging through logs. Function-key protected because
/// the response contains LINE identifiers.
/// </summary>
public sealed class SourcesFunction(SourceStore sources)
{
    [Function("LineSources")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "line/sources")] HttpRequest request,
        CancellationToken cancellationToken)
        => new OkObjectResult(new { sources = await sources.ListAsync(cancellationToken) });
}
