using System;
using System.Collections.Generic;
using Nncase.IR;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

public class NodeInfo
{
	public Call Op { get; set; }

	public SegmentND Ofmap { get; set; }

	public NodeBuffer Nb { get; set; }

	public List<NodeInfo> Children { get; set; }

	public int[] OutShape { get; set; }

	public NodeInfo(Call op, SegmentND ofmap, NodeBuffer nb, List<NodeInfo> children, int[] outshape)
	{
		Op = op;
		Ofmap = ofmap;
		Nb = nb;
		Children = children;
		OutShape = outshape;
	}

	public NodeInfo(Call op, SegmentND ofmap, NodeBuffer nb, List<NodeInfo> children)
	{
		Op = op;
		Ofmap = ofmap;
		Nb = nb;
		Children = children;
		OutShape = Array.Empty<int>();
	}

	public NodeInfo(Call op, List<NodeInfo> children)
	{
		Op = op;
		Ofmap = new SegmentND();
		Nb = new NodeBuffer();
		Children = children;
		OutShape = Array.Empty<int>();
	}
}
