namespace Nncase.Passes.Rules.K230;

public struct SsrKey
{
	private long _value;

	private bool _basement;

	public SsrKey(long value, bool basement)
	{
		_value = value;
		_basement = basement;
	}

	public SsrKey(long value)
	{
		_value = value;
		_basement = false;
	}

	public static bool operator ==(SsrKey lhs, SsrKey rhs)
	{
		if (lhs._value == rhs._value)
		{
			return lhs._basement == rhs._basement;
		}
		return false;
	}

	public static bool operator !=(SsrKey lhs, SsrKey rhs)
	{
		return !(lhs == rhs);
	}

	public override bool Equals(object? obj)
	{
		if (obj is SsrKey ssrKey && _value == ssrKey._value)
		{
			return _basement == ssrKey._basement;
		}
		return false;
	}
}
