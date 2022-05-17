using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Utils
{
    public static class LanguageProvider
    {
        public static List<LanguagePair> LanguagePairs = new List<LanguagePair>();
        public class LanguagePair
        {
            public string textToken;
            public string ingameText;
            public Language tokenLanguage;
            public LanguagePair(string token, string name, Language language = null)
            {
                textToken = token;
                ingameText = name;
                tokenLanguage = language ?? Language.english;
                LanguagePairs.Add(this);
            }
        }

        [SystemInitializer]
        public static void AddLanguagePairs()
        {
            foreach(LanguagePair pair in LanguagePairs)
            {
                pair.tokenLanguage.SetStringByToken(pair.textToken, pair.ingameText);
            }
        }

    }
}
