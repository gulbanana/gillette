using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gillette
{
    internal static class CSCodeGenerator
    {
        private const string _Namespaces = @"using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

";

        private const string _ClassFormat = @"{3}static class {2}Template
{{
    public static string TransformText({0} Model)
    {{
        var builder = new StringBuilder();
{1}
        return builder.ToString();
    }}
}}";

        public static string Generate(Node ast, Type forModel, bool withNamespaces)
        {
            var shortModelName = forModel.Name.Replace("[]", "Array");
            var qualifiedModelName = forModel.FullName.Replace("+", ".");

            var statements = Flatten(ast);

            return String.Format(_ClassFormat, qualifiedModelName, String.Join(Environment.NewLine, statements), shortModelName, (withNamespaces ? _Namespaces : ""));
        }

        private static IEnumerable<string> Flatten(Node n)
        {
            switch (n.Type)
            {
                case NodeType.Sequence:
                    foreach (var statement in n.Children.SelectMany(Flatten))
                        yield return statement;
                    break;

                case NodeType.Literal:
                    yield return string.Format("builder.Append(@\"{0}\");", n.Text.Replace("\"", "\"\""));
                    break;

                case NodeType.Value:
                    yield return string.Format("builder.Append({0});", n.Text);
                    break;

                case NodeType.Block:
                    yield return string.Format("{0}", n.Text);
                    break;

                case NodeType.NamedBlock:
                    if (string.IsNullOrWhiteSpace(n.Text))
                        yield return string.Format("{0} {{", n.Name, n.Text);
                    else
                        yield return string.Format("{0} {1} {{", n.Name, n.Text);

                    foreach (var statement in n.Children.SelectMany(Flatten))
                        yield return statement;

                    yield return string.Format("}}");
                    break;

                case NodeType.Empty:
                    break;
            }
        }
    }
}
