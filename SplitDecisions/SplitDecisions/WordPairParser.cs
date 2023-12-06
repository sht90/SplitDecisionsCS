using System.Diagnostics.Metrics;

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
            string before = parts[0].Substring(0, openParenIndex);
            string split1 = parts[0].Substring(openParenIndex + 1, slashIndex - (openParenIndex + 1));
            string split2 = parts[0].Substring(slashIndex + 1, closeParenIndex - (slashIndex + 1));
            string after = parts[0].Substring(closeParenIndex + 1, parts[0].Length - (closeParenIndex + 1));
            // technically this is enough to get a wordPair, but it won't have the anchors or mistakeables yet
            WordPair wp = new(new Shape(before.Length + after.Length + 2, before.Length), before + split1 + after, before + split2 + after, 2);
            // TODO:
            // parse the mistakeables
            // parse the anchor conditions
            return wp;
		}

        internal static List<WordPair> LoadFile(string fileName)
        {
            StreamReader file = new(fileName);
            string? line;
            List<WordPair> retVal = new() { };
            while ((line = file.ReadLine()) != null)
            {
                retVal.Append(Parse(line));
            }
            return retVal;
        }
	}
}

