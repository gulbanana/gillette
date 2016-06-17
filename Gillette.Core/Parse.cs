using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gillette
{
    internal delegate ParseResult Parser(string input);

    internal static class Parse
    {
        const RegexOptions _RegexOpts = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace;

        public static Match Consume(ref string text, Regex pattern)
        {
            var m = pattern.Match(text);

            if (m.Success)
            {
                text = text.Substring(m.Value.Length);
                return m;
            }
            else
            {
                return null;
            }
        }

        public static string Consume(ref string text, string pattern)
        {
            if (text.StartsWith(pattern))
            {
                text = text.Substring(pattern.Length);
                return pattern;
            }
            else
            {
                return null;
            }
        }

        public static Parser Term(string name, Regex pattern)
        {
            if (pattern == null) throw new ArgumentNullException("pattern");

            return text =>
            {
                var m = Consume(ref text, pattern);
                if (m == null) return ParseResult.Failure(text, pattern.ToString());

                return ParseResult.Success(new Node(NodeType.Literal, name, m.Value), text);
            };
        }

        public static Parser Term(string name, string uncompiledPattern)
        {
            return Term(name, new Regex(uncompiledPattern, _RegexOpts));
        }

        public static Parser Optional(Parser p)
        {
            return text =>
            {
                var r = p(text);
                if (r.IsSuccess)
                    return r;
                else
                    return ParseResult.Empty(r.Remainder);
            };
        }

        public static Parser Sequence(Func<IEnumerable<Node>, Node> reduce, params Parser[] parsers)
        {
            return text =>
            {
                var nodes = new List<Node>();

                foreach (var p in parsers)
                {
                    var r = p(text);
                    if (!r.IsSuccess) return r;
                    nodes.Add(r.Tree);
                    text = r.Remainder;
                }

                return ParseResult.Success(reduce(nodes), text);
            };
        }

        public static Parser Sequence(string name, params Parser[] parsers)
        {
            return Sequence(nodes => new Node(NodeType.Sequence, name, null, nodes), parsers);
        }

        public static Parser Set(string name, Parser parser)
        {
            return text =>
            {
                var nodes = new List<Node>();

                ParseResult r;
                do
                {
                    r = parser(text);
                    if (r.IsSuccess)
                    {
                        nodes.Add(r.Tree);
                        text = r.Remainder;
                    }
                } while (r.IsSuccess);

                if (nodes.Count == 0)
                    return ParseResult.Failure(text, $"SET OF ({parser("").Expected})");
                else if (nodes.Count == 1)
                    return ParseResult.Success(nodes.Single(), text);
                else
                    return ParseResult.Success(new Node(NodeType.Sequence, name, null, nodes), text);
            };
        }

        public static Parser Any(params Parser[] parsers)
        {
            return text =>
            {
                ParseResult r;
                var failures = new List<string>();
                foreach (var p in parsers)
                {
                    r = p(text);
                    if (r.IsSuccess)
                        return r;
                    else
                        failures.Add(r.Expected);
                }

                return ParseResult.Failure(text, $"ANY OF ({string.Join(",", failures)})");
            };
        }

        public static Parser Custom(Regex pattern, Func<Match, Node> map)
        {
            if (pattern == null) throw new ArgumentNullException("pattern");

            return text =>
            {
                var m = Consume(ref text, pattern);
                if (m == null) return ParseResult.Failure(text, pattern.ToString());

                return ParseResult.Success(map(m), text);
            };
        }

        public static Parser Custom(string uncompiledPattern, Func<Match, Node> map)
        {
            return Custom(new Regex(uncompiledPattern, _RegexOpts), map);
        }
    }
}
