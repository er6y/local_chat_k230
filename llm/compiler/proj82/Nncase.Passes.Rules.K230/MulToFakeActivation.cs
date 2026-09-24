using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class MulToFakeActivation : BinaryToFakeActivation
{
	public override BinaryOp Op => BinaryOp.Mul;

	public override void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
		actParam.Ks[0, i] = (actParam.Ks[1, i] = v);
	}
}
