using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class SubToFakeActivation : BinaryToFakeActivation
{
	public override BinaryOp Op => BinaryOp.Sub;

	public override void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
		if (isCRHS)
		{
			actParam.Bs[0, i] = (actParam.Bs[1, i] = 0f - v);
			return;
		}
		actParam.Ks[0, i] = (actParam.Ks[1, i] = -1f);
		actParam.Bs[0, i] = (actParam.Bs[1, i] = v);
	}
}
