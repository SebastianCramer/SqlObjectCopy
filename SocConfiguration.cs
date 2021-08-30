namespace SqlObjectCopy.Configuration
{
    public class SocConfiguration
    {
        public Connection[] Connections { get; set; }
        public int MaxParallelTransferThreads { get; set; }
    }

    public class Connection
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public bool Selected { get; set; }
    }
}