using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.NN;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToGNNEPad : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.NN.IsPad("pad", "call", (Pad p) => p.PadMode == PadMode.Constant, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = (GNNETypePatternUtility.ValidDType() & TypePatternUtility.HasShape((Shape sp) => sp.Rank <= 4 && sp.IsFixed, "fixed gnne shape"))
	}, Nncase.PatternMatch.Utility.IsTensorConst("pads"), Nncase.PatternMatch.Utility.IsTensorConst("value"));


	private Expr? GetReplace(Expr input, TensorConst pads, TensorConst value, Call call)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(input.CheckedShape.ToValueArray(), 0, array, array.Length - input.CheckedShape.Count, input.CheckedShape.Count);
		Call input2 = Nncase.IR.F.Tensors.Reshape(input, array);
		int[] array2 = pads.Value.ToArray<int>();
		int[] array3 = Enumerable.Repeat(0, 8).ToArray();
		Array.Copy(array2, 0, array3, array3.Length - array2.Length, array2.Length);
		DataType dataType = ((input.CheckedDataType == DataTypes.Float32) ? DataTypes.Float16 : input.CheckedDataType);
		Expr input3 = ((value.Value.Rank == 0) ? value : value[0]);
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.K230.F.Tensors.GNNEStore(call.CheckedDataType, Nncase.IR.K230.F.Tensors.GNNEPad(Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, input2), Tensor.From(array3, new int[]{1,1,1,1}), Const.FromValue(Nncase.IR.F.Tensors.Cast(input3, dataType).Evaluate()))), call.CheckedShape);
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr input = (Expr)__result["input"];
		TensorConst pads = (TensorConst)__result["pads"];
		TensorConst value = (TensorConst)__result["value"];
		Call call = (Call)__result["call"];
		return GetReplace(input, pads, value, call);
	}
}
