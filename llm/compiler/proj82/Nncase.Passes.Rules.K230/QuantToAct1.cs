using System;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class QuantToAct1 : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Math.IsQuantize("quant", "call", (Quantize _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = (TypePatternUtility.HasRank((int r) => r <= 4, "GNNE not support more than 4D") & TypePatternUtility.HasFixedShape())
	}, Nncase.PatternMatch.Utility.IsWildcard("qp"));


	private Expr? GetReplace(Call call, Quantize quant, Expr input, QuantParam qp)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(call.CheckedShape.ToValueArray(), 0, array, array.Length - call.CheckedShape.ToValueArray().Length, call.CheckedShape.Count);
		int num = array[1];
		ActParam2 actParam = new ActParam2(num);
		actParam.FusedQuantParam(qp);
		ValueRange<float> fusedClamp = FuseQuantHelper.GetFusedClamp(actParam, quant.TargetType);
		actParam.SetFusedClamp(fusedClamp);
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.K230.F.Tensors.GNNEStore(quant.TargetType, Nncase.IR.K230.F.Tensors.SimpleSingleInputGNNEAct(GetReplaceHelper.LoadActIF(Nncase.IR.F.Tensors.Reshape(input, array)), actParam, num, qp, quant.TargetType, actParam, array)), call.CheckedShape);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Quantize quant = (Quantize)__result["quant"];
		Expr input = (Expr)__result["input"];
		QuantParam qp = ((TensorConst)__result["qp"]).Value.ToScalar<QuantParam>();
		return GetReplace(call, quant, input, qp);
	}
}
