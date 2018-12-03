using System;

namespace Reflector.Ps.Languages
{
	public class PowerShellLanguagePackage : IPackage
	{
		public void Load(IServiceProvider serviceProvider)
		{
			_language = new PowerShellLanguage();
			_languageManager = (ILanguageManager)serviceProvider.GetService(typeof(ILanguageManager));
			_languageManager.RegisterLanguage(_language);
		}

		public void Unload()
		{
			_languageManager.UnregisterLanguage(_language);
		}

		private ILanguageManager _languageManager;

		private PowerShellLanguage _language;
	}
}
