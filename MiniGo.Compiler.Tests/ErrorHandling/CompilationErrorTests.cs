using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Shared;

namespace MiniGo.Compiler.Tests.ErrorHandling;

public sealed class CompilationErrorTests
{
	[Fact]
	public void Constructor_SetsAllProperties()
	{
		var span = new SourceSpan("file.txt", 1, 0, 3);

		var error = new CompilationError(Severity.Error, "something went wrong", span, CompilationPhase.Type);

		error.Severity.Should().Be(Severity.Error);
		error.Message.Should().Be("something went wrong");
		error.Span.Should().Be(span);
		error.Phase.Should().Be(CompilationPhase.Type);
	}

	[Fact]
	public void ToString_Error_IncludesPhaseAndErrorLabel()
	{
		var span = new SourceSpan("file.txt", 5, 2);
		var error = new CompilationError(Severity.Error, "type mismatch", span, CompilationPhase.Type);

		var result = error.ToString();

		result.Should().Contain("[TYPE Error]");
		result.Should().Contain("type mismatch");
		result.Should().Contain("line 5:2");
	}

	[Fact]
	public void ToString_Warning_UsesWarningLabel()
	{
		var span = new SourceSpan("file.txt", 3, 1);
		var error = new CompilationError(Severity.Warning, "unused variable", span, CompilationPhase.Type);

		var result = error.ToString();

		result.Should().Contain("Warning");
		result.Should().Contain("unused variable");
		result.Should().Contain("unused variable");
	}

	[Fact]
	public void ToString_ContainsPhaseName()
	{
		var span = new SourceSpan("file.txt", 1, 0);
		var error = new CompilationError(Severity.Error, "lexer error", span, CompilationPhase.Lexer);

		error.ToString().Should().Contain("LEXER");
		error.ToString().Should().Contain("Error");
	}
}
