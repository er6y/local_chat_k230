using System;
using System.Linq;
using Nncase.TIR;

namespace Nncase.Passes.Rules.K230;

public class TensorOnGlb : IBufferView<TensorOnGlb>
{
	private readonly SelectedRange[] _selectedRanges;

	private readonly int[] _stride;

	private readonly int[] _dimensions;

	public ReadOnlySpan<SelectedRange> SelectedRanges => _selectedRanges;

	public ReadOnlySpan<int> Stride => _stride;

	public ReadOnlySpan<int> Dimensions => _dimensions;

	public int GetGlbNElement => (int)Math.Ceiling(1.0 * (double)(Addr + Dimensions[0] * Stride[0]) / (double)DType.SizeInBytes);

	public int GlbNByte => Addr + Dimensions[0] * Stride[0];

	public int Addr { get; private set; }

	public MmuItem Mmu { get; set; }

	public int AllocatedBytes { get; set; }

	public int AddrOffset => SelectedRanges.ToArray().Zip(Stride.ToArray()).Aggregate(0, (int acc, (SelectedRange First, int Second) t) => acc + t.First.Start * t.Second);

	public int CurAddr => Addr + AddrOffset;

	public TensorOnGlb Parent { get; init; }

	public TensorOnGlb RootParent { get; init; }

	public DataType DType { get; init; }

	public bool IsSubView { get; init; }

	public TensorOnGlb this[SegmentND segments]
	{
		get
		{
			TileUtilities.Assert(segments.Count == SelectedRanges.Length, "segments.Count == SelectedRanges.Length", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 205);
			return new TensorOnGlb((from t in segments.Zip(SelectedRanges.ToArray())
				select t.Second.Slice(t.First)).ToArray(), this);
		}
	}

	public TensorOnGlb this[params Segment1D[] segments] => this[new SegmentND(segments)];

	public TensorOnGlb(ReadOnlySpan<int> fullShape, DataType dataType, int startAddr, MmuItem mmu = null)
	{
		DType = dataType;
		_selectedRanges = (from i in fullShape.ToArray()
			select new SelectedRange(0, i, Padding.Zero())).ToArray();
		_dimensions = (from it in SelectedRanges.ToArray()
			select it.End - it.Start).ToArray();
		_stride = (from i in TensorUtilities.GetStrides(fullShape)
			select i * DType.SizeInBytes).ToArray();
		IsSubView = false;
		Parent = this;
		RootParent = this;
		Addr = startAddr;
		Mmu = mmu ?? new MmuItem();
		AllocatedBytes = GlbNByte;
	}

	public TensorOnGlb(ReadOnlySpan<SelectedRange> shape_ranges, TensorOnGlb parent)
	{
		DType = parent.DType;
		_selectedRanges = shape_ranges.ToArray();
		_dimensions = _selectedRanges.Select((SelectedRange it) => it.End - it.Start).ToArray();
		_stride = parent.Stride.ToArray();
		IsSubView = true;
		Parent = parent;
		RootParent = parent.RootParent;
		Addr = parent.Addr;
		Mmu = parent.Mmu;
		AllocatedBytes = GlbNByte;
	}

	public int GetAddr(int dim0, int dim1, int dim2, int dim3, int pp = 0, int bufNum = 2)
	{
		TileUtilities.Assert(dim0 < Dimensions[0] || (dim0 == 0 && dim0 == Dimensions[0]), "dim0 < Dimensions[0] || (dim0 == 0 && dim0 == Dimensions[0])", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 217);
		TileUtilities.Assert(dim1 < Dimensions[1] || (dim1 == 0 && dim1 == Dimensions[1]), "dim1 < Dimensions[1] || (dim1 == 0 && dim1 == Dimensions[1])", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 218);
		TileUtilities.Assert(dim2 < Dimensions[2] || (dim2 == 0 && dim2 == Dimensions[2]), "dim2 < Dimensions[2] || (dim2 == 0 && dim2 == Dimensions[2])", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 219);
		TileUtilities.Assert(dim3 < Dimensions[3] || (dim3 == 0 && dim3 == Dimensions[3]), "dim3 < Dimensions[3] || (dim3 == 0 && dim3 == Dimensions[3])", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileHelper/SpaceSearcher.cs", 220);
		int num = 0;
		if (pp > 0)
		{
			num = Mmu.Width * GNNEEnv.GlbBankWidth * Mmu.Depth / bufNum;
			while (num % 4 != 0)
			{
				num--;
			}
			num *= pp;
		}
		return num + dim0 * Stride[0] + dim1 * Stride[1] + dim2 * Stride[2] + dim3 * Stride[3];
	}

	public void SetGlbStride(int noconflictH = 0, int noconflictC = 0, int noconflictN = 0, int alignedFactor = 1)
	{
		ReadOnlySpan<int> dimensions = Dimensions;
		if (noconflictH == 1)
		{
			int i;
			for (i = TileUtilities.GetAlignedNum(_stride[2], GNNEEnv.GlbBankWidth); i / GNNEEnv.GlbBankWidth % 2 != 1; i += GNNEEnv.GlbBankWidth)
			{
			}
			_stride[2] = i;
			_stride[2] = TileUtilities.GetAlignedNum(_stride[2], alignedFactor);
		}
		else
		{
			_stride[2] = dimensions[3] * _stride[3];
		}
		if (noconflictC == 1)
		{
			int j;
			for (j = TileUtilities.GetAlignedNum(_stride[1], GNNEEnv.GlbBankWidth); j / GNNEEnv.GlbBankWidth % 2 != 1; j += GNNEEnv.GlbBankWidth)
			{
			}
			_stride[1] = j;
		}
		else
		{
			_stride[1] = dimensions[2] * _stride[2];
		}
		if (noconflictN == 1)
		{
			int k;
			for (k = TileUtilities.GetAlignedNum(_stride[0], GNNEEnv.GlbBankWidth); k / GNNEEnv.GlbBankWidth % 2 != 1; k += GNNEEnv.GlbBankWidth)
			{
			}
			_stride[0] = k;
		}
		else
		{
			_stride[0] = dimensions[1] * _stride[1];
		}
	}

	public override string ToString()
	{
		return DType.GetDisplayName() + "[" + string.Join(",", _dimensions) + "]";
	}
}
