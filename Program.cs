using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace azure_public_ip
{
    class Program
    {
        static HttpClient _client = new HttpClient();

        static async Task Main(string[] args)
        {
            // Microsoft Azure Datacenter IP Ranges
            await GetMicrosoftUpdateDocument("https://www.microsoft.com/en-us/download/details.aspx?id=41653", async x => await IpRangeToCsv(x));

            // Azure IP Ranges and Service Tags – Public Cloud
            await GetMicrosoftUpdateDocument("https://www.microsoft.com/en-us/download/details.aspx?id=56519");
        }

        static async Task<string> GetMicrosoftUpdateDocument(string refUrl, Func<string, Task> postAction = null)
        {
            var html = await _client.GetStringAsync(refUrl);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var downloadButton = htmlDoc.DocumentNode.QuerySelector("a.mscom-link.download-button");
            var confirmUrl = downloadButton.GetAttributeValue("href", null);

            html = await _client.GetStringAsync($"https://www.microsoft.com/en-us/download/{confirmUrl}");

            htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var failoverLink = htmlDoc.DocumentNode.QuerySelector("a.mscom-link.failoverLink");
            var downloadUrl = failoverLink.GetAttributeValue("href", null);

            var uri = new Uri(downloadUrl);
            var fileName = Path.GetFileName(uri.PathAndQuery);
            var outputFileName = $"output/{fileName}";
            
            using (var output = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(output))
            {
                var content = await _client.GetStringAsync(downloadUrl);
                await writer.WriteLineAsync(content);
            }

            if (postAction != null)
            {
                await postAction(outputFileName);
            }

            return outputFileName;
        }

        public static async Task IpRangeToCsv(string outputFileName)
        {
            var cancellationToken = new CancellationToken();

            using (var reader = new StreamReader(outputFileName))
            {
                var xdoc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
                var regions = xdoc.Descendants(XName.Get("Region"));

                var regionIpRanges = regions.Select(x => new 
                                                { 
                                                    IpRanges = x.Elements().Select(e => new 
                                                    {
                                                        Region = x.Attribute("Name").Value,
                                                        IpRange = e.Attribute("Subnet").Value
                                                    })
                                                })
                                                .SelectMany(x => x.IpRanges);

                var csvFileName = Path.ChangeExtension(outputFileName, ".csv");

                using (var output = new FileStream(csvFileName, FileMode.Create, FileAccess.ReadWrite))
                using (var writer = new StreamWriter(output))
                {
                    await writer.WriteLineAsync("Region,IpRange");
                    foreach (var ipRange in regionIpRanges)
                    {
                        await writer.WriteLineAsync($"{ipRange.Region},{ipRange.IpRange}");
                    }
                }
            }
        }
    }
}
