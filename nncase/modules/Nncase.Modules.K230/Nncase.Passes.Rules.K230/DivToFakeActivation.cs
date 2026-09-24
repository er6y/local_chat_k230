#define TRACE
using System.Diagnostics;
using Nncase.IR.K230;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class DivToFakeActivation : BinaryToFakeActivation
{
	public override BinaryOp Op => BinaryOp.Div;

	public override bool CanSupportedConstantLhs => false;

	public override void ProcessActParam(ActParam2 actParam, bool isCRHS, int i, float v)
	{
		Trace.Assert(isCRHS);
		actParam.Ks[0, i] = (actParam.Ks[1, i] = 1f / v);
	}
}
