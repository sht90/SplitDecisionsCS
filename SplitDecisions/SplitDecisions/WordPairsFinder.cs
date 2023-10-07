namespace SplitDecisions
{
    internal class WordPairsFinder
    {
        private int MinWordLength;
        private int MaxWordLength;
        public WordPairsFinder(BoardSettings settings)
        {
            MinWordLength = settings.MinWordLength;
            MaxWordLength = settings.MaxWordLength;
        }

        public List<WordPair> FindWordPairs(string usableWordsDictionary, string referenceWordsDictionary = "")
        {
            bool sameDictionary = false;
            if (referenceWordsDictionary == "")
            {
                sameDictionary = true;
                referenceWordsDictionary = usableWordsDictionary;
            }
            List<Word> words = GetWords(usableWordsDictionary, referenceWordsDictionary, sameDictionary);
            List<WordPair> wordPairs = GetWordPairs(words);
            return wordPairs;
        }

        private List<Word> GetWords(string usableWordsFile, string referenceWordsFile, bool sameDictionary = false)
        {
            List<Word> words = new() { };
            // Find all the reference words
            string[] lines = File.ReadAllLines(referenceWordsFile);
            int usability = sameDictionary ? 1 : 0;
            foreach (string line in lines)
            {
                // Only add words that are a valid length
                if (line.Length <= this.MaxWordLength && line.Length >= this.MinWordLength)
                {
                    // also, ensure everything has the same capitalization
                    words.Add(new Word(line.ToLower(), usability));
                }
            }
            // Sort the words list
            words.Sort();
            // if the usable dictionary and reference dictionary are the same, you're good to go!
            if (sameDictionary) { return words; }
            // Find all the usable words
            string[] usableLines = File.ReadAllLines(usableWordsFile);
            int index;
            foreach (string line in usableLines)
            {
                // Prune out all words that are too big or too small
                if (line.Length > this.MaxWordLength || line.Length < this.MinWordLength)
                {
                    continue;
                }
                Word usableWord = new(line);
                // Use binary search to find usable words within larger words list.
                index = words.BinarySearch(usableWord);
                // Mark the usable words as usable by incrementing their usability score.
                if (index >= 0)
                {
                    words[index].Usability = 1;
                    continue;
                }
                // If the usable word wasn't in the reference list for some reason, you
                // can still include it.
                usableWord.Usability = 1;
                words.Insert(~index, usableWord);
            }
            // Return a list of words, sorted by length then alphabetical order, with usability metadata
            return words;
        }

        private List<WordPair> GetWordPairs(List<Word> words)
        {
            List<WordPair> wordPairs = new() { };
            // Traverse every possible rotation of every word
            // It's very easy to find WordPairs where the split is at the very end of the words, like visit(ed/or).
            // We rotate the words so that we can find words where the split isn't at the very end of the words.
            // Eg, after one rotation, we catch WordPairs where the split is at the second-to-last index, like exam(in/pl)e.
            // shallow copy
            List<Word> rotatedWords = new() { };
            foreach (Word word in words) { rotatedWords.Add(word); }
            int l;
            for (int r = 0; r < this.MaxWordLength; r++)
            {
                // Traverse each word with the given rotation
                for (int i = 0; i < rotatedWords.Count; i++)
                {
                    Word baseWord = rotatedWords[i];
                    l = baseWord.Letters.Length;
                    // Traverse each subsequent word with the given rotation
                    for (int j = i + 1; j < rotatedWords.Count; j++)
                    {
                        Word nextWord = rotatedWords[j];
                        // If the next word is the wrong length, no subsequent word will be the right length either.
                        if (l != nextWord.Letters.Length) { break; }
                        // If the next word doesn't have its first l-2 letters in common with the base word, no subsequent word will either.
                        if (baseWord.Letters[..(l-2)] != nextWord.Letters[..(l-2)]) { break; }
                        // If the next word has either of the last 2 letters in common with the base word, next word might still match
                        if (baseWord.Letters[l-1] == nextWord.Letters[l-1] || baseWord.Letters[l-2] == nextWord.Letters[l-2]) { continue; }
                        // If the next word made it this far, baseWord and nextWord match.
                        string unrotatedBaseLetters = baseWord.Letters[r..] + baseWord.Letters[..r];
                        string unrotatedNextLetters = nextWord.Letters[r..] + nextWord.Letters[..r];
                        int totalUsability = baseWord.Usability + nextWord.Usability;
                        Shape shape = new(l, l - r - 2);
                        WordPair wordPair = new WordPair(shape, unrotatedBaseLetters, unrotatedNextLetters, totalUsability);
                        wordPairs.Add(wordPair);
                        // Subsequent words might also match baseWord
                    }
                    // After you're done using the baseWord, rotate it for next time
                    if (l - 2 <= r)
                    {
                        // if you're all out of rotations for baseWord, just remove it from the list
                        rotatedWords.RemoveAt(i);
                        i--;
                        continue;
                    }
                    rotatedWords[i].Letters = baseWord.Letters[l - 1] + baseWord.Letters[..(l - 1)];
                }
                // Before you move on to the next rotation, sort the list
                if (rotatedWords.Count > 0)
                {
                    rotatedWords.Sort();
                }
            }
            // sort and return wordPairs
            wordPairs.Sort();
            return wordPairs;
        }

        private class Word : IComparable, IComparable<Word>
        {
            public string Letters;
            public int Usability;
            public Word(string letters, int usability = 0)
            {
                Letters = letters;
                Usability = usability;
            }

            public int CompareTo(object? other)
            {
                if (other == null) { return 1; }
                if (other is Word word) { return this.CompareTo(word); }
                throw new ArgumentException("Object is not a Word");
            }

            public int CompareTo(Word? other)
            {
                if (other == null) { return 1; }
                // In this context, we prefer to sort words first by length...
                int compareByLength = this.Letters.Length.CompareTo(other.Letters.Length);
                if (compareByLength != 0) { return compareByLength; }
                // ...then by alphabetical order
                return this.Letters.CompareTo(other.Letters);
            }
        }
    }
}
