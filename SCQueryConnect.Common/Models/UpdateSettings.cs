namespace SCQueryConnect.Common.Models
{
    public class UpdateSettings
    {
        public string TargetStoryId { get; set; }
        public string QueryString { get; set; }
        public string QueryStringPanels { get; set; }
        public string QueryStringRels { get; set; }
        public string QueryStringResourceUrls { get; set; }
        public string ConnectionString { get; set; }
        public DatabaseType DBType { get; set; }
        public int MaxRowCount { get; set; }
        public bool UnpublishItems { get; set; }
        public bool BuildRelationships { get; set; }
    }
}
