using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.NN;
using Nncase.IR.Tensors;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class InnodePadReshapeMotion : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.NN.IsPad("p", "call", (Pad _) => true, Nncase.PatternMatch.F.Tensors.IsReshape("bc", (Reshape _) => true, Nncase.PatternMatch.Utility.IsTensorConst("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}), Nncase.PatternMatch.Utility.IsTensorConst("pads"), Nncase.PatternMatch.Utility.IsTensorConst("value"));


	private Expr? GetReplace(Pad p, Call call, Reshape bc, TensorConst input, TensorConst pads, TensorConst value)
	{
		if (bc.CheckedShape.Count != 4 || input.CheckedShape.Count != 3 || bc.CheckedShape[1] != input.CheckedShape[0] || bc.CheckedShape[2] != input.CheckedShape[1] || bc.CheckedShape[3] != input.CheckedShape[2])
		{
			return null;
		}
		Shape checkedShape = call.CheckedShape;
		Call input2 = Nncase.IR.F.Tensors.Reshape(input, new Dimension[4]
		{
			1,
			checkedShape[0],
			checkedShape[1],
			checkedShape[2]
		});
		Tensor<int> tensor = pads.Value.Cast<int>();
		int[][] array = new int[4][];
		for (int i = 0; i < 4; i++)
		{
			if (i != 0)
			{
				for (int j = 0; j < tensor.Length; j++)
				{
					array[i][j] = tensor[new int[2]
					{
						i - 1,
						j
					}];
				}
			}
		}
		return Nncase.IR.F.NN.Pad(input2, array, p.PadMode, value);
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Pad p = (Pad)__result["p"];
		Call call = (Call)__result["call"];
		Reshape bc = (Reshape)__result["bc"];
		TensorConst input = (TensorConst)__result["input"];
		TensorConst pads = (TensorConst)__result["pads"];
		TensorConst value = (TensorConst)__result["value"];
		return GetReplace(p, call, bc, input, pads, value);
	}
}
