using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class AddToFakeActivation : BinaryToFakeActivation
{
	public override BinaryOp Op => BinaryOp.Add;

	public override void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
		actParam.Bs[0, i] = (actParam.Bs[1, i] = v);
	}
}
