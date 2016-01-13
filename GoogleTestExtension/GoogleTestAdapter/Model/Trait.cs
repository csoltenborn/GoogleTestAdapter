namespace GoogleTestAdapter.Model
{
    public class Trait
    {
        public string Name { get; }
        public string Value { get; }

        public Trait(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return $"({Name},{Value})";
        }

    }

}