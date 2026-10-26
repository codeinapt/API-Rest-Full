using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Inventory.Api.Tests.ErrorHandling;

public static class HttpResponseReader
{
    public static HttpContext CreateContextWithReadableBody()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }

    public static async Task<JsonElement> ReadJsonBodyAsync(HttpResponse response)
    {
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);

        return document.RootElement.Clone();
    }
}
