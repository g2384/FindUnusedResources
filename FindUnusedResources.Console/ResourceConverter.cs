namespace FindUnusedResources.Console
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal static class ResourceConverter
    {
        public static ResxResource[] GetResourceStrings(string resourceFilePath)
        {
            return GetResourceStrings(File.ReadAllText(resourceFilePath), resourceFilePath);
        }

        // get resources from *.Designer.cs
        public static ResxResource[] GetResourceStrings(string resourceSourceCode, string fileName = "")
        {
            var stringsTree = CSharpSyntaxTree.ParseText(resourceSourceCode);
            var stringsModel = CSharpCompilation.Create("temp.dll", syntaxTrees: new[] { stringsTree }).GetSemanticModel(stringsTree);

            var descendantNodes = stringsTree.GetRoot().DescendantNodes().ToArray();

            var classProperty = descendantNodes.OfType<ClassDeclarationSyntax>().ToArray();
            if (classProperty.Length > 1)
            {
                // assume the auto-generated designer file contains only one class
                throw new InvalidOperationException();
            }

            var className = classProperty.Single().Identifier.Text;

            var stringProperties = descendantNodes.OfType<PropertyDeclarationSyntax>();

            var resourceStrings = new List<ResxResource>();
            foreach (var property in stringProperties)
            {
                if (!property.Modifiers.Any(SyntaxKind.InternalKeyword)
                    || !property.Modifiers.Any(SyntaxKind.StaticKeyword)
                    || !property.Type.ToString().Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Skip if it's not an internal static string property
                }

                resourceStrings.Add(new ResxResource
                {
                    ClassName = className,
                    Name = property.Identifier.Text,
                    FileName = fileName
                });
            }

            return resourceStrings.ToArray();
        }
    }
}