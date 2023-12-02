namespace Groceries;

public static class HttpRequestExtensions
{
    public static bool IsTurboFrameRequest(this HttpRequest request, string frameId)
    {
        return request.Headers.TryGetValue("Turbo-Frame", out var values) && values.Contains(frameId);
    }
}
