using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Scriban;

namespace news.jss.sh
{
    class Program
    {
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _cts.Cancel();
                eventArgs.Cancel = true;
            };

            await new NewsCollator().RunAsync(_cts.Token);
        }
    }

    public class NewsCollator
    {
        private string[] _feeds = new[]
        {
            "https://fuliba2020.net/feed",
            "https://www.yddx.net/feed",
            "https://hao.su/feed",
            "https://rsshub.app/zhihuzhuanlan/c_1085975047386050560",
            "https://rsshub.app/zaker/source/660",
            "https://rsshub.app/zhihu/daily",
            "https://www.zhihu.com/rss",
	    "https://rsshub.app/netease/news/special/1",
	    "https://rsshub.app/banyuetan/jicengzhili",
	    "https://rsshub.app/zaobao/realtime/china"
        };

        private Dictionary<string, string> _hostOverrides = new Dictionary<string, string>() {
            {"http://feeds.hanselman.com/scotthanselman", "hanselman.com"},
            {"http://feeds.feedburner.com/CodeCodeAndMoreCode", "blog.marcgravell.com"},
            {"https://feeds.feedburner.com/PythonInsider", "pythoninsider.blogspot.com"},
            {"https://www.blogger.com/feeds/4838136820032157985/posts/default", "googleprojectzero.blogspot.com"},
            {"http://feeds.feedburner.com/TroyHunt", "troyhunt.com"},
            {"http://feeds.feedburner.com/HaveIBeenPwnedLatestBreaches", "haveibeenpwned.com"},
            {"https://feeds.feedburner.com/codinghorror", "blog.codinghorror.com"},
            {"http://mypy-lang.blogspot.com/feeds/posts/default", "mypy-lang.org"},
            {"http://feeds.feedburner.com/FritzOnTheWeb", "jeffreyfritz.com"},
        };

        private TimeSpan RelevantDuration => TimeSpan.FromDays(-60);

        public async Task RunAsync(CancellationToken cancel)
        {
            var tasks = _feeds.Select(f => GetFeedItemsAsync(f, cancel));
            var feeds = await Task.WhenAll(tasks);
            var feedItems = feeds.SelectMany(f => f);

            var template = Template.Parse(Resources.HtmlTemplate);
            var result = template.Render(new
            {
                Timestamp = DateTime.UtcNow.ToString("R"),
                Posts = feedItems.OrderByDescending(i => i.PublishingDate).Select(i => new
                {
                    Title = i.Title,
                    Link = i.Link,
                    Host = _hostOverrides.ContainsKey(i.FeedUrl) ? _hostOverrides[i.FeedUrl] : new Uri(i.Link).Host
                })
            });
            using (var outputFile = new StreamWriter(Path.Combine("docs", "index.html")))
            {
                await outputFile.WriteAsync(result);
            }
        }

        public class FeedItemWithHost : FeedItem
        {
            public string FeedUrl { get; set; }
        }
        private async Task<FeedItemWithHost[]> GetFeedItemsAsync(string url, CancellationToken cancel)
        {
            try
            {
                var feed = await FeedReader.ReadAsync(url, cancel);
                return feed.Items
                    .Where(i => i.PublishingDate >= DateTime.UtcNow.Add(RelevantDuration))
                    .Select(i => new FeedItemWithHost { Title = i.Title, Link = i.Link, PublishingDate = i.PublishingDate, FeedUrl = url })
                    .ToArray();
            }
            catch
            {
                Console.WriteLine($"Failed to fetch feed {url}");
                return Array.Empty<FeedItemWithHost>();
            }

        }
    }

    public static class Resources
    {
        public static string HtmlTemplate = @"
        <!DOCTYPE html>
<html>
	<head>
		<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
		<title>舞动的风 | Feed</title>
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
			<p><a href=""https://github.com/wudongdefeng/rss.io"">Source</a></p>
			<p>Last updated on {{ timestamp }}</p>
		</footer>
	</body>
</html>";
    }
}
