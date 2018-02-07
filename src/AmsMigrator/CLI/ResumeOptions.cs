using CommandLine;

namespace AmsMigrator.CLI
{
    [Verb("resume", HelpText="Resume migration from given firmId.")]
    public class ResumeOptions
    {
        [Option('i', "firmId", Required = true, HelpText = "Firm id to start start.")]
        public long StartFromFirmId { get; set; }
        [Option('k', "dbKey", Required = true, HelpText = "Erm database instance to start from.")]
        public string InstanceKey { get; set; }
    }
}
