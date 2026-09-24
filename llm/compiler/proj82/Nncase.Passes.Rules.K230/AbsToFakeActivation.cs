using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class AbsToFakeActivation : UnaryToFakeActivation
{
	public override UnaryOp Op => UnaryOp.Abs;

	public override void ProcessActParam(ActParam2 actParam, int i)
	{
		actParam.Ks[0, i] = -1f;
	}
}
