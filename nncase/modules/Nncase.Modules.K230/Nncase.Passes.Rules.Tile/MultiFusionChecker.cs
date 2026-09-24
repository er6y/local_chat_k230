using System;
using System.Collections.Generic;
using System.Linq;
using Nncase.IR;
using Nncase.Passes.Rules.K230;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

internal sealed class MultiFusionChecker : IFusionChecker
{
	private readonly List<(FusionConvertVisitor, FusionType, FusionInfo)> _caches = new List<(FusionConvertVisitor, FusionType, FusionInfo)>();

	public PrimFunction Convert()
	{
		var (fusionConvertVisitor, fusionType, fusionInfo) = _caches.First();
		return fusionConvertVisitor.BuildSchedule(fusionType, fusionInfo);
	}

	public bool Check(Fusion fusion, RunPassContext passOptions)
	{
		FusionType[] obj = new FusionType[2]
		{
			FusionType.L2IfFullWPp,
			FusionType.L2IfSplitWPp
		};
		AllocateResult allocateResult = new AllocateResult
		{
			IsOk = false
		};
		FusionType[] array = obj;
		foreach (FusionType fusionType in array)
		{
			FusionConvertVisitor fusionConvertVisitor = new FusionConvertVisitor(passOptions, new TileOptions(Array.Empty<int>()));
			allocateResult = fusionConvertVisitor.TryL2Fuse(fusion, fusionType);
			if (allocateResult.IsOk)
			{
				List<NodeInfo> fusionInfo = fusionConvertVisitor.GetFusionInfo(fusion, allocateResult);
				_caches.Add((fusionConvertVisitor, fusionType, new FusionInfo(fusionInfo, allocateResult.LastOutShape, allocateResult.Items, fusion.Parameters.ToArray(), fusion)));
				if (_caches.Count > 1)
				{
					_caches.RemoveAt(0);
				}
				return true;
			}
		}
		return allocateResult.IsOk;
	}
}
