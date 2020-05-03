namespace NeoClient
{
    public abstract class EntityBase 
    {
        public EntityBase(string label)
        {
            Label = label;
        }

        public string Label { get; internal set; }
        public string Uuid { get; internal set; }
        public bool IsDeleted { get; internal set; }
    }
}