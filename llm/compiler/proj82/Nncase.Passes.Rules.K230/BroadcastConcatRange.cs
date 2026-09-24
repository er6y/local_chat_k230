using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.Tensors;
using Nncase.Passes.Analysis;
using Nncase.PatternMatch;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class BroadcastConcatRange : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.Utility.IsRangeOfMarker("marker", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst("range"));


	private Expr? GetReplace(Marker marker, Tensor<float> range, Expr input, RunPassContext context)
	{
		float num = 2f;
		IExprUserAnalysisResult analysis = context.GetAnalysis<IExprUserAnalysisResult>();
		if (analysis[marker].Count((Expr u) => u is Nncase.IR.Tuple) == 1)
		{
			Expr expr = analysis[marker].First((Expr u) => u is Nncase.IR.Tuple);
			if (analysis[expr].Count((Expr u) => u is Call call2 && call2.Target is Concat) == 1)
			{
				Call expr2 = analysis[expr].First((Expr u) => u is Call call && call.Target is Concat) as Call;
				if (analysis[expr2].Count((Expr u) => u is Marker marker3 && marker3.Name == "RangeOf") == 1)
				{
					Tensor<float> tensor = ((TensorConst)(analysis[expr2].First((Expr u) => u is Marker marker2 && marker2.Name == "RangeOf") as Marker).Attribute).Value.Cast<float>();
					if (!range.Equals(tensor) && System.Math.Abs(new float[4]
					{
						range[new int[1]] / (tensor[new int[1]] + float.Epsilon),
						range[new int[1] { 1 }] / (tensor[new int[1] { 1 }] + float.Epsilon),
						tensor[new int[1]] / (range[new int[1]] + float.Epsilon),
						tensor[new int[1] { 1 }] / (range[new int[1] { 1 }] + float.Epsilon)
					}.Max()) < num)
					{
						return Nncase.IR.F.Math.RangeOfMarker(input, tensor).With(null, null, null, adaQuantInfo: marker.AdaQuantInfo, mixQuantInfo: marker.MixQuantInfo);
					}
				}
			}
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Marker marker = (Marker)__result["marker"];
		Tensor<float> range = ((TensorConst)__result["range"]).Value.Cast<float>();
		Expr input = (Expr)__result["input"];
		return GetReplace(marker, range, input, __context);
	}
}
