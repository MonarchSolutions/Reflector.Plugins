using System;
using Reflector.CodeModel;
using Reflector.Disassembler;
using Reflector.Emit.Languages;

namespace Reflector.Emit.ReflectionEmitLanguage
{
    internal sealed class EmitLanguage : ILanguage
    {
        public EmitLanguage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Name => "Reflection.Emit";

        public string FileExtension => "cs";

        public bool Translate => false;

        ILanguageWriter ILanguage.GetWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
        {
            return new EmitLanguageWriter(_serviceProvider, new EmitFormatter(formatter), configuration);
        }

        public Language LanguageType => Language.IlNet;

        private readonly IServiceProvider _serviceProvider;
    }
}
