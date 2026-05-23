using Antlr4.Runtime;
using Generated;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Errors.Listeners;

namespace MiniGo.Compiler.Tests.Pipeline;

public sealed class ParserTests
{
	[Fact]
	public void Parse_EmptyPackage_ProducesRootWithNoErrors()
	{
		var (tree, collector) = ParseWithCollector("package main;\n");

		tree.Should().NotBeNull();
		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Parse_VariableDeclaration_NoErrors()
	{
		var (_, collector) = ParseWithCollector("package main;\nfunc main() {\nvar x int = 5;\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Parse_FunctionDeclaration_NoErrors()
	{
		var (_, collector) = ParseWithCollector("package main;\nfunc add(a int, b int) int {\nreturn a + b;\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Parse_IfElse_NoErrors()
	{
		var (_, collector) = ParseWithCollector("package main;\nfunc main() {\nif x > 0 {\nprint(x);\n} else {\nprint(-x);\n};\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Parse_ForLoop_NoErrors()
	{
		var (_, collector) = ParseWithCollector("package main;\nfunc main() {\nfor i := 0; i < 10; i++ {\nprint(i);\n};\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Parse_Switch_NoErrors()
	{
		var (_, collector) = ParseWithCollector("package main;\nfunc main() {\nswitch x {\ncase 1:\nprint(x);\ndefault:\nprint(x);\n};\n};");

		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Parse_SyntaxError_ReportsError()
	{
		var (_, collector) = ParseWithCollector("package main;\nfunc main() {\nprint(1 +);\n};");

		collector.HasErrors.Should().BeTrue();
	}

	private static (MiniGoParser.RootContext, ErrorCollector) ParseWithCollector(string source)
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
		return (tree, collector);
	}
}
