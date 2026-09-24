using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.TIR;
using Nncase.Utilities;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class SplitLargeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern => Nncase.PatternMatch.Utility.IsCallSpecific("call", Nncase.PatternMatch.Utility.IsOp<FakeActivation>(), (FakeActivation.InputA, Nncase.PatternMatch.Utility.IsWildcard("input")with
	{
		TypePattern = TypePatternUtility.HasFixedShape()
	}));

	public Expr? GetReplace(Call call, Expr input)
	{
		Call call2 = call;
		Expr rhs = call2.Arguments[FakeActivation.InputB.Index];
		FakeActivation t = (FakeActivation)call2.Target;
		bool splitRhs = false;
		if (rhs != None.Default)
		{
			if (!rhs.CheckedShape.SequenceEqual(input.CheckedShape))
			{
				if (rhs.CheckedShape.Any((Dimension x) => x.FixedValue > 65535))
				{
					return null;
				}
			}
			else
			{
				splitRhs = true;
			}
		}
		return SplitLarge.Split(call2, input, input.CheckedShape.ToValueArray(), ((int Dim, int Axis) pair) => pair.Dim > 65535, (int _) => 1, (int axis) => axis, ((int Count, int ChunkSize, int CurrentSize, int Index, int Axis) param) => (NewBegin: param.Index * param.ChunkSize, NewEnd: param.Index * param.ChunkSize + param.CurrentSize, Pads: Padding.Zero()), delegate(Expr lhsSlice, Padding padding, int axis)
		{
			lhsSlice.InferenceType();
			FakeActivation target = new FakeActivation(t.Type, t.ActParam, lhsSlice.CheckedShape.ToValueArray());
			if (splitRhs)
			{
				Expr item = ((!(lhsSlice is Marker marker)) ? ((Expr)ReplaceUtility.ReplaceCallFirstParam(((Call)lhsSlice).Target, ((Call)lhsSlice).Arguments.ToArray(), rhs)) : ((Expr)marker.With(null, ReplaceUtility.ReplaceCallFirstParam(((Call)marker.Target).Target, ((Call)marker.Target).Arguments.ToArray(), rhs), null, null, null)));
				return ReplaceUtility.ReplaceCallParams(target, call2.Arguments.ToArray(), (FakeActivation.InputA, lhsSlice), (FakeActivation.InputB, item));
			}
			return ReplaceUtility.ReplaceCallFirstParam(target, call2.Arguments.ToArray(), lhsSlice);
		});
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(call, input);
	}
}
