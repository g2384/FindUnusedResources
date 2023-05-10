namespace FindUnusedResources.Console
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Immutable;

    internal class Program
    {
        static void Main(string[] args)
        {
            string stringsFilePath = @"";
            string[] codeFiles = Directory.GetFiles(@"", "*.cs", SearchOption.AllDirectories);

            SyntaxTree stringsTree = CSharpSyntaxTree.ParseText(File.ReadAllText(stringsFilePath));
            SemanticModel stringsModel = CSharpCompilation.Create("temp.dll", syntaxTrees: new[] { stringsTree }).GetSemanticModel(stringsTree);

            var unusedVariables = new List<string>();

            var descendantNodes = stringsTree.GetRoot().DescendantNodes().ToArray();

            var classProperty = descendantNodes.OfType<ClassDeclarationSyntax>().ToArray();
            if(classProperty.Length > 1)
            {
                throw new InvalidOperationException();
            }

            var className = classProperty.Single().Identifier.Text;

            IEnumerable<PropertyDeclarationSyntax> stringProperties = stringsTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>();

            var resourceStrings = new List<string>();
            foreach (PropertyDeclarationSyntax property in stringProperties)
            {
                ISymbol symbol = stringsModel.GetDeclaredSymbol(property);

                if (symbol == null || !symbol.Kind.Equals(SymbolKind.Property)) continue; // Skip if it's not a property
                if (!property.Modifiers.Any(SyntaxKind.InternalKeyword) || !property.Modifiers.Any(SyntaxKind.StaticKeyword) || !property.Type.ToString().Equals("string")) continue; // Skip if it's not an internal static string property
                resourceStrings.Add($"{className}.{property.Identifier.Text}");
            }

            foreach (string filePath in codeFiles)
            {
                if (filePath.Equals(stringsFilePath)) continue;

                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
                SemanticModel model = CSharpCompilation.Create("temp.dll", syntaxTrees: new[] { tree }).GetSemanticModel(tree);

                var usages = model.GetAllResxStrings();
                foreach (var s in resourceStrings)
                {
                    if (!usages.Contains(s))
                    {
                        unusedVariables.Add(s);
                    }
                }
            }

            if (unusedVariables.Any())
            {
                Console.WriteLine("The following variables are unused:");
                foreach (string variable in unusedVariables)
                {
                    Console.WriteLine(variable);
                }
            }
            else
            {
                Console.WriteLine("No unused variables found.");
            }
        }
    }

    public static class SematicModelExtensions
    {
        public static string[] GetAllResxStrings(this SemanticModel model)
        {
            var matched = new List<string>();
            var desendantNodes = model.SyntaxTree.GetRoot().DescendantNodes().ToArray();
            foreach (var node in desendantNodes)
            {
                if (node is MemberAccessExpressionSyntax m)
                {
                    if (m.Expression is IdentifierNameSyntax e)
                    {
                        var name = e.ToString();
                        var value = m.Name.ToString();
                        matched.Add($"{name}.{value}");
                    }
                }
            }
            return matched.ToArray();
        }
    }
}