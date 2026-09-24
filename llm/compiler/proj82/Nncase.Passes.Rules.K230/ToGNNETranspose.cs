using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class ToGNNETranspose : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.Tensors.IsTranspose("transpose", "call", (Transpose _) => true, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = (GNNETypePatternUtility.ValidDType() & TypePatternUtility.HasShape((Shape sp) => sp.IsFixed && sp.Rank <= 4, "rank must <= 4"))
	}, Nncase.PatternMatch.Utility.IsTensorConst("perm"));


	private Expr? GetReplace(Expr input, int[] perm, Call call)
	{
		int[] array = new int[4] { 1, 1, 1, 1 };
		Array.Copy(input.CheckedShape.ToValueArray(), 0, array, array.Length - input.CheckedShape.Count, input.CheckedShape.Count);
		Call input2 = Nncase.IR.F.Tensors.Reshape(input, array);
		int[] perm2 = ToGNNEPerm(perm);
		DataType dataType = ((input.CheckedDataType == DataTypes.Float32) ? DataTypes.Float16 : input.CheckedDataType);
		return Nncase.IR.F.Tensors.Reshape(Nncase.IR.K230.F.Tensors.GNNEStore(call.CheckedDataType, Nncase.IR.K230.F.Tensors.GNNETranspose(Nncase.IR.K230.F.Tensors.GNNELoad((PrimType)dataType, input2), GNNETypePatternUtility.ToMFUPerm(perm2))), call.CheckedShape);
	}

	private int[] ToGNNEPerm(int[] oldPerm)
	{
		int[] oldPerm2 = oldPerm;
		return Enumerable.Range(0, 4 - oldPerm2.Length).Concat(oldPerm2.Select((int x) => x + (4 - oldPerm2.Length))).ToArray();
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Expr input = (Expr)__result["input"];
		int[] perm = ((TensorConst)__result["perm"]).Value.ToArray<int>();
		Call call = (Call)__result["call"];
		return GetReplace(input, perm, call);
	}
}
