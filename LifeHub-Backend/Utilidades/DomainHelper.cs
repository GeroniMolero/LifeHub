namespace LifeHub.Utilidades
{
    public static class DomainHelper
    {
        // Normalizes a raw URI host (already scheme-free, e.g. from Uri.Host).
        public static string NormalizeHost(string value)
        {
            var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (trimmed.StartsWith("www.")) trimmed = trimmed[4..];
            return trimmed;
        }

        // Normalizes user-supplied domain input that may include scheme, www prefix, or path.
        public static string NormalizeUserInputDomain(string value)
        {
            var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (trimmed.StartsWith("http://"))  trimmed = trimmed[7..];
            if (trimmed.StartsWith("https://")) trimmed = trimmed[8..];
            if (trimmed.StartsWith("www."))     trimmed = trimmed[4..];
            var slashIndex = trimmed.IndexOf('/');
            if (slashIndex >= 0) trimmed = trimmed[..slashIndex];
            return trimmed;
        }
    }
}
