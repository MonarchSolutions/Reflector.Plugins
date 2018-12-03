using Reflector.CodeModel;
using Reflector.Disassembler;

namespace Reflector.Ps.Languages
{
    internal sealed class PowerShellLanguage : ILanguage
    {
        public string FileExtension => "ps1";

        public ILanguageWriter GetWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
        {
            return new PowerShellLanguageWriter(formatter, configuration);
        }

        public Language LanguageType => Language.CSharpNet;

        public string Name => "PowerShell";

        public bool Translate => true;
    }
}
