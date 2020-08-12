using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CloudRiches.Azure.DataCenter.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CloudRiches.Azure.DataCenter
{
    class Program
    {
        static HttpClient _client = new HttpClient();

        static async Task Main(string[] args)
        {


            // Microsoft Azure Datacenter IP Ranges
            var ipRangeFiles = await GetMicrosoftUpdateDocument(41653, async x => await IpRangeToCsv(x));
            CopyToLastest(ipRangeFiles);

            // Microsoft Azure Datacenter IP Ranges in China
            var chinaIpRangeFiles = await GetMicrosoftUpdateDocument(42064, async x => await IpRangeToCsv(x));
            CopyToLastest(chinaIpRangeFiles);

            // Azure IP Ranges and Service Tags – Public Cloud
            var serviceTagNames = await GetMicrosoftUpdateDocument(56519, async x => await ServiceTagProcess(x));
            CopyToLastest(serviceTagNames);

            // Azure IP Ranges and Service Tags – US Government Cloud
            var usGovServiceTagNames = await GetMicrosoftUpdateDocument(57063, async x => await ServiceTagProcess(x));
            CopyToLastest(usGovServiceTagNames);

            // Microsoft IP Range GeoLocation
            var microsoftIpRangeGeolocations = await GetMicrosoftUpdateDocument(53601);
            CopyToLastest(microsoftIpRangeGeolocations);

            // Microsoft Public IP Space
            var microsoftPublicIpSpaces = await GetMicrosoftUpdateDocument(53602);
            CopyToLastest(microsoftPublicIpSpaces);
        }

        static async Task<IList<string>> GetMicrosoftUpdateDocument(int id, Func<string, Task<string>> postAction = null)
        {
            return await GetMicrosoftUpdateDocument($"https://www.microsoft.com/en-us/download/details.aspx?id={id}", postAction);
        }

        static async Task<IList<string>> GetMicrosoftUpdateDocument(string refUrl, Func<string, Task<string>> postAction = null)
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

            var files = new List<string> { outputFileName };

            if (postAction != null)
            {
                var additionalFileName = await postAction(outputFileName);

                if (!string.IsNullOrEmpty(additionalFileName))
                {
                    files.Add(additionalFileName);
                }
            }

            return files;
        }

        public static async Task<string> IpRangeToCsv(string outputFileName)
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

                return csvFileName;
            }
        }

        public static async Task<string> ServiceTagProcess(string outputFileName)
        {
            var json = await File.ReadAllTextAsync(outputFileName);

            var serviceTag = JsonConvert.DeserializeObject<ServiceTag>(json);

            return null;
        }

        static void CopyToLastest(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var fileWithoutTimestamp = Regex.Replace(file, @"_20\d{6}\."                , ".");

                if (file != fileWithoutTimestamp)
                {
                    File.Copy(file, fileWithoutTimestamp, true);
                }
            }
        }
    }
}
