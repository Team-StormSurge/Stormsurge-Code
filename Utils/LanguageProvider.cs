using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Utils
{
    public static class LanguageProvider
    {
        public static Dictionary<Language,List<LanguagePair>> LanguagePairs = new();
        public static List<LanguagePair> unusedPairs = new();
        public class LanguagePair
        {
            public string textToken;
            public string ingameText;
            public Language? tokenLanguage;
            public LanguagePair(string token, string name)
            {
                textToken = token;
                ingameText = name;
                unusedPairs.Add(this);
            }
        }

        [SystemInitializer]
        public static void SubscribeForLanguage()
        {
            foreach(LanguagePair pair in unusedPairs)
            {
                pair.tokenLanguage ??= Language.english;
                if (!LanguagePairs.ContainsKey(pair.tokenLanguage)) LanguagePairs.Add(pair.tokenLanguage, new List<LanguagePair>());
                LanguagePairs[pair.tokenLanguage].Add(pair);
            }
            Language.onCurrentLanguageChanged += Language_onCurrentLanguageChanged;
            Language_onCurrentLanguageChanged();
        }

        private static void Language_onCurrentLanguageChanged()
        {
            if(!LanguagePairs.ContainsKey(Language.currentLanguage)) LanguagePairs.Add(Language.currentLanguage, new List<LanguagePair>());
            var currentLanguageList = LanguagePairs[Language.currentLanguage];
            foreach(LanguagePair pair in currentLanguageList)
            {
                Language.currentLanguage.SetStringByToken(pair.textToken, pair.ingameText);
            }
        }
    }
}
