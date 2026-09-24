using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class MaxToFakeActivation : BinaryToFakeActivation
{
	public override BinaryOp Op => BinaryOp.Max;

	public override void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
		actParam.Ks[0, i] = 0f;
		actParam.Bs[0, i] = (actParam.Xs[0, i] = v);
	}
}
