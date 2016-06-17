using System;
using System.Linq;
using static gillette.Parse;

namespace gillette
{
    /// <summary>
    /// Parses a Razor-like language, which can detect and special-case RTF rather than HTML
    /// </summary>
    /// <remarks>
    /// supported forms:
    /// 
    ///     @expression
    /// 
    ///     @(complex expression, disambiguated)    
    /// 
    ///     @{
    ///       statements;
    ///     }
    /// 
    ///     @foreach (var x in y)  //or other control keywords such as using, try, etc
    ///     {
    ///       sub-template
    ///     }
    /// 
    ///     @if (condition) 
    ///     {
    ///       sub-template
    ///     }
    ///     else //or @else, else if (), etc
    ///     {
    ///       sub-template
    ///     }
    /// 
    /// and so on for other keywords. arbitrary nesting and chaining is permitted.
    /// 
    /// pseudo-EBNF rules:
    /// 
    ///     template = magic number, rtf content | content
    ///     magic number = "{\rtf"
    ///     content = section, [razor]
    ///     rtf content = section, [rtf razor]
    ///     section = {unicode-character - "@"}
    ///     razor = (escape | (sigil, code) | sigil), [content]
    ///     rtf razor = (escape | (sigil, rtf code) | sigil), [rtf content]
    ///     sigil = "@"
    ///     escape = "@@"
    ///     code = "@", (block chain | statement block | unambiguous expression | autoclosed expression)
    ///     rtf code = "@", (rtf block chain | rtf statement block | unambiguous expression | autoclosed expression)
    ///     block chain = named block, {whitespace, named block}
    ///     rtf block chain = rtf named block, {rtf whitespace, rtf named block}
    ///     named block = c#-keyword, [whitespace], [condition], [whitespace], "{", content, "}"
    ///     rtf named block = c#-keyword, [rtf whitespace], [condition], [rtf whitespace], "{", rtf content, "}"
    ///     statement block = "{", c#-statements, "}"
    ///     rtf statement block = "\{", c#-statements, "\}"
    ///     unambiguous expression = "(", c#-expression, ")"
    ///     autoclosed-expression = { allowed character | argument exception }
    ///     allowed character = unicode-character - "@" - "\" - " "
    ///     argument exception = ",", { " " }
    /// </remarks>
    static class CSTemplateParser
    {
        private static readonly Parser _GeneratedParser = BuildParser();

        public static Node Parse(string text)
        {
            var r = _GeneratedParser(text);
            if (!r.IsSuccess) throw new Exception($"parse error: {r.Remainder}{Environment.NewLine}expected: {r.Expected}");

            return r.Tree;
        }

        private static Parser BuildParser()
        {
            Parser section = null; //closed over by NamedBlock
            Parser sectionRTF = null; //closed over by NamedBlockRTF

            var unambiguousExpression = Custom(@"
                ^
                (?'P'\()                           # begin balanced parens
                    ((?:                           # capture any number of..
                        (?'Pi'\() | (?'-Pi'\)) | . # .. ( or ) as inner parens, or anything else ungrouped
                    )*?) 
                (?'-P'\))                          # end balanced parens
                (?(Pi)(?!))                        # fail if any inner parens are dangling, e.g. @(()
            ", m => new Node(NodeType.Value, "expression", m.Groups[1].Value));

            var autoclosedExpression = Custom(@"
                ^
                (?:                        # any of..
                    ,\s+ |                 # .. either a comma followed by whitespace, or 
                    (?'S'"") .+? (?'-S'"") | # a balanced string, or
                    [^@ \\ \{ \} \s]       # something that isn't rtf/razor
                )+
            ", m => new Node(NodeType.Value, "expression", m.Value));

            var statementBlock = Custom(@"
                ^
                (?'B'\{)                           # begin balanced braces
                    ((?:                           # capture any number of...
                        (?'Bi'\{) | (?'-Bi'\}) | . # ... { or } as inner braces, or anything else ungrouped
                    )*?) 
                (?'-B'\})                          # end balanced braces
                (?(Bi)(?!))                        # fail if any inner braces are dangling, e.g. @{{}
            ", m => new Node(NodeType.Block, "statements", m.Groups[1].Value));

            var statementBlockRTF = Custom(@"
                ^
                (?'B'\\\{)                             # begin balanced braces
                    (?: \s* \\par \s*)?                # allow an rtf newline
                    ((?:                               # capture any number of...
                        (?'Bi'\\\{) | (?'-Bi'\\\}) | . # ... { or } as inner braces, or anything else ungrouped
                    )*?) 
                (?'-B'\\\})                            # end balanced braces
                (?: \s* \\par \s*)?                    # allow another newline
                (?(Bi)(?!))                            # fail if any inner braces are dangling, e.g. @{{}
            ", m => new Node(NodeType.Block, "statements", m.Groups[1].Value.Replace(@"\par", "")));

            var namedBlock = Custom(@"
                ^
                (\w+|\w+\ \w+)                         # 1-2 keywords in the form 'if' or 'else if'
                \s*                                    
                ((?'P'\() .*? (?'-P'\))|)              # optional condition/using vars/etc
                \s*                                    
                (?'B'\{)                               # begin balanced braces
                    ((?:                               # capture any number of...
                        (?'Bi'\{) | (?'-Bi'\}) | .     # ... { or } as inner braces, or anything else ungrouped
                    )*?)                               
                (?'-B'\})                              # end balanced braces
                (?(Bi)(?!))                            # fail if any inner braces are dangling, e.g. @{{}
            ", m =>
            {
                var keyword = m.Groups[1].Value;
                var condition = m.Groups[2].Value;
                var innerContent = m.Groups[3].Value;

                var r = Set("inner-sections", section)(innerContent);

                return new Node(NodeType.NamedBlock, keyword, condition, new[] { r.Tree });
            });

            var namedBlockRTF = Custom(@"
                ^
                (\w+|\w+\ \w+)                         # 1-2 keywords in the form 'if' or 'else if'
                \s*
                ((?'P'\() .*? (?'-P'\))|)              # optional condition/using vars/etc
                \s*
                (?'B'\\\{)                             # begin balanced braces
                    (?: \s* \\par \s*)?                # allow an rtf newline
                    ((?:                               # capture any number of...
                        (?'Bi'\\\{) | (?'-Bi'\\\}) | . # ... { or } as inner braces, or anything else ungrouped
                    )*?) 
                (?'-B'\\\})                            # end balanced braces
                (?: \s* \\par \s*)?                    # allow another newline
                (?(Bi)(?!))                            # fail if any inner braces are dangling, e.g. @{{}
            ", m =>
            {
                var keyword = m.Groups[1].Value;
                var condition = m.Groups[2].Value;
                var innerContent = m.Groups[3].Value;

                var r = Set("inner-rtfsections", sectionRTF)(innerContent);

                return new Node(NodeType.NamedBlock, keyword, condition, new[] { r.Tree });
            });

            var whitespace = Term("whitespace", @"^\s*");
            var whitespaceRTF = Term("whitespace", @"^(?: \s | \\par )*");

            var blockChain = Set("block-chain", Sequence(Enumerable.First, namedBlock, whitespace));
            var blockChainRTF = Set("block-chain", Sequence(Enumerable.First, namedBlockRTF, whitespaceRTF));

            var code = Any(blockChain, statementBlock, unambiguousExpression, autoclosedExpression);
            var codeRTF = Any(blockChainRTF, statementBlockRTF, unambiguousExpression, autoclosedExpression);

            var sigil = Term("bare-sigil", @"^@");

            var escape = Custom(@"^@@", m => new Node(NodeType.Literal, "escaped-sigil", "@"));

            var codeSection = Set("code", Any(escape, Sequence(Enumerable.Last, sigil, code), sigil));
            var codeSectionRTF = Set("code", Any(escape, Sequence(Enumerable.Last, sigil, codeRTF), sigil));

            var contentSection = Term("content", @"^ [^@]+");

            var rtfMagic = Term("rtf-marker", @"^ \{ \\rtf");

            section = Any(contentSection, codeSection);
            sectionRTF = Any(contentSection, codeSectionRTF);

            return Optional(Any(Sequence("root", rtfMagic, Set("rtfroot", sectionRTF)), Set("root", section)));
        }
    }
}
