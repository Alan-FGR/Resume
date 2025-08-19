using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
//using QuestPDF.Fluent;
//using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

string baseUrl = "https://alanresume.com/";
string siteName = "Alan Ciampaglia";
string description = "Senior Software Engineer (18+ yrs). C#/.NET & C++ expertise building real-time systems, networking frameworks, and secure backends across gaming, fintech, and embedded.";

string htmlTemplate = $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>{{siteName}}</title>
  <meta name="description" content="{{description}}">
  <link rel="canonical" href="{{baseUrl}}">

  <meta name="viewport" content="width=device-width, initial-scale=1">
  <!-- <meta name="theme-color" content="#ffffff">            --> <!-- Chrome/Android -->
  <!-- <meta name="color-scheme" content="light dark">        --> <!-- iOS 15+ dark mode -->

  <meta property="og:type"        content="website">
  <meta property="og:url"         content="{{baseUrl}}">
  <meta property="og:title"       content="{{siteName}}">
  <meta property="og:description" content="{{description}}">
  <meta property="og:image"       content="{{baseUrl}}og-image.jpg">

  <!-- <meta name="twitter:card"        content="summary_large_image"> -->
  <!-- <meta name="twitter:site"        content="@YourHandle"> -->
  <!-- <meta name="twitter:title"       content="{{siteName}}"> -->
  <!-- <meta name="twitter:description" content="{{description}}"> -->
  <!-- <meta name="twitter:image"       content="{{baseUrl}}og-image.jpg"> -->
  
  <script type="application/ld+json">
  {
    "@context": "https://schema.org",
    "@type": "WebSite",
    "url": "{{baseUrl}}",
    "name": "{{siteName}}",
    "description": "{{description}}",
    "image":   "{{baseUrl}}og-image.jpg"
  }
  </script>

  <link rel="icon" type="image/svg+xml" href="/favicon.svg">

  <!-- <meta name="google-site-verification" content="token"> -->
  <!-- <meta name="msvalidate.01" content="token"> -->

  <!-- 1) Warm up connections -->
  <link rel="preconnect" href="https://fonts.googleapis.com">
  
  <!-- 2) Preload the stylesheet (non-blocking) -->
  <link rel="preload"
        href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700&display=swap"
        as="style">
  
  <!-- 3) Load it asynchronously, then apply -->
  <link rel="stylesheet"
        href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700&display=swap"
        media="print" onload="this.media='all'">
  
  <!-- 4) Fallback for no-JS -->
  <noscript>
    <link rel="stylesheet"
          href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700&display=swap">
  </noscript>

  <link rel="stylesheet" href="style.css">

  <!-- <script src="/js/app.js" defer></script> -->
</head>
<body>
<header>
  <nav>
    $TOC
  </nav>
</header>
<article>
$CONTENT
</article>
</body>
</html>
""";

string markdown = File.ReadAllText(args[0], Encoding.UTF8)
    .Replace("\r\n<!--BR-->", " {.breakable}")
    .Replace("·", "<span class=\"mobile-break\"></span><span class=\"inline-nobreak\"> · <wbr></span>");

bool isShort = !args.Contains("--full");

if (isShort)
    markdown = Regex.Replace(markdown, @"<!--FS-->.*?<!--FE-->", "", RegexOptions.Singleline);

string html = Markdown.ToHtml(markdown);

var pipeline = new MarkdownPipelineBuilder()
                 .UseAdvancedExtensions()   // tables, footnotes, etc.
                 .Build();

MarkdownDocument doc = Markdown.Parse(markdown, pipeline);

/* -------- gather the headings -------- */
var headings = doc                      // searches all nested blocks
               .Descendants<HeadingBlock>()        // Markdig helper
               .Where(h => h.Level == 2) // filter by heading level
               .Skip(1)
               .Select(h => $"<a class=\"toc-item\" href=\"#{ToSlug(h.Inline)}\">{GetInlineText(h.Inline)}</a>")
               .Prepend("<a class=\"toc-item hide-mobile\" href=\"#\">Top</a>") // add a "Top" link
               .ToList();

headings.Add(isShort
    ? "<a class=\"toc-item\" href=\"/full.html\">Full Version</a>"
    : "<a class=\"toc-item\" href=\"/\">Short Version</a>");

html = Markdown.ToHtml(markdown, pipeline);

File.WriteAllText(args[1], htmlTemplate
    .Replace("$CONTENT", html)
    .Replace("$TOC", string.Join("\n", headings)), new UTF8Encoding(true, true));

static string GetInlineText(ContainerInline? root)
{
    if (root == null) return string.Empty;

    var sb = new StringBuilder();
    foreach (var node in root.Descendants())          // depth-first
        if (node is LiteralInline lit) sb.Append(lit.Content);
    return sb.ToString();
}

static string ToSlug(ContainerInline? root)
{
    var text = GetInlineText(root);

    Regex _slugSanitizer = new("[^a-z0-9\\- ]+", RegexOptions.IgnoreCase);

    var slug = _slugSanitizer.Replace(text.ToLowerInvariant(), "")
                             .Trim()
                             .Replace(' ', '-')
                             .Replace("--", "-");
    return slug;
}
