// See https://aka.ms/new-console-template for more information

using Antlr4.Runtime;
using Generated;

if (args.Length == 0)
{
    Console.WriteLine("Usage: MiniGo.Compiler <path-to-source-file>");
    return;
}

string filePath = args[0];

if (!File.Exists(filePath))
{
    Console.WriteLine($"Error: File not found: {filePath}");
    return;
}

string input = File.ReadAllText(filePath);

ICharStream stream = CharStreams.fromString(input);
MiniGoLexer lexer = new MiniGoLexer(stream);
CommonTokenStream tokens = new CommonTokenStream(lexer);
MiniGoParser parser = new MiniGoParser(tokens);

parser.RemoveErrorListeners();
parser.AddErrorListener(new ConsoleErrorListener<IToken>());

MiniGoParser.RootContext tree = parser.root();

if (parser.NumberOfSyntaxErrors == 0)
{
    Console.WriteLine("Parsing completed successfully.");
}
else
{
    Console.WriteLine($"Parsing failed with {parser.NumberOfSyntaxErrors} error(s).");
}
