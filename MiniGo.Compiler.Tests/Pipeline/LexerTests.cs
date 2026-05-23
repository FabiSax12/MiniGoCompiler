using Antlr4.Runtime;
using Generated;
using MiniGo.Compiler.Errors;

namespace MiniGo.Compiler.Tests.Pipeline;

public sealed class LexerTests
{
	[Fact]
	public void Tokenize_Keywords_ReturnsCorrectTokenTypes()
	{
		var tokens = Lex("package var func type struct if else for switch return break continue print println");

		var types = tokens.Select(t => t.Type).ToList();
		types.Should().Contain(MiniGoLexer.PACKAGE);
		types.Should().Contain(MiniGoLexer.VAR);
		types.Should().Contain(MiniGoLexer.FUNC);
		types.Should().Contain(MiniGoLexer.TYPE);
		types.Should().Contain(MiniGoLexer.STRUCT);
		types.Should().Contain(MiniGoLexer.IF);
		types.Should().Contain(MiniGoLexer.ELSE);
		types.Should().Contain(MiniGoLexer.FOR);
		types.Should().Contain(MiniGoLexer.SWITCH);
		types.Should().Contain(MiniGoLexer.RETURN);
		types.Should().Contain(MiniGoLexer.BREAK);
		types.Should().Contain(MiniGoLexer.CONTINUE);
		types.Should().Contain(MiniGoLexer.PRINT);
		types.Should().Contain(MiniGoLexer.PRINTLN);
	}

	[Fact]
	public void Tokenize_Operators_ReturnsCorrectTokenTypes()
	{
		var tokens = Lex("+ - * / % == != < <= > >= && || ! = += -= *= /=");

		var types = tokens.Select(t => t.Type).ToList();
		types.Should().Contain(MiniGoLexer.PLUS);
		types.Should().Contain(MiniGoLexer.MINUS);
		types.Should().Contain(MiniGoLexer.STAR);
		types.Should().Contain(MiniGoLexer.DIV);
		types.Should().Contain(MiniGoLexer.MOD);
		types.Should().Contain(MiniGoLexer.EQUALS);
		types.Should().Contain(MiniGoLexer.NOT_EQUALS);
		types.Should().Contain(MiniGoLexer.LESS);
		types.Should().Contain(MiniGoLexer.LESS_EQUALS);
		types.Should().Contain(MiniGoLexer.GREATER);
		types.Should().Contain(MiniGoLexer.GREATER_EQUALS);
		types.Should().Contain(MiniGoLexer.LOGICAL_AND);
		types.Should().Contain(MiniGoLexer.LOGICAL_OR);
		types.Should().Contain(MiniGoLexer.NOT);
		types.Should().Contain(MiniGoLexer.ASSIGN);
		types.Should().Contain(MiniGoLexer.PLUS_ASSIGN);
		types.Should().Contain(MiniGoLexer.MINUS_ASSIGN);
		types.Should().Contain(MiniGoLexer.STAR_ASSIGN);
		types.Should().Contain(MiniGoLexer.DIV_ASSIGN);
	}

	[Fact]
	public void Tokenize_IntegerLiteral_HasCorrectText()
	{
		var tokens = Lex("42");

		tokens.Should().HaveCountGreaterOrEqualTo(1);
		tokens[0].Type.Should().Be(MiniGoLexer.INTLITERAL);
		tokens[0].Text.Should().Be("42");
	}

	[Fact]
	public void Tokenize_FloatLiteral_HasCorrectText()
	{
		var tokens = Lex("3.14");

		tokens.Should().HaveCountGreaterOrEqualTo(1);
		tokens[0].Type.Should().Be(MiniGoLexer.FLOATLITERAL);
		tokens[0].Text.Should().Be("3.14");
	}

	[Fact]
	public void Tokenize_StringLiteral_HasCorrectType()
	{
		var tokens = Lex("\"hello\"");

		tokens.Should().HaveCountGreaterOrEqualTo(1);
		var types = tokens.Select(t => t.Type);
		types.Should().IntersectWith(new[] { MiniGoLexer.INTERPRETEDSTRINGLITERAL, MiniGoLexer.RAWSTRINGLITERAL });
	}

	[Fact]
	public void Tokenize_Identifier_HasCorrectText()
	{
		var tokens = Lex("myVar");

		tokens.Should().HaveCountGreaterOrEqualTo(1);
		tokens[0].Type.Should().Be(MiniGoLexer.IDENTIFIER);
		tokens[0].Text.Should().Be("myVar");
	}

	[Fact]
	public void Tokenize_Empty_ReturnsEofOnly()
	{
		var tokens = Lex("");

		tokens.Should().HaveCount(1);
		tokens[0].Type.Should().Be(Antlr4.Runtime.TokenConstants.EOF);
	}

	[Fact]
	public void Tokenize_VariableDeclaration_ReturnsExpectedTokens()
	{
		var tokens = Lex("var x int = 5");

		tokens.Select(t => t.Type).Should().ContainInOrder(
			MiniGoLexer.VAR,
			MiniGoLexer.IDENTIFIER,
			MiniGoLexer.IDENTIFIER,
			MiniGoLexer.ASSIGN,
			MiniGoLexer.INTLITERAL
		);
	}

	private static List<IToken> Lex(string source)
	{
		var input = CharStreams.fromString(source);
		var lexer = new MiniGoLexer(input);
		var tokens = new List<IToken>();
		lexer.RemoveErrorListeners();

		while (true)
		{
			var token = lexer.NextToken();
			tokens.Add(token);
			if (token.Type == TokenConstants.EOF)
				break;
		}

		return tokens;
	}
}
