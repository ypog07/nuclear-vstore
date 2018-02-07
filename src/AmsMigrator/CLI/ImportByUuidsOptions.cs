using CommandLine;

namespace AmsMigrator.CLI
{
    [Verb("import", HelpText = "Import given subset of comma-saparated uuids")]
    public class ImportByUuidsOptions
    {
        [Option('u', "uuids", Required = true, HelpText = "Input ad uuids to be processed.")]
        public string AdvertisementUuids { get; set; }

        [Option('k', "key", Required = true, HelpText = "Db instance key")]
        public string InstanceKey { get; set; }
    }
}
