namespace Nncase.Passes.Rules.K230;

public class NodeBuffer
{
	public int OfBufferIndex { get; set; }

	public int OutputsSize { get; set; }

	public int OfmapOffset { get; set; }

	public int WeightOffset { get; set; }

	public int ActOffset { get; set; }

	public int WeightQargOffset { get; set; }

	public int DwWeightOffset { get; set; }

	public int DwActOffset { get; set; }

	public int DwWeightQargOffset { get; set; }

	public int Ifmap2Offset { get; set; }

	public int Act1Offset { get; set; }

	public int Pdp0ActOffset { get; set; }

	public int WeightPreloadOffset { get; set; } = -1;


	public AlignedType AlignType { get; set; }

	public NodeBuffer()
	{
	}

	public NodeBuffer(NodeBuffer other)
	{
		OfBufferIndex = other.OfBufferIndex;
		OutputsSize = other.OutputsSize;
		OfmapOffset = other.OfmapOffset;
		WeightOffset = other.WeightOffset;
		ActOffset = other.ActOffset;
		WeightQargOffset = other.WeightQargOffset;
		DwWeightOffset = other.DwWeightOffset;
		DwActOffset = other.DwActOffset;
		DwWeightQargOffset = other.DwWeightQargOffset;
		Ifmap2Offset = other.Ifmap2Offset;
		Act1Offset = other.Act1Offset;
		Pdp0ActOffset = other.Pdp0ActOffset;
		WeightPreloadOffset = other.WeightPreloadOffset;
		AlignType = other.AlignType;
	}
}
