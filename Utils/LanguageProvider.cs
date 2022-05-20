using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Utils
{
    public static class LanguageProvider
    {
        public static Dictionary<Language,List<LanguagePair>> LanguagePairs = new();
        public class LanguagePair
        {
            public string textToken;
            public string ingameText;
            public Language tokenLanguage;
            public LanguagePair(string token, string name, Language? language = null)
            {
                textToken = token;
                ingameText = name;
                tokenLanguage = language ?? Language.english;
                LanguagePairs[tokenLanguage] ??= new List<LanguagePair>();
                LanguagePairs[tokenLanguage].Add(this);
            }
        }

        [SystemInitializer]
        public static void SubscribeForLanguage()
        {
            Language.onCurrentLanguageChanged += Language_onCurrentLanguageChanged;
        }

        private static void Language_onCurrentLanguageChanged()
        {
            var currentLanguageList = LanguagePairs[Language.currentLanguage];
            foreach(LanguagePair pair in currentLanguageList)
            {
                UnityEngine.Debug.LogWarning($"Pairing {pair.textToken}:{pair.ingameText} for language {Language.currentLanguageName}");
                Language.currentLanguage.SetStringByToken(pair.textToken, pair.ingameText);
            }
        }
    }
}
