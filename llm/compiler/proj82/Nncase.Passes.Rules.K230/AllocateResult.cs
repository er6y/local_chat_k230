using System.Collections.Generic;

namespace Nncase.Passes.Rules.K230;

internal class AllocateResult
{
	public bool IsOk { get; set; }

	public Dictionary<ItemName, MmuItem> Items { get; set; }

	public List<BoxOnGlb> Boxes { get; set; }

	public int[] LastOutShape { get; set; }

	public Dictionary<ItemName, int> BufferSize { get; set; }

	public List<NodeInfo> NodeInfo { get; set; }

	public Dictionary<ItemName, TensorOnGlb> GlbMap { get; set; }

	public AllocateResult(bool isOk, Dictionary<ItemName, MmuItem> items, List<BoxOnGlb> boxes, int[] lastOutShape, Dictionary<ItemName, int> bufferSize, List<NodeInfo> nodeInfo, Dictionary<ItemName, TensorOnGlb> glbMap)
	{
		IsOk = isOk;
		Items = items;
		Boxes = boxes;
		LastOutShape = lastOutShape;
		BufferSize = bufferSize;
		NodeInfo = nodeInfo;
		GlbMap = glbMap;
	}

	public AllocateResult()
	{
	}
}
