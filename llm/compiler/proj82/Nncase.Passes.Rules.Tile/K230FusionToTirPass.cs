using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nncase.Diagnostics;
using Nncase.IR;
using Nncase.Passes.Analysis;
using Nncase.Passes.Mutators;
using Nncase.Passes.Rules.K230;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

public sealed class K230FusionToTirPass : ModulePass
{
	private readonly int[] _tileSizes;

	private readonly Dictionary<string, RoofLineInfo> _fusionMacsMap;

	private IAnalyzerManager AnalyzerManager => base.CompileSession.GetRequiredService<IAnalyzerManager>();

	public K230FusionToTirPass(int[]? tileSizes = null)
	{
		_tileSizes = tileSizes ?? Array.Empty<int>();
		_fusionMacsMap = new Dictionary<string, RoofLineInfo>();
	}

	protected override Task<IRModule> RunCoreAsync(IRModule module, RunPassContext options)
	{
		Dictionary<Fusion, BaseFunction> dictionary = new Dictionary<Fusion, BaseFunction>(ReferenceEqualityComparer.Instance);
		Dictionary<Fusion, BaseFunction> dictionary2 = new Dictionary<Fusion, BaseFunction>(ReferenceEqualityComparer.Instance);
		for (int i = 0; i < module.Functions.Count; i++)
		{
			BaseFunction baseFunction = module.Functions[i];
			if (baseFunction is Fusion fusion && baseFunction.ModuleKind == "k230")
			{
				PrimFunction primFunction = (PrimFunction)new K230FusionConvertVisitor(options).Rewrite(fusion);
				primFunction.InferenceType();
				dictionary[fusion] = primFunction;
				dictionary2[fusion] = primFunction;
				module.Replace(i, primFunction);
			}
		}
		for (int j = 0; j < module.Functions.Count; j++)
		{
			BaseFunction baseFunction = module.Functions[j];
			if (baseFunction is Function function && baseFunction.ModuleKind == "stackvm")
			{
				Dictionary<Type, IAnalysisResult> analysisResults = new Dictionary<Type, IAnalysisResult> { [typeof(IExprUserAnalysisResult)] = AnalyzerManager.GetAnaylsis<IExprUserAnalysisResult>(function) };
				DataFlowMergeRewriter dataFlowMergeRewriter = new DataFlowMergeRewriter();
				Dictionary<Fusion, IFusionChecker> fusionCheckCache = new Dictionary<Fusion, IFusionChecker>(ReferenceEqualityComparer.Instance);
				Function expr = (Function)dataFlowMergeRewriter.Rewrite(function, new IMergeRewriteRule[3]
				{
					new GNNESameInputFusionMergeRule(),
					new GNNEShortCutFusionMergeRuleLeft(),
					new GNNEShortCutFusionMergeRuleRight()
				}, (IMergeRewriteRule rule, RunPassContext option) => new GNNEPreFusionGroupMutator(fusionCheckCache, rule, option), new RunPassContext
				{
					AnalysisResults = analysisResults,
					MatchOptions = new FusionGroupMutator.GroupedMatchOptions()
				});
				if (DumpScope.Current.IsEnabled(DumpFlags.PassIR))
				{
					DumpScope.Current.DumpIR(expr, $"l1_fuse{j}", null, displayCallable: true);
				}
				Function expr2 = (Function)dataFlowMergeRewriter.Rewrite(expr, new IMergeRewriteRule[6]
				{
					new GNNESameInputFusionMergeRule(),
					new GNNEMultiInputFusionMergeRule(),
					new GNNEShortCutFusionMergeRuleLeft(),
					new GNNEShortCutFusionMergeRuleRight(),
					new GNNEReshapeFusionMergeRule(),
					new GNNEFusionReshapeMergeRule()
				}, (IMergeRewriteRule rule, RunPassContext option) => new GNNEFusionGroupMutator(fusionCheckCache, rule, option), new RunPassContext
				{
					AnalysisResults = analysisResults,
					MatchOptions = new FusionGroupMutator.GroupedMatchOptions()
				});
				CheckedConvertMutator checkedConvertMutator = new CheckedConvertMutator(dictionary, _fusionMacsMap, fusionCheckCache, options);
				expr2 = (Function)CompilerServices.Rewrite(expr2, new IRewriteRule[1]
				{
					new Reshape1x1Conv()
				}, new RunPassContext());
				Function function2 = (Function)checkedConvertMutator.Rewrite(expr2);
				function2.InferenceType();
				if (checkedConvertMutator.IsMutated)
				{
					module.Replace(j, function2);
				}
			}
		}
		foreach (BaseFunction value in dictionary.Values)
		{
			if (value is PrimFunctionWrapper primFunctionWrapper)
			{
				module.Add(primFunctionWrapper);
				module.Add(primFunctionWrapper.Target);
			}
		}
		DumpScope.Current.DumpDotIR(module.Entry, "final");
		return Task.FromResult(module);
	}

	protected override async Task OnPassEndAsync(IRModule post, RunPassContext context)
	{
		await base.OnPassEndAsync(post, context);
		if (DumpScope.Current.IsEnabled(DumpFlags.PassIR))
		{
			using StreamWriter streamWriter = new StreamWriter(DumpScope.Current.OpenFile("mac.csv"));
			streamWriter.WriteLine("Name, Mac, FLOPs, OnChipMemTraffic, OffChipMemTraffic, OffChipMemLWTraffic, OffChipLoadStoreCnt, Time");
			foreach (var (text2, roofLineInfo2) in _fusionMacsMap)
			{
				streamWriter.WriteLine($"{text2}, {roofLineInfo2}, 0.0");
				using StreamWriter streamWriter2 = new StreamWriter(DumpScope.Current.OpenFile(text2 + ".csv"));
				foreach (string @operator in roofLineInfo2.Operators)
				{
					streamWriter2.WriteLine(@operator);
				}
			}
		}
		_fusionMacsMap.Clear();
	}
}
