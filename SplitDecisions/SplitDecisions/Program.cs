// See https://aka.ms/new-console-template for more information
using SplitDecisions;

Console.WriteLine("Hello World");
System.Diagnostics.Stopwatch watch = new();

// Set some constants. These should probably be inputs/arguments/configs in the future.
// board dimensions
int BOARD_LENGTH = 13;
int BOARD_WIDTH = 13;
// other board settings
int MINIMUM_USABILITY = (int)Usability.BOTH_WORDS;
int MINIMUM_WORD_LENGTH = 3;
int MAXIMUM_WORD_LENGTH = 10;
BoardSettings settings = new(MINIMUM_USABILITY, BOARD_WIDTH, BOARD_LENGTH, MINIMUM_WORD_LENGTH, MAXIMUM_WORD_LENGTH);
// dictionaries (in the literal sense, not like the Dictionary<A,B> sense)
string USABLE_DICTIONARY_PATH = "/Users/samtaylor/Documents/SplitDecTextFiles/UsableDictionary.txt";
string REFERENCE_DICTIONARY_PATH = "/Users/samtaylor/Documents/SplitDecTextFiles/ReferenceDictionary.txt";
string CONSTRAINED_WORDPAIRS_LOG = "/Users/samtaylor/Documents/SplitDecTextFiles/ConstrainedWordPairs.txt";

// Find all possible word pairs from the starting dictionaries
watch.Start();
WordPairsFinder wordPairsFinder = new(settings);
List<WordPair> wordPairs = wordPairsFinder.FindWordPairs(USABLE_DICTIONARY_PATH, REFERENCE_DICTIONARY_PATH);
watch.Stop();
Console.WriteLine("Found " + wordPairs.Count.ToString() + " word pairs in " + watch.ElapsedMilliseconds.ToString() + " milliseconds.");

// Find constraints for usable word pairs
watch.Start();
ConstraintsFinder constraintsFinder = new(settings);
List<WordPair> boardWordPairs = constraintsFinder.FindConstraints(wordPairs);
watch.Stop();
Console.WriteLine("Found constraints in " + watch.ElapsedMilliseconds.ToString() + " milliseconds.");
// Write results to corresponding files
watch.Start();
string constrainedWordPairsLog = "";
foreach (WordPair wordPair in boardWordPairs) { constrainedWordPairsLog += wordPair.ToString() + "  " + wordPair.ShowMistakeables() + "  " + wordPair.ShowAnchors() + "\n"; }
File.WriteAllText(CONSTRAINED_WORDPAIRS_LOG, constrainedWordPairsLog);
watch.Stop();
Console.WriteLine("Wrote to file in " + watch.ElapsedMilliseconds.ToString() + " milliseconds.");

// Make the board