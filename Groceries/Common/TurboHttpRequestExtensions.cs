namespace Groceries.Common;

public static class TurboHttpRequestExtensions
{
    public static bool IsTurboFrameRequest(this HttpRequest request)
    {
        return request.Headers.ContainsKey("Turbo-Frame");
    }

    public static bool IsTurboFrameRequest(this HttpRequest request, string frameId)
    {
        return request.Headers.TryGetValue("Turbo-Frame", out var values) && values.Contains(frameId);
    }

    public static bool AcceptsTurboStream(this HttpRequest request)
    {
        return request.GetTypedHeaders().Accept.Any(value => value.MediaType == "text/vnd.turbo-stream.html");
    }
}
