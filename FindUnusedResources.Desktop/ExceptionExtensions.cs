using System;
using System.Linq;

namespace FindUnusedResources.Desktop
{
    public static class ExceptionExtensions
    {
        public static bool IsTypeOf<T>(this Exception i) where T : Exception
        {
            var isPure = true;
            if (i is AggregateException e)
            {
                isPure &= e.InnerExceptions.All(IsTypeOf<T>);
                return isPure;
            }
            return i is T;
        }
    }
}
