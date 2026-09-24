using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;
using Nncase.Targets;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class DisableConvDWPermitInt16Quant : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv2", "call2", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsRangeOfMarker("marker", Nncase.PatternMatch.F.K230.IsFakeConv2D("fakeConv1", "call1", (FakeConv2D _) => true, Nncase.PatternMatch.Utility.IsWildcard("input1")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsWildcard("weights1")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}, Nncase.PatternMatch.Utility.IsTensorConst("act1"), Nncase.PatternMatch.Utility.IsTensorConst("padding1"), Nncase.PatternMatch.Utility.IsTensorConst("stride1"), Nncase.PatternMatch.Utility.IsTensorConst("dilation1"), Nncase.PatternMatch.Utility.IsTensorConst("groups1"), Nncase.PatternMatch.Utility.IsTensorConst("padValue1")), Nncase.PatternMatch.Utility.IsTensorConst("fakeConv1OutputRange")), Nncase.PatternMatch.Utility.IsRangeOfMarker("markerW", Nncase.PatternMatch.Utility.IsWildcard("weights2"), Nncase.PatternMatch.Utility.IsTensorConst("weights2Range")), Nncase.PatternMatch.Utility.IsTensorConst("act2"), Nncase.PatternMatch.Utility.IsTensorConst("padding2"), Nncase.PatternMatch.Utility.IsTensorConst("stride2"), Nncase.PatternMatch.Utility.IsTensorConst("dilation2"), Nncase.PatternMatch.Utility.IsTensorConst("groups2"), Nncase.PatternMatch.Utility.IsTensorConst("padValue2"));


	private Expr? GetReplace(Call call1, TensorConst groups1, Marker marker, Call call2, Expr weights2, TensorConst groups2, Marker markerW)
	{
		bool flag = call1.Arguments[0].CheckedShape[1] == call1.CheckedShape[1] && call1.CheckedShape[1] == groups1.Value.ToScalar<int>() && groups1 != 1;
		bool num = call1.CheckedShape[1] == call2.CheckedShape[1] && call2.CheckedShape[1] == groups2.Value.ToScalar<int>() && groups2 != 1;
		DataType quantType = base.CompileSession.CompileOptions.QuantizeOptions.QuantType;
		DataType wQuantType = base.CompileSession.CompileOptions.QuantizeOptions.WQuantType;
		if (num && weights2.CheckedShape[2].FixedValue <= 3 && weights2.CheckedShape[3].FixedValue <= 3 && !flag)
		{
			if (marker.MixQuantInfo == null)
			{
				marker.MixQuantInfo = new K230Target.K230MixQuantInfo();
			}
			if (quantType != DataTypes.Int16 && base.CompileSession.CompileOptions.QuantizeOptions.QuantScheme == string.Empty)
			{
				((K230Target.K230MixQuantInfo)marker.MixQuantInfo).PermitInt16Quant = false;
			}
			if (markerW.MixQuantInfo == null)
			{
				markerW.MixQuantInfo = new K230Target.K230MixQuantInfo();
			}
			if (wQuantType != DataTypes.Int16 && base.CompileSession.CompileOptions.QuantizeOptions.QuantScheme == string.Empty)
			{
				((K230Target.K230MixQuantInfo)markerW.MixQuantInfo).PermitInt16Quant = false;
			}
			return null;
		}
		return null;
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call1"];
		TensorConst groups = (TensorConst)__result["groups1"];
		Marker marker = (Marker)__result["marker"];
		Call call2 = (Call)__result["call2"];
		Expr weights = (Expr)__result["weights2"];
		TensorConst groups2 = (TensorConst)__result["groups2"];
		Marker markerW = (Marker)__result["markerW"];
		return GetReplace(call, groups, marker, call2, weights, groups2, markerW);
	}
}
