namespace DNS_Optimizer.PingTest
{
    internal class PingResults
    {
        public string IPAddress { get; set; }
        public string BufferSize { get; set; }
        public double TTL { get; set; }
        public double RTT { get; set; }
        public string Status { get; set; }
    }
}