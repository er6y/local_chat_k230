using System;
using System.Collections.Generic;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.IR.K230;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class ProcessShiftBitsGNNEMatmul : IRewriteRule
{
	private static readonly OrPattern _actPattern = Nncase.PatternMatch.Utility.IsAlt(Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.F.Tensors.IsExpand(Nncase.PatternMatch.Utility.IsTensorConst("act"), Nncase.PatternMatch.Utility.IsWildcard("expand_shape")));

	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsDynamicGNNEMatMul("dynamicGnneMatMul", null, (DynamicGNNEMatMul _) => true, Nncase.PatternMatch.Utility.IsTensorConst("text"), Nncase.PatternMatch.Utility.IsWildcard("inputa"), Nncase.PatternMatch.Utility.IsWildcard("inputb"), Nncase.PatternMatch.Utility.IsWildcard("inputabias"), Nncase.PatternMatch.Utility.IsWildcard("inputbbias"), _actPattern, Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst("dynamicchannel"));


	private Expr GetReplace(DynamicGNNEMatMul dynamicGnneMatMul, Expr text, Expr inputa, Expr inputb, Expr inputabias, Expr inputbbias, Tensor<Half> act, Expr dynamicchannel, IMatchResult result, RunPassContext options)
	{
		ActivationParameter<Half> activationParameter = new ActivationParameter<Half>(act);
		sbyte shiftBits = activationParameter.ShiftBits;
		activationParameter.FusedShiftBits(shiftBits);
		Expr expr = Const.FromTensor(activationParameter.ToAct0Data<Half>());
		try
		{
			Expr shape = (Expr)result["expand_shape"];
			expr = Nncase.IR.F.Tensors.Expand(expr, shape);
		}
		catch (KeyNotFoundException)
		{
		}
		Call call = new Call(dynamicGnneMatMul, text, inputa, inputb, inputabias, inputbbias, expr, Tensor.FromScalar((int)shiftBits), dynamicchannel);
		options.MatchOptions.SuppressPattern(call, Pattern);
		return call;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		DynamicGNNEMatMul dynamicGnneMatMul = (DynamicGNNEMatMul)__result["dynamicGnneMatMul"];
		Expr text = (Expr)__result["text"];
		Expr inputa = (Expr)__result["inputa"];
		Expr inputb = (Expr)__result["inputb"];
		Expr inputabias = (Expr)__result["inputabias"];
		Expr inputbbias = (Expr)__result["inputbbias"];
		Tensor<Half> act = ((TensorConst)__result["act"]).Value.Cast<Half>();
		Expr dynamicchannel = (Expr)__result["dynamicchannel"];
		return GetReplace(dynamicGnneMatMul, text, inputa, inputb, inputabias, inputbbias, act, dynamicchannel, __result, __context);
	}
}
