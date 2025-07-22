namespace FASTBueno.Classes
{
    public class Property
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public Property(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public Property(string name, string value, string description)
        {
            Name = name;
            Value = value;
            Description = description;
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Description))
                return $"{Name}={Value}";
            else
                return $"{Name}={Value} ({Description})";
        }
    }
}
