using Antlr4.Runtime;
using Generated;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Errors.Listeners;
using MiniGo.Compiler.Semantic;

namespace MiniGo.Compiler.Tests.Pipeline;

public sealed class TypeCheckerTests
{
	[Fact]
	public void Valid_Main_NoErrors()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nvar x int = 42;\nprint(x);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Valid_ArithmeticExpressions_NoErrors()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nvar a int = 1 + 2;\nvar b int = 3 * 4;\nvar c int = 10 - 5;\nvar d int = 20 / 4;\nprint(a + b + c + d);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Valid_ComparisonOperators_NoErrors()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nvar flag bool = 5 > 3;\nif flag {\nprint(true);\n};\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Valid_FunctionCall_NoErrors()
	{
		var collector = RunTypeChecker("package main;\nfunc add(a int, b int) int {\nreturn a + b;\n};\nfunc main() {\nvar result int = add(1, 2);\nprint(result);\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Invalid_UndeclaredVariable_ReportsError()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nprint(z);\n};");

		collector.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void Invalid_RedeclaredVariable_ReportsError()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nvar x int = 1;\nvar x int = 2;\n};");

		collector.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void Valid_ForLoop_NoErrors()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nfor i := 0; i < 10; i++ {\nprint(i);\n};\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Valid_SwitchCase_NoErrors()
	{
		var collector = RunTypeChecker("package main;\nfunc main() {\nvar x int = 2;\nswitch x {\ncase 1:\nprint(1);\ncase 2:\nprint(2);\ndefault:\nprint(0);\n};\n};");

		collector.HasErrors.Should().BeFalse();
	}

	private static ErrorCollector RunTypeChecker(string source)
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
