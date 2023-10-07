namespace SplitDecisions
{
    internal static class LetterCode
    {
        private static readonly Dictionary<char, int> Encoder = Enumerable.Range(0, 26).ToDictionary(x => (char)((int)'a' + x), x => 1 << x);
        private static readonly Dictionary<int, char> Decoder = Enumerable.Range(0, 26).ToDictionary(x => x, x => (char)((int)'a' + x));

        public static int Encode(char letter)
        {
            return Encoder[letter];
        }

        public static List<char> Decode(int code)
        {
            List<char> letters = new() { };
            for (int i = 0; i < 26; i++)
            {
                if ((code & (1 << i)) > 0)
                {
                    letters.Add(Decoder[i]);
                }
            }
            return letters;
        }
    }
}
