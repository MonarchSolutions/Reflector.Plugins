using System;

namespace Reflector.Js.Languages
{
    internal class JavaScriptLanguagePackage : IPackage
    {
        private ILanguageManager languageManager;
        private JavaScriptLanguage delphiLanguage;

        public void Load(IServiceProvider serviceProvider)
        {
            delphiLanguage = new JavaScriptLanguage(true);
            //this.delphiLanguage.VisibilityConfiguration = (IVisibilityConfiguration) serviceProvider.GetService(typeof(IVisibilityConfiguration));
            //this.delphiLanguage.FormatterConfiguration = (IFormatterConfiguration) serviceProvider.GetService(typeof(IFormatterConfiguration));

            languageManager = (ILanguageManager)serviceProvider.GetService(typeof(ILanguageManager));

            for (int i = languageManager.Languages.Count - 1; i >= 0; i--)
            {
                if (languageManager.Languages[i].Name == "JavaScript")
                {
                    languageManager.UnregisterLanguage(languageManager.Languages[i]);
                }
            }

            languageManager.RegisterLanguage(delphiLanguage);
        }

        public void Unload()
        {
            languageManager.UnregisterLanguage(delphiLanguage);
        }
    }
}
