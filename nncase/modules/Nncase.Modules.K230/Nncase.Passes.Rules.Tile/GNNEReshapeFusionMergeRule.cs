using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.IR.F;
using Nncase.Passes.Analysis;
using Nncase.Passes.Mutators;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.Tile;

internal sealed class GNNEReshapeFusionMergeRule : IMergeRewriteRule
{
	private Pattern? _pattern;

	public string ModuleKind => "k230";

	public IPattern Pattern => _pattern ?? (_pattern = CreatePattern(ModuleKind));

	public Pattern CreatePattern(string target_module_kind)
	{
		VArgsPattern parameters = Nncase.PatternMatch.Utility.IsVArgsRepeat(() => Nncase.PatternMatch.Utility.IsWildcard());
		return Nncase.PatternMatch.F.Tensors.IsReshape("reshape", "caller", Nncase.PatternMatch.Utility.IsCall("callee", Nncase.PatternMatch.Utility.IsFusion("callee_fusion", target_module_kind, Nncase.PatternMatch.Utility.IsWildcard(), Nncase.PatternMatch.Utility.IsVArgsRepeat(() => Nncase.PatternMatch.Utility.IsWildcard())), parameters), Nncase.PatternMatch.Utility.IsWildcard("shape"));
	}

	public Expr? GetReplace(Func<Expr, Expr> mergedFusionRewriteCallBack, Func<Fusion, HashSet<Fusion>, bool> mergedFusionCheckCallBack, Func<HashSet<Fusion>, bool> candidateFusionCheckCallBack, Action<HashSet<Fusion>> candidateFusionRecordCallBack, IExprUserAnalysisResult usedByReslut, IMatchResult result, RunPassContext options)
	{
		Call call = (Call)result["callee"];
		Fusion callee_fusion = (Fusion)result["callee_fusion"];
		Expr shape = (Expr)result["shape"];
		if (usedByReslut[call].Count() > 1)
		{
			return null;
		}
		if (!ProcessFusionMerge(mergedFusionRewriteCallBack, candidateFusionCheckCallBack, shape, callee_fusion, result, out HashSet<Fusion> candidate_fusions, out Fusion merged_fusion))
		{
			return null;
		}
		if (mergedFusionCheckCallBack(merged_fusion, candidate_fusions))
		{
			return new Call(merged_fusion, call.Arguments);
		}
		return null;
	}

	private bool ProcessFusionMerge(Func<Expr, Expr> mergedFusionRewriteCallBack, Func<HashSet<Fusion>, bool> candidate_fusion_checker, Expr shape, Fusion callee_fusion, IMatchResult result, out HashSet<Fusion> candidate_fusions, out Fusion merged_fusion)
	{
		merged_fusion = null;
		candidate_fusions = new HashSet<Fusion>();
		string name = callee_fusion.Name + "_reshape";
		candidate_fusions.Add(callee_fusion);
		Expr arg = Nncase.IR.F.Tensors.Reshape(callee_fusion.Body, shape);
		arg = mergedFusionRewriteCallBack(arg);
		if (!arg.InferenceType())
		{
			throw new InvalidOperationException("Merged Fusion Type Infer Error!");
		}
		merged_fusion = new Fusion(name, ModuleKind, arg, callee_fusion.Parameters);
		return true;
	}
}
