namespace AuthServer.Services;

/// <summary>
/// Provides request accepts functionality.
/// </summary>
public static class RequestAccepts
{
    public static bool WantsHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(accept))
        {
            return false;
        }

        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
