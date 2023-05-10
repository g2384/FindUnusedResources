namespace FindUnusedResources.Console
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;
    using System.Linq;

    internal static class SematicModelExtensions
    {
        public static Resource[] GetAllResxReferences(this SemanticModel model)
        {
            var matched = new List<Resource>();
            var descendantNodes = model.SyntaxTree.GetRoot().DescendantNodes().ToArray();
            foreach (var node in descendantNodes)
            {
                if (node is MemberAccessExpressionSyntax m
                    && m.Expression is IdentifierNameSyntax e)
                {
                    var name = e.ToString();
                    var value = m.Name.ToString();
                    matched.Add(new Resource
                    {
                        ClassName = name,
                        Name = value
                    });
                }
            }
            return matched.ToArray();
        }
    }
}