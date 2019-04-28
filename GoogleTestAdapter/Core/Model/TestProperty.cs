namespace GoogleTestAdapter.Model
{
    public abstract class TestProperty
    {
        public string Serialization { get; }

        protected TestProperty(string serialization)
        {
            Serialization = serialization;
        }

        public override string ToString()
        {
            return $"{GetType()}: {Serialization}";
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            var other = (TestProperty) obj;
            return string.Equals(Serialization, other.Serialization);
        }

        public override int GetHashCode()
        {
            return Serialization != null ? Serialization.GetHashCode() : 0;
        }
    }
}