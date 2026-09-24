using System;
using System.Collections.Generic;
using Nncase.Diagnostics;
using Nncase.IR;
using Nncase.Passes.Mutators;
using Nncase.Passes.Rules.K230;

namespace Nncase.Passes.Rules.Tile;

internal sealed class GNNEFusionGroupMutator : FusionGroupMutator
{
	private readonly Dictionary<Fusion, IFusionChecker> _fusioncheckerCache;

	private bool _checked;

	public GNNEFusionGroupMutator(Dictionary<Fusion, IFusionChecker> fusioncheckerCache, IMergeRewriteRule rule, RunPassContext passOptions)
		: base(rule, passOptions)
	{
		_fusioncheckerCache = fusioncheckerCache;
		_checked = false;
	}

	public override bool MergedFusionCheckCallBack(Fusion mergedFusion, HashSet<Fusion> candidateFusions)
	{
		bool flag = false;
		if (!_checked)
		{
			MultiFusionChecker multiFusionChecker = new MultiFusionChecker();
			flag = multiFusionChecker.Check(mergedFusion, base.PassOptions);
			if (flag)
			{
				_checked = true;
				_fusioncheckerCache.Add(mergedFusion, multiFusionChecker);
				foreach (Fusion candidateFusion in candidateFusions)
				{
					_fusioncheckerCache.Remove(candidateFusion);
				}
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

	protected override Expr RewriteLeafCall(Call expr)
	{
		if (!_checked)
		{
			return base.RewriteLeafCall(expr);
		}
		return expr;
	}
}
