using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gillette
{
    internal sealed class Node
    {
        public readonly NodeType Type;
        public readonly string Name;
        public readonly string Text;
        public readonly List<Node> Children;

        public Node(NodeType type, string name, string text = null, IEnumerable<Node> children = null)
        {
            Type = type;
            Name = name;
            Text = text;
            Children = (children == null ? Enumerable.Empty<Node>() : children).ToList();
        }

        public override string ToString()
        {
            var formattedChildren = (Children.Any() ? "{" + string.Join(", ", Children.Select(c => c.ToString())) + "}" : "");
            var formattedText = (!string.IsNullOrWhiteSpace(Text) ? string.Format("'{0}' ", Text) : "");
            return string.Format("{0}: {1}{2}", Name, formattedText, formattedChildren);
        }
    }
}
