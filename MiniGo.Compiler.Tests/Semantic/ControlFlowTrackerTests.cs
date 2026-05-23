using MiniGo.Compiler.Semantic;

namespace MiniGo.Compiler.Tests.Semantic;

public sealed class ControlFlowTrackerTests
{
	[Fact]
	public void IsInLoop_FalseByDefault()
	{
		var tracker = new ControlFlowTracker();

		tracker.IsInLoop.Should().BeFalse();
	}

	[Fact]
	public void IsInBreakable_FalseByDefault()
	{
		var tracker = new ControlFlowTracker();

		tracker.IsInBreakable.Should().BeFalse();
	}

	[Fact]
	public void EnterLoop_SetsIsInLoop()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterLoop();

		tracker.IsInLoop.Should().BeTrue();
		tracker.IsInBreakable.Should().BeTrue();
	}

	[Fact]
	public void ExitLoop_ResetsIsInLoop()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterLoop();
		tracker.ExitLoop();

		tracker.IsInLoop.Should().BeFalse();
	}

	[Fact]
	public void NestedLoops_TracksCorrectly()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterLoop();
		tracker.EnterLoop();

		tracker.IsInLoop.Should().BeTrue();

		tracker.ExitLoop();
		tracker.IsInLoop.Should().BeTrue();

		tracker.ExitLoop();
		tracker.IsInLoop.Should().BeFalse();
	}

	[Fact]
	public void EnterSwitch_SetsIsInBreakable()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterSwitch(Types.Integer);

		tracker.IsInLoop.Should().BeFalse();
		tracker.IsInBreakable.Should().BeTrue();
		tracker.SwitchType.Should().Be(Types.Integer);
	}

	[Fact]
	public void ExitSwitch_ResetsBreakableIfNotInLoop()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterSwitch(Types.String);
		tracker.ExitSwitch();

		tracker.IsInBreakable.Should().BeFalse();
	}

	[Fact]
	public void SwitchInLoop_BothBreakableAfterExitSwitch()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterLoop();
		tracker.EnterSwitch(Types.Float);

		tracker.IsInBreakable.Should().BeTrue();

		tracker.ExitSwitch();
		tracker.IsInBreakable.Should().BeTrue();
		tracker.IsInLoop.Should().BeTrue();
	}

	[Fact]
	public void SwitchType_NestedSwitch_ReturnsInnermost()
	{
		var tracker = new ControlFlowTracker();

		tracker.EnterSwitch(Types.Integer);
		tracker.SwitchType.Should().Be(Types.Integer);

		tracker.EnterSwitch(Types.String);
		tracker.SwitchType.Should().Be(Types.String);

		tracker.ExitSwitch();
		tracker.SwitchType.Should().Be(Types.Integer);
	}

	[Fact]
	public void SwitchType_Default_ReturnsUnknown()
	{
		var tracker = new ControlFlowTracker();

		tracker.SwitchType.Should().Be(Types.Unknown);
	}
}
