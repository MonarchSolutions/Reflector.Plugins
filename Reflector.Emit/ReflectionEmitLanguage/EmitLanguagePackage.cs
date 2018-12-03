using System;

namespace Reflector.Emit.ReflectionEmitLanguage
{
	public sealed class EmitLanguagePackage : IPackage
	{
		public void Load(IServiceProvider serviceProvider)
		{
			_language = new EmitLanguage(serviceProvider);
			_languageManager = (ILanguageManager)serviceProvider.GetService(typeof(ILanguageManager));
			_languageManager.RegisterLanguage(_language);
		}

		public void Unload()
		{
			_languageManager.UnregisterLanguage(_language);
		}

		private ILanguageManager _languageManager;

		private EmitLanguage _language;
	}
}
