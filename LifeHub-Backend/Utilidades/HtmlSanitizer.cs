using System.Text.RegularExpressions;

namespace LifeHub.Utilidades
{
    public class HtmlSanitizer : IHtmlSanitizer
    {
        public string Sanitize(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            content = Regex.Replace(content,
                @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            content = Regex.Replace(content,
                @"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]*)",
                string.Empty,
                RegexOptions.IgnoreCase);

            content = Regex.Replace(content,
                @"href\s*=\s*(""javascript:[^""]*""|'javascript:[^']*')",
                string.Empty,
                RegexOptions.IgnoreCase);

            return content;
        }
    }
}
