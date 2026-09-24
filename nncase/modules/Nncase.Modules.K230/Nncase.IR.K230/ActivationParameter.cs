namespace Nncase.IR.K230;

public class ActivationParameter : ActParamBase
{
	public override int N => 2;

	public override Shape Shape => ActHelper.GetAct16SegementsShape;

	public ActivationParameter(ActivationParameter other)
		: base(other)
	{
	}

	public ActivationParameter(int channel, QuantParam quantParam = default(QuantParam), bool isDeq = false)
		: base(channel, quantParam, isDeq)
	{
		InitValues();
	}

	public override int FusedShiftBits()
	{
		sbyte act1ShiftBits = ShiftBitsHelper.GetAct1ShiftBits(this);
		FusedShiftBits(act1ShiftBits);
		return act1ShiftBits;
	}
}
