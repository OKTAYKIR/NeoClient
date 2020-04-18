namespace NeoClient
{
    public abstract class EntityBase
    {
        public string uuid { get; set; }
        public double createdAt { get; set; } = 0;
        public double? updatedAt { get; set; }
        public bool isDeleted { get; set; }
        public abstract string GetLabelName();
    }
}
