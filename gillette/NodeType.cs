using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gillette
{
    internal enum NodeType
    {
        Sequence,
        Literal,
        Value,
        Block,
        NamedBlock,
        HalfBlock,
        Empty,
    }
}
