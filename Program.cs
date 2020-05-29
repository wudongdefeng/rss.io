using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Scriban;

namespace news.sedders123.me
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new NewsCollator().RunAsync();
        }
    }


    public class NewsCollator
    {
        private string[] _feeds = new[]
        {
            "https://themargins.substack.com/feed.xml",
            "https://jvns.ca/atom.xml",
            "https://danluu.com/atom.xml",
            "https://studentrobotics.org/feed.xml",
            "https://theorangeone.net/posts/index.xml",
            "https://nickcraver.com/blog/feed.xml",
            "http://feeds.feedburner.com/CodeCodeAndMoreCode",
            "http://feeds.hanselman.com/scotthanselman",
            "https://devblogs.microsoft.com/dotnet/feed/",
            // "https://code.visualstudio.com/feed.xml",
            "https://devblogs.microsoft.com/python/feed/",
            "https://devblogs.microsoft.com/commandline/feed/",
            "https://devblogs.microsoft.com/aspnet/feed/",
            "https://routley.io/posts/index.xml"

        };

        private Dictionary<string, string> _hostOverrides = new Dictionary<string, string>() {
            {"http://feeds.hanselman.com/scotthanselman", "hanselman.com"},
            {"http://feeds.feedburner.com/CodeCodeAndMoreCode", "blog.marcgravell.com"}
        };

        private TimeSpan RelevantDuration => TimeSpan.FromDays(-60);

        public async Task RunAsync()
        {
            var feedItems = new ConcurrentBag<(string feedUrl, SyndicationItem item)>();

            Parallel.ForEach(_feeds, feed =>
            {
                foreach (var item in GetFeedItems(feed))
                {
                    feedItems.Add((feed, item));
                }
            });

            var template = Template.Parse(Resources.HtmlTemplate);
            var result = template.Render(new
            {
                Timestamp = DateTime.UtcNow.ToString("R"),
                Posts = feedItems.OrderByDescending(i => i.item.PublishDate).Select(i => new
                {
                    Title = i.item.Title.Text,
                    Link = i.item.Links.FirstOrDefault(l => l.RelationshipType == "alternate").Uri,
                    Host = _hostOverrides.ContainsKey(i.feedUrl) ? _hostOverrides[i.feedUrl] : i.item.Links.FirstOrDefault(l => l.RelationshipType == "alternate").Uri.Host
                })
            });
            using (var outputFile = new StreamWriter(Path.Combine("docs", "index.html")))
            {
                await outputFile.WriteAsync(result);
            }
        }

        private SyndicationItem[] GetFeedItems(string url)
        {
            using var reader = XmlReader.Create(url);
            var feed = SyndicationFeed.Load(reader);
            return feed.Items.Where(i => i.PublishDate >= DateTime.UtcNow.Add(RelevantDuration)).ToArray();
        }
    }

    public static class Resources
    {
        public static string HtmlTemplate = @"
        <!DOCTYPE html>
<html>
	<head>
		<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
		<title>James Seden Smith | Feed</title>
		<style>
			@import url(""https://fonts.googleapis.com/css2?family=Nanum+Myeongjo&display=swap"");
			body {
				font-family: ""Nanum Myeongjo"", serif;
				line-height: 1.5;
				max-width: 700px;
				margin: 1vh auto 1vh;
			}
			li {
				padding-bottom: 1.25rem;
			}

		</style>
	</head>
	<body>
		<h1>News</h1>
		<ol>
			{{ for post in posts }}
                <li>
                    <a href=""{{ post.link }}"">{{ post.title }}</a> ({{ post.host }})
                </li>
			{{ end }}
		</ol>
		<footer>
			<p><a href=""https://github.com/sedders123/news.sedders123.me"">Source</a></p>
			<p>Last updated on {{ timestamp }}</p>
		</footer>
	</body>
</html>";
    }
}
