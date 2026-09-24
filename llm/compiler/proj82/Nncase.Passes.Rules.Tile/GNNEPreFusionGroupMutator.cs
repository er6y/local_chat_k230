using System;
using System.Collections.Generic;
using Nncase.Diagnostics;
using Nncase.IR;
using Nncase.Passes.Mutators;
using Nncase.Passes.Rules.K230;

namespace Nncase.Passes.Rules.Tile;

internal sealed class GNNEPreFusionGroupMutator : FusionGroupMutator
{
	private readonly Dictionary<Fusion, IFusionChecker> _fusioncheckerCache;

	public GNNEPreFusionGroupMutator(Dictionary<Fusion, IFusionChecker> fusioncheckerCache, IMergeRewriteRule rule, RunPassContext passOptions)
		: base(rule, passOptions)
	{
		_fusioncheckerCache = fusioncheckerCache;
	}

	public override bool MergedFusionCheckCallBack(Fusion mergedFusion, HashSet<Fusion> candidateFusions)
	{
		L1FusionChecker l1FusionChecker = new L1FusionChecker();
		bool flag = l1FusionChecker.Check(mergedFusion, base.PassOptions);
		if (flag)
		{
			_fusioncheckerCache.Add(mergedFusion, l1FusionChecker);
			foreach (Fusion candidateFusion in candidateFusions)
			{
				_fusioncheckerCache.Remove(candidateFusion);
			}
		}
		return flag;
	}

	public override Expr MergedFusionRewriteCallBack(Expr mergedFusionBody)
	{
		DumpScope dumpScope = new DumpScope("MergedFusionClear");
		try
		{
			return CompilerServices.ERewrite(mergedFusionBody, new FoldStoreLoad[1]
			{
				new FoldStoreLoad()
			}, new RunPassContext());
		}
		finally
		{
			((IDisposable)dumpScope).Dispose();
		}
	}
}
