namespace Nncase.Passes.Rules.K230;

public class QuantT
{
	private bool _isU8;

	private byte _u8Value;

	private sbyte _i8Value;

	public byte U8
	{
		get
		{
			return _u8Value;
		}
		set
		{
			_isU8 = true;
			_u8Value = value;
		}
	}

	public sbyte I8
	{
		get
		{
			return _i8Value;
		}
		set
		{
			_isU8 = false;
			_i8Value = value;
		}
	}

	public QuantT(bool isU8 = true)
	{
		U8 = 0;
		I8 = 0;
		_isU8 = isU8;
	}
}
