using Antlr4.Runtime;
using Generated;
using MiniGo.Compiler.Encoder;   // ADDED (commit 10): exposes MiniGoEncoder
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

		// Type Checking
		TypeChecker typeChecker = new TypeChecker(collector, filePath);
		typeChecker.Visit(tree);

		// Report all collected errors sorted by source position
		collector.Report(Console.Error);

		if (collector.HasErrors)
		{
			Console.Error.WriteLine($"Compilation failed with {collector.ErrorCount} error(s).");

			// BEFORE (commit 10): the pipeline stopped here in all cases.
			// We exit early only on errors so the encoder never sees a malformed tree.
			return;
		}

		// ── Code generation ───────────────────────────────────────────────────
		// BEFORE (commit 10): this block did not exist. After a successful parse +
		// type-check, the program printed "Compilation completed successfully." and
		// exited without producing any output file.
		//
		// WHAT IS ADDED: a third compiler phase — the LLVM IR encoder.
		//   1. MiniGoEncoder traverses the same parse tree that the TypeChecker used.
		//   2. It emits LLVM IR via LLVMSharp.Interop into an in-memory module.
		//   3. TryVerify() validates the structural integrity of the generated IR.
		//   4. The verified IR is written to a .ll text file next to the source.
		//
		// WHY NOW (not earlier): the encoder must only run when semantic analysis
		// passed. Visiting a tree that contains type errors can produce undefined
		// LLVM values and corrupt the module, so the early return above is the guard.
		//
		// RUNTIME NOTE (LLVMSharp): if the process crashes on the first LLVM call
		// with a DllNotFoundException or SEH exception, add the following to
		// MiniGo.Compiler.csproj inside the first <PropertyGroup>:
		//     <RuntimeIdentifier>win-x64</RuntimeIdentifier>
		// This forces .NET to resolve libLLVM.dll from the correct RID-specific path.

		// Output path: same directory and name as the source, with .ll extension.
		// e.g.  "samples/hello.go"  →  "samples/hello.ll"
		string outputPath = Path.ChangeExtension(filePath, ".ll");

		// MiniGoEncoder is IDisposable (owns LLVMModuleRef + LLVMBuilderRef).
		// The using block guarantees LLVM resources are released even if an
		// exception is thrown during IR generation or verification.
		using var encoder = new MiniGoEncoder(Path.GetFileNameWithoutExtension(filePath));

		try
		{
			// Walk the parse tree; every Visit* method emits the corresponding IR.
			encoder.Visit(tree);

			// Verify + serialise. TryVerify() is called internally by EmitIrToFile
			// via EmitIr(). If verification fails it throws InvalidOperationException
			// with the LLVM diagnostic message — surfaced below.
			encoder.EmitIrToFile(outputPath);

			Console.Error.WriteLine("Compilation completed successfully.");
			Console.Error.WriteLine($"LLVM IR written to: {outputPath}");
		}
		catch (InvalidOperationException ex)
		{
			// LLVM module verification failed — this is a bug in the encoder, not
			// in the user's MiniGo source (semantic analysis already passed).
			Console.Error.WriteLine($"LLVM IR verification error (encoder bug):\n{ex.Message}");
		}
	}
}