namespace SCQueryConnect.Helpers
{
    public class QueryEntityType
    {
        public const string Items = "Items";
        public const string Relationships = "Relationships";
        public const string ResourceUrls = "ResourceUrls";
        public const string Panels = "Panels";

        public string ItemsType { get; } = Items;
        public string RelationshipsType { get; } = Relationships;
        public string ResourceUrlsType { get; } = ResourceUrls;
        public string PanelsType { get; } = Panels;
    }
}
