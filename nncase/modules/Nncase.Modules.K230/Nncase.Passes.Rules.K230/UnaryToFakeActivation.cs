using System;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.IR.Math;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public class UnaryToFakeActivation : RewriteRule<Pattern>
{
	public override Pattern Pattern { get; }

	public virtual UnaryOp Op => UnaryOp.Abs;

	public virtual void ProcessActParam(ActParam2 actParam, int i)
	{
	}

	public Expr? GetReplace(Unary u, Call call, Expr input)
	{
		if (u.UnaryOp != Op)
		{
			return null;
		}
		int num = GNNETypePatternUtility.GetGNNEShape(call.CheckedShape.ToValueArray())[1];
		ActParam2 actParam = new ActParam2(num);
		actParam.ForEachChannel(ProcessActParam);
		Tensor<float> tensor = new Tensor<float>(actParam.GetAct1Data, new int[2] { num, 7 });
		return Nncase.IR.K230.F.Tensors.FakeActivation(input, None.Default, tensor, num, 0, 0, 0, false, GnneActivationType.Uninitialized, actParam, call.CheckedShape.ToValueArray());
	}

	public override Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		Unary u = (Unary)__result["u"];
		Call call = (Call)__result["call"];
		Expr input = (Expr)__result["input"];
		return GetReplace(u, call, input);
	}

	public UnaryToFakeActivation()
	{
		Func<Unary, bool> condition = (Unary _) => true;
		ExprPattern input = Nncase.PatternMatch.Utility.IsWildcard("input")with
		{
			TypePattern = (TypePatternUtility.HasFixedShape() & TypePatternUtility.HasRank((int r) => r <= 4, "Only support rank <= 4"))
		};
		Pattern = Nncase.PatternMatch.F.Math.IsUnary("u", "call", condition, input)with
		{
			TypePattern = TypePatternUtility.HasDataType(DataTypes.Float32)
		};
	}
}
