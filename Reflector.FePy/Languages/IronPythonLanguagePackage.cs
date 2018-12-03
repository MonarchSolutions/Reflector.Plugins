using System;

namespace Reflector.FePy.Languages
{
	internal class IronPythonLanguagePackage : IPackage
	{
		public void Load(IServiceProvider serviceProvider)
		{
			_ironPythonLanguage = new IronPythonLanguage(true);
			_languageManager = (ILanguageManager)serviceProvider.GetService(typeof(ILanguageManager));
			for (int i = _languageManager.Languages.Count - 1; i >= 0; i--)
			{
				if (_languageManager.Languages[i].Name == "IronPython")
				{
					_languageManager.UnregisterLanguage(_languageManager.Languages[i]);
				}
			}
			_languageManager.RegisterLanguage(_ironPythonLanguage);
		}

		public void Unload()
		{
			_languageManager.UnregisterLanguage(_ironPythonLanguage);
		}

		private ILanguageManager _languageManager;

		private IronPythonLanguage _ironPythonLanguage;
	}
}
