namespace SplitDecisions
{
    internal class WordPair : IComparable, IComparable<WordPair>
    {
        public Shape Shape;
        public string[] Words;
        public string[] Splits;
        public string Letters;
        public string Before;
        public string After;
        public int Usability;
        public List<int> Mistakeables;
        public List<List<bool>> Anchors;

        public WordPair(Shape shape, string word1, string word2, int usability)
        {
            this.Shape = shape;
            this.Words = new string[] { word1, word2 };
            this.Splits = new string[] {
                word1[shape.Index..(shape.Index+2)],
                word2[shape.Index..(shape.Index+2)]
            };
            this.Before = word1[..shape.Index];
            this.After = word1[(shape.Index + 2)..];
            this.Letters = Before + After;
            this.Mistakeables = new List<int> { };
            this.Anchors = new List<List<bool>> { };
            this.Usability = usability;
        }

        public string this[int key]
        {
            get => GetValue(key);
        }

        public string GetValue(int key)
        {
            if (key < this.Shape.Index) return this.Letters[key].ToString();
            if (key == Shape.Index) return Splits[0];
            if (key == Shape.Index + 1) return Splits[1];
            return this.Letters[key - 2].ToString();
        }

        public override string ToString()
        {
            return String.Format("{0}({1}/{2}){3}", Before, Splits[0], Splits[1], After);
        }

        public string GetPrompt()
        {
            string tmpBefore = String.Concat("-", Before.Length);
            string tmpAfter = String.Concat("-", After.Length);
            return String.Format("{0}({1}/{2}){3}", tmpBefore, Splits[0], Splits[1], tmpAfter);
        }

        public string ShowMistakeables()
        {
            string mistakeablesString = "[ ";
            foreach (int code in Mistakeables)
            {
                if (code == 0)
                {
                    mistakeablesString += "[] ";
                    continue;
                }
                mistakeablesString += "[";
                List<char> letters = LetterCode.Decode(code);
                foreach (char letter in letters)
                {
                    mistakeablesString += letter;
                }
                mistakeablesString += "] ";
            }
            mistakeablesString += "]";
            return mistakeablesString;
        }

        public string ShowAnchors()
        {
            string anchorsString = "[ ";
            foreach (List<bool> anchor in Anchors)
            {
                anchorsString += "[ ";
                foreach (bool a in anchor)
                {
                    anchorsString += a ? "1" : "0";
                }
                anchorsString += " ] ";
            }
            anchorsString += "]";
            return anchorsString;
        }

        public int CompareTo(object? other)
        {
            if (other == null)
            {
                return 1;
            }
            if (other is WordPair wordPair)
            {
                return this.CompareTo(wordPair);
            }
            throw new ArgumentException("Object is not a WordPair");
        }

        public int CompareTo(WordPair? other)
        {
            if (other == null)
            {
                return 1;
            }
            // Compare WordPairs by shape
            int compareByShape = this.Shape.CompareTo(other.Shape);
            if (compareByShape != 0)
            {
                return compareByShape;
            }
            // Compare WordPairs by prompt
            int compareByPrompt;
            for (int i = 0; i < this.Splits.Length; i++)
            {
                compareByPrompt = this.Splits[i].CompareTo(other.Splits[i]);
                if (compareByPrompt != 0)
                {
                    return compareByPrompt;
                }
            }
            // Compare WordPairs by solution
            return this.Letters.CompareTo(other.Letters);
        }
    }

    internal class BoardWordPair : WordPair
    {
        public Placement Placement { get; set; }
        public BoardWordPair(Shape shape, string word1, string word2, int usability, Placement placement) : base(shape, word1, word2, usability)
        {
            Placement = placement;
        }

        public BoardWordPair(WordPair wordPair, Placement placement) : this(wordPair.Shape, wordPair.Words[0], wordPair.Words[1], wordPair.Usability, placement) { }

        public bool ContainsCell(int row, int col)
        {
            // assume that the cell you're analyzing is actually on the board
            // if this BoardWordPair was ever made at all, that should always be true.
            bool horizontalRowCheck = row == Placement.Row;
            bool horizontalColCheck = col >= Placement.Col && col <= Shape.Length;
            bool verticalRowCheck = row >= Placement.Row && row <= Shape.Length;
            bool verticalColCheck = col == Placement.Col;
            return (Placement.Dir == Orientation.Horizontal && horizontalColCheck && horizontalRowCheck) || (Placement.Dir == Orientation.Vertical && verticalColCheck && verticalRowCheck);
        }

        public int AtFull(int row, int col)
        {
            if (!ContainsCell(row, col)) return -1;
            if (Placement.Dir == Orientation.Horizontal)
            {
                return col - Placement.Col;
            }
            return row - Placement.Row;
        }

        public int AtSingles(int row, int col)
        {
            int at = AtFull(row, col);
            if (at < 0 || at == Shape.Index || at == Shape.Index + 1) return -1;
            return at;
        }

        public int At(int row, int col, bool full = true)
        {
            return full ? AtFull(row, col) : AtSingles(row, col);
        }

        public int MistakeablesAt(int row, int col)
        {
            return Mistakeables[AtSingles(row, col)];
        }
    }
}
