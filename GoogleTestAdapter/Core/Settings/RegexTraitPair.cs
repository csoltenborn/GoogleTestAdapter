using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.Settings
{
    public class RegexTraitPair
    {
        public string Regex { get; }
        public Trait Trait { get; }

        public RegexTraitPair(string regex, string name, string value)
        {
            Regex = regex;
            Trait = new Trait(name, value);
        }

        public override string ToString()
        {
            return $"'{Regex}': {Trait}";
        }
    }
}