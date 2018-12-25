namespace CloudRiches.Azure.DataCenter.Models
{
    public class ServiceDescription
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public ServiceProperties Properties { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}