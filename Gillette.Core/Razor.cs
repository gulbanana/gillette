using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gillette
{
    public static class Razor
    {
        private static readonly List<PortableExecutableReference> _CSRefs;
        private static readonly CSharpCompilationOptions _CSOpts;

        static Razor()
        {
            var netLocation = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var netRefs = new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }.Select(r => Path.Combine(netLocation, r));

            var gilletteLocation = Path.GetDirectoryName(typeof(Razor).Assembly.Location);
            var gilletteRefs = Directory.EnumerateFiles(gilletteLocation, "*.dll");

            _CSRefs = netRefs.Concat(gilletteRefs).Select(r => MetadataReference.CreateFromFile(r)).ToList();
            _CSOpts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        }

        public static IEnumerable<ValidationError> Validate<T>(string text)
        {
            var compilation = Compile<T>(text);

            using (var dllStream = new MemoryStream())
            {
                var result = compilation.Emit(dllStream);
                if (result.Success)
                    return Enumerable.Empty<ValidationError>();
                else
                    return result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => new ValidationError(d));
            }
        }

        public static string Generate<T>(string text, T model)
        {
            var compilation = Compile<T>(text);

            using (var dllStream = new MemoryStream())
            {
                var result = compilation.Emit(dllStream);
                if (!result.Success) throw new Exception("Template compilation failed - use Validate() to find out why.");

                var templateDLL = Assembly.Load(dllStream.ToArray());
                var templateType = templateDLL.GetType(typeof(T).Name.Replace("[]", "Array") + "Template");
                return (string)(templateType.GetMethod("TransformText").Invoke(null, new object[] { model }));
            }
        }

        public static string Precompile<T>(string text)
        {
            var ast = CSTemplateParser.Parse(text);
            return CSCodeGenerator.Generate(ast, typeof(T), withNamespaces: false);
        }

        private static Compilation Compile<T>(string template)
        {
            var ast = CSTemplateParser.Parse(template);
            var code = CSCodeGenerator.Generate(ast, typeof(T), withNamespaces: true);
            var tree = CSharpSyntaxTree.ParseText(code);
            return CSharpCompilation.Create("TemplateAssembly", new[] { tree }, _CSRefs, _CSOpts);
        }
    }
}
