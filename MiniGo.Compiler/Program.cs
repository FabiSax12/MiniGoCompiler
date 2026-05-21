using Antlr4.Runtime;
using Generated;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Errors.Listeners;
using MiniGo.Compiler.Semantic;

namespace MiniGo.Compiler;

public static class Program
{
	public static void Main(string[] args)
	{
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

		var collector = new ErrorCollector(filePath);

		// Lexer: replace default error listeners with our custom one
		lexer.RemoveErrorListeners();
		lexer.AddErrorListener(new LexerErrorListener(collector));

		CommonTokenStream tokens = new CommonTokenStream(lexer);
		MiniGoParser parser = new MiniGoParser(tokens);

		// Parser: replace default error listeners with our custom one
		parser.RemoveErrorListeners();
		parser.AddErrorListener(new ParserErrorListener(collector));

		// Parser: use our custom error strategy (delimiter-based recovery)
		parser.ErrorHandler = new MiniGoErrorStrategy();

		MiniGoParser.RootContext tree = parser.root();

		// Report all collected errors sorted by source position
		collector.Report(Console.Error);

		if (collector.HasErrors)
		{
			Console.WriteLine($"Parsing failed with {collector.ErrorCount} error(s).");
		}
		else
		{
			Console.WriteLine("Parsing completed successfully.");
		}

		// Type Checking
		TypeChecker typeChecker = new TypeChecker();
		typeChecker.Visit(tree);

	}
}