using Antlr4.Runtime;
using Generated;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Errors.Listeners;
using MiniGo.Compiler.Semantic;

namespace MiniGo.Compiler.Tests.Pipeline;

public sealed class IntegrationTests
{
	[Fact]
	public void FullPipeline_ValidProgram_NoErrorsAcrossPhases()
	{
		var collector = Compile("package main;\nfunc add(a int, b int) int {\nreturn a + b;\n};\nfunc main() {\nvar x int = add(1, 2);\nvar y int = add(3, 4);\nvar z int = x + y;\nprint(z);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void FullPipeline_ComplexProgram_NoErrors()
	{
		var collector = Compile("package main;\nfunc fib(n int) int {\nif n <= 1 {\nreturn n;\n};\nreturn fib(n-1) + fib(n-2);\n};\nfunc main() {\nvar result int = fib(10);\nprint(result);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void FullPipeline_SyntaxError_Detected()
	{
		var collector = Compile("package main;\nfunc main() {\nprint(1 +);\n};");

		collector.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void FullPipeline_StructType_Valid()
	{
		var collector = Compile("package main;\ntype Point struct {\nx int;\ny int;\n};\nfunc main() {\nvar p Point;\np.x = 10;\np.y = 20;\nprint(p.x);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void FullPipeline_TypeAlias_Valid()
	{
		var collector = Compile("package main;\ntype Celsius float;\nfunc main() {\nvar temp Celsius = 36.6;\nprint(temp);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void FullPipeline_Println_Valid()
	{
		var collector = Compile("package main;\nfunc main() {\nprintln(\"hello\");\nprintln(42);\nprintln(3.14);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	private static ErrorCollector Compile(string source)
	{
		var input = CharStreams.fromString(source);
		var lexer = new MiniGoLexer(input);
		lexer.RemoveErrorListeners();

		var collector = new ErrorCollector("inline");
		lexer.AddErrorListener(new LexerErrorListener(collector));

		var tokens = new CommonTokenStream(lexer);
		var parser = new MiniGoParser(tokens);
		parser.RemoveErrorListeners();
		parser.AddErrorListener(new ParserErrorListener(collector));
		parser.ErrorHandler = new MiniGoErrorStrategy();

		var tree = parser.root();

		var typeChecker = new TypeChecker(collector, "inline");
		typeChecker.Visit(tree);

		return collector;
	}
}
