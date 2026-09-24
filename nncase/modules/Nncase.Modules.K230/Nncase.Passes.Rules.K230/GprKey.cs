namespace Nncase.Passes.Rules.K230;

public struct GprKey
{
	public int Value;

	public bool Basement;

	public GprKey(int value, bool basement)
	{
		Value = value;
		Basement = basement;
	}

	public GprKey(int value)
	{
		Value = value;
		Basement = false;
	}

	public static bool operator ==(GprKey lhs, GprKey rhs)
	{
		if (lhs.Value == rhs.Value)
		{
			return lhs.Basement == rhs.Basement;
		}
		return false;
	}

	public static bool operator !=(GprKey lhs, GprKey rhs)
	{
		return !(lhs == rhs);
	}

	public override bool Equals(object? obj)
	{
		if (obj is GprKey gprKey && Value == gprKey.Value)
		{
			return Basement == gprKey.Basement;
		}
		return false;
	}
}
