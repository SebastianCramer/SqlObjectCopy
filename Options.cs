using CommandLine;

namespace SqlObjectCopy
{
    public class Options
    {
        [Option('s', "schema", Required = false, HelpText = "The Schema to be copied to the test system")]
        public string Schema { get; set; }

        [Option('l', "list", Required = false, HelpText = "Full path to a txt file containing the full names of objects to copy")]
        public string ListFile { get; set; }

        [Option('o', "object", Required = false, HelpText = "Single file to be copied to the test system")]
        public string ObjectName { get; set; }

        [Option('e', "empty", Required = false, HelpText = "Copy tables without content")]
        public bool Empty { get; set; }

        [Option('d', "delta", Required = false, HelpText = "DeltaColumn name for delta data transport")]
        public string DeltaColumnName { get; set; }
        
        [Option('u', "unattended", Required = false, HelpText = "Unattended mode. Skips deletion dialog.")]
        public bool Unattended { get; set; }
    }
}
