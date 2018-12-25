using System.Collections.Generic;

namespace CloudRiches.Azure.DataCenter.Models
{
    public class ServiceProperties
    {
        public int ChangeNumber { get; set; }

        public string Region { get; set; }

        public string Platform { get; set; }

        public string SystemService { get; set; }

        public IEnumerable<string> AddressPrefixes { get; set; }

        public override string ToString()
        {
            return $"{SystemService} - {Region}";
        }
    }
}