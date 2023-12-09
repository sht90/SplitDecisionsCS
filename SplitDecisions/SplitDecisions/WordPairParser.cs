namespace SplitDecisions
{
    internal static class WordPairParser
	{
        /// <summary>
        /// Expects a string to look like: "b(as/yt)e  [ [m] [h] ]  [ [ 10 ] [ 01 ] ]"
        /// as in "{before}({double1}/{double2}){after}  [ [{mistakeables[0]}] {repeat for each mistakeable} ]  [ [{anchors[0]}] {repeat for all anchors} ]
        /// </summary> 
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        internal static WordPair Parse(string inputString)
		{
            String[] parts = inputString.Split("  ");
            // parse the l(in/os)e part of the string. This is enough to make the initial WordPair
            // first, find where (, /, and ) are
            parts[0] = parts[0].Trim();
            int openParenIndex = parts[0].IndexOf('(');
            int slashIndex = parts[0].IndexOf('/');
            int closeParenIndex = parts[0].IndexOf(')');
            string before = parts[0][..openParenIndex];
            string split1 = parts[0][(openParenIndex + 1)..slashIndex];
            string split2 = parts[0][(slashIndex + 1)..closeParenIndex];
            string after = parts[0][(closeParenIndex + 1)..];
            // technically this is enough to get a wordPair, but it won't have the anchors or mistakeables yet
            WordPair wp = new(new Shape(before.Length + after.Length + 2, before.Length), before + split1 + after, before + split2 + after, 2);
            // parse the mistakeables
            // trim off outer brackets from mistakeables portion
            parts[1] = parts[1][2..^2];
            List<int> mistakeables = new() { };
            // now, for each new set of brackets, make an int to store all the letters
            foreach (char c in parts[1])
            {
                if (c == '[')
                {
                    mistakeables.Add(0);
                    continue;
                }
                else if (c == ']' || c == ' ')
                {
                    continue;
                }
                mistakeables[^1] += LetterCode.Encode(c);
            }
            // finally add mistakeables to the wordpair
            wp.Mistakeables = mistakeables;
            // parse the anchor conditions
            // trim off outer brackets from anchor portion
            parts[2] = parts[2][2..^2];
            List<List<bool>> anchors = new() { };
            // now, for each new set of brackets, make a list of bools
            foreach (char c in parts[2])
            {
                if (c == '[')
                {
                    anchors.Add(new List<bool>() { });
                    continue;
                }
                else if (c == ']' || c == ' ')
                {
                    continue;
                }
                // I feel like there's a more compact way to write this
                if (c == '0')
                {
                    anchors[^1].Add(false);
                }
                else if (c == '1')
                {
                    anchors[^1].Add(true);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            // add anchors to wordpair
            wp.Anchors = anchors;
            return wp;
		}

        internal static List<WordPair> LoadFile(string fileName)
        {
            StreamReader file = new(fileName);
            string? line;
            List<WordPair> retVal = new() { };
            while ((line = file.ReadLine()) != null)
            {
                retVal.Add(Parse(line));
            }
            return retVal;
        }
	}
}

