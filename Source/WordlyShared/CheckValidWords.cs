using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WordGame
{
    public class CheckValidWords
    {
        List<string> validWords = new List<string>();

        public void LoadWords()
        {       
            var type = this.GetType();

            
            for (char c = 'A'; c <= 'Z'; c++)
            { 
                var resource = "WordGame.ValidWords." + c + " Words.csv";
#if WINDOWS_UAP
                var stream = type.GetTypeInfo().Assembly.GetManifestResourceStream(resource);
#else
                var stream = type.Assembly.GetManifestResourceStream(resource);
#endif
                var reader = new StreamReader(stream);                
                var doc = reader.ReadToEnd();

                
                validWords.Add(doc.Replace("\r\n", " "));
            }
        }

        public bool IsWordValidNoWildcards(string word)
        {
            if (word.Length < 2)
                return false;

            word = word.ToLower();
            var indexToCheck = (int)word[0] - (int)'a';

            if (validWords.Count == 0)
                LoadWords();

            word = "\n" + word + "\n";
          
            
            bool foundWord = validWords[indexToCheck].Contains(word);            
            

            return foundWord;
        }

        public bool IsWordValid(string word)
        {
            if (word.Length < 2)
                return false;

            word = word.ToLower();
            string pattern = "\\b" + word.ToLower() + "\\b";
            if (validWords.Count == 0)
                LoadWords();

            bool foundWord = false;
            if (word[0] != '.')
            {
                var indexToCheck = (int)word[0] - (int)'a';

                foundWord = Regex.IsMatch(validWords[indexToCheck], pattern);
            }
            else
            {
                for (int i=0; i < validWords.Count; i++)
                {
                    foundWord= Regex.IsMatch(validWords[i], pattern);
                    if (foundWord)
                        return foundWord;
                }

            }

            return foundWord;
        }
        
    }
}
