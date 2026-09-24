using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Targets;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class DisableConvPdp0ReducePermitInt16Quant : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakePdp("fakePdp", "call2", (FakePdp _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("marker", Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv", "call1", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsWildcard("input1")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsWildcard("weights1")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("act1"), Nncase.PatternMatch.Utility.IsTensorConst("padding1"), Nncase.PatternMatch.Utility.IsTensorConst("stride1"), Nncase.PatternMatch.Utility.IsTensorConst("dilation1"), Nncase.PatternMatch.Utility.IsTensorConst("groups1"), Nncase.PatternMatch.Utility.IsTensorConst("padValue1")), Nncase.PatternMatch.Utility.IsTensorConst("fakeConvOutputRange")), Nncase.PatternMatch.Utility.IsTensorConst("padValue2"), Nncase.PatternMatch.Utility.IsTensorConst("filter2"), Nncase.PatternMatch.Utility.IsTensorConst("stride2"), Nncase.PatternMatch.Utility.IsTensorConst("padding2"), Nncase.PatternMatch.Utility.IsTensorConst("countIncludePad2"));


	private Expr? GetReplace(Call call1, TensorConst groups1, Marker marker, TensorConst filter2)
	{
		bool flag = call1.Arguments[0].CheckedShape[1] == call1.CheckedShape[1] && call1.CheckedShape[1] == groups1.Value.ToScalar<int>() && groups1 != 1;
		if (base.CompileSession.CompileOptions.QuantizeOptions.QuantType != DataTypes.Int16 && !flag && filter2.Value.ToArray<int>()[0] <= 3 && filter2.Value.ToArray<int>()[1] <= 3)
		{
			if (marker.MixQuantInfo == null)
			{
				marker.MixQuantInfo = new K230Target.K230MixQuantInfo();
			}
			((K230Target.K230MixQuantInfo)marker.MixQuantInfo).PermitInt16Quant = false;
			return null;
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call1"];
		TensorConst groups = (TensorConst)__result["groups1"];
		Marker marker = (Marker)__result["marker"];
		TensorConst filter = (TensorConst)__result["filter2"];
		return GetReplace(call, groups, marker, filter);
	}
}
