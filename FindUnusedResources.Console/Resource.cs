namespace FindUnusedResources.Console
{
    using System;

    internal class Resource : IEquatable<Resource>
    {
        public string ClassName { get; init; }
        public string Name { get; init; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            if (obj is Resource reference)
            {
                return Equals(reference);
            }

            return false;
        }

        public bool Equals(Resource other)
        {
            if (other == null)
            {
                return false;
            }

            return other.ClassName == ClassName
                && other.Name == Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClassName, Name);
        }

        public override string ToString()
        {
            return $"{ClassName}.{Name}";
        }
    }
}