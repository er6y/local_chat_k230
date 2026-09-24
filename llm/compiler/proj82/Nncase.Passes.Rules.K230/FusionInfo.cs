using System.Collections.Generic;
using Nncase.IR;

namespace Nncase.Passes.Rules.K230;

public class FusionInfo
{
	public List<NodeInfo> FusedNodes { get; set; }

	public int[] LastOutShape { get; set; }

	public Dictionary<ItemName, MmuItem> Mmu { get; set; }

	public IRArray<Var> Inputs { get; set; }

	public Fusion Fusion { get; set; }

	public FusionInfo(List<NodeInfo> fusedNodes, int[] lastOutShape, Dictionary<ItemName, MmuItem> mmu, IRArray<Var> inputs, Fusion fusion)
	{
		FusedNodes = fusedNodes;
		LastOutShape = lastOutShape;
		Mmu = mmu;
		Inputs = inputs;
		Fusion = fusion;
	}
}
