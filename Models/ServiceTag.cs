using System.Collections.Generic;

namespace CloudRiches.Azure.DataCenter.Models
{
    public class ServiceTag
    {
        public int ChangeNumber { get; set; }

        public string Cloud { get; set; }

        public IEnumerable<ServiceDescription> Values { get; set; }
    }
}