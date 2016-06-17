using Microsoft.CodeAnalysis;

namespace gillette
{
    public sealed class ValidationError
    {
        private readonly Diagnostic _Diagnostic;

        internal ValidationError(Diagnostic diagnostic)
        {
            _Diagnostic = diagnostic;
        }

        public override string ToString()
        {
            return _Diagnostic.ToString();
        }
    }
}
