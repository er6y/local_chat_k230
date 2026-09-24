namespace Nncase.Passes.Rules.K230;

public class GnneActionMfuMemset : GnneAction
{
	public Gpr AddrD { get; }

	public Gpr Value { get; }

	public Ssr Stride { get; }

	public Ssr Shape { get; }

	public DataType L2Datatype { get; }

	public GnneActionMfuMemset(Gpr addrD, Gpr value, Ssr stride, Ssr shape, DataType l2Datatype)
		: base(GnneActionName.MfuMemset)
	{
		AddrD = addrD;
		Value = value;
		Stride = stride;
		Shape = shape;
		L2Datatype = l2Datatype;
	}
}
