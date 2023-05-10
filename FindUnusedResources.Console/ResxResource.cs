namespace FindUnusedResources.Console
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class ResxResource : Resource, IEquatable<ResxResource>
    {
        public string FileName { get; init; }
        public IList<Reference> References { get; } = new List<Reference>();

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

            if (obj is ResxResource reference)
            {
                return Equals(reference);
            }

            return false;
        }

        public bool Equals(ResxResource other)
        {
            if (other == null)
            {
                return false;
            }

            return other.FileName == FileName
                && ((Resource)other).Equals(this)
                && other.References.Count == References.Count
                && other.References.All(References.Contains);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(FileName);
            foreach (var item in References)
            {
                hashCode.Add(item);
            }
            return hashCode.ToHashCode();
        }

        public override string ToString()
        {
            return $"{FileName}: {ClassName}.{Name}";
        }

        public string ToShortString()
        {
            return $"{ClassName}.{Name}";
        }
    }
}