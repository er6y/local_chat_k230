using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.Passes.Analysis;
using Nncase.Passes.Mutators;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.Tile;

internal sealed class GNNEFusionReshapeMergeRule : IMergeRewriteRule
{
	private Pattern? _pattern;

	public string ModuleKind => "k230";

	public IPattern Pattern => _pattern ?? (_pattern = CreatePattern(ModuleKind));

	public Pattern CreatePattern(string target_module_kind)
	{
		return Nncase.PatternMatch.Utility.IsCall("caller", Nncase.PatternMatch.Utility.IsFusion("caller_fusion", target_module_kind, Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsVArgs(Nncase.PatternMatch.Utility.IsWildcard())), Nncase.PatternMatch.F.Tensors.IsReshape("reshape", "callee", Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsWildcard("shape")));
	}

	public Expr? GetReplace(Func<Expr, Expr> mergedFusionRewriteCallBack, Func<Fusion, HashSet<Fusion>, bool> mergedFusionCheckCallBack, Func<HashSet<Fusion>, bool> candidateFusionCheckCallBack, Action<HashSet<Fusion>> candidateFusionRecordCallBack, IExprUserAnalysisResult usedByReslut, IMatchResult result, RunPassContext options)
	{
		Call expr = (Call)result["callee"];
		Fusion caller_fusion = (Fusion)result["caller_fusion"];
		Expr shape = (Expr)result["shape"];
		if (usedByReslut[expr].Count() > 1)
		{
			return null;
		}
		if (!ProcessFusionMerge(mergedFusionRewriteCallBack, candidateFusionCheckCallBack, shape, caller_fusion, result, out HashSet<Fusion> candidate_fusions, out Fusion merged_fusion))
		{
			return null;
		}
		if (mergedFusionCheckCallBack(merged_fusion, candidate_fusions))
		{
			return new Call(merged_fusion, (Expr)result["input"]);
		}
		return null;
	}

	private bool ProcessFusionMerge(Func<Expr, Expr> mergedFusionRewriteCallBack, Func<HashSet<Fusion>, bool> candidate_fusion_checker, Expr shape, Fusion caller_fusion, IMatchResult result, out HashSet<Fusion> candidate_fusions, out Fusion merged_fusion)
	{
		Expr expr = (Expr)result["input"];
		Var var = new Var(new TensorType(expr.CheckedDataType, expr.CheckedShape));
		merged_fusion = null;
		candidate_fusions = new HashSet<Fusion> { caller_fusion };
		string name = caller_fusion.Name + "_reshape";
		Expr arg = new IMergeRewriteRule.FusionMerger(new Dictionary<Var, Expr> { 
		{
			caller_fusion.Parameters[0],
			Nncase.IR.F.Tensors.Reshape(var, shape)
		} }, new Dictionary<Var, Var> { { var, var } }).Clone(caller_fusion.Body, default(Unit));
		arg = mergedFusionRewriteCallBack(arg);
		if (!arg.InferenceType())
		{
			throw new InvalidOperationException("Merged Fusion Type Infer Error!");
		}
		merged_fusion = new Fusion(name, ModuleKind, arg, var);
		return true;
	}
}
