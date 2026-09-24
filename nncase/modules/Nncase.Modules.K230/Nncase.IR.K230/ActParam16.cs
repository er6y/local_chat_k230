using System;

namespace Nncase.IR.K230;

public class ActParam16 : ActParamBase
{
	public override int N => 16;

	public override Shape Shape => ActHelper.GetAct16SegementsShape;

	public ActParam16(ActParam16 other)
		: base(other)
	{
	}

	public ActParam16(int channel, QuantParam quantizeParam = default(QuantParam), bool isDeq = false)
		: base(channel, quantizeParam, isDeq)
	{
		InitValues();
	}

	public override int FusedShiftBits()
	{
		sbyte act1ShiftBits = ShiftBitsHelper.GetAct1ShiftBits(this);
		FusedShiftBits(act1ShiftBits);
		return act1ShiftBits;
	}

	public void ForEachChannel(Action<ActParam16, int> f)
	{
		for (int i = 0; i < base.Channels; i++)
		{
			f(this, i);
		}
	}
}
