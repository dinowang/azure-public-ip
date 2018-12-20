using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace azure_public_ip
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var url = "https://download.microsoft.com/download/0/1/8/018E208D-54F8-44CD-AA26-CD7BC9524A8C/PublicIPs_20181217.xml";

            var client = new HttpClient();
            var xml = await client.GetStringAsync(url);

            using (var output = new FileStream("output/ip.xml", FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(output))
            {
                writer.WriteLine(xml);
            }

            var cancellationToken = new CancellationToken();
            var reader = new StringReader(xml);
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

            using (var output = new FileStream("output/ip.csv", FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(output))
            {
                writer.WriteLine("Region,IpRange");
                foreach (var ipRange in regionIpRanges)
                {
                    writer.WriteLine($"{ipRange.Region},{ipRange.IpRange}");
                }
            }
        }
    }
}
