namespace MiniGo.Compiler.Semantic;

public sealed class ControlFlowTracker
{
	private int _loopDepth;
	private int _switchDepth;
	private readonly Stack<Types> _switchTypes = new();

	public void EnterLoop() => _loopDepth++;
	public void ExitLoop() => _loopDepth--;

	public void EnterSwitch(Types switchType)
	{
		_switchDepth++;
		_switchTypes.Push(switchType);
	}

	public void ExitSwitch()
	{
		_switchDepth--;
		_switchTypes.Pop();
	}

	public bool IsInLoop => _loopDepth > 0;
	public bool IsInBreakable => _loopDepth > 0 || _switchDepth > 0;
	public Types SwitchType => _switchTypes.Count > 0 ? _switchTypes.Peek() : Types.Unknown;
}
