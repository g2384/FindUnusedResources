namespace FindUnusedResources.Console
{
    using System;

    internal sealed class Reference : IEquatable<Reference>
    {
        public string FileName { get; init; }
        public string ClassName { get; init; }
        public int Count { get; init; }

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

            if (obj is Reference reference)
            {
                return Equals(reference);
            }

            return false;
        }

        public bool Equals(Reference other)
        {
            if (other == null)
            {
                return false;
            }

            return FileName == other.FileName
                && ClassName == other.ClassName
                && Count == other.Count;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileName, ClassName, Count);
        }

        public override string ToString()
        {
            return $"{FileName}: {ClassName} ({Count})";
        }
    }
}