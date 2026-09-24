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
public sealed class DeqToAct1 : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.Math.IsDequantize("deq", "call", (Dequantize _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsWildcard("qp"));


	private Expr? GetReplace(Call call, Expr input, QuantParam qp)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(call.CheckedShape.ToValueArray(), 0, array, array.Length - call.CheckedShape.ToValueArray().Length, call.CheckedShape.Count);
		int num = array[1];
		ActParam2 actParam = new ActParam2(num);
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.K230.F.Tensors.GNNEStore(DataTypes.Float32, Nncase.IR.K230.F.Tensors.GNNEActivation(Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)input.CheckedDataType, Nncase.IR.F.Tensors.Reshape(input, array)), None.Default, Nncase.IR.K230.F.Tensors.GNNELoadW(DataTypes.Float16, actParam.ToAct1Data()), 0, 0, 0, Tensor.FromArray(new DeQuantizeParam[1]
		{
			new DeQuantizeParam(qp.ZeroPoint, qp.Scale)
		}), Tensor.FromArray(new DeQuantizeParam[1]
		{
			new DeQuantizeParam(qp.ZeroPoint, qp.Scale)
		}), num, GnneActivationType.Uninitialized, false, DataTypes.Float16, actParam, array)), call.CheckedShape);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		QuantParam qp = ((TensorConst)__result["qp"]).Value.ToScalar<QuantParam>();
		return GetReplace(call, input, qp);
	}
}
