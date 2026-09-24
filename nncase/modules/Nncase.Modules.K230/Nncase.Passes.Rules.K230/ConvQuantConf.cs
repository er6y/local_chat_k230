namespace Nncase.Passes.Rules.K230;

public class ConvQuantConf
{
	private DataType? _quantType;

	private DataType? _wQuantType;

	private bool _useMseQuantW;

	public DataType? QuantType
	{
		get
		{
			return _quantType;
		}
		set
		{
			_quantType = value;
		}
	}

	public DataType? WQuantType
	{
		get
		{
			return _wQuantType;
		}
		set
		{
			_wQuantType = value;
		}
	}

	public bool UseMseQuantW
	{
		get
		{
			return _useMseQuantW;
		}
		set
		{
			_useMseQuantW = value;
		}
	}
}
