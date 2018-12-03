using System.Globalization;
using System.IO;
using Reflector.CodeModel;

namespace Reflector.Ps.Languages
{
    internal class TextFormatter : IFormatter
    {
        public override string ToString()
        {
            return _writer.ToString();
        }

        public void WriteCustom(string value)
        {
            //TODO:
            Write(value);
        }

        public void Write(string text)
        {
            ApplyIndent();
            _writer.Write(text);
        }

        public void WriteDeclaration(string text)
        {
            WriteBold(text);
        }

        public void WriteDeclaration(string text, object target)
        {
            WriteBold(text);
        }

        public void WriteComment(string text)
        {
            WriteColor(text, 0x808080);
        }

        public void WriteLiteral(string text)
        {
            WriteColor(text, 0x800000);
        }

        public void WriteKeyword(string text)
        {
            WriteColor(text, 0x80);
        }

        public void WriteIndent()
        {
            indent++;
        }

        public void WriteLine()
        {
            _writer.WriteLine();
            newLine = true;
        }

        public void WriteOutdent()
        {
            indent--;
        }

        public void WriteReference(string text, string toolTip, object reference)
        {
            ApplyIndent();
            _writer.Write(text);
        }

        public void WriteProperty(string propertyName, string propertyValue)
        {
        }

        private void WriteBold(string text)
        {
            ApplyIndent();
            _writer.Write(text);
        }

        private void WriteColor(string text, int color)
        {
            ApplyIndent();
            _writer.Write(text);
        }

        private void ApplyIndent()
        {
            if (newLine)
            {
                for (int i = 0; i < indent; i++)
                {
                    _writer.Write("    ");
                }
                newLine = false;
            }
        }

        private StringWriter _writer = new StringWriter(CultureInfo.InvariantCulture);

        private bool newLine;

        private int indent = 0;
    }
}
