using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class MinToFakeActivation : BinaryToFakeActivation
{
	public override BinaryOp Op => BinaryOp.Min;

	public override void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
		actParam.Ks[1, i] = 0f;
		actParam.Bs[1, i] = (actParam.Xs[0, i] = v);
	}
}
