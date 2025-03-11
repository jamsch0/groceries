namespace Groceries;

public static class HttpRequestExtensions
{
    public static Uri GetUri(this HttpRequest request)
    {
        var builder = new UriBuilder
        {
            Scheme = request.Scheme,
            Host = request.Host.Host,
            Port = request.Host.Port.GetValueOrDefault(-1),
            Path = request.Path.ToUriComponent(),
            Query = request.QueryString.ToUriComponent(),
        };
        return builder.Uri;
    }

    private static Uri GetOrigin(this HttpRequest request)
    {
        var builder = new UriBuilder
        {
            Scheme = request.Scheme,
            Host = request.Host.Host,
            Port = request.Host.Port.GetValueOrDefault(-1),
        };
        return builder.Uri;
    }

    private static bool IsSameOrigin(this HttpRequest request, Uri uri)
    {
        var origin = request.GetOrigin();
        return origin.IsBaseOf(uri);
    }

    public static Uri? GetReferrerIfSameOrigin(this HttpRequest request)
    {
        var referrer = request.GetTypedHeaders().Referer;
        return referrer != null && request.IsSameOrigin(referrer) ? referrer : null;
    }

    public static bool IsTurboFrameRequest(this HttpRequest request, string frameId)
    {
        return request.Headers.TryGetValue("Turbo-Frame", out var values) && values.Contains(frameId);
    }
}
