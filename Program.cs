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
            "https://themargins.substack.com/feed.xml",
            "https://jvns.ca/atom.xml",
            "https://danluu.com/atom.xml",
            "https://studentrobotics.org/feed.xml",
            "https://theorangeone.net/posts/index.xml",
            "https://nickcraver.com/blog/feed.xml",
            "http://feeds.feedburner.com/CodeCodeAndMoreCode",
            "http://feeds.hanselman.com/scotthanselman",
            "https://devblogs.microsoft.com/dotnet/feed/",
            "https://code.visualstudio.com/feed.xml",
            "https://devblogs.microsoft.com/python/feed/",
            "https://devblogs.microsoft.com/commandline/feed/",
            "https://devblogs.microsoft.com/aspnet/feed/",
            "https://routley.io/posts/index.xml",
            "https://www.encode.io/feeds/articles.rss",
            "https://engineering.fb.com/feed/",
            "https://medium.com/feed/netflix-techblog",
            "https://trickey.io/posts/index.xml",
            "https://feeds.feedburner.com/PythonInsider",
            "https://www.raspberrypi.org/feed/",
            "https://stackshare.io/featured-posts.atom",
            "https://technology.blog.gov.uk/feed/",
            "https://www.scorpia.co.uk/feed/",
            "https://alexwlchan.net/atom.xml",
            "https://blog.cloudflare.com/rss/",
            "https://blog.digitalocean.com/rss/",
            "https://blog.sentry.io/feed.xml",
            "https://github.blog/all.atom",
            "https://thread.engineering/rss.xml",
            "https://doist.com/blog/category/todoist/feed/",
            "https://snyk.io/blog/feed/",
            "https://letsencrypt.org/feed.xml",
            "https://blog.mozilla.org/security/feed/",
            "https://www.blogger.com/feeds/4838136820032157985/posts/default",
            "https://scotthelme.co.uk/rss/",
            "https://www.namecheap.com/blog/category/security-privacy/feed/",
            "https://tails.boum.org/news/index.en.rss",
            "http://feeds.feedburner.com/TroyHunt",
            "https://blog.torproject.org/rss.xml",
            "http://feeds.feedburner.com/HaveIBeenPwnedLatestBreaches",
            "https://blog.github.com/changelog/feed",
            "https://increment.com/feed.xml",
            "https://alistapart.com/main/feed/",
            "https://feeds.feedburner.com/codinghorror",
            "https://overreacted.io/rss.xml",
            "https://css-tricks.com/feed/",
            "https://davidwalsh.name/feed",
            "https://brendanforster.com/notes/index.xml",
            "http://feeds.haacked.com/haacked",
            "http://irisclasson.com/feed/",
            "https://blog.jonskeet.uk/feed/",
            "https://claires.site/feed/",
            "http://www.roji.org/feed",
            "https://blog.ganssle.io/feeds/all.atom.xml",
            "https://angiejones.tech/feed",
            "https://edwardthomson.com/blog/rss.xml",
            "https://bakedbean.org.uk/index.xml",
            "https://m0sa.net/index.xml",
            "https://den.dev/index.xml",
            "https://localghost.dev/index.xml",
            "https://dustingram.com/atom.xml",
            "https://snarky.ca/rss/",
            "https://natemcmaster.com/feed",
            "https://ericlippert.com/feed/atom/",
            "https://una.im/feed",
            "http://mypy-lang.blogspot.com/feeds/posts/default",
            "https://codeopinion.com/feed/",
            "https://daveaglick.com/feed.atom",
            "https://mariatta.ca/feeds/all.atom.xml",
            "https://emilyemorehouse.com/rss.xml",
            "http://feeds.newtonking.com/jamesnewtonking",
            "https://blog.alicegoldfuss.com/feed.xml",
            "https://rob.conery.io/feed/",
            "https://devonzuegel.com/feed.xml",
            "https://kevinmontrose.com/feed/",
            "https://dizzyd.com/index.xml",
            "http://feeds.feedburner.com/FritzOnTheWeb",
            "https://www.matvelloso.com/index.php/feed/",
            "https://www.tarynpivots.com/index.xml",
            "https://eileencodes.com/feed.xml",
            "https://kentcdodds.com/blog/rss.xml",
            "https://doublepulsar.com/feed",
            "https://paulstovell.com/rss/",
            "https://blog.jessfraz.com/index.xml",
            "https://montemagno.com/rss/",
            "https://jvns.ca/atom.xml",
            "https://codeofmatt.com/rss/",
            "https://seldo.com/rss.xml",
            "https://sophiebits.com/atom.xml",
            "https://tisiphone.net/feed/",
            "https://tooslowexception.com/feed/",
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
            var feed = await FeedReader.ReadAsync(url, cancel);
            return feed.Items
                .Where(i => i.PublishingDate >= DateTime.UtcNow.Add(RelevantDuration))
                .Select(i => new FeedItemWithHost { Title = i.Title, Link = i.Link, PublishingDate = i.PublishingDate, FeedUrl = url })
                .ToArray();
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
			<p><a href=""https://github.com/sedders123/news.jss.sh"">Source</a></p>
			<p>Last updated on {{ timestamp }}</p>
		</footer>
	</body>
</html>";
    }
}
