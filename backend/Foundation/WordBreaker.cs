using System;
using System.Collections.Generic;

namespace BizSrt.Api.Foundation
{
    public class WordBreaker
    {
        public enum ChangeCasing : byte
        {
            Preserve = 0,
            Capitalize = 1,
            Lowercase = 2
        }

        public enum WordType : byte
        {
            Separator = 0,
            Word = 1,
            Number = 2,
            Punctuation = 2,
        }

        [Flags]
        public enum ParseOptions : byte
        {
            Default = 1,
            Sentence = PreserveNumbers + PreservePunctuation,
            Preserve = PreserveCasing + PreserveNumbers + PreservePunctuation,
            PreserveCasing = 1,
            PreserveNumbers = 2,
            PreservePunctuation = 4
        }

        public static ICollection<string> Parse(string text)
        {
            return Parse(text, null);
        }

        public static ICollection<string> Parse(string text, ParseOptions options)
        {
            return Parse(text, null, options);
        }

        public static ICollection<string> Parse(string text, ICollection<string>? words)
        {
            return Parse(text, words, ParseOptions.Default);
        }

        public static ICollection<string> Parse(string text, ICollection<string>? words, ParseOptions options)
        {
            if(words == null)
                words = new List<string>();

            if (!string.IsNullOrWhiteSpace(text))
            {
                int start = -1, index = 0;
                char character;
                WordType currentWord = WordType.Separator;
                while (true)
                {
                    character = text[index];
                    if (char.IsLetter(character) || (currentWord == WordType.Word && (options & ParseOptions.PreservePunctuation) > 0 && char.IsPunctuation(character)))
                    {
                        if (currentWord != WordType.Word && addWord(words, text, start, index, (options & ParseOptions.PreserveCasing) == 0 ? (words.Count == 0 ? ChangeCasing.Capitalize : ChangeCasing.Lowercase) : ChangeCasing.Preserve))
                            start = -1;

                        currentWord = WordType.Word;
                        if (start < 0)
                            start = index;
                    }
                    else if ((char.IsNumber(character) && (options & ParseOptions.PreserveNumbers) > 0) ||
                        (currentWord == WordType.Number && (options & ParseOptions.PreservePunctuation) > 0 && char.IsPunctuation(character)))
                    {
                        if (currentWord != WordType.Number && addWord(words, text, start, index, (options & ParseOptions.PreserveCasing) == 0 ? (words.Count == 0 ? ChangeCasing.Capitalize : ChangeCasing.Lowercase) : ChangeCasing.Preserve))
                            start = -1;

                        currentWord = WordType.Number;
                        if (start < 0)
                            start = index;
                    }
                    else if (char.IsPunctuation(character) && (options & ParseOptions.PreservePunctuation) > 0)
                    {
                        if (currentWord != WordType.Punctuation && addWord(words, text, start, index, (options & ParseOptions.PreserveCasing) == 0 ? (words.Count == 0 ? ChangeCasing.Capitalize : ChangeCasing.Lowercase) : ChangeCasing.Preserve))
                            start = -1;

                        currentWord = WordType.Punctuation;
                        if (start < 0)
                            start = index;
                    }
                    else //if (Char.IsWhiteSpace(letter) || Char.IsSeparator(letter))
                    {
                        if (currentWord != WordType.Separator && addWord(words, text, start, index, (options & ParseOptions.PreserveCasing) == 0 ? (words.Count == 0 ? ChangeCasing.Capitalize : ChangeCasing.Lowercase) : ChangeCasing.Preserve))
                            start = -1;

                        currentWord = WordType.Separator;
                    }

                    index++;

                    if (index >= text.Length)
                    {
                        if ((currentWord != WordType.Separator) && start >= 0)
                            addWord(words, text, start, index, (options & ParseOptions.PreserveCasing) == 0 ? (words.Count == 0 ? ChangeCasing.Capitalize : ChangeCasing.Lowercase) : ChangeCasing.Preserve);

                        break;
                    }
                }
            }

            return words;
        }

        static bool addWord(ICollection<string> words, string text, int start, int index, ChangeCasing casing)
        {
            if (start >= 0 && index > start)
            {
                switch (casing)
                {
                    case ChangeCasing.Capitalize:
                        if (index - 1 > start)
                            words.Add((char.IsUpper(text[start]) ? text[start] : char.ToUpper(text[start])) + text.Substring(start + 1, index - start - 1).ToLower());
                        else 
                            words.Add((char.IsUpper(text[start]) ? text[start] : char.ToUpper(text[start])).ToString());
                        break;
                    case ChangeCasing.Lowercase:
                        words.Add(text.Substring(start, index - start).ToLower());
                        break;
                    default:
                        words.Add(text.Substring(start, index - start));
                        break;
                }
                return true;
            }
            else
                return false;
        }

        public static IEnumerable<string> Parse(string[] text)
        {
            var words = new HashSet<string>();

            foreach (string part in text)
            {
                Parse(part, words, ParseOptions.Default);
            }

            return words;
        }
    }
}
