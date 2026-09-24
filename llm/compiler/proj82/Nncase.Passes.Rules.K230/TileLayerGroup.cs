using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.Tensors;
using Nncase.Runtime.K230;
using Nncase.TIR;
using Nncase.TIR.Builders;
using Nncase.TIR.Instructions;

namespace Nncase.Passes.Rules.K230;

public class TileLayerGroup
{
	private enum L1FusedType
	{
		NoFused,
		FusedPool,
		FusedAct1,
		FusedDw
	}

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<SegmentND, global::_003C_003Ef__AnonymousType1<SegmentND, int>> _003C_003E9__109_0;

		public static Func<global::_003C_003Ef__AnonymousType1<SegmentND, int>, _003C_003Ef__AnonymousType2<global::_003C_003Ef__AnonymousType1<SegmentND, int>, int>> _003C_003E9__109_1;

		public static Func<_003C_003Ef__AnonymousType3<_003C_003Ef__AnonymousType2<global::_003C_003Ef__AnonymousType1<SegmentND, int>, int>, int[]>, SegmentND> _003C_003E9__109_3;

		public static Func<_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>>, IEnumerable<Segment1D>> _003C_003E9__111_5;

		public static Func<_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>>, Segment1D, _003C_003Ef__AnonymousType6<_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>>, Segment1D>> _003C_003E9__111_6;

		public static Func<_003C_003Ef__AnonymousType7<_003C_003Ef__AnonymousType6<_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>>, Segment1D>, List<Segment1D>>, IEnumerable<Segment1D>> _003C_003E9__111_8;

		public static Func<NodeInfo, int> _003C_003E9__111_12;

		public static Func<int, int, int, Tuple<int, int>> _003C_003E9__116_0;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_0;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_1;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_2;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_3;

		public static Func<List<Tuple<SegmentND, TensorStat>>, bool> _003C_003E9__118_5;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_4;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_6;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>, _003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__118_7;

		public static Func<_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>>, bool> _003C_003E9__118_8;

		public static Func<_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003C_003E9__118_9;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__119_0;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__119_1;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, IEnumerable<List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__119_2;

		public static Func<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>, _003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>>> _003C_003E9__119_3;

		public static Func<_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>>, bool> _003C_003E9__119_4;

		public static Func<_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003C_003E9__119_5;

		internal global::_003C_003Ef__AnonymousType1<SegmentND, int> _003CArrangeDwWeights_003Eb__109_0(SegmentND wg)
		{
			return new global::_003C_003Ef__AnonymousType1<Nncase.TIR.SegmentND, int>(wg, wg[1].Start);
		}

		internal _003C_003Ef__AnonymousType2<global::_003C_003Ef__AnonymousType1<SegmentND, int>, int> _003CArrangeDwWeights_003Eb__109_1(global::_003C_003Ef__AnonymousType1<SegmentND, int> _003C_003Eh__TransparentIdentifier0)
		{
			return new global::_003C_003Ef__AnonymousType2<global::_003C_003Ef__AnonymousType1<Nncase.TIR.SegmentND, int>, int>(_003C_003Eh__TransparentIdentifier0, _003C_003Eh__TransparentIdentifier0.wg[1].End);
		}

		internal SegmentND _003CArrangeDwWeights_003Eb__109_3(_003C_003Ef__AnonymousType3<_003C_003Ef__AnonymousType2<global::_003C_003Ef__AnonymousType1<SegmentND, int>, int>, int[]> _003C_003Eh__TransparentIdentifier2)
		{
			return new SegmentND(..1, _003C_003Eh__TransparentIdentifier2._003C_003Eh__TransparentIdentifier1._003C_003Eh__TransparentIdentifier0.cs.._003C_003Eh__TransparentIdentifier2._003C_003Eh__TransparentIdentifier1.ce, .._003C_003Eh__TransparentIdentifier2.dwShape[2], .._003C_003Eh__TransparentIdentifier2.dwShape[3]);
		}

		internal IEnumerable<Segment1D> _003CGetSliceInfo_003Eb__111_5(_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>> _003C_003Eh__TransparentIdentifier0)
		{
			return _003C_003Eh__TransparentIdentifier0.outputRowSeg;
		}

		internal _003C_003Ef__AnonymousType6<_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>>, Segment1D> _003CGetSliceInfo_003Eb__111_6(_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>> _003C_003Eh__TransparentIdentifier0, Segment1D glbOutputRow)
		{
			return new global::_003C_003Ef__AnonymousType6<global::_003C_003Ef__AnonymousType5<Nncase.TIR.Segment1D, System.Collections.Generic.List<Nncase.TIR.Segment1D>>, Nncase.TIR.Segment1D>(_003C_003Eh__TransparentIdentifier0, glbOutputRow);
		}

		internal IEnumerable<Segment1D> _003CGetSliceInfo_003Eb__111_8(_003C_003Ef__AnonymousType7<_003C_003Ef__AnonymousType6<_003C_003Ef__AnonymousType5<Segment1D, List<Segment1D>>, Segment1D>, List<Segment1D>> _003C_003Eh__TransparentIdentifier2)
		{
			return _003C_003Eh__TransparentIdentifier2.outputColSeg;
		}

		internal int _003CGetSliceInfo_003Eb__111_12(NodeInfo node)
		{
			return node.Nb.OfBufferIndex;
		}

		internal Tuple<int, int> _003CGetGlbLayouts_003Eb__116_0(int a, int b, int alignElement)
		{
			int item = a;
			int item2 = b;
			if (a * b % alignElement == 0)
			{
				return new Tuple<int, int>(item, item2);
			}
			int num = a + alignElement - 1;
			int num2 = b + alignElement - 1;
			long num3 = 4294836225L;
			for (int i = a; i < num; i++)
			{
				for (int j = b; j < num2; j++)
				{
					int num4 = i * j;
					if (num4 % alignElement == 0 && num4 < num3)
					{
						num3 = num4;
						item = i;
						item2 = j;
					}
				}
			}
			return new Tuple<int, int>(item, item2);
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_0(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_1(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_2(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_3(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_4(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter.Where((List<Tuple<SegmentND, TensorStat>> t) => t.Count > 0);
		}

		internal bool _003CUpdateCcrRecStat_003Eb__118_5(List<Tuple<SegmentND, TensorStat>> t)
		{
			return t.Count > 0;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_6(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal _003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003CUpdateCcrRecStat_003Eb__118_7(List<List<Tuple<SegmentND, TensorStat>>> iter, List<Tuple<SegmentND, TensorStat>> t)
		{
			return new global::_003C_003Ef__AnonymousType8<System.Collections.Generic.List<System.Collections.Generic.List<System.Tuple<Nncase.TIR.SegmentND, TensorStat>>>, System.Collections.Generic.List<System.Tuple<Nncase.TIR.SegmentND, TensorStat>>>(iter, t);
		}

		internal bool _003CUpdateCcrRecStat_003Eb__118_8(_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003C_003Eh__TransparentIdentifier0)
		{
			return _003C_003Eh__TransparentIdentifier0.t.Count > 0;
		}

		internal List<Tuple<SegmentND, TensorStat>> _003CUpdateCcrRecStat_003Eb__118_9(_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003C_003Eh__TransparentIdentifier0)
		{
			return _003C_003Eh__TransparentIdentifier0.t;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateAi2dCcrRecStat_003Eb__119_0(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateAi2dCcrRecStat_003Eb__119_1(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal IEnumerable<List<Tuple<SegmentND, TensorStat>>> _003CUpdateAi2dCcrRecStat_003Eb__119_2(List<List<Tuple<SegmentND, TensorStat>>> iter)
		{
			return iter;
		}

		internal _003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003CUpdateAi2dCcrRecStat_003Eb__119_3(List<List<Tuple<SegmentND, TensorStat>>> iter, List<Tuple<SegmentND, TensorStat>> t)
		{
			return new global::_003C_003Ef__AnonymousType8<System.Collections.Generic.List<System.Collections.Generic.List<System.Tuple<Nncase.TIR.SegmentND, TensorStat>>>, System.Collections.Generic.List<System.Tuple<Nncase.TIR.SegmentND, TensorStat>>>(iter, t);
		}

		internal bool _003CUpdateAi2dCcrRecStat_003Eb__119_4(_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003C_003Eh__TransparentIdentifier0)
		{
			return _003C_003Eh__TransparentIdentifier0.t.Count > 0;
		}

		internal List<Tuple<SegmentND, TensorStat>> _003CUpdateAi2dCcrRecStat_003Eb__119_5(_003C_003Ef__AnonymousType8<List<List<Tuple<SegmentND, TensorStat>>>, List<Tuple<SegmentND, TensorStat>>> _003C_003Eh__TransparentIdentifier0)
		{
			return _003C_003Eh__TransparentIdentifier0.t;
		}
	}

	private static int _count = -1;

	private readonly ILogger<TileLayerGroup> _logger = CompileSessionScope.GetCurrentThrowIfNull().GetRequiredService<ILogger<TileLayerGroup>>();

	private readonly Dictionary<Call, WeightGroupHandler> _weightGroups = new Dictionary<Call, WeightGroupHandler>();

	private readonly Dictionary<Call, Tuple<int, int>> _weightSplitPattern = new Dictionary<Call, Tuple<int, int>>();

	private readonly List<Tuple<Call, int>> _nodesQueNeedClearFake = new List<Tuple<Call, int>>();

	private readonly Dictionary<Var, Nncase.TIR.Buffer> _ifBufferMap = new Dictionary<Var, Nncase.TIR.Buffer>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<Call, L1FusedType> _l1FusedInfos = new Dictionary<Call, L1FusedType>();

	private CcrHandler _ccrHandler = new CcrHandler();

	private GprHandler _gpr = new GprHandler();

	private SsrHandler _ssr = new SsrHandler();

	private NodeInfo _preNi;

	private NodeInfo _ni;

	private NodeInfo _l1FuseNi;

	private bool _l1Fused;

	private bool _swapAB;

	private bool _h2C;

	private int _memsetValue;

	private SegmentND _ifmap;

	private SegmentND _ifmap2;

	private int _ifmapOffset;

	private int _ifmap2Offset;

	private ItemName _src2ItemName;

	private SegmentND _ofmap;

	private int _ofmapOffset;

	private SegmentND _ofmapSt;

	private SegmentND _ifmapLd;

	private SegmentND _ofmapConv;

	private SegmentND _ifmapA;

	private SegmentND _ifmapB;

	private DataType _inputType;

	private DataType _outputType;

	private DataType _weightType;

	private DataType _if2Type;

	private Call _conv;

	private Call _pool;

	private Call _dw;

	private Call _act1;

	private Call _resize;

	private int _icPerGroup;

	private int _ocPerGroup;

	private int _groupPerPass;

	private Call? _lif;

	private Call? _lw;

	private Call? _lact;

	private Call? _lwQarg;

	private Call? _sof;

	private int[]? _inputShape;

	private int[]? _outputShape;

	private int[]? _convOutputShape;

	private int[]? _weightsShape;

	private Padding? _paddingH;

	private Padding? _paddingW;

	private int _strideH;

	private int _strideW;

	private int _dilationH;

	private int _dilationW;

	private int _groups;

	private int _fusedKernelH;

	private int _fusedKernelW;

	private Padding? _fusedPaddingH;

	private Padding? _fusedPaddingW;

	private int _fusedStrideH;

	private int _fusedStrideW;

	private int _fusedDilationH;

	private int _fusedDilationW;

	private Call? _lif2;

	private Call? _pdp1;

	private Call? _transpose;

	private Call? _cat;

	private WeightGroupHandler _weightGroup = new WeightGroupHandler(DataTypes.UInt8, DataTypes.UInt8);

	private SegmentND? _weight;

	private bool _isGlobalPdp;

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesWeightRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesOfmapRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesG2LIfRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesG2RWRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesL2GOfRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesL2RIf2Rec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesG2RWSliceRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesAi2dIfRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>> _nodesAi2dOfRec = new Dictionary<Call, List<List<Tuple<SegmentND, TensorStat>>>>();

	private List<List<Tuple<Call, int>>> _nodesQuenesAsW = new List<List<Tuple<Call, int>>>(2)
	{
		new List<Tuple<Call, int>>(),
		new List<Tuple<Call, int>>()
	};

	private int _if1BufIdx = -1;

	private int _if2BufIdx = -1;

	private int _ofBufIdx = -1;

	private int _weightBufIdx = -1;

	public PrimFunction BuildSchedule(FusionInfo fusionInfo)
	{
		_count++;
		TiledGlb glb = new TiledGlb();
		InitParameters(fusionInfo);
		GetSliceInfo(fusionInfo, glb, out List<List<NodeInfo>> currSliceInfo, out List<Dictionary<Call, NodeInfo>> preSliceInfo);
		List<Sequential> list = new List<Sequential>();
		List<Nncase.TIR.Buffer> list2 = new List<Nncase.TIR.Buffer>();
		List<Nncase.TIR.Buffer> list3 = new List<Nncase.TIR.Buffer>();
		_gpr = new GprHandler(GNNEEnv.GprNum);
		_ssr = new SsrHandler(GNNEEnv.SsrNum);
		_ccrHandler = new CcrHandler();
		list.Add(BuildMmu(glb));
		ItemRecStatusInit(currSliceInfo);
		for (int i = 0; i < currSliceInfo.Count; i++)
		{
			List<NodeInfo> list4 = currSliceInfo[i];
			Dictionary<Call, NodeInfo> sliceInfo = preSliceInfo[i];
			for (int j = 0; j < list4.Count; j += ((!_l1Fused) ? 1 : 2))
			{
				UpdateL2FusePara(fusionInfo, list4[j], sliceInfo, glb, weightGroupOnly: true, j == 0);
				ItemRecStatusUpdate();
				if ((object)_conv != null)
				{
					BuildConv2d(glb, weightGroupOnly: true, list2);
				}
				if ((object)_resize != null)
				{
					BuildResize(glb, list4[i], weightGroupOnly: true);
				}
			}
		}
		UpdateCcrRecStat();
		UpdateAi2dCcrRecStat();
		ILogger<TileLayerGroup> logger = _logger;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 3);
		List<NodeInfo> fusedNodes = fusionInfo.FusedNodes;
		defaultInterpolatedStringHandler.AppendFormatted(fusedNodes[fusedNodes.Count - 1].Op);
		defaultInterpolatedStringHandler.AppendLiteral(" -> slice: ");
		defaultInterpolatedStringHandler.AppendFormatted(currSliceInfo.Count);
		defaultInterpolatedStringHandler.AppendLiteral(", layer: ");
		defaultInterpolatedStringHandler.AppendFormatted(currSliceInfo[0].Count);
		logger.LogTrace(defaultInterpolatedStringHandler.ToStringAndClear());
		for (int k = 0; k < currSliceInfo.Count; k++)
		{
			List<NodeInfo> list5 = currSliceInfo[k];
			Dictionary<Call, NodeInfo> sliceInfo2 = preSliceInfo[k];
			bool flag = k == 0;
			List<Nncase.TIR.Buffer> list6 = new List<Nncase.TIR.Buffer>(list2);
			List<Nncase.TIR.Buffer> list7 = new List<Nncase.TIR.Buffer>(list3);
			ILogger<TileLayerGroup> logger2 = _logger;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 4);
			defaultInterpolatedStringHandler.AppendLiteral("dim2-> start: ");
			defaultInterpolatedStringHandler.AppendFormatted(list5[list5.Count - 1].Ofmap[2].Start);
			defaultInterpolatedStringHandler.AppendLiteral(", end: ");
			defaultInterpolatedStringHandler.AppendFormatted(list5[list5.Count - 1].Ofmap[2].End);
			defaultInterpolatedStringHandler.AppendLiteral(", dim3-> start: ");
			defaultInterpolatedStringHandler.AppendFormatted(list5[list5.Count - 1].Ofmap[3].Start);
			defaultInterpolatedStringHandler.AppendLiteral(", end: ");
			defaultInterpolatedStringHandler.AppendFormatted(list5[list5.Count - 1].Ofmap[3].End);
			logger2.LogTrace(defaultInterpolatedStringHandler.ToStringAndClear());
			for (int l = 0; l < list5.Count; l += ((!_l1Fused) ? 1 : 2))
			{
				UpdateL2FusePara(fusionInfo, list5[l], sliceInfo2, glb, weightGroupOnly: false, l == 0);
				if ((object)_lif != null)
				{
					int iPp = 0;
					list.Add(BuildLoadIf(glb, iPp, list2, flag, list6));
				}
				if ((object)_conv != null)
				{
					list.Add(BuildConv2d(glb, weightGroupOnly: false, list2, flag, list6));
				}
				if ((object)_act1 != null && !_l1Fused)
				{
					list.Add(BuildAct1(glb, list2, flag, flag, list6));
				}
				if ((object)_pdp1 != null)
				{
					list.Add(BuildPdp1(glb));
				}
				if ((object)_transpose != null)
				{
					list.Add(BuildTranspose(glb));
				}
				_ = _cat;
				if ((object)_resize != null)
				{
					BuildResize(glb, list5[k], weightGroupOnly: false);
				}
				if ((object)_sof != null)
				{
					int ofPp = 0;
					list.Add(BuildStore(glb, ofPp, list3, preSliceInfo[k], flag, list7, fusionInfo.Fusion));
				}
			}
			TileUtilities.Assert(list6.Count == 0 && list7.Count == 0, "ifBuffersCopy.Count == 0 && ofBuffersCopy.Count == 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 260);
		}
		TileUtilities.Assert(_ccrHandler.CcrSanityCheck(), "_ccrHandler.CcrSanityCheck()", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 263);
		List<Nncase.TIR.Buffer> list8 = fusionInfo.Inputs.Select((Var v) => _ifBufferMap[v]).ToList();
		list8.AddRange(list3);
		ISequentialBuilder<PrimFunction> sequentialBuilder = T.PrimFunc($"TileLayerGroup_{_count}", K230RtModule.Kind, list8.ToArray());
		object[] exprOrBuilders = list.ToArray();
		return sequentialBuilder.Body(exprOrBuilders).Body(I.END(GP_REGISTER.x0)).Build();
	}

	private Sequential BuildMmu(TiledGlb glb)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		gnneActionUpdater.UpdateMmuConf();
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildLoadIf(TiledGlb glb, int iPp, List<Nncase.TIR.Buffer> ifBuffers, bool isFirstSlice, List<Nncase.TIR.Buffer> ifBuffersCopy)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		Nncase.TIR.Buffer buffer;
		if (isFirstSlice)
		{
			Call call = _lif;
			if (_lif[GNNELoad.Input] is Call call2 && call2.Target is Reshape)
			{
				call = call2;
			}
			T.CreateBuffer(new TensorType(_lif[GNNELoad.Input].CheckedDataType, call.CheckedShape), MemoryLocation.Input, out buffer, "ddrIf");
			ifBuffers.Add(buffer);
			_ifBufferMap.Add(((object)call != null && call.Target is GNNELoad) ? ((Var)call[GNNELoad.Input]) : ((Var)call[Reshape.Input]), buffer);
		}
		else
		{
			buffer = ifBuffersCopy[0];
			ifBuffersCopy.RemoveAt(0);
		}
		GetCcrSetAndClrVec(_ni).Deconstruct<List<CcrSet>, List<CcrClr>>(out var item, out var item2);
		List<CcrSet> ccrsToSet = item;
		List<CcrClr> ccrsToClr = item2;
		List<int> stridesD = new int[3]
		{
			glb.GlbMap[ItemName.Ofmap].Dimensions[1],
			glb.GlbMap[ItemName.Ofmap].Dimensions[2],
			glb.GlbMap[ItemName.Ofmap].Dimensions[3]
		}.ToList();
		gnneActionUpdater.UpdateLoadIf(_ofmapSt, _lif, iPp, buffer, _ni.Nb.OfmapOffset, stridesD, ItemName.Ifmap, ccrsToSet, ccrsToClr, _h2C, _memsetValue, null, null, _lif.CheckedShape.ToValueArray());
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildStore(TiledGlb glb, int ofPp, List<Nncase.TIR.Buffer> ofBuffers, Dictionary<Call, NodeInfo> preSliceInfo, bool isFirstSlice, List<Nncase.TIR.Buffer> ofBuffersCopy, Fusion fusion)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		Nncase.TIR.Buffer buffer;
		if (isFirstSlice)
		{
			T.CreateBuffer(new TensorType(_sof.CheckedDataType, fusion.Body.CheckedShape), MemoryLocation.Output, out buffer, "ddrOf");
			ofBuffers.Add(buffer);
		}
		else
		{
			buffer = ofBuffersCopy[0];
			ofBuffersCopy.RemoveAt(0);
		}
		var (ccrsToSet, ccrsToClr) = GetCcrSetAndClrVec(_ni);
		gnneActionUpdater.UpdateStoreT(_ofmapSt, _sof, ofPp, buffer, preSliceInfo[_ni.Op[GNNEStore.Input] as Call].Nb.OfmapOffset, null, ccrsToSet, ccrsToClr, ItemName.Ifmap);
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildConv2d(TiledGlb glb, bool weightGroupOnly, List<Nncase.TIR.Buffer> ifBuffers, bool firstSlice = false, List<Nncase.TIR.Buffer>? ifBuffersCopy = null)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		byte[] array = ((TensorConst)((Call)_ni.Op[GNNEConv2D.Weights])[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
		byte[] array2 = new byte[array.Length];
		Array.Copy(array, array2, array2.Length);
		T.AttachBuffer(Const.FromTensor(Tensor.FromBytes(DataTypes.UInt8, array2.ToArray(), new int[1] { array2.Length })), out Nncase.TIR.Buffer buffer, "var ddrW");
		T.AttachBuffer((TensorConst)((Call)_ni.Op[GNNEConv2D.WeightsBias])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer2, "var ddrWQarg");
		T.AttachBuffer((TensorConst)((Call)_ni.Op[GNNEConv2D.Act])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer3, "var ddrAct");
		Nncase.TIR.Buffer buffer4 = null;
		Nncase.TIR.Buffer buffer5 = null;
		Nncase.TIR.Buffer buffer6 = null;
		Nncase.TIR.Buffer buffer7 = null;
		Nncase.TIR.Buffer buffer8 = null;
		Nncase.TIR.Buffer buffer9 = null;
		if ((object)_dw != null)
		{
			byte[] array3 = ((TensorConst)((Call)_dw[GNNEPdp0DW.Weights])[GNNELoadW.Input]).Value.BytesBuffer.ToArray();
			byte[] array4 = new byte[array3.Length];
			Array.Copy(array3, array4, array4.Length);
			T.AttachBuffer(Const.FromTensor(Tensor.FromBytes(DataTypes.UInt8, array4.ToArray(), new int[1] { array4.Length })), out buffer4, "ddrDW");
			T.AttachBuffer((TensorConst)((Call)_dw[GNNEPdp0DW.WeightsBias])[GNNELoadW.Input], out buffer5, "ddrDWQarg");
			T.AttachBuffer((TensorConst)((Call)_dw[GNNEPdp0DW.Act])[GNNELoadW.Input], out buffer6, "ddrDWAct");
		}
		if ((object)_pool != null)
		{
			T.AttachBuffer((TensorConst)((Call)_pool[GNNEPdp0Reduce.Act])[GNNELoadW.Input], out buffer8, "ddrPdpAct");
		}
		if ((object)_act1 != null)
		{
			T.AttachBuffer((TensorConst)((Call)_act1[GNNEActivation.Act])[GNNELoadW.Input], out buffer7, "ddrAct1");
			if ((object)_lif2 != null)
			{
				if (!(_lif2[GNNELoad.Input] is TensorConst))
				{
					if (!weightGroupOnly && firstSlice)
					{
						T.CreateBuffer(new TensorType(_lif2[GNNELoad.Input].CheckedDataType, _lif2.CheckedShape), MemoryLocation.Input, out buffer9, "ddrIf2");
						ifBuffers.Add(buffer9);
						_ifBufferMap.Add((Var)_lif2[GNNELoad.Input], buffer9);
					}
					else if (!weightGroupOnly && !firstSlice)
					{
						buffer9 = ifBuffersCopy[0];
						ifBuffersCopy.RemoveAt(0);
					}
				}
				else
				{
					T.AttachBuffer((TensorConst)_lif2[GNNELoad.Input], out buffer9, "ddrIf2");
				}
			}
		}
		BuildScheduleConv(gnneActionUpdater, glb, buffer, buffer2, buffer3, buffer4, buffer5, buffer6, buffer7, buffer8, buffer9, weightGroupOnly, firstSlice);
		if (weightGroupOnly)
		{
			return null;
		}
		Span<byte> bytesBuffer = buffer.Const().Value.BytesBuffer;
		ArrangeWeights(_weightType, _weightsShape, bytesBuffer, _weightGroup);
		if ((object)_dw != null)
		{
			ArrangeDwWeights(_dw[GNNEPdp0DW.Weights].CheckedDataType, _dw[GNNEPdp0DW.Weights].CheckedShape.ToValueArray(), buffer4.Const().Value.BytesBuffer, _weightGroup);
		}
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildAct1(TiledGlb glb, List<Nncase.TIR.Buffer> ifBuffers, bool firstSlice, bool isFirstSlice, List<Nncase.TIR.Buffer> ifBuffersCopy)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		Nncase.TIR.Buffer buffer = null;
		T.AttachBuffer((TensorConst)((Call)_act1[GNNEActivation.Act])[GNNELoadW.Input], out Nncase.TIR.Buffer buffer2, "var ddrAct1");
		if ((object)_lif2 != null)
		{
			if (!(_lif2[GNNELoad.Input] is TensorConst))
			{
				if (isFirstSlice)
				{
					T.CreateBuffer(new TensorType(_lif2[GNNELoad.Input].CheckedDataType, _lif2.CheckedShape), MemoryLocation.Input, out buffer, "ddrIf2");
					ifBuffers.Add(buffer);
					_ifBufferMap.Add((Var)_lif2[GNNELoad.Input], buffer);
				}
				else
				{
					buffer = ifBuffersCopy[0];
					ifBuffersCopy.RemoveAt(0);
				}
			}
			else
			{
				T.AttachBuffer((TensorConst)_lif2[GNNELoad.Input], out buffer, "ddrIf2");
			}
		}
		BuildScheduleAct1(gnneActionUpdater, glb, buffer2, buffer, firstSlice);
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildPdp1(TiledGlb glb)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		BuildSchedulePdp1(gnneActionUpdater, glb);
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildTranspose(TiledGlb glb)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		BuildScheduleTranspose(gnneActionUpdater, glb);
		return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
	}

	private Sequential BuildResize(TiledGlb glb, NodeInfo currNode, bool weightGroupOnly, bool firstSlice = false)
	{
		GnneActionUpdater gnneActionUpdater = new GnneActionUpdater(new List<GnneAction>(), glb, _ccrHandler, _gpr, _ssr);
		BuildScheduleResize(gnneActionUpdater, glb, weightGroupOnly, currNode);
		if (!weightGroupOnly)
		{
			return new ActionToInstruct().Instructions(gnneActionUpdater.Actions);
		}
		return null;
	}

	private void BuildScheduleConv(GnneActionUpdater actionUpdater, TiledGlb glb, Nncase.TIR.Buffer ddrW, Nncase.TIR.Buffer ddrWQarg, Nncase.TIR.Buffer ddrAct, Nncase.TIR.Buffer ddrDw, Nncase.TIR.Buffer ddrDWQarg, Nncase.TIR.Buffer ddrDWAct, Nncase.TIR.Buffer ddrAct1, Nncase.TIR.Buffer ddrPdpAct, Nncase.TIR.Buffer ddrIf2, bool weightGroupOnly, bool firstSlice = false)
	{
		int iPp = 0;
		int num = 0;
		int wPp = 0;
		if (!weightGroupOnly && firstSlice)
		{
			if (_weightType == DataTypes.UInt8 || _weightType == DataTypes.Int16)
			{
				List<CcrSet> list = new List<CcrSet>();
				List<CcrClr> ccrsToClr = null;
				list.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WQarg)), 1));
				actionUpdater.UpdateLoadWQarg(_lwQarg, _weightGroup, ddrWQarg, _ni.Nb.WeightQargOffset, list, ccrsToClr);
			}
			List<CcrSet> list2 = new List<CcrSet>();
			List<CcrClr> ccrsToClr2 = new List<CcrClr>();
			list2.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Act)), 1));
			actionUpdater.UpdateLoadAct(_lact, ddrAct, ItemName.Act, _ni.Nb.ActOffset, list2, ccrsToClr2);
			if ((object)_dw != null)
			{
				List<CcrSet> list3 = new List<CcrSet>();
				List<CcrClr> ccrsToClr3 = null;
				list3.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwWeight)), 1));
				actionUpdater.UpdateLoadDw(_dw, _weightGroup, ddrDw, _l1FuseNi.Nb.DwWeightOffset, list3, ccrsToClr3);
				if (_dw[GNNEPdp0DW.Weights].CheckedDataType == DataTypes.UInt8)
				{
					list3 = new List<CcrSet>();
					ccrsToClr3 = null;
					list3.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwQarg)), 1));
					actionUpdater.UpdateLoadDwQarg(_dw, _weightGroup, ddrDWQarg, _l1FuseNi.Nb.DwWeightQargOffset, list3, ccrsToClr3);
				}
			}
			if ((object)_act1 != null)
			{
				List<CcrSet> list4 = new List<CcrSet>();
				List<CcrClr> ccrsToClr4 = null;
				list4.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.MfuAct1)), 1));
				actionUpdater.UpdateLoadAct(_act1[GNNEActivation.Act] as Call, ddrAct1, ItemName.MfuAct1, _l1FuseNi.Nb.Act1Offset, list4, ccrsToClr4);
			}
			if ((object)_dw != null)
			{
				List<CcrSet> list5 = new List<CcrSet>();
				List<CcrClr> ccrsToClr5 = null;
				list5.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwAct1)), 1));
				actionUpdater.UpdateLoadAct(_dw[GNNEPdp0DW.Act] as Call, ddrDWAct, ItemName.DwAct1, _l1FuseNi.Nb.DwActOffset, list5, ccrsToClr5);
			}
			else if ((object)_pool != null)
			{
				List<CcrSet> list6 = new List<CcrSet>();
				List<CcrClr> ccrsToClr6 = null;
				list6.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.PdpAct1)), 1));
				actionUpdater.UpdateLoadAct(_pool[GNNEPdp0Reduce.Act] as Call, ddrPdpAct, ItemName.PdpAct1, _l1FuseNi.Nb.Pdp0ActOffset, list6, ccrsToClr6);
			}
		}
		_weightGroup.Current_aligned_offset_init();
		if (weightGroupOnly && _weightBufIdx != -1)
		{
			_nodesWeightRec[_conv][_weightBufIdx].Add(new Tuple<SegmentND, TensorStat>(_weight, new TensorStat(isFirstSlice: false, isLastSlice: false)));
		}
		if (!weightGroupOnly && (object)_act1 != null && (object)_lif2 != null && _ifmap[1].End == _inputShape[1])
		{
			TileUtilities.Assert(_if2BufIdx == -1, "_if2BufIdx == -1", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 559);
			TensorStat item = _nodesL2RIf2Rec[_conv][0][0].Item2;
			int value = ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
			List<CcrSet> ccrsToSet = new List<CcrSet>
			{
				new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2)), value)
			};
			List<int> stridesD = new List<int>
			{
				glb.GlbMap[ItemName.Ifmap2].Dimensions[1],
				glb.GlbMap[ItemName.Ifmap2].Dimensions[2],
				glb.GlbMap[ItemName.Ifmap2].Dimensions[3]
			};
			actionUpdater.UpdateLoadIf(_ifmap2, _lif2, num, ddrIf2, _ifmap2Offset, stridesD, _src2ItemName, ccrsToSet);
		}
		BuildL1Schedule(actionUpdater, glb, _ifmap, _weight, _ofmap, iPp, num, wPp, _ifmap2, weightGroupOnly, _weightGroup, ddrW);
		if (weightGroupOnly)
		{
			_nodesOfmapRec[_conv][_ofBufIdx].Add(new Tuple<SegmentND, TensorStat>(_ofmap, new TensorStat(isFirstSlice: false, isLastSlice: false)));
		}
		else
		{
			_nodesOfmapRec[_conv][_ofBufIdx].RemoveAt(0);
		}
	}

	private void BuildScheduleResize(GnneActionUpdater actionUpdater, TiledGlb glb, bool weightGroupOnly, NodeInfo currNode)
	{
	}

	private void BuildScheduleAct1(GnneActionUpdater actionUpdater, TiledGlb glb, Nncase.TIR.Buffer ddrAct, Nncase.TIR.Buffer ddrIf2, bool firstSlice = false)
	{
		int iPp = 0;
		List<CcrSet> list = new List<CcrSet>();
		List<CcrClr> ccrsToClr = null;
		if (firstSlice)
		{
			list.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.MfuAct1)), 1));
			actionUpdater.UpdateLoadAct(_lact, ddrAct, ItemName.MfuAct1, _ni.Nb.Act1Offset, list, ccrsToClr);
		}
		if ((object)_lif2 != null)
		{
			List<CcrSet> ccrsToSet = new List<CcrSet>
			{
				new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2)), 1)
			};
			List<int> stridesD = new List<int>
			{
				glb.GlbMap[ItemName.Ifmap2].Dimensions[1],
				glb.GlbMap[ItemName.Ifmap2].Dimensions[2],
				glb.GlbMap[ItemName.Ifmap2].Dimensions[3]
			};
			actionUpdater.UpdateLoadIf(_ifmap2, _lif2, iPp, ddrIf2, _ifmap2Offset, stridesD, ItemName.Ifmap2, ccrsToSet);
		}
		bool flag = _act1[GNNEActivation.InputB] == None.Default;
		DataType checkedDataType = _act1[GNNEActivation.InputA].CheckedDataType;
		DataType dataType = checkedDataType;
		if (!flag)
		{
			dataType = _act1[GNNEActivation.InputB].CheckedDataType;
		}
		DeQuantizeParam deQuantizeParam = new DeQuantizeParam(0, 1f);
		if (checkedDataType != DataTypes.Float16)
		{
			deQuantizeParam = ((TensorConst)_act1[GNNEActivation.DeqAParams]).Value.ToScalar<DeQuantizeParam>();
		}
		DeQuantizeParam deqParams = deQuantizeParam;
		if (!flag && dataType != DataTypes.Float16)
		{
			deqParams = ((TensorConst)_act1[GNNEActivation.DeqBParams]).Value.ToScalar<DeQuantizeParam>();
		}
		int num = ((TensorConst)_act1[GNNEActivation.InAShiftBits]).Value.ToScalar<int>();
		int rshiftBits = num;
		if (!flag)
		{
			rshiftBits = ((TensorConst)_act1[GNNEActivation.InBShiftBits]).Value.ToScalar<int>();
		}
		int rshiftBitsD = ((TensorConst)_act1[GNNEActivation.OutShiftBits]).Value.ToScalar<int>();
		bool is16Segments = ((TensorConst)_act1[GNNEActivation.Is16Segments]).Value.ToScalar<bool>();
		if (_act1[GNNEActivation.InputA] is Call call && call.Target is GNNELoad && !flag && (object)_lif2 != null && _swapAB)
		{
			checkedDataType = _act1[GNNEActivation.InputB].CheckedDataType;
			dataType = _act1[GNNEActivation.InputA].CheckedDataType;
			deQuantizeParam = ((TensorConst)_act1[GNNEActivation.DeqBParams]).Value.ToScalar<DeQuantizeParam>();
			deqParams = ((TensorConst)_act1[GNNEActivation.DeqAParams]).Value.ToScalar<DeQuantizeParam>();
			num = ((TensorConst)_act1[GNNEActivation.InBShiftBits]).Value.ToScalar<int>();
			rshiftBits = ((TensorConst)_act1[GNNEActivation.InAShiftBits]).Value.ToScalar<int>();
		}
		(list, ccrsToClr) = GetCcrSetAndClrVec(_ni);
		if ((object)_lif2 != null)
		{
			ccrsToClr.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2))));
		}
		if (firstSlice)
		{
			ccrsToClr.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.MfuAct1))));
		}
		List<int> src2Stride = null;
		SegmentND segmentND = _ifmap;
		SegmentND ifmap = _ifmap2;
		if ((object)_lif2 == null && _ifmap2.Shape_size != 0)
		{
			src2Stride = new List<int>
			{
				glb.GlbMap[ItemName.Ifmap2].Dimensions[1],
				glb.GlbMap[ItemName.Ifmap2].Dimensions[2],
				glb.GlbMap[ItemName.Ifmap2].Dimensions[3]
			};
			segmentND = _ifmapA;
			ifmap = _ifmapB;
		}
		if (_ifmap2.Shape_size == 0 && segmentND.Shape_size != 0 && (_ofmap[0].Length % segmentND[0].Length != 0 || _ofmap[1].Length % segmentND[1].Length != 0 || _ofmap[2].Length % segmentND[2].Length != 0 || _ofmap[3].Length % segmentND[3].Length != 0))
		{
			segmentND = _ofmap;
		}
		if (_ifmap2.Shape_size != 0 && _ifmap.Shape_size > _ifmap2.Shape_size && _ifmap.Shape_size % _ifmap2.Shape_size != 0)
		{
			TileUtilities.Assert(_ifmap[0].Start <= _ifmap2[0].Start && _ifmap[1].Start <= _ifmap2[1].Start && _ifmap[2].Start <= _ifmap2[2].Start && _ifmap[3].Start <= _ifmap2[3].Start, "_ifmap[0].Start <= _ifmap2[0].Start\n                && _ifmap[1].Start <= _ifmap2[1].Start\n                && _ifmap[2].Start <= _ifmap2[2].Start\n                && _ifmap[3].Start <= _ifmap2[3].Start", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 822);
			_ifmapOffset += TileUtilities.GetSliceOffsetInTensor(in _ifmap, in _ifmap2) * TileUtilities.GetBytesPerElement(checkedDataType);
		}
		else if (_ifmap2.Shape_size != 0 && _ifmap2.Shape_size > _ifmap.Shape_size && _ifmap2.Shape_size % _ifmap.Shape_size != 0)
		{
			TileUtilities.Assert(_ifmap[0].Start >= _ifmap2[0].Start && _ifmap[1].Start >= _ifmap2[1].Start && _ifmap[2].Start >= _ifmap2[2].Start && _ifmap[3].Start >= _ifmap2[3].Start, "_ifmap[0].Start >= _ifmap2[0].Start\n                && _ifmap[1].Start >= _ifmap2[1].Start\n                && _ifmap[2].Start >= _ifmap2[2].Start\n                && _ifmap[3].Start >= _ifmap2[3].Start", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 830);
			_ifmap2Offset += TileUtilities.GetSliceOffsetInTensor(in _ifmap2, in _ifmap) * TileUtilities.GetBytesPerElement(dataType);
		}
		actionUpdater.UpdateMfuAct1(segmentND, ifmap, _ofmap, ACT1_SOURCE_TYPE.l2, ACT1_SOURCE_TYPE.l2, checkedDataType, dataType, _act1.CheckedDataType, deQuantizeParam, deqParams, num, rshiftBits, rshiftBitsD, is16Segments, iPp, list, ccrsToClr, _ifmapOffset, _ifmap2Offset, _ni.Nb.OfmapOffset, _ni.Nb.Act1Offset, _src2ItemName, ItemName.Ifmap, ItemName.MfuAct1, (((GNNEActivation)_act1.Target).Type == GnneActivationType.Mul) ? MFU_ACT1_FUNCTION.mul : MFU_ACT1_FUNCTION.add, ItemName.Ofmap, null, src2Stride);
	}

	private void BuildSchedulePdp1(GnneActionUpdater action_updater, TiledGlb glb)
	{
		int iPp = 0;
		List<CcrSet> item;
		List<CcrClr> item2;
		if (!_isGlobalPdp)
		{
			GetCcrSetAndClrVec(_ni).Deconstruct<List<CcrSet>, List<CcrClr>>(out item, out item2);
			List<CcrSet> ccrsToSet = item;
			List<CcrClr> ccrsToClr = item2;
			int[] array = ((TensorConst)_pdp1[GNNEPdp1.Padding]).Value.ToArray<int>();
			Padding p = new Padding(array[0], array[1]);
			Padding p2 = new Padding(array[2], array[3]);
			int[] array2 = ((TensorConst)_pdp1[GNNEPdp1.Filter]).Value.ToArray<int>();
			int[] array3 = ((TensorConst)_pdp1[GNNEPdp1.Stride]).Value.ToArray<int>();
			Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(_ofmap[2].Start, _ofmap[2].Length, _inputShape[2], array2[0], array3[0], 1, in p);
			Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(_ofmap[3].Start, _ofmap[3].Length, _inputShape[3], array2[1], array3[1], 1, in p2);
			SegmentND segmentND = new SegmentND(_ifmap[0], _ifmap[1], inputRowSegment, inputRowSegment2);
			int bytesPerElement = TileUtilities.GetBytesPerElement(_pdp1[GNNEPdp1.Input].CheckedDataType);
			int num = glb.GlbMap[ItemName.Ifmap].Dimensions[1];
			int num2 = glb.GlbMap[ItemName.Ifmap].Dimensions[2];
			int num3 = glb.GlbMap[ItemName.Ifmap].Dimensions[3];
			int offsetS = (segmentND[0].Start - _ifmap[0].Start) * num * num2 * num3 * bytesPerElement + (segmentND[1].Start - _ifmap[1].Start) * num2 * num3 * bytesPerElement + (segmentND[2].Start - _ifmap[2].Start) * num3 * bytesPerElement + (segmentND[3].Start - _ifmap[3].Start) * bytesPerElement + _ifmapOffset;
			action_updater.UpdateMfuPdp1(_pdp1, segmentND, _ofmap, iPp, ccrsToSet, ccrsToClr, offsetS, _ni.Nb.OfmapOffset);
			return;
		}
		GetCcrSetAndClrVec(_ni).Deconstruct<List<CcrSet>, List<CcrClr>>(out item, out item2);
		List<CcrSet> ccrsToSet2 = item;
		List<CcrClr> list = item2;
		int h2 = ((TensorConst)_pdp1[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
		(int R, int S) tuple = SplitGlobalPdp1(((TensorConst)_pdp1[GNNEPdp1.Filter]).Value.ToArray<int>()[1], h2);
		int item3 = tuple.R;
		int item4 = tuple.S;
		SegmentND segmentND2 = new SegmentND(_ofmap);
		int count = TileUtilities.GetSegmentStartEndLength(0, item3, _inputShape[2]).Count;
		segmentND2[2] = new Segment1D(..count, Padding.Zero());
		TensorOnGlb tensorOnGlb = glb.GlbMap[ItemName.Ofmap];
		glb.GlbMap[ItemName.Ofmap] = new TensorOnGlb(new int[4]
		{
			segmentND2[0].Length,
			segmentND2[1].Length,
			segmentND2[2].Length,
			tensorOnGlb.Dimensions[3]
		}, DataTypes.Float16, 0, tensorOnGlb.Mmu);
		List<SegmentND> list2 = new List<SegmentND> { segmentND2 };
		List<SegmentND> list3 = new List<SegmentND> { _ifmap };
		for (int i = 0; i < list2.Count; i++)
		{
			SegmentND segmentND3 = list2[i];
			SegmentND segmentND4 = list3[i];
			List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(0, item3, _inputShape[2]);
			List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(0, item4, _inputShape[3]);
			SegmentND segmentND5 = new SegmentND(segmentND4);
			int alignedNum = TileUtilities.GetAlignedNum(segmentND4[3].Length, (TileUtilities.GetBytesPerElement(_inputType) == 1) ? 32 : 16);
			SegmentND tensor = new SegmentND(segmentND4[0], segmentND4[1], segmentND4[2], new Segment1D(..alignedNum, Padding.Zero()));
			alignedNum = TileUtilities.GetAlignedNum(segmentStartEndLength2.Count, 16);
			SegmentND segmentND6 = new SegmentND(segmentND3[0], segmentND3[1], new Segment1D(..segmentStartEndLength.Count, Padding.Zero()), new Segment1D(..segmentStartEndLength2.Count, Padding.Zero()));
			SegmentND tensor2 = new SegmentND(segmentND3[0], segmentND3[1], new Segment1D(..segmentStartEndLength.Count, Padding.Zero()), new Segment1D(..alignedNum, Padding.Zero()));
			PDP_FUNCTION pdpOp = PdpFunc((((GNNEPdp1)_pdp1.Target).ReduceOp == MFU_PDP_OP.AVERAGE) ? MFU_PDP_OP.SUM : ((GNNEPdp1)_pdp1.Target).ReduceOp);
			Half sumScale = (Half)(1f / (float)_inputShape[2]);
			Half sumScale2 = (Half)(1f / (float)_inputShape[3]);
			for (int j = 0; j < segmentStartEndLength.Count; j++)
			{
				for (int k = 0; k < segmentStartEndLength2.Count; k++)
				{
					int num4 = ((j == 0 && k == 0) ? 1 : 0);
					List<CcrClr> list4 = new List<CcrClr>();
					if (num4 > 0)
					{
						list4.Add(list[0]);
					}
					SegmentND slice = new SegmentND(segmentND5[0], segmentND5[1], segmentStartEndLength[j], segmentStartEndLength2[k]);
					SegmentND slice2 = new SegmentND(segmentND6[0], segmentND6[1], new Segment1D(j..(j + 1), Padding.Zero()), new Segment1D(k..(k + 1), Padding.Zero()));
					List<int> ofStride = new List<int>
					{
						tensor2[1].Length,
						tensor2[2].Length,
						tensor2[3].Length
					};
					int offsetS2 = TileUtilities.GetSliceOffsetInTensor(in tensor, in slice) * TileUtilities.GetBytesPerElement(_inputType) + _ifmapOffset;
					int offsetD = TileUtilities.GetSliceOffsetInTensor(in tensor2, in slice2) * TileUtilities.GetBytesPerElement(DataTypes.Float16) + _ofmapOffset;
					action_updater.UpdateMfuGlobalPdp1(_pdp1, _inputType, DataTypes.Float16, pdpOp, slice, slice2, iPp, sumScale, null, list4, offsetS2, offsetD, ItemName.Ifmap, null, ofStride);
				}
			}
			SegmentND ifmap = segmentND6;
			SegmentND segmentND7 = new SegmentND(segmentND6[0], segmentND6[1], new Segment1D(..1, Padding.Zero()), new Segment1D(..1, Padding.Zero()));
			List<int> ifStride = new List<int>
			{
				segmentND6[1].Length,
				segmentND6[2].Length,
				tensor2[3].Length
			};
			List<int> ofStride2 = new List<int>
			{
				segmentND6[1].Length,
				segmentND7[2].Length,
				tensorOnGlb.Dimensions[3]
			};
			int ofmapOffset = _ofmapOffset;
			int ofmapOffset2 = _ofmapOffset;
			action_updater.UpdateMfuGlobalPdp1(_pdp1, DataTypes.Float16, _pdp1.CheckedDataType, pdpOp, ifmap, segmentND7, iPp, sumScale2, ccrsToSet2, null, ofmapOffset, ofmapOffset2, ItemName.Ofmap, ifStride, ofStride2);
		}
		glb.GlbMap[ItemName.Ofmap] = tensorOnGlb;
		static PDP_FUNCTION PdpFunc(MFU_PDP_OP op)
		{
			if (op <= MFU_PDP_OP.SUM)
			{
				switch (op)
				{
				case MFU_PDP_OP.MIN:
					return PDP_FUNCTION.min;
				case MFU_PDP_OP.MAX:
					return PDP_FUNCTION.max;
				case MFU_PDP_OP.AVERAGE:
					return PDP_FUNCTION.average;
				case MFU_PDP_OP.SUM:
					return PDP_FUNCTION.sum;
				}
			}
			return PDP_FUNCTION.min;
		}
		static (int R, int S) SplitGlobalPdp1(int w, int h)
		{
			int num5 = ((h > 16) ? 16 : h);
			int item5 = Math.Min(Math.Min(256 / num5, w), 64);
			return (R: num5, S: item5);
		}
	}

	private void BuildScheduleTranspose(GnneActionUpdater actionUpdater, TiledGlb glb)
	{
		int iPp = 0;
		MFU_TRANS_PERMUTE perm = ((GNNETranspose)_transpose.Target).Perm;
		var (ccrsToSet, ccrsToClr) = GetCcrSetAndClrVec(_ni);
		actionUpdater.UpdateMfuTranspose(_ifmap, _ofmap, _inputType, perm, iPp, ccrsToSet, ccrsToClr, _ifmapOffset, _ofmapOffset);
	}

	private void BuildL1Schedule(GnneActionUpdater actionUpdater, TiledGlb glb, SegmentND ifmap1, SegmentND weight1, SegmentND psum, int iPp, int ofPp, int wPp, SegmentND ifmap2, bool weightGroupOnly, WeightGroupHandler weightGroup, Nncase.TIR.Buffer ddrW)
	{
		int num = 0;
		List<int> list = L1Search(glb, weight1, psum);
		List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[2].Start, list[2], psum[2].End);
		List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(psum[3].Start, list[3], psum[3].End);
		List<Segment1D> segmentStartEndLength3 = TileUtilities.GetSegmentStartEndLength(psum[0].Start, 1, psum[0].End);
		List<Segment1D> segmentStartEndLength4 = TileUtilities.GetSegmentStartEndLength(weight1[2].Start, list[4], weight1[2].End);
		List<Segment1D> segmentStartEndLength5 = TileUtilities.GetSegmentStartEndLength(weight1[3].Start, list[5], weight1[3].End);
		List<List<Segment1D>> l1McSeg = GetL1McSeg(ifmap1, psum, list[1], list[0]);
		List<Segment1D> list2 = l1McSeg[0];
		List<Segment1D> list3 = l1McSeg[1];
		int chunkSize = list[4];
		int chunkSize2 = Math.Min(GNNEEnv.PuKernelSpad / 2, list[5]);
		if (_dilationH > 1)
		{
			chunkSize = 1;
		}
		if (_dilationW > 1)
		{
			chunkSize2 = 1;
		}
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		bool flag4 = true;
		foreach (Segment1D item in list2)
		{
			foreach (Segment1D item2 in segmentStartEndLength3)
			{
				foreach (Segment1D item3 in segmentStartEndLength)
				{
					Segment1D inputRowSegment;
					if (Conv1X1(_conv))
					{
						int outputRowStart = item3.Start * _ofmapSt[2].Length;
						int outputRowLength = item3.Length * _ofmapSt[2].Length;
						inputRowSegment = TileUtilities.GetInputRowSegment(outputRowStart, outputRowLength, _conv.CheckedShape.ToValueList()[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
						inputRowSegment /= _ofmapConv[2].Length;
					}
					else
					{
						inputRowSegment = TileUtilities.GetInputRowSegment(item3.Start, item3.Length, _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
					}
					Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(inputRowSegment.Start, inputRowSegment.Length, _inputShape[2], _weightsShape[2], _strideH, _dilationH, in _paddingH);
					foreach (Segment1D item4 in segmentStartEndLength2)
					{
						Segment1D segment1D;
						if (Conv1X1(_conv))
						{
							TileUtilities.Assert(item4.Length == _ofmap[3].Length, "f.Length == _ofmap[3].Length", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 1037);
							int outputRowStart2 = item4.Start / _ofmapSt[2].Length;
							int outputRowLength2 = item4.Length / _ofmapSt[2].Length;
							segment1D = TileUtilities.GetInputRowSegment(outputRowStart2, outputRowLength2, _conv.CheckedShape.ToValueList()[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
							segment1D *= _ofmapConv[2].Length;
						}
						else
						{
							segment1D = TileUtilities.GetInputColumnSegment(item4.Start, item4.Length, _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
						}
						Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(segment1D.Start, segment1D.Length, _inputShape[3], _weightsShape[3], _strideW, _dilationW, in _paddingW);
						SegmentND segmentND = new SegmentND(item2, item, item3, item4);
						RestoreTensorShape(_conv, ItemName.Ofmap, segmentND);
						SegmentND r2LPsum = new SegmentND(item2, item, inputRowSegment, segment1D);
						SegmentND segmentND2 = new SegmentND(item2, item, inputRowSegment, segment1D);
						RestoreTensorShape(_conv, ItemName.Psum, segmentND2);
						int num2 = item.Start / _ocPerGroup * _icPerGroup;
						int num3 = (item.End - 1) / _ocPerGroup * _icPerGroup + _icPerGroup;
						Segment1D segment1D2 = new Segment1D(..0, Padding.Zero());
						bool flag5 = list3[0].Start == 0;
						foreach (Segment1D item5 in list3)
						{
							if (item5.Start < num2 || item5.End > num3)
							{
								continue;
							}
							int num4 = item5.Start % _icPerGroup;
							segment1D2 = new Segment1D(new System.Range(end: Math.Min(num4 + item5.Length, _icPerGroup), start: num4), Padding.Zero());
							foreach (Segment1D item6 in segmentStartEndLength4)
							{
								foreach (Segment1D item7 in segmentStartEndLength5)
								{
									Segment1D segment1D3 = item5;
									List<Segment1D> segmentStartEndLength6 = TileUtilities.GetSegmentStartEndLength(item6.Start, chunkSize, item6.End);
									List<Segment1D> segmentStartEndLength7 = TileUtilities.GetSegmentStartEndLength(item7.Start, chunkSize2, item7.End);
									Segment1D segment1D4 = item2;
									Segment1D segment1D5 = item;
									SegmentND x = new SegmentND(segment1D4, segment1D3, inputRowSegment2, inputColumnSegment);
									SegmentND w = new SegmentND(item, segment1D2, item6, item7);
									bool flag6 = false;
									SegmentND segmentND3 = TileUtilities.ShiftInputTensor(in x, in w, _weightsShape[2], _weightsShape[3], _strideH, _strideW, _dilationH, _dilationW);
									if (segmentND3[0].Length > 0 && segmentND3[1].Length > 0 && segmentND3[2].Length > 0 && segmentND3[3].Length > 0)
									{
										flag6 = true;
										if (!weightGroupOnly)
										{
											int num5 = _nodesG2LIfRec[_conv][_if1BufIdx][0].Item2.Stat_cnt();
											_nodesG2LIfRec[_conv][_if1BufIdx].RemoveAt(0);
											List<CcrClr> list4 = new List<CcrClr>();
											if (num5 > 0)
											{
												list4.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _if1BufIdx))));
											}
											SegmentND slice = new SegmentND(segmentND3);
											bool flag7 = RestoreTensorShape(_conv);
											int strideNReshape = 0;
											int strideCReshape = 0;
											int strideHReshape = 0;
											if (flag7)
											{
												if (_preNi.Nb.AlignType == AlignedType.EAligned)
												{
													strideNReshape = ifmap1[1].Length;
													strideCReshape = ifmap1[2].Length;
													strideHReshape = glb.GlbMap[ItemName.Ifmap].Dimensions[2] * glb.GlbMap[ItemName.Ifmap].Dimensions[3];
												}
												else if (_preNi.Nb.AlignType == AlignedType.FAligned)
												{
													RestoreTensorShape(_conv, ItemName.Ifmap, slice);
													strideNReshape = ifmap1[1].Length;
													strideCReshape = ifmap1[2].Length * _ifmapLd[2].Length;
													strideHReshape = glb.GlbMap[ItemName.Ifmap].Dimensions[3];
												}
												else
												{
													strideNReshape = ifmap1[1].Length;
													strideCReshape = ifmap1[2].Length;
													strideHReshape = ifmap1[3].Length;
												}
											}
											actionUpdater.UpdateG2LIf(slice, ifmap1, _lif, iPp, null, list4, _ifmapOffset, _inputType, ItemName.Ifmap, flag7, strideNReshape, strideCReshape, strideHReshape, _h2C, _weightsShape[2], ((TensorConst)_conv[GNNEConv2D.Stride]).Value.ToArray<int>()[0]);
										}
										else
										{
											if (_nodesG2LIfRec[_conv][_if1BufIdx].Count > 0)
											{
												List<Tuple<SegmentND, TensorStat>> list5 = _nodesG2LIfRec[_conv][_if1BufIdx];
												list5[list5.Count - 1].Item2.IsLastSlice = flag;
											}
											_nodesG2LIfRec[_conv][_if1BufIdx].Add(new Tuple<SegmentND, TensorStat>(ifmap1, new TensorStat(flag, isLastSlice: false)));
											flag = false;
										}
									}
									foreach (Segment1D item8 in segmentStartEndLength6)
									{
										foreach (Segment1D item9 in segmentStartEndLength7)
										{
											SegmentND w2 = new SegmentND(segment1D5, segment1D2, item8, item9);
											SegmentND slice2 = TileUtilities.ShiftInputTensor(in x, in w2, _weightsShape[2], _weightsShape[3], _strideH, _strideW, _dilationH, _dilationW);
											int num6 = ((!(_weightType == DataTypes.Int16)) ? 1 : 2);
											int num7 = ((!(_inputType == DataTypes.Int16)) ? 1 : 2);
											for (int i = 0; i < num6; i++)
											{
												for (int num8 = 0; num8 < num7; num8++)
												{
													if (!weightGroupOnly)
													{
														int num9 = 0;
														int num11;
														int num12;
														ItemName wName;
														int offsetWS;
														if (_weightBufIdx != -1)
														{
															Tuple<SegmentND, TensorStat> tuple = _nodesWeightRec[_conv][_weightBufIdx][0];
															Tuple<SegmentND, TensorStat> tuple2 = _nodesG2RWRec[_conv][_weightBufIdx][0];
															_nodesG2RWRec[_conv][_weightBufIdx].RemoveAt(0);
															if (tuple2.Item2.IsLastSlice)
															{
																_nodesWeightRec[_conv][_weightBufIdx].RemoveAt(0);
															}
															Tuple<SegmentND, TensorStat> tuple3 = _nodesG2RWSliceRec[_conv][_weightBufIdx][0];
															_nodesG2RWSliceRec[_conv][_weightBufIdx].RemoveAt(0);
															TileUtilities.Assert(w2 == tuple3.Item1 && tuple2.Item1 == tuple.Item1, "l2RW == g2RWSliceRecStat.Item1 && g2RWRecStat.Item1 == weightRecStat.Item1", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 1174);
															if (tuple3.Item2.IsFirstSlice)
															{
																int num10 = ((_nodesQuenesAsW[_weightBufIdx][0].Item2 != 0 && tuple2.Item2.IsFirstSlice) ? 1 : 0);
																List<CcrClr> list6 = new List<CcrClr>();
																if (num10 > 0)
																{
																	list6.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WeightFake, _weightBufIdx))));
																}
																List<CcrSet> ccrsToSet = new List<CcrSet>
																{
																	new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Weight, (_weightBufIdx << 1) + (tuple3.Item2.SliceIdx & 1))), 1)
																};
																actionUpdater.UpdateLoadW(w2, _lw, weightGroup, wPp, ddrW, ccrsToSet, list6, _ni.Nb.WeightOffset, _h2C, 0, ItemName.Weight, i);
															}
															if (tuple2.Item2.IsLastSlice && _nodesQuenesAsW[_weightBufIdx].Count > 0)
															{
																num9 = ((_nodesQuenesAsW[_weightBufIdx].Count > 1) ? 1 : 0);
																_nodesQuenesAsW[_weightBufIdx].RemoveAt(0);
															}
															num11 = (tuple3.Item2.IsFirstSlice ? 1 : 0);
															num12 = tuple3.Item2.SliceIdx;
															wName = ItemName.Weight;
															offsetWS = _ni.Nb.WeightOffset;
														}
														else
														{
															num9 = 0;
															num11 = 0;
															num12 = 0;
															wName = ItemName.WeightPreload;
															offsetWS = _ni.Nb.WeightPreloadOffset;
														}
														List<CcrSet> list7 = new List<CcrSet>();
														if (num9 > 0)
														{
															list7.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WeightFake, _weightBufIdx)), 1));
														}
														List<CcrClr> list8 = new List<CcrClr>();
														if (num11 > 0)
														{
															list8.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Weight, (_weightBufIdx << 1) + (num12 & 1)))));
														}
														if (_ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WQarg))) > 0)
														{
															list8.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.WQarg))));
														}
														actionUpdater.UpdateG2RW(w2, weightGroup, _ocPerGroup, _lw, wPp, i, list7, list8, offsetWS, _ni.Nb.WeightQargOffset, wName, ItemName.WQarg, _h2C);
													}
													else
													{
														weightGroup.UpdateWeightGroup(w2);
														if (_weightBufIdx != -1)
														{
															if (_nodesG2RWRec[_conv][_weightBufIdx].Count > 0)
															{
																List<Tuple<SegmentND, TensorStat>> list9 = _nodesG2RWRec[_conv][_weightBufIdx];
																list9[list9.Count - 1].Item2.IsLastSlice = flag2;
															}
															_nodesG2RWRec[_conv][_weightBufIdx].Add(new Tuple<SegmentND, TensorStat>(weight1, new TensorStat(flag2, isLastSlice: false)));
															_nodesG2RWSliceRec[_conv][_weightBufIdx].Add(new Tuple<SegmentND, TensorStat>(w2, new TensorStat(isFirstSlice: false, isLastSlice: false, -1)));
															flag2 = false;
														}
													}
													if (!weightGroupOnly)
													{
														actionUpdater.UpdateL2RIf(slice2, segmentND3, _strideH, _strideW, _icPerGroup, _lif, 0, num8, ((TensorConst)_conv[GNNEConv2D.DeqBias]).Value.ToArray<int>()[0], _inputType, _h2C, _weightsShape[2]);
													}
													bool releaseIf = false;
													int num13;
													if (item9 == segmentStartEndLength7[segmentStartEndLength7.Count - 1])
													{
														if (item8 == segmentStartEndLength6[segmentStartEndLength6.Count - 1] && i == num6 - 1)
														{
															num13 = ((num8 == num7 - 1) ? 1 : 0);
															goto IL_0ef7;
														}
													}
													num13 = 0;
													goto IL_0ef7;
													IL_0ef7:
													if (((uint)num13 & (flag6 ? 1u : 0u)) != 0)
													{
														releaseIf = true;
														flag6 = false;
													}
													bool loopStart = false;
													if (flag5 && i == 0 && num8 == 0)
													{
														loopStart = true;
														flag5 = false;
													}
													bool flag8 = segment1D2.End == _weightsShape[1] && item9.End == weight1[3].End && item8.End == weight1[2].End && i == num6 - 1 && num8 == num7 - 1;
													DataType ofType = _outputType;
													ACT0_OUTPUT_DEST aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.dm;
													if ((object)_pool != null || (object)_dw != null || (object)_act1 != null)
													{
														aCT0_OUTPUT_DEST = ACT0_OUTPUT_DEST.psum;
														ofType = _conv.CheckedDataType;
													}
													if (!weightGroupOnly)
													{
														int shift = ((TensorConst)_conv[GNNEConv2D.ShiftBits]).Value.ToScalar<int>();
														int num14 = 0;
														int num15 = 0;
														int num16 = 0;
														int value = 0;
														if (flag8 && aCT0_OUTPUT_DEST == ACT0_OUTPUT_DEST.dm)
														{
															Tuple<SegmentND, TensorStat> tuple4 = _nodesL2GOfRec[_conv][_ofBufIdx][0];
															_nodesL2GOfRec[_conv][_ofBufIdx].RemoveAt(0);
															if (tuple4.Item2.IsFirstSlice && _nodesQueNeedClearFake.Count > 0 && _nodesQueNeedClearFake[0].Item1 == _conv)
															{
																num15 = ((_nodesQueNeedClearFake[0].Item2 != 0) ? 1 : 0);
																_nodesQueNeedClearFake.RemoveAt(0);
															}
															if (tuple4.Item2.IsLastSlice)
															{
																num14 = 1;
																value = GetCcrSetAccordingPostNodes(_ni);
															}
														}
														if (flag8 && _ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Act))) > 0)
														{
															num16 = 1;
														}
														List<CcrSet> list10 = new List<CcrSet>();
														if (num14 > 0)
														{
															list10.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _ofBufIdx)), value));
														}
														List<CcrClr> list11 = new List<CcrClr>();
														if (num15 > 0)
														{
															list11.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, _ofBufIdx))));
														}
														List<CcrClr> list12 = new List<CcrClr>();
														if (num16 > 0)
														{
															list12.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Act))));
														}
														actionUpdater.UpdateR2LPsum(shift, r2LPsum, segmentND2, _ofmapSt, ofPp, num, aCT0_OUTPUT_DEST, releaseIf, Math.Max(i, num8), TcuComputeMode.NormalConv2d, loopStart, flag8, _strideH, _strideW, _ocPerGroup, _inputType, _weightType, ofType, _lact.CheckedDataType, list10, list11, _ni.Nb.ActOffset, _ni.Nb.OfmapOffset, list12);
													}
													else if (flag8 && aCT0_OUTPUT_DEST == ACT0_OUTPUT_DEST.dm)
													{
														if (_nodesL2GOfRec[_conv][_ofBufIdx].Count > 0)
														{
															List<Tuple<SegmentND, TensorStat>> list13 = _nodesL2GOfRec[_conv][_ofBufIdx];
															list13[list13.Count - 1].Item2.IsLastSlice = flag3;
														}
														_nodesL2GOfRec[_conv][_ofBufIdx].Add(new Tuple<SegmentND, TensorStat>(psum, new TensorStat(flag3, isLastSlice: false)));
														flag3 = false;
													}
												}
											}
										}
									}
								}
							}
						}
						if (!weightGroupOnly)
						{
							if (((object)_pool == null && (object)_dw == null && (object)_act1 == null) || segment1D2.End != _weightsShape[1])
							{
								continue;
							}
							Segment1D segment1D6 = new Segment1D(..0, new Padding(0, 0));
							SegmentND l2RDw = new SegmentND(segment1D6, segment1D6, segment1D6, segment1D6);
							SegmentND l2RIf = new SegmentND(segment1D6, segment1D6, segment1D6, segment1D6);
							List<CcrClr> list14 = new List<CcrClr>();
							int offsetAct = _l1FuseNi.Nb.Pdp0ActOffset;
							if ((object)_act1 != null && _act1[GNNEActivation.InputB] != None.Default)
							{
								l2RIf = segmentND2;
								int index = ((_if2BufIdx != -1) ? _if2BufIdx : 0);
								int num17 = _nodesL2RIf2Rec[_conv][index][0].Item2.Stat_cnt();
								_nodesL2RIf2Rec[_conv][index].RemoveAt(0);
								if ((object)_lif2 != null)
								{
									if (num17 > 0)
									{
										list14.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ifmap2))));
									}
								}
								else if (num17 > 0)
								{
									list14.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _if2BufIdx))));
								}
							}
							if ((object)_dw != null)
							{
								int[] array = _dw[GNNEPdp0DW.Weights].CheckedShape.ToValueArray();
								l2RDw = new SegmentND(new Segment1D(..1, Padding.Zero()), item, new Segment1D(..array[2], Padding.Zero()), new Segment1D(..array[3], Padding.Zero()));
								offsetAct = _l1FuseNi.Nb.DwActOffset;
							}
							int num18 = 0;
							int num19 = 0;
							int value2 = 0;
							Tuple<SegmentND, TensorStat> tuple5 = _nodesL2GOfRec[_conv][_ofBufIdx][0];
							_nodesL2GOfRec[_conv][_ofBufIdx].RemoveAt(0);
							if (tuple5.Item2.IsFirstSlice && _nodesQueNeedClearFake.Count > 0 && _nodesQueNeedClearFake[0].Item1 == _conv)
							{
								num19 = ((_nodesQueNeedClearFake[0].Item2 != 0) ? 1 : 0);
								_nodesQueNeedClearFake.RemoveAt(0);
							}
							if (tuple5.Item2.IsLastSlice)
							{
								num18 = 1;
								value2 = GetCcrSetAccordingPostNodes(_ni.Children[0]);
							}
							List<CcrClr> list15 = new List<CcrClr>();
							List<CcrClr> list16 = new List<CcrClr>();
							if (_ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwWeight))) > 0)
							{
								list15.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwWeight))));
							}
							if (_ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwQarg))) > 0)
							{
								list15.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwQarg))));
							}
							if (_ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwAct1))) > 0)
							{
								list16.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.DwAct1))));
							}
							if (_ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.MfuAct1))) > 0)
							{
								list16.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.MfuAct1))));
							}
							if (_ccrHandler.GetValue(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.PdpAct1))) > 0)
							{
								list16.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.PdpAct1))));
							}
							List<CcrSet> list17 = new List<CcrSet>();
							if (num18 > 0)
							{
								list17.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _ofBufIdx)), value2));
							}
							List<CcrClr> list18 = new List<CcrClr>();
							if (num19 > 0)
							{
								list18.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, _ofBufIdx))));
							}
							actionUpdater.UpdateL2RPsum(_dw, _pool, _act1, l2RIf, ifmap2, l2RDw, segmentND2, segmentND, _ofmapSt, ofPp, num, ACT0_OUTPUT_DEST.dm, TcuComputeMode.NormalConv2d, _ocPerGroup, weightGroup, list14, list17, list18, _ifmap2Offset, offsetAct, _l1FuseNi.Nb.Act1Offset, _ni.Nb.OfmapOffset, _l1FuseNi.Nb.DwWeightOffset, _l1FuseNi.Nb.DwWeightQargOffset, _src2ItemName, list15, list16, _swapAB);
							num = (num + 1) % 2;
							continue;
						}
						if ((object)_act1 != null && segment1D2.End == _weightsShape[1] && _act1[GNNEActivation.InputB] != None.Default)
						{
							int index2 = ((_if2BufIdx != -1) ? _if2BufIdx : 0);
							if (_nodesL2RIf2Rec[_conv][index2].Count > 0)
							{
								List<Tuple<SegmentND, TensorStat>> list19 = _nodesL2RIf2Rec[_conv][index2];
								list19[list19.Count - 1].Item2.IsLastSlice = flag4;
							}
							_nodesL2RIf2Rec[_conv][index2].Add(new Tuple<SegmentND, TensorStat>(ifmap2, new TensorStat(flag4, isLastSlice: false)));
							flag4 = false;
						}
						if (((object)_pool != null || (object)_dw != null || (object)_act1 != null) && segment1D2.End == _weightsShape[1])
						{
							if (_nodesL2GOfRec[_conv][_ofBufIdx].Count > 0)
							{
								List<Tuple<SegmentND, TensorStat>> list20 = _nodesL2GOfRec[_conv][_ofBufIdx];
								list20[list20.Count - 1].Item2.IsLastSlice = flag3;
							}
							_nodesL2GOfRec[_conv][_ofBufIdx].Add(new Tuple<SegmentND, TensorStat>(psum, new TensorStat(flag3, isLastSlice: false)));
							flag3 = false;
						}
					}
				}
			}
		}
	}

	private List<List<Segment1D>> GetL1McSeg(SegmentND ifmap, SegmentND psum, int mInloop, int cInloop)
	{
		int num = Math.Max(ifmap[1].Length / _icPerGroup, 1);
		List<Segment1D> list = new List<Segment1D>();
		List<Segment1D> list2 = new List<Segment1D>();
		if (num >= 2 && _groupPerPass < 2)
		{
			for (int i = 0; i < num; i++)
			{
				List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(psum[1].Start + i * _ocPerGroup, mInloop, psum[1].Start + (i + 1) * _ocPerGroup);
				List<Segment1D> segmentStartEndLength2 = TileUtilities.GetSegmentStartEndLength(ifmap[1].Start + i * _icPerGroup, cInloop, ifmap[1].Start + (i + 1) * _icPerGroup);
				list.AddRange(segmentStartEndLength);
				list2.AddRange(segmentStartEndLength2);
			}
		}
		else
		{
			list = TileUtilities.GetSegmentStartEndLength(psum[1].Start, mInloop, psum[1].End);
			list2 = TileUtilities.GetSegmentStartEndLength(ifmap[1].Start, cInloop, ifmap[1].End);
		}
		return new List<List<Segment1D>> { list, list2 };
	}

	private int GetCcrSetAccordingPostNodes(NodeInfo currNode)
	{
		int num = 0;
		int num2 = (_l1Fused ? _l1FuseNi.Nb.OutputsSize : _ni.Nb.OutputsSize);
		Call call = currNode.Children[0].Op;
		Call call2 = call;
		if ((object)call2 != null)
		{
			Expr target = call2.Target;
			if (target is GNNEConv2D)
			{
				goto IL_0105;
			}
			if (!(target is GNNEActivation))
			{
				if (target is Ai2dResize)
				{
					if (_nodesAi2dIfRec[call][_ofBufIdx].Count > 0)
					{
						int num3 = num;
						TensorStat item = _nodesAi2dIfRec[call][_ofBufIdx][0].Item2;
						num = num3 + ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
					}
					goto IL_02b9;
				}
			}
			else if (((GNNEActivation)call.Target).InputFromL1.Count > 0 && ((((GNNEActivation)call.Target).InputFromL1[0] && call[GNNEActivation.InputA] != currNode.Op) || (((GNNEActivation)call.Target).InputFromL1[1] && call[GNNEActivation.InputB] != currNode.Op)))
			{
				goto IL_0105;
			}
		}
		num = ((num2 != 2 || (object)call == null || !(call.Target is Concat)) ? (num + 1) : num);
		goto IL_02b9;
		IL_0105:
		bool flag = false;
		if ((object)call != null && call.Target is GNNEActivation)
		{
			flag = true;
			call = ((!((GNNEActivation)call.Target).InputFromL1[0]) ? ((Call)call[GNNEActivation.InputB]) : ((Call)call[GNNEActivation.InputA]));
		}
		if (_nodesL2RIf2Rec.ContainsKey(call) && _nodesL2RIf2Rec[call][_ofBufIdx].Count > 0 && flag)
		{
			int num4 = num;
			TensorStat item = _nodesL2RIf2Rec[call][_ofBufIdx][0].Item2;
			num = num4 + ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
		}
		else if (_nodesG2LIfRec[call][_ofBufIdx].Count > 0)
		{
			int num5 = num;
			TensorStat item = _nodesG2LIfRec[call][_ofBufIdx][0].Item2;
			num = num5 + ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
		}
		goto IL_02b9;
		IL_02b9:
		if (num2 != 2)
		{
			return num;
		}
		Call call3 = currNode.Children[1].Op;
		call2 = call3;
		if ((object)call2 != null)
		{
			Expr target = call2.Target;
			if (target is GNNEConv2D)
			{
				goto IL_0397;
			}
			if (!(target is GNNEActivation))
			{
				if (target is Ai2dResize)
				{
					if (_nodesAi2dIfRec[call3][_ofBufIdx].Count > 0)
					{
						int num6 = num;
						TensorStat item = _nodesAi2dIfRec[call3][_ofBufIdx][0].Item2;
						num = num6 + ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
					}
					goto IL_054b;
				}
			}
			else if (((GNNEActivation)call3.Target).InputFromL1.Count > 0 && ((((GNNEActivation)call3.Target).InputFromL1[0] && call3[GNNEActivation.InputA] != currNode.Op) || (((GNNEActivation)call3.Target).InputFromL1[1] && call3[GNNEActivation.InputB] != currNode.Op)))
			{
				goto IL_0397;
			}
		}
		num = ((num2 != 2 || (object)call3 == null || !(call3.Target is Concat)) ? (num + 1) : num);
		goto IL_054b;
		IL_0397:
		bool flag2 = false;
		if ((object)call3 != null && call3.Target is GNNEActivation)
		{
			flag2 = true;
			call3 = ((!((GNNEActivation)call3.Target).InputFromL1[0]) ? ((Call)call3[GNNEActivation.InputB]) : ((Call)call3[GNNEActivation.InputA]));
		}
		if (_nodesL2RIf2Rec.ContainsKey(call3) && _nodesL2RIf2Rec[call3][_ofBufIdx].Count > 0 && flag2)
		{
			int num7 = num;
			TensorStat item = _nodesL2RIf2Rec[call3][_ofBufIdx][0].Item2;
			num = num7 + ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
		}
		else if (_nodesG2LIfRec[call3][_ofBufIdx].Count > 0)
		{
			int num8 = num;
			TensorStat item = _nodesG2LIfRec[call3][_ofBufIdx][0].Item2;
			num = num8 + ((item != null && item.IsFirstSlice && item.IsLastSlice) ? 1 : 2);
		}
		goto IL_054b;
		IL_054b:
		return num;
	}

	private bool Conv1X1(Call conv)
	{
		bool result = false;
		if ((object)conv == null)
		{
			return result;
		}
		int[] array = conv[GNNEConv2D.Input].CheckedShape.ToValueArray();
		int[] array2 = conv.CheckedShape.ToValueArray();
		int[] array3 = conv[GNNEConv2D.Weights].CheckedShape.ToValueArray();
		int[] array4 = ((TensorConst)conv[GNNEConv2D.Padding]).Value.ToArray<int>();
		Padding padding = new Padding(array4[0], array4[1]);
		Padding padding2 = new Padding(array4[2], array4[3]);
		int num = ((TensorConst)conv[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
		int num2 = ((TensorConst)conv[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
		int num3 = ((TensorConst)conv[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
		int num4 = ((TensorConst)conv[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
		int num5 = ((TensorConst)conv[GNNEConv2D.Groups]).Value.ToScalar<int>();
		bool flag = array[1] == array2[1] && array2[1] == num5 && num5 != 1;
		DataType checkedDataType = conv[GNNEConv2D.Input].CheckedDataType;
		if ((object)conv != null && !flag && num5 == 1 && num == 1 && num2 == 1 && array3[2] == 1 && array3[3] == 1 && num3 == 1 && num4 == 1 && padding.Sum() == 0 && padding2.Sum() == 0 && (checkedDataType == DataTypes.Int8 || checkedDataType == DataTypes.UInt8) && array2[2] * array2[3] < 512 && array3[1] % 24 == 0 && _ifmapLd[2].Range.Equals(_ofmapSt[2].Range) && _ifmapLd[3].Range.Equals(_ofmapSt[3].Range))
		{
			result = true;
		}
		return result;
	}

	private bool RestoreTensorShape(Call convCall, ItemName item_type = ItemName.None, SegmentND slice = null)
	{
		if (convCall == null)
		{
			throw new ArgumentNullException("convCall");
		}
		if (!Conv1X1(convCall))
		{
			return false;
		}
		switch (item_type)
		{
		case ItemName.None:
			return true;
		case ItemName.Ofmap:
		case ItemName.Psum:
		{
			Segment1D segment1D3 = _ofmapSt[2];
			Segment1D segment1D4 = _ofmapSt[3];
			if (item_type == ItemName.Psum)
			{
				segment1D3 = _ofmapConv[2];
				segment1D4 = _ofmapConv[3];
			}
			TileUtilities.Assert(slice[3].Start % segment1D4.Length == 0 && slice[3].End % segment1D4.Length == 0, "slice[3].Start % dim3.Length == 0 && slice[3].End % dim3.Length == 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 1710);
			slice[0] = slice[0];
			slice[1] = new Segment1D((slice[1].Start * slice[2].Length)..(slice[1].End * slice[2].Length), Padding.Zero());
			slice[2] = new Segment1D((segment1D3.Start + slice[3].Start / segment1D4.Length)..(segment1D3.Start + slice[3].End / segment1D4.Length), segment1D3.Padding);
			slice[3] = new Segment1D(segment1D4.Start..segment1D4.End, segment1D4.Padding);
			break;
		}
		default:
		{
			Segment1D segment1D = _ifmapLd[2];
			Segment1D segment1D2 = _ifmapLd[3];
			TileUtilities.Assert(slice[3].Start % segment1D2.Length == 0 && slice[3].End % segment1D2.Length == 0, "slice[3].Start % dim3.Length == 0 && slice[3].End % dim3.Length == 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 1727);
			slice[0] = slice[0];
			slice[1] = slice[1];
			slice[2] = new Segment1D((slice[2].Start * slice[3].Length / segment1D2.Length)..(slice[2].End * slice[3].Length / segment1D2.Length), segment1D.Padding);
			slice[3] = new Segment1D((slice[3].Start / segment1D.Length)..(slice[3].End / segment1D.Length), segment1D2.Padding);
			break;
		}
		}
		return true;
	}

	private List<int> L1Search(TiledGlb glb, SegmentND weight1, SegmentND psum)
	{
		bool isConv1X1 = Conv1X1(_conv);
		bool flag = (isConv1X1 || _l1FusedInfos[_conv] == L1FusedType.FusedAct1 || _l1FusedInfos[_conv] == L1FusedType.NoFused) && !_h2C;
		int item = Math.Min(GNNEEnv.PuHeight, _groupPerPass * _icPerGroup);
		int item2 = Math.Min(GNNEEnv.PuWidth, _groupPerPass * _ocPerGroup);
		int h = 1;
		int w = 1;
		int e = 1;
		int f = ((!flag) ? 1 : _ofmap[3].Length);
		int fStep = ((!flag) ? 1 : _ofmap[3].Length);
		bool flag2 = false;
		int r;
		int s;
		int[] originalConvOutputShape;
		int hConvOut;
		int wConvOut;
		int psumPingPangSplit;
		int ifBytesPerElementGlb;
		bool flag3;
		while (true)
		{
			r = ((_weightSplitPattern[_conv].Item1 == 0 || _dilationH > 3) ? 1 : _weightSplitPattern[_conv].Item1);
			s = ((_weightSplitPattern[_conv].Item2 == 0 || _dilationW > 3) ? 1 : _weightSplitPattern[_conv].Item2);
			originalConvOutputShape = _conv.CheckedShape.ToValueArray();
			if (isConv1X1)
			{
				int e2 = e * _ofmapSt[2].Length;
				hConvOut = SpaceSearcher.GetInputHeight(e2, originalConvOutputShape[2], _fusedKernelH, _ofmapSt[2].Length, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
				hConvOut /= _ofmapConv[2].Length;
			}
			else
			{
				hConvOut = SpaceSearcher.GetInputHeight(e, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
			}
			int e3 = hConvOut;
			int inH = _inputShape[2];
			int r2 = r;
			int outH = _convOutputShape[2];
			int strideH = _strideH;
			int dilationH = _dilationH;
			Padding p = Padding.Zero();
			h = SpaceSearcher.GetInputHeight(e3, inH, r2, outH, strideH, dilationH, in p);
			if (isConv1X1)
			{
				TileUtilities.Assert(f % _ofmapSt[2].Length == 0, "f % _ofmapSt[2].Length == 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 1778);
				int e4 = f / _ofmapSt[2].Length;
				wConvOut = SpaceSearcher.GetInputHeight(e4, originalConvOutputShape[3], _fusedKernelW, _ofmapSt[3].Length, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
				wConvOut *= _ofmapConv[2].Length;
			}
			else
			{
				wConvOut = SpaceSearcher.GetInputHeight(f, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
			}
			int e5 = wConvOut;
			int inH2 = _inputShape[3];
			int r3 = s;
			int outH2 = _convOutputShape[3];
			int strideW = _strideW;
			int dilationW = _dilationW;
			p = Padding.Zero();
			w = SpaceSearcher.GetInputHeight(e5, inH2, r3, outH2, strideW, dilationW, in p);
			psumPingPangSplit = 1;
			if ((object)_pool != null || (object)_dw != null || (object)_act1 != null)
			{
				psumPingPangSplit = 2;
			}
			ifBytesPerElementGlb = TileUtilities.GetBytesPerElement(_inputType);
			flag3 = HandleL1Allocate(h, w, Math.Max(e, hConvOut), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb);
			if (flag3 || flag2)
			{
				break;
			}
			flag2 = true;
			f = 1;
			fStep = 1;
		}
		if (!flag3)
		{
			throw new NotSupportedException("L1 is too small");
		}
		while (f < psum[3].Length && e < psum[2].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1SizePerChan / 2)
		{
			if (f <= e)
			{
				if (!IncreaseFByStep1())
				{
					break;
				}
			}
			else if (!IncreaseEByStep1())
			{
				break;
			}
		}
		while (f < psum[3].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1SizePerChan / 2 && IncreaseFByStep1())
		{
		}
		while (e < psum[2].Length && h * w * ifBytesPerElementGlb < GNNEEnv.IfL1SizePerChan / 2 && IncreaseEByStep1())
		{
		}
		if (_weightSplitPattern[_conv].Equals(new Tuple<int, int>(0, 0)))
		{
			while (s < weight1[3].Length && r < weight1[2].Length && s < 31 && r < 31 && _dilationW <= 3 && _dilationH <= 3)
			{
				if (s <= r)
				{
					if (!IncreaseSBy1())
					{
						break;
					}
				}
				else if (isConv1X1)
				{
					if (!IncreaseRBy11X1Conv())
					{
						break;
					}
				}
				else if (!IncreaseRBy1())
				{
					break;
				}
			}
			while (s < weight1[3].Length && s < 31 && _dilationW <= 3 && IncreaseSBy1())
			{
			}
			while (r < weight1[2].Length && r < 31 && _dilationH <= 3)
			{
				if (isConv1X1)
				{
					if (!IncreaseRBy11X1Conv())
					{
						break;
					}
				}
				else if (!IncreaseRBy1())
				{
					break;
				}
			}
		}
		while (f < psum[3].Length && e < psum[2].Length)
		{
			if (f <= e)
			{
				if (!IncreaseFByStep1())
				{
					break;
				}
			}
			else if (!IncreaseEByStep1())
			{
				break;
			}
		}
		while (f < psum[3].Length && IncreaseFByStep1())
		{
		}
		while (e < psum[2].Length && IncreaseEByStep1())
		{
		}
		if (_dilationW > 3)
		{
			r = 1;
		}
		if (_dilationH > 3)
		{
			s = 1;
		}
		_weightSplitPattern[_conv] = new Tuple<int, int>(r, s);
		return new List<int> { item, item2, e, f, r, s };
		bool IncreaseEByStep1()
		{
			int num6 = e + 1;
			int inputHeight7;
			if (isConv1X1)
			{
				inputHeight7 = SpaceSearcher.GetInputHeight(num6 * _ofmapSt[2].Length, originalConvOutputShape[2], _fusedKernelH, _ofmapSt[2].Length, _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
				inputHeight7 /= _ofmapConv[2].Length;
			}
			else
			{
				inputHeight7 = SpaceSearcher.GetInputHeight(num6, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
			}
			int e6 = inputHeight7;
			int inH6 = _inputShape[2];
			int r4 = r;
			int outH6 = _convOutputShape[2];
			int strideH4 = _strideH;
			int dilationH4 = _dilationH;
			Padding p5 = new Padding(0, 0);
			int inputHeight8 = SpaceSearcher.GetInputHeight(e6, inH6, r4, outH6, strideH4, dilationH4, in p5);
			bool num7 = HandleL1Allocate(inputHeight8, w, Math.Max(num6, inputHeight7), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb);
			if (num7)
			{
				e = num6;
				h = inputHeight8;
				hConvOut = inputHeight7;
			}
			return num7;
		}
		bool IncreaseFByStep1()
		{
			int num8 = f + fStep;
			int inputHeight9;
			if (isConv1X1)
			{
				inputHeight9 = SpaceSearcher.GetInputHeight(num8 / _ofmapSt[2].Length, originalConvOutputShape[3], _fusedKernelW, _ofmapSt[3].Length, _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
				inputHeight9 *= _ofmapConv[2].Length;
			}
			else
			{
				inputHeight9 = SpaceSearcher.GetInputHeight(num8, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
			}
			int e7 = inputHeight9;
			int inH7 = _inputShape[3];
			int r5 = s;
			int outH7 = _convOutputShape[3];
			int strideW3 = _strideW;
			int dilationW3 = _dilationW;
			Padding p6 = new Padding(0, 0);
			int inputHeight10 = SpaceSearcher.GetInputHeight(e7, inH7, r5, outH7, strideW3, dilationW3, in p6);
			bool num9 = HandleL1Allocate(h, inputHeight10, Math.Max(e, hConvOut), Math.Max(num8, inputHeight9), psumPingPangSplit, ifBytesPerElementGlb);
			if (num9)
			{
				f = num8;
				w = inputHeight10;
				wConvOut = inputHeight9;
			}
			return num9;
		}
		bool IncreaseRBy1()
		{
			int num = r + 1;
			int inputHeight = SpaceSearcher.GetInputHeight(e, originalConvOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
			int inH3 = _inputShape[2];
			int outH3 = _convOutputShape[2];
			int strideH2 = _strideH;
			int dilationH2 = _dilationH;
			Padding p2 = new Padding(0, 0);
			int inputHeight2 = SpaceSearcher.GetInputHeight(inputHeight, inH3, num, outH3, strideH2, dilationH2, in p2);
			bool flag4 = HandleL1Allocate(inputHeight2, w, Math.Max(e, inputHeight), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb);
			if (!flag4)
			{
				return flag4;
			}
			r = num;
			h = inputHeight2;
			hConvOut = inputHeight;
			return flag4;
		}
		bool IncreaseRBy11X1Conv()
		{
			int num2 = r + 1;
			int inputHeight3 = SpaceSearcher.GetInputHeight(e, _convOutputShape[2], _fusedKernelH, _outputShape[2], _fusedStrideH, _fusedDilationH, in _fusedPaddingH);
			int inH4 = _inputShape[2];
			int outH4 = _convOutputShape[2];
			int strideH3 = _strideH;
			int dilationH3 = _dilationH;
			Padding p3 = new Padding(0, 0);
			int inputHeight4 = SpaceSearcher.GetInputHeight(inputHeight3, inH4, num2, outH4, strideH3, dilationH3, in p3);
			bool num3 = HandleL1Allocate(inputHeight4, _ofmap[2].Length * _ofmap[3].Length, Math.Max(e, inputHeight3), Math.Max(f, wConvOut), psumPingPangSplit, ifBytesPerElementGlb);
			if (num3)
			{
				r = num2;
				h = inputHeight4;
				hConvOut = inputHeight3;
			}
			return num3;
		}
		bool IncreaseSBy1()
		{
			int num4 = s + 1;
			int inputHeight5 = SpaceSearcher.GetInputHeight(f, _convOutputShape[3], _fusedKernelW, _outputShape[3], _fusedStrideW, _fusedDilationW, in _fusedPaddingW);
			int inH5 = _inputShape[3];
			int outH5 = _convOutputShape[3];
			int strideW2 = _strideW;
			int dilationW2 = _dilationW;
			Padding p4 = new Padding(0, 0);
			int inputHeight6 = SpaceSearcher.GetInputHeight(inputHeight5, inH5, num4, outH5, strideW2, dilationW2, in p4);
			bool num5 = HandleL1Allocate(h, inputHeight6, Math.Max(e, hConvOut), Math.Max(f, inputHeight5), psumPingPangSplit, ifBytesPerElementGlb);
			if (num5)
			{
				s = num4;
				w = inputHeight6;
				wConvOut = inputHeight5;
			}
			return num5;
		}
	}

	private bool HandleL1Allocate(int h, int w, int e, int f, int psumPingPangSplit, int ifBytesPerElementGlb)
	{
		if (e * f > GNNEEnv.PsumL1ElePerChan / psumPingPangSplit)
		{
			return false;
		}
		return GNNEEnv.PuHeight * h * w * ifBytesPerElementGlb <= GNNEEnv.IfL1Size;
	}

	private void ArrangeWeights(DataType weightsType, int[] weightsShape, Span<byte> oldWeights, WeightGroupHandler weightGroup)
	{
		int bytesPerElement = TileUtilities.GetBytesPerElement(weightsType);
		List<SegmentND> list = weightGroup.WeightGroupSlice();
		byte[] array = new byte[oldWeights.Length];
		if (_h2C)
		{
			int num = 0;
			int num2 = 0;
			foreach (SegmentND item in list)
			{
				TileUtilities.Assert(num == weightGroup.WeightGroupOffset(item) * bytesPerElement, "offset == weightGroup.WeightGroupOffset(slice) * bytesPerElement", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 2093);
				for (int i = 0; i < bytesPerElement; i++)
				{
					for (int j = 0; j < item[0].Length; j++)
					{
						for (int k = 0; k < item[3].Length; k++)
						{
							for (int l = 0; l < item[2].Length; l++)
							{
								for (int m = 0; m < item[1].Length; m++)
								{
									int num3 = ((((j + item[0].Start) * weightsShape[1] + m + item[1].Start) * weightsShape[2] + l + item[2].Start) * weightsShape[3] + k + item[3].Start) * bytesPerElement;
									array[num2++] = oldWeights[num3 + i];
									num++;
								}
							}
						}
					}
				}
			}
		}
		else
		{
			int[] inputShape = _conv[GNNEConv2D.Input].CheckedShape.ToValueArray();
			int[] outputShape = _conv.CheckedShape.ToValueArray();
			int[] convOutputShape = _conv.CheckedShape.ToValueArray();
			int[] weightsShape2 = _conv[GNNEConv2D.Weights].CheckedShape.ToValueArray();
			ReshapeConv(_conv, ref inputShape, ref outputShape, ref convOutputShape, ref weightsShape2);
			int num4 = 0;
			int num5 = 0;
			foreach (SegmentND item2 in list)
			{
				TileUtilities.Assert(num4 == weightGroup.WeightGroupOffset(item2) * bytesPerElement, "offset == weightGroup.WeightGroupOffset(slice) * bytesPerElement", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 2126);
				for (int n = 0; n < bytesPerElement; n++)
				{
					for (int num6 = 0; num6 < item2[0].Length; num6++)
					{
						for (int num7 = 0; num7 < item2[2].Length; num7++)
						{
							for (int num8 = 0; num8 < item2[3].Length; num8++)
							{
								for (int num9 = 0; num9 < item2[1].Length; num9++)
								{
									int num10 = ((((num6 + item2[0].Start) * weightsShape2[1] + num9 + item2[1].Start) * weightsShape2[2] + num7 + item2[2].Start) * weightsShape2[3] + num8 + item2[3].Start) * bytesPerElement;
									array[num5++] = oldWeights[num10 + n];
									num4++;
								}
							}
						}
					}
				}
			}
		}
		array.CopyTo(oldWeights);
	}

	private void ArrangeDwWeights(DataType weightsType, int[] weightsShape, Span<byte> oldWeights, WeightGroupHandler weightGroup)
	{
		int bytesPerElement = TileUtilities.GetBytesPerElement(weightsType);
		int num = 0;
		int num2 = 0;
		byte[] array = new byte[oldWeights.Length];
		ref int reference = ref weightsShape[0];
		ref int reference2 = ref weightsShape[1];
		int num3 = weightsShape[1];
		int num4 = weightsShape[0];
		reference = num3;
		reference2 = num4;
		foreach (SegmentND item in from wg in weightGroup.DwGroupSlice()
			let cs = wg[1].Start
			let ce = wg[1].End
			let dwShape = _dw[GNNEPdp0DW.Weights].CheckedShape.ToValueArray()
			select new SegmentND(..1, cs..ce, ..dwShape[2], ..dwShape[3]))
		{
			for (int i = 0; i < item[0].Length; i++)
			{
				for (int j = 0; j < item[2].Length; j++)
				{
					for (int k = 0; k < item[3].Length; k++)
					{
						for (int l = 0; l < item[1].Length; l++)
						{
							int num5 = ((((i + item[0].Start) * weightsShape[1] + l + item[1].Start) * weightsShape[2] + j + item[2].Start) * weightsShape[3] + k + item[3].Start) * bytesPerElement;
							for (int m = 0; m < bytesPerElement; m++)
							{
								array[num2 * GNNEEnv.PuWidth + l] = oldWeights[num5 + m];
							}
							num++;
						}
						num2++;
					}
				}
			}
		}
		array.CopyTo(oldWeights);
	}

	private void InitParameters(FusionInfo fusionInfo)
	{
		if (_weightGroups.Count > 0)
		{
			_weightGroups.Clear();
		}
		foreach (NodeInfo fusedNode in fusionInfo.FusedNodes)
		{
			Call op = fusedNode.Op;
			if ((object)op != null && op.Target is GNNEConv2D)
			{
				DataType checkedDataType = fusedNode.Op[GNNEConv2D.Weights].CheckedDataType;
				_weightGroups.Add(fusedNode.Op, new WeightGroupHandler(checkedDataType, checkedDataType));
				_weightSplitPattern.Add(fusedNode.Op, new Tuple<int, int>(0, 0));
			}
		}
		_l1FusedInfos.Clear();
	}

	private void GetSliceInfo(FusionInfo fusionInfo, TiledGlb glb, out List<List<NodeInfo>> currSliceInfo, out List<Dictionary<Call, NodeInfo>> preSliceInfo)
	{
		FusionInfo fusionInfo2 = fusionInfo;
		currSliceInfo = new List<List<NodeInfo>>();
		preSliceInfo = new List<Dictionary<Call, NodeInfo>>();
		List<NodeInfo> fusedNodes = fusionInfo2.FusedNodes;
		int[] outputShape = fusedNodes[fusedNodes.Count - 1].Op.CheckedShape.ToValueArray();
		int[] lastOutShape = fusionInfo2.LastOutShape;
		List<SegmentND> list = new List<SegmentND>();
		foreach (Segment1D glbOutputBatch in TileUtilities.GetSegmentStartEndLength(0, lastOutShape[0], outputShape[0]))
		{
			List<Segment1D> segmentStartEndLength = TileUtilities.GetSegmentStartEndLength(0, lastOutShape[1], outputShape[1]);
			list.AddRange(from glbOutputChannel in segmentStartEndLength
				let outputRowSeg = TileUtilities.GetSegmentStartEndLength(0, lastOutShape[2], outputShape[2])
				from glbOutputRow in outputRowSeg
				let outputColSeg = TileUtilities.GetSegmentStartEndLength(0, lastOutShape[3], outputShape[3])
				from glbOutputColumn in outputColSeg
				select new SegmentND(glbOutputBatch, glbOutputChannel, glbOutputRow, glbOutputColumn));
		}
		Dictionary<int, int> ofBufMap = new Dictionary<int, int>
		{
			{ 0, 0 },
			{ 1, 1 },
			{ 2, 2 }
		};
		Dictionary<int, int> ofBufOffset = new Dictionary<int, int>
		{
			{ 0, 0 },
			{ 1, 0 },
			{ 2, 0 }
		};
		Dictionary<int, int> weightBufOffset = new Dictionary<int, int>
		{
			{ 0, 0 },
			{ 1, 0 }
		};
		List<NodeInfo> fusedNodes2 = fusionInfo2.FusedNodes;
		List<NodeInfo> currSlice;
		Call prevNode;
		for (int i = 0; i < list.Count; i++)
		{
			currSlice = new List<NodeInfo>();
			Dictionary<Call, NodeInfo> dictionary = new Dictionary<Call, NodeInfo>();
			for (int num = fusedNodes2.Count - 1; num >= 0; num--)
			{
				Call op = fusedNodes2[num].Op;
				if ((object)op != null && op.Target is GNNEStore)
				{
					prevNode = (Call)op[GNNEStore.Input];
					NodeBuffer nodeBuffer = GetPreNodeBuffer(prevNode);
					nodeBuffer.OfBufferIndex = OfBufferIdx(prevNode);
					nodeBuffer.OfmapOffset = GetOfBufOffset(nodeBuffer.OfBufferIndex);
					nodeBuffer.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer, i);
					currSlice.Add(new NodeInfo(op, list[i], fusedNodes2[num].Nb, fusedNodes2[num].Children));
					dictionary.Add(prevNode, new NodeInfo(prevNode, list[i], nodeBuffer, new List<NodeInfo> { currSlice.Find((NodeInfo n) => n.Op == op) }));
				}
				if ((object)op != null)
				{
					Expr target = op.Target;
					if (!(target is GNNEConv2D))
					{
						if (!(target is GNNEPdp0DW))
						{
							if (!(target is GNNEPdp0Reduce))
							{
								if (!(target is GNNEPdp1))
								{
									if (!(target is GNNETranspose))
									{
										if (!(target is Concat))
										{
											if (!(target is Ai2dResize))
											{
												if (!(target is GNNEActivation))
												{
													if (target is GNNELoad)
													{
														currSlice.Add(dictionary[op]);
													}
												}
												else
												{
													SegmentND ofmap = dictionary[op].Ofmap;
													bool flag = op[GNNEActivation.InputB] == None.Default;
													var (segmentND3, segmentND4) = GetAct1InputsShape(op[GNNEActivation.InputA].CheckedShape.ToValueArray(), flag ? op[GNNEActivation.InputA].CheckedShape.ToValueArray() : op[GNNEActivation.InputB].CheckedShape.ToValueArray(), op.CheckedShape.ToValueArray(), ofmap);
													if (fusedNodes2.Find((NodeInfo n) => n.Op == op[GNNEActivation.InputA]) != null)
													{
														SegmentND segmentND5 = new SegmentND(segmentND3[0], segmentND3[1], segmentND3[2], segmentND3[3]);
														prevNode = (Call)op[GNNEActivation.InputA];
														NodeBuffer nodeBuffer2 = GetPreNodeBuffer(prevNode);
														nodeBuffer2.OfBufferIndex = OfBufferIdx(prevNode);
														nodeBuffer2.OfmapOffset = GetOfBufOffset(nodeBuffer2.OfBufferIndex);
														nodeBuffer2.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer2, i);
														if (dictionary.ContainsKey(prevNode))
														{
															SegmentND ofmap2 = dictionary[prevNode].Ofmap;
															dictionary[prevNode].Nb = nodeBuffer2;
															dictionary[prevNode].Children.Add(dictionary[op]);
															dictionary[prevNode].Ofmap = ofmap2 + segmentND5;
														}
														else
														{
															dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND5, nodeBuffer2, new List<NodeInfo> { dictionary[op] }));
														}
													}
													if (!flag && fusedNodes2.Find((NodeInfo n) => n.Op == op[GNNEActivation.InputB]) != null)
													{
														SegmentND segmentND6 = new SegmentND(segmentND4[0], segmentND4[1], segmentND4[2], segmentND4[3]);
														prevNode = (Call)op[GNNEActivation.InputB];
														NodeBuffer nodeBuffer3 = GetPreNodeBuffer(prevNode);
														nodeBuffer3.OfBufferIndex = OfBufferIdx(prevNode);
														nodeBuffer3.OfmapOffset = GetOfBufOffset(nodeBuffer3.OfBufferIndex);
														nodeBuffer3.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer3, i);
														if (dictionary.ContainsKey(prevNode))
														{
															SegmentND ofmap3 = dictionary[prevNode].Ofmap;
															dictionary[prevNode].Nb = nodeBuffer3;
															dictionary[prevNode].Children.Add(dictionary[op]);
															dictionary[prevNode].Ofmap = ofmap3 + segmentND6;
														}
														else
														{
															dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND6, nodeBuffer3, new List<NodeInfo> { dictionary[op] }));
														}
													}
													currSlice.Add(dictionary[op]);
												}
											}
											else
											{
												int[] array = op[Ai2dResize.Input].CheckedShape.ToValueArray();
												SegmentND ofmap4 = dictionary[_resize].Ofmap;
												Segment1D segment1D = new Segment1D(..array[2], Padding.Zero());
												Segment1D segment1D2 = new Segment1D(..array[3], Padding.Zero());
												SegmentND segmentND7 = new SegmentND(ofmap4[0], new Segment1D(..array[1], Padding.Zero()), segment1D, segment1D2);
												prevNode = (Call)op[Ai2dResize.Input];
												NodeBuffer nodeBuffer4 = GetPreNodeBuffer(prevNode);
												nodeBuffer4.OfBufferIndex = OfBufferIdx(prevNode);
												if (dictionary.ContainsKey(prevNode))
												{
													SegmentND ofmap5 = dictionary[prevNode].Ofmap;
													dictionary[prevNode].Nb = nodeBuffer4;
													dictionary[prevNode].Children.Add(dictionary[op]);
													dictionary[prevNode].Ofmap = ofmap5 + segmentND7;
												}
												else
												{
													dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND7, nodeBuffer4, new List<NodeInfo> { dictionary[op] }));
												}
												currSlice.Add(dictionary[op]);
											}
										}
										else
										{
											int[] array2 = op[Concat.Input][0].CheckedShape.ToValueArray();
											Segment1D segment1D3 = new Segment1D(..array2[0], Padding.Zero());
											Segment1D segment1D4 = new Segment1D(..array2[1], Padding.Zero());
											Segment1D segment1D5 = new Segment1D(..array2[2], Padding.Zero());
											Segment1D segment1D6 = new Segment1D(..array2[3], Padding.Zero());
											SegmentND segmentND8 = new SegmentND(segment1D3, segment1D4, segment1D5, segment1D6);
											prevNode = (Call)op[Concat.Input][0];
											NodeBuffer nodeBuffer5 = GetPreNodeBuffer(prevNode);
											_ = fusedNodes2.Find((NodeInfo n) => n.Op == prevNode).Children;
											nodeBuffer5.OfBufferIndex = OfBufferIdx(prevNode);
											nodeBuffer5.OfmapOffset = GetOfBufOffset(nodeBuffer5.OfBufferIndex);
											nodeBuffer5.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer5, i);
											if (dictionary.ContainsKey(prevNode))
											{
												SegmentND ofmap6 = dictionary[prevNode].Ofmap;
												dictionary[prevNode].Nb = nodeBuffer5;
												dictionary[prevNode].Children.Add(dictionary[op]);
												dictionary[prevNode].Ofmap = ofmap6 + segmentND8;
											}
											else
											{
												dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND8, nodeBuffer5, new List<NodeInfo> { dictionary[op] }));
											}
											int[] array3 = op[Concat.Input][1].CheckedShape.ToValueArray();
											Segment1D segment1D7 = new Segment1D(..array3[0], Padding.Zero());
											Segment1D segment1D8 = new Segment1D(..array3[1], Padding.Zero());
											Segment1D segment1D9 = new Segment1D(..array3[2], Padding.Zero());
											Segment1D segment1D10 = new Segment1D(..array3[3], Padding.Zero());
											SegmentND segmentND9 = new SegmentND(segment1D7, segment1D8, segment1D9, segment1D10);
											prevNode = (Call)op[Concat.Input][1];
											NodeBuffer nodeBuffer6 = GetPreNodeBuffer(prevNode);
											List<NodeInfo> children = fusedNodes2.Find((NodeInfo n) => n.Op == prevNode).Children;
											nodeBuffer6.OfBufferIndex = OfBufferIdx(prevNode);
											nodeBuffer6.OfmapOffset = GetOfBufOffset(nodeBuffer6.OfBufferIndex);
											nodeBuffer6.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer6, i);
											if (dictionary.ContainsKey(prevNode))
											{
												SegmentND ofmap7 = dictionary[prevNode].Ofmap;
												dictionary[prevNode] = new NodeInfo(prevNode, ofmap7 + segmentND9, nodeBuffer6, children);
											}
											else
											{
												dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND9, nodeBuffer6, children));
											}
											currSlice.Add(dictionary[op]);
										}
									}
									else
									{
										op[GNNETranspose.Input].CheckedShape.ToValueArray();
										SegmentND ofmap8 = dictionary[op].Ofmap;
										MFU_TRANS_PERMUTE perm2 = ((GNNETranspose)op.Target).Perm;
										Segment1D[] array4 = GetInputSeg(ofmap8[0], ofmap8[1], ofmap8[2], ofmap8[3], perm2);
										Segment1D segment1D11 = array4[0];
										Segment1D segment1D12 = array4[1];
										Segment1D segment1D13 = array4[2];
										Segment1D segment1D14 = array4[3];
										SegmentND segmentND10 = new SegmentND(segment1D11, segment1D12, segment1D13, segment1D14);
										prevNode = (Call)op[GNNETranspose.Input];
										NodeBuffer nodeBuffer7 = GetPreNodeBuffer(prevNode);
										nodeBuffer7.OfBufferIndex = OfBufferIdx(prevNode);
										nodeBuffer7.OfmapOffset = GetOfBufOffset(nodeBuffer7.OfBufferIndex);
										nodeBuffer7.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer7, i);
										if (dictionary.ContainsKey(prevNode))
										{
											SegmentND ofmap9 = dictionary[prevNode].Ofmap;
											dictionary[prevNode].Nb = nodeBuffer7;
											dictionary[prevNode].Children.Add(dictionary[op]);
											dictionary[prevNode].Ofmap = ofmap9 + segmentND10;
										}
										else
										{
											dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND10, nodeBuffer7, new List<NodeInfo> { dictionary[op] }));
										}
										currSlice.Add(dictionary[op]);
									}
								}
								else
								{
									int[] array5 = op[GNNEPdp1.Input].CheckedShape.ToValueArray();
									SegmentND ofmap10 = dictionary[op].Ofmap;
									int[] array6 = ((TensorConst)op[GNNEPdp1.Padding]).Value.ToArray<int>();
									Padding p = new Padding(array6[0], array6[1]);
									Padding p2 = new Padding(array6[2], array6[3]);
									int u = ((TensorConst)op[GNNEPdp1.Stride]).Value.ToArray<int>()[0];
									int u2 = ((TensorConst)op[GNNEPdp1.Stride]).Value.ToArray<int>()[1];
									int d4 = 1;
									int d5 = 1;
									int r = ((TensorConst)op[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
									int s = ((TensorConst)op[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
									Segment1D inputRowSegment = TileUtilities.GetInputRowSegment(ofmap10[2].Start, ofmap10[2].Length, array5[2], r, u, d4, in p);
									Segment1D inputColumnSegment = TileUtilities.GetInputColumnSegment(ofmap10[3].Start, ofmap10[3].Length, array5[3], s, u2, d5, in p2);
									SegmentND segmentND11 = new SegmentND(ofmap10[0], new Segment1D(..array5[1], Padding.Zero()), inputRowSegment, inputColumnSegment);
									prevNode = (Call)op[GNNEPdp1.Input];
									NodeBuffer nodeBuffer8 = GetPreNodeBuffer(prevNode);
									nodeBuffer8.OfBufferIndex = OfBufferIdx(prevNode);
									nodeBuffer8.OfmapOffset = GetOfBufOffset(nodeBuffer8.OfBufferIndex);
									nodeBuffer8.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer8, i);
									if (dictionary.ContainsKey(prevNode))
									{
										SegmentND ofmap11 = dictionary[prevNode].Ofmap;
										dictionary[prevNode].Nb = nodeBuffer8;
										dictionary[prevNode].Children.Add(dictionary[op]);
										dictionary[prevNode].Ofmap = ofmap11 + segmentND11;
									}
									else
									{
										dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND11, nodeBuffer8, new List<NodeInfo> { dictionary[op] }));
									}
									currSlice.Add(dictionary[op]);
								}
							}
							else
							{
								int[] array7 = op[GNNEPdp0Reduce.Input].CheckedShape.ToValueArray();
								SegmentND ofmap12 = dictionary[op].Ofmap;
								int[] array8 = ((TensorConst)op[GNNEPdp0Reduce.Padding]).Value.ToArray<int>();
								Padding p3 = new Padding(array8[0], array8[1]);
								Padding p4 = new Padding(array8[2], array8[3]);
								int u3 = ((TensorConst)op[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[0];
								int u4 = ((TensorConst)op[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[1];
								int d6 = 1;
								int d7 = 1;
								int r2 = ((TensorConst)op[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[0];
								int s2 = ((TensorConst)op[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[1];
								Segment1D inputRowSegment2 = TileUtilities.GetInputRowSegment(ofmap12[2].Start, ofmap12[2].Length, array7[2], r2, u3, d6, in p3);
								Segment1D inputColumnSegment2 = TileUtilities.GetInputColumnSegment(ofmap12[3].Start, ofmap12[3].Length, array7[3], s2, u4, d7, in p4);
								SegmentND segmentND12 = new SegmentND(ofmap12[0], new Segment1D(..array7[1], Padding.Zero()), inputRowSegment2, inputColumnSegment2);
								prevNode = (Call)op[GNNEPdp0Reduce.Input];
								NodeBuffer nodeBuffer9 = GetPreNodeBuffer(prevNode);
								nodeBuffer9.OfBufferIndex = OfBufferIdx(prevNode);
								nodeBuffer9.OfmapOffset = GetOfBufOffset(nodeBuffer9.OfBufferIndex);
								nodeBuffer9.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer9, i);
								if (dictionary.ContainsKey(prevNode))
								{
									SegmentND ofmap13 = dictionary[prevNode].Ofmap;
									dictionary[prevNode].Nb = nodeBuffer9;
									dictionary[prevNode].Children.Add(dictionary[op]);
									dictionary[prevNode].Ofmap = ofmap13 + segmentND12;
								}
								else
								{
									dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND12, nodeBuffer9, new List<NodeInfo> { dictionary[op] }));
								}
								currSlice.Add(dictionary[op]);
							}
						}
						else
						{
							int[] array9 = op[GNNEPdp0DW.Input].CheckedShape.ToValueArray();
							SegmentND ofmap14 = dictionary[op].Ofmap;
							int[] array10 = op[GNNEPdp0DW.Weights].CheckedShape.ToValueArray();
							int[] array11 = ((TensorConst)op[GNNEPdp0DW.Padding]).Value.ToArray<int>();
							Padding p5 = new Padding(array11[0], array11[1]);
							Padding p6 = new Padding(array11[2], array11[3]);
							int u5 = ((TensorConst)op[GNNEPdp0DW.Stride]).Value.ToArray<int>()[0];
							int u6 = ((TensorConst)op[GNNEPdp0DW.Stride]).Value.ToArray<int>()[1];
							int d8 = ((TensorConst)op[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[0];
							int d9 = ((TensorConst)op[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[1];
							Segment1D inputRowSegment3 = TileUtilities.GetInputRowSegment(ofmap14[2].Start, ofmap14[2].Length, array9[2], array10[2], u5, d8, in p5);
							Segment1D inputColumnSegment3 = TileUtilities.GetInputColumnSegment(ofmap14[3].Start, ofmap14[3].Length, array9[3], array10[3], u6, d9, in p6);
							SegmentND segmentND13 = new SegmentND(ofmap14[0], new Segment1D(..array9[1], Padding.Zero()), inputRowSegment3, inputColumnSegment3);
							prevNode = (Call)op[GNNEPdp0DW.Input];
							NodeBuffer nodeBuffer10 = GetPreNodeBuffer(prevNode);
							nodeBuffer10.OfBufferIndex = OfBufferIdx(prevNode);
							nodeBuffer10.OfmapOffset = GetOfBufOffset(nodeBuffer10.OfBufferIndex);
							nodeBuffer10.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer10, i);
							if (dictionary.ContainsKey(prevNode))
							{
								SegmentND ofmap15 = dictionary[prevNode].Ofmap;
								dictionary[prevNode].Nb = nodeBuffer10;
								dictionary[prevNode].Children.Add(dictionary[op]);
								dictionary[prevNode].Ofmap = ofmap15 + segmentND13;
							}
							else
							{
								dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND13, nodeBuffer10, new List<NodeInfo> { dictionary[op] }));
							}
							currSlice.Add(dictionary[op]);
						}
					}
					else
					{
						int[] array12 = op[GNNEConv2D.Input].CheckedShape.ToValueArray();
						SegmentND ofmap16 = dictionary[op].Ofmap;
						int[] array13 = op[GNNEConv2D.Weights].CheckedShape.ToValueArray();
						int[] array14 = ((TensorConst)op[GNNEConv2D.Padding]).Value.ToArray<int>();
						Padding p7 = new Padding(array14[0], array14[1]);
						Padding p8 = new Padding(array14[2], array14[3]);
						int u7 = ((TensorConst)op[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
						int u8 = ((TensorConst)op[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
						int d10 = ((TensorConst)op[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
						int d11 = ((TensorConst)op[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
						Segment1D inputRowSegment4 = TileUtilities.GetInputRowSegment(ofmap16[2].Start, ofmap16[2].Length, array12[2], array13[2], u7, d10, in p7);
						Segment1D inputColumnSegment4 = TileUtilities.GetInputColumnSegment(ofmap16[3].Start, ofmap16[3].Length, array12[3], array13[3], u8, d11, in p8);
						prevNode = (Call)op[GNNEConv2D.Input];
						SegmentND segmentND14 = new SegmentND(ofmap16[0], new Segment1D(..array12[1], Padding.Zero()), inputRowSegment4, inputColumnSegment4);
						NodeBuffer nodeBuffer11 = GetPreNodeBuffer(prevNode);
						nodeBuffer11.OfBufferIndex = OfBufferIdx(prevNode);
						nodeBuffer11.OfmapOffset = GetOfBufOffset(nodeBuffer11.OfBufferIndex);
						nodeBuffer11.WeightOffset = GetWeightBufOffset(prevNode, nodeBuffer11, i);
						if (dictionary.ContainsKey(prevNode))
						{
							SegmentND ofmap17 = dictionary[prevNode].Ofmap;
							dictionary[prevNode].Nb = nodeBuffer11;
							dictionary[prevNode].Children.Add(dictionary[op]);
							dictionary[prevNode].Ofmap = ofmap17 + segmentND14;
						}
						else
						{
							dictionary.Add(prevNode, new NodeInfo(prevNode, segmentND14, nodeBuffer11, new List<NodeInfo> { dictionary[op] }));
						}
						currSlice.Add(dictionary[op]);
					}
				}
			}
			currSlice.Reverse();
			foreach (NodeInfo item in currSlice)
			{
				item.Nb.OutputsSize = item.Children.Count;
			}
			foreach (NodeInfo value in dictionary.Values)
			{
				value.Nb.OutputsSize = value.Children.Count;
			}
			currSliceInfo.Add(currSlice);
			preSliceInfo.Add(dictionary);
			int bufNum2 = GetOfBufferNum();
			UpdateOfBufMap(bufNum2);
			int GetOfBufferNum()
			{
				int ofBufferIndex = currSlice[0].Nb.OfBufferIndex;
				ofBufferIndex = currSlice.Select((NodeInfo node) => node.Nb.OfBufferIndex).Prepend(ofBufferIndex).Max();
				return ofBufferIndex + 1;
			}
		}
		glb.Items = fusionInfo2.Mmu;
		glb.LastOutShape = lastOutShape;
		foreach (KeyValuePair<ItemName, MmuItem> item2 in fusionInfo2.Mmu)
		{
			glb.GlbMap.Add(item2.Key, new TensorOnGlb(new int[]{0,0,0,0}, DataTypes.Float16, 0));
			glb.GlbMap[item2.Key].Mmu = item2.Value;
		}
		glb.GlbMap.Add(ItemName.Ifmap, new TensorOnGlb(new int[]{0,0,0,0}, DataTypes.Float16, 0));
		glb.GlbMap[ItemName.Ifmap].Mmu = fusionInfo2.Mmu[ItemName.Ofmap];
		if (!fusionInfo2.Mmu.ContainsKey(ItemName.Ifmap2))
		{
			glb.GlbMap.Add(ItemName.Ifmap2, new TensorOnGlb(new int[]{0,0,0,0}, DataTypes.Float16, 0));
			glb.GlbMap[ItemName.Ifmap2].Mmu = fusionInfo2.Mmu[ItemName.Ofmap];
		}
		static Segment1D[] GetInputSeg(Segment1D d0, Segment1D d1, Segment1D d2, Segment1D d3, MFU_TRANS_PERMUTE perm)
		{
			return perm switch
			{
				MFU_TRANS_PERMUTE.NCHW => new Segment1D[4] { d0, d1, d2, d3 }, 
				MFU_TRANS_PERMUTE.NCWH => new Segment1D[4] { d0, d1, d3, d2 }, 
				MFU_TRANS_PERMUTE.NHCW => new Segment1D[4] { d0, d2, d1, d3 }, 
				MFU_TRANS_PERMUTE.NHWC => new Segment1D[4] { d0, d3, d1, d2 }, 
				MFU_TRANS_PERMUTE.NWCH => new Segment1D[4] { d0, d2, d3, d1 }, 
				MFU_TRANS_PERMUTE.NWHC => new Segment1D[4] { d0, d3, d2, d1 }, 
				MFU_TRANS_PERMUTE.CNHW => new Segment1D[4] { d1, d0, d2, d3 }, 
				MFU_TRANS_PERMUTE.CNWH => new Segment1D[4] { d1, d0, d3, d2 }, 
				MFU_TRANS_PERMUTE.CHNW => new Segment1D[4] { d2, d0, d1, d3 }, 
				MFU_TRANS_PERMUTE.CHWN => new Segment1D[4] { d3, d0, d1, d2 }, 
				MFU_TRANS_PERMUTE.CWNH => new Segment1D[4] { d2, d0, d3, d1 }, 
				MFU_TRANS_PERMUTE.CWHN => new Segment1D[4] { d3, d0, d2, d1 }, 
				MFU_TRANS_PERMUTE.HNCW => new Segment1D[4] { d1, d2, d0, d3 }, 
				MFU_TRANS_PERMUTE.HNWC => new Segment1D[4] { d1, d3, d0, d2 }, 
				MFU_TRANS_PERMUTE.HCNW => new Segment1D[4] { d2, d1, d0, d3 }, 
				MFU_TRANS_PERMUTE.HCWN => new Segment1D[4] { d3, d1, d0, d2 }, 
				MFU_TRANS_PERMUTE.HWNC => new Segment1D[4] { d2, d3, d0, d1 }, 
				MFU_TRANS_PERMUTE.HWCN => new Segment1D[4] { d3, d2, d0, d1 }, 
				MFU_TRANS_PERMUTE.WNCH => new Segment1D[4] { d1, d2, d3, d0 }, 
				MFU_TRANS_PERMUTE.WNHC => new Segment1D[4] { d1, d3, d2, d0 }, 
				MFU_TRANS_PERMUTE.WCNH => new Segment1D[4] { d2, d1, d3, d0 }, 
				MFU_TRANS_PERMUTE.WCHN => new Segment1D[4] { d3, d1, d2, d0 }, 
				MFU_TRANS_PERMUTE.WHNC => new Segment1D[4] { d2, d3, d1, d0 }, 
				MFU_TRANS_PERMUTE.WHCN => new Segment1D[4] { d3, d2, d1, d0 }, 
				_ => new Segment1D[4] { d0, d1, d2, d3 }, 
			};
		}
		int GetOfBufOffset(int bufIdx)
		{
			foreach (NodeInfo fusedNode in fusionInfo2.FusedNodes)
			{
				TileUtilities.Assert(ofBufOffset[fusedNode.Nb.OfBufferIndex] == 0 || ofBufOffset[fusedNode.Nb.OfBufferIndex] == fusedNode.Nb.OfmapOffset, "ofBufOffset[node.Nb.OfBufferIndex] == 0 || ofBufOffset[node.Nb.OfBufferIndex] == node.Nb.OfmapOffset", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 2258);
				ofBufOffset[fusedNode.Nb.OfBufferIndex] = fusedNode.Nb.OfmapOffset;
			}
			if (bufIdx != -1)
			{
				return ofBufOffset[bufIdx];
			}
			return -1;
		}
		NodeBuffer GetPreNodeBuffer(Call node)
		{
			foreach (NodeInfo fusedNode2 in fusionInfo2.FusedNodes)
			{
				if (node == fusedNode2.Op)
				{
					return new NodeBuffer(fusedNode2.Nb);
				}
			}
			return new NodeBuffer();
		}
		int GetWeightBufOffset(Call curNode, NodeBuffer nb, int sliceIdx)
		{
			int num2 = 0;
			int num3 = 0;
			foreach (NodeInfo fusedNode3 in fusionInfo2.FusedNodes)
			{
				Call op2 = fusedNode3.Op;
				if ((object)op2 != null && op2.Target is GNNEConv2D)
				{
					TileUtilities.Assert(fusedNode3.Nb.WeightOffset == weightBufOffset[0] || fusedNode3.Nb.WeightOffset == weightBufOffset[1] || (weightBufOffset[0] == 0 && weightBufOffset[1] == 0), "node.Nb.WeightOffset == weightBufOffset[0] || node.Nb.WeightOffset == weightBufOffset[1] || (weightBufOffset[0] == 0 && weightBufOffset[1] == 0)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 2276);
					weightBufOffset[(fusedNode3.Nb.WeightOffset != 0) ? 1 : 0] = fusedNode3.Nb.WeightOffset;
					num2++;
				}
				if (fusedNode3.Op == curNode)
				{
					num3 = nb.WeightOffset;
				}
			}
			if ((object)curNode != null && curNode.Target is GNNEConv2D)
			{
				int key = ((num3 != 0) ? 1 : (sliceIdx * num2)) & 1;
				num3 = weightBufOffset[key];
			}
			return num3;
		}
		int OfBufferIdx(Call currNode)
		{
			foreach (NodeInfo fusedNode4 in fusionInfo2.FusedNodes)
			{
				if (currNode == fusedNode4.Op)
				{
					return ofBufMap[fusedNode4.Nb.OfBufferIndex];
				}
			}
			return -1;
		}
		void UpdateOfBufMap(int bufNum1)
		{
			Dictionary<int, int> dictionary2 = ofBufMap;
			List<NodeInfo> list2 = currSlice;
			dictionary2[0] = (list2[list2.Count - 2].Nb.OfBufferIndex + 1) % bufNum1;
			Dictionary<int, int> dictionary3 = ofBufMap;
			List<NodeInfo> list3 = currSlice;
			dictionary3[1] = (list3[list3.Count - 2].Nb.OfBufferIndex + 2) % bufNum1;
			Dictionary<int, int> dictionary4 = ofBufMap;
			List<NodeInfo> list4 = currSlice;
			dictionary4[2] = (list4[list4.Count - 2].Nb.OfBufferIndex + 3) % bufNum1;
		}
	}

	private Tuple<SegmentND, SegmentND> GetAct1InputsShape(int[] inputAShape, int[] inputBShape, int[] outputShape, SegmentND slice)
	{
		int[] array = new int[4];
		for (int i = 0; i < 4; i++)
		{
			array[i] = Math.Min(inputAShape[i], inputBShape[i]);
		}
		int[] array2 = new int[4];
		int[] array3 = new int[4];
		int[] array4 = new int[4];
		for (int j = 0; j < 4; j++)
		{
			array2[j] = ((array[j] == 1) ? 1 : (inputAShape[j] / array[j]));
			array3[j] = ((array[j] == 1) ? 1 : (inputBShape[j] / array[j]));
			array4[j] = ((array[j] == 1) ? 1 : (outputShape[j] / array[j]));
		}
		Segment1D segment1D = slice[0];
		Segment1D segment1D2 = segment1D / (array4[0] / array2[0]);
		Segment1D segment1D3 = segment1D / (array4[0] / array3[0]);
		if (inputAShape[0] == 1)
		{
			segment1D2 = new Segment1D(..1, Padding.Zero());
		}
		if (inputBShape[0] == 1)
		{
			segment1D3 = new Segment1D(..1, Padding.Zero());
		}
		Segment1D segment1D4 = slice[1];
		Segment1D segment1D5 = segment1D4 / (array4[1] / array2[1]);
		Segment1D segment1D6 = segment1D4 / (array4[1] / array3[1]);
		if (inputAShape[1] == 1)
		{
			segment1D5 = new Segment1D(..1, Padding.Zero());
		}
		if (inputBShape[1] == 1)
		{
			segment1D6 = new Segment1D(..1, Padding.Zero());
		}
		Segment1D segment1D7 = slice[2];
		Segment1D segment1D8 = segment1D7 / (array4[2] / array2[2]);
		Segment1D segment1D9 = segment1D7 / (array4[2] / array3[2]);
		if (inputAShape[2] == 1)
		{
			segment1D8 = new Segment1D(..1, Padding.Zero());
		}
		if (inputBShape[2] == 1)
		{
			segment1D9 = new Segment1D(..1, Padding.Zero());
		}
		Segment1D segment1D10 = slice[3];
		Segment1D segment1D11 = segment1D10 / (array4[3] / array2[3]);
		Segment1D segment1D12 = segment1D10 / (array4[3] / array3[3]);
		if (inputAShape[3] == 1)
		{
			segment1D11 = new Segment1D(..1, Padding.Zero());
		}
		if (inputBShape[3] == 1)
		{
			segment1D12 = new Segment1D(..1, Padding.Zero());
		}
		SegmentND item = new SegmentND(segment1D2, segment1D5, segment1D8, segment1D11);
		SegmentND item2 = new SegmentND(segment1D3, segment1D6, segment1D9, segment1D12);
		return new Tuple<SegmentND, SegmentND>(item, item2);
	}

	private void ItemRecStatusInit(List<List<NodeInfo>> currSliceInfo)
	{
		_nodesWeightRec.Clear();
		_nodesOfmapRec.Clear();
		_nodesG2LIfRec.Clear();
		_nodesG2RWRec.Clear();
		_nodesL2GOfRec.Clear();
		_nodesL2RIf2Rec.Clear();
		_nodesG2RWSliceRec.Clear();
		_nodesAi2dIfRec.Clear();
		_nodesAi2dOfRec.Clear();
		_nodesQuenesAsW.Clear();
		_nodesQuenesAsW = new List<List<Tuple<Call, int>>>(2)
		{
			new List<Tuple<Call, int>>(),
			new List<Tuple<Call, int>>()
		};
		_nodesQueNeedClearFake.Clear();
		for (int i = 0; i < currSliceInfo.Count - 1; i++)
		{
			List<NodeInfo> list = currSliceInfo[i];
			int ofBufferIndex = list[list.Count - 2].Nb.OfBufferIndex;
			for (int j = 0; j < currSliceInfo[i + 1].Count - 1; j++)
			{
				NodeInfo nodeInfo = currSliceInfo[i + 1][j];
				if (nodeInfo.Nb.OfBufferIndex == ofBufferIndex)
				{
					Call op = nodeInfo.Op;
					if ((object)op != null && op.Target is GNNEConv2D)
					{
						_nodesQueNeedClearFake.Add(new Tuple<Call, int>(nodeInfo.Op, i + 1));
					}
					break;
				}
			}
		}
		if (_nodesQueNeedClearFake.Count > 0)
		{
			_nodesQueNeedClearFake.Insert(0, new Tuple<Call, int>(_nodesQueNeedClearFake[0].Item1, 0));
		}
	}

	private void UpdateL2FusePara(FusionInfo fusionInfo, NodeInfo curNode, Dictionary<Call, NodeInfo> sliceInfo, TiledGlb glb, bool weightGroupOnly = false, bool firstLayer = false)
	{
		_conv = null;
		_pool = null;
		_dw = null;
		_act1 = null;
		_lif = null;
		_lw = null;
		_lact = null;
		_lwQarg = null;
		_sof = null;
		_lif2 = null;
		_pdp1 = null;
		_transpose = null;
		_cat = null;
		_resize = null;
		_preNi = null;
		_ni = null;
		_l1FuseNi = null;
		_l1Fused = false;
		if (firstLayer)
		{
			_l1FusedInfos.Clear();
		}
		_l1FusedInfos.Add(curNode.Op, L1FusedType.NoFused);
		_swapAB = false;
		_h2C = false;
		_src2ItemName = ItemName.Ifmap2;
		Segment1D segment1D = new Segment1D(..0, Padding.Zero());
		_ifmap = new SegmentND(segment1D, segment1D, segment1D, segment1D);
		_ifmap2 = _ifmap;
		_ofmap = _ifmap;
		_ofmapSt = _ifmap;
		_ifmapLd = _ifmap;
		_ofmapConv = _ifmap;
		_ifmapA = _ifmap;
		_ifmapB = _ifmap;
		_if2Type = DataTypes.Float16;
		_ni = curNode;
		_if1BufIdx = -1;
		_if2BufIdx = -1;
		_ofBufIdx = -1;
		_weightBufIdx = -1;
		Call op = curNode.Op;
		_lif = (((object)op != null && op.Target is GNNELoad && !(curNode.Op[GNNELoad.Input] is TensorConst)) ? curNode.Op : null);
		if ((object)_lif != null)
		{
			_ifmap = _ni.Ofmap;
			_ifmapOffset = 0;
			_ofmap = _ni.Ofmap;
			_if1BufIdx = -1;
			_if2BufIdx = -1;
			_ofBufIdx = _ni.Nb.OfBufferIndex;
			_inputType = _lif[GNNELoad.Input].CheckedDataType;
			_outputType = _lif.CheckedDataType;
			Call op2 = curNode.Children[0].Op;
			if ((object)op2 != null && op2.Target is GNNEConv2D)
			{
				int[] array = op2[GNNEConv2D.Weights].CheckedShape.ToValueArray();
				int[] array2 = op2[GNNEConv2D.Input].CheckedShape.ToValueArray();
				int[] array3 = op2.CheckedShape.ToValueArray();
				int num = ((TensorConst)op2[GNNEConv2D.Groups]).Value.ToScalar<int>();
				bool flag = array2[1] == array3[1] && array3[1] == num && num != 1;
				int num2 = ((TensorConst)op2[GNNEConv2D.DeqBias]).Value.ToScalar<int>();
				_h2C = (object)op2 != null && op2.Target is GNNEConv2D && array[1] * array[2] <= GNNEEnv.PuHeight && array[2] != 1 && array2[2] > 200 && array2[3] > 200 && !flag && _outputType != DataTypes.Int16;
				_memsetValue = (_h2C ? num2 : 0);
			}
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEStore)
		{
			_sof = curNode.Op;
			Call call = (Call)_sof[GNNEStore.Input];
			_ifmap = sliceInfo[call].Ofmap;
			_ifmapOffset = sliceInfo[call].Nb.OfmapOffset;
			_ofmap = _ni.Ofmap;
			_if1BufIdx = sliceInfo[call].Nb.OfBufferIndex;
			_if2BufIdx = -1;
			_ofBufIdx = sliceInfo[call].Nb.OfBufferIndex;
			_inputType = call.CheckedDataType;
			_outputType = _sof.CheckedDataType;
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEConv2D)
		{
			_conv = curNode.Op;
			_preNi = sliceInfo[(Call)_conv[GNNEConv2D.Input]];
			_inputShape = _conv[GNNEConv2D.Input].CheckedShape.ToValueArray();
			_outputShape = _conv.CheckedShape.ToValueArray();
			_convOutputShape = _outputShape;
			_weightsShape = _conv[GNNEConv2D.Weights].CheckedShape.ToValueArray();
			int[] array4 = ((TensorConst)_conv[GNNEConv2D.Padding]).Value.ToArray<int>();
			_paddingH = new Padding(array4[0], array4[1]);
			_paddingW = new Padding(array4[2], array4[3]);
			_strideH = ((TensorConst)_conv[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
			_strideW = ((TensorConst)_conv[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
			_dilationH = ((TensorConst)_conv[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
			_dilationW = ((TensorConst)_conv[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
			_groups = ((TensorConst)_conv[GNNEConv2D.Groups]).Value.ToScalar<int>();
			int num3 = ((TensorConst)_conv[GNNEConv2D.DeqBias]).Value.ToScalar<int>();
			bool flag2 = _inputShape[1] == _outputShape[1] && _outputShape[1] == _groups && _groups != 1;
			_icPerGroup = ((_groups == 1) ? _weightsShape[1] : (_inputShape[1] / _groups));
			_ocPerGroup = _outputShape[1] / _groups;
			_groupPerPass = Math.Min(Math.Max(Math.Min(GNNEEnv.PuHeight / _icPerGroup, GNNEEnv.PuWidth / _ocPerGroup), 1), _groups);
			op = curNode.Children[0].Op;
			if ((object)op != null && op.Target is GNNEPdp0Reduce)
			{
				_pool = curNode.Children[0].Op;
			}
			op = curNode.Children[0].Op;
			if ((object)op != null && op.Target is GNNEPdp0DW)
			{
				_dw = curNode.Children[0].Op;
			}
			op = curNode.Children[0].Op;
			if ((object)op != null && op.Target is GNNEActivation && curNode.Op.CheckedDataType == DataTypes.Float16 && curNode.Children.Count == 1 && ((GNNEActivation)curNode.Children[0].Op.Target).InputFromL1.Count > 0 && ((((GNNEActivation)curNode.Children[0].Op.Target).InputFromL1[0] && curNode.Children[0].Op[GNNEActivation.InputA] == curNode.Op) || (((GNNEActivation)curNode.Children[0].Op.Target).InputFromL1[1] && curNode.Children[0].Op[GNNEActivation.InputB] == curNode.Op)))
			{
				_act1 = curNode.Children[0].Op;
			}
			_inputType = _conv[GNNEConv2D.Input].CheckedDataType;
			_weightType = _conv[GNNEConv2D.Weights].CheckedDataType;
			_outputType = _conv.CheckedDataType;
			if ((object)_pool != null)
			{
				_outputType = _pool.CheckedDataType;
			}
			if ((object)_dw != null)
			{
				_outputType = _dw.CheckedDataType;
			}
			if ((object)_act1 != null)
			{
				_outputType = _act1.CheckedDataType;
			}
			_lw = (Call)_conv[GNNEConv2D.Weights];
			_lact = (Call)_conv[GNNEConv2D.Act];
			_lwQarg = (Call)_conv[GNNEConv2D.WeightsBias];
			Call key = (Call)_conv[GNNEConv2D.Input];
			_ifmap = sliceInfo[key].Ofmap;
			if (_groups == 1)
			{
				_ifmap = new SegmentND(_ifmap[0], new Segment1D(.._weightsShape[1], Padding.Zero()), _ifmap[2], _ifmap[3]);
			}
			_ifmapOffset = sliceInfo[key].Nb.OfmapOffset;
			_ofmap = _ni.Ofmap;
			_ofmapOffset = _ni.Nb.OfmapOffset;
			_ofmapConv = _ni.Ofmap;
			_h2C = _weightsShape[1] * _weightsShape[2] <= GNNEEnv.PuHeight && _weightsShape[2] != 1 && _conv[GNNEConv2D.Input] is Call call2 && call2.Target is GNNELoad && _inputShape[2] > 200 && _inputShape[3] > 200 && !flag2 && _inputType != DataTypes.Int16;
			_memsetValue = (_h2C ? num3 : 0);
			_fusedKernelH = 1;
			_fusedKernelW = 1;
			_fusedPaddingH = Padding.Zero();
			_fusedPaddingW = Padding.Zero();
			_fusedStrideH = 1;
			_fusedStrideW = 1;
			_fusedDilationH = 1;
			_fusedDilationW = 1;
			if ((object)_pool != null)
			{
				_outputShape = _pool.CheckedShape.ToValueArray();
				_fusedKernelH = ((TensorConst)_pool[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[0];
				_fusedKernelW = ((TensorConst)_pool[GNNEPdp0Reduce.Filter]).Value.ToArray<int>()[1];
				int[] array5 = ((TensorConst)_pool[GNNEPdp0Reduce.Padding]).Value.ToArray<int>();
				_fusedPaddingH = new Padding(array5[0], array5[1]);
				_fusedPaddingW = new Padding(array5[2], array5[3]);
				_fusedStrideH = ((TensorConst)_pool[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[0];
				_fusedStrideW = ((TensorConst)_pool[GNNEPdp0Reduce.Stride]).Value.ToArray<int>()[1];
				_ofmap = sliceInfo[_pool].Ofmap;
				_ofmapOffset = sliceInfo[_pool].Nb.OfmapOffset;
				_l1FuseNi = sliceInfo[_pool];
				_l1Fused = true;
				_l1FusedInfos[curNode.Op] = L1FusedType.FusedPool;
			}
			else if ((object)_dw != null)
			{
				_outputShape = _dw.CheckedShape.ToValueArray();
				int[] array6 = _dw[GNNEPdp0DW.Weights].CheckedShape.ToValueArray();
				_fusedKernelH = array6[2];
				_fusedKernelW = array6[3];
				int[] array7 = ((TensorConst)_dw[GNNEPdp0DW.Padding]).Value.ToArray<int>();
				_fusedPaddingH = new Padding(array7[0], array7[1]);
				_fusedPaddingW = new Padding(array7[2], array7[3]);
				_fusedStrideH = ((TensorConst)_dw[GNNEPdp0DW.Stride]).Value.ToArray<int>()[0];
				_fusedStrideW = ((TensorConst)_dw[GNNEPdp0DW.Stride]).Value.ToArray<int>()[1];
				_fusedDilationH = ((TensorConst)_dw[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[0];
				_fusedDilationW = ((TensorConst)_dw[GNNEPdp0DW.Dilation]).Value.ToArray<int>()[1];
				_ofmap = sliceInfo[_dw].Ofmap;
				_ofmapOffset = sliceInfo[_dw].Nb.OfmapOffset;
				_l1FuseNi = sliceInfo[_dw];
				_l1Fused = true;
				_l1FusedInfos[curNode.Op] = L1FusedType.FusedDw;
			}
			else if ((object)_act1 != null)
			{
				bool num4 = _act1[GNNEActivation.InputB] == None.Default;
				_outputShape = _act1.CheckedShape.ToValueArray();
				TileUtilities.Assert(_outputShape.SequenceEqual(_convOutputShape), "_outputShape.SequenceEqual(_convOutputShape)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3097);
				_ofmap = sliceInfo[_act1].Ofmap;
				_ofmapOffset = sliceInfo[_act1].Nb.OfmapOffset;
				if (!num4)
				{
					NodeInfo nodeInfo = sliceInfo[_act1];
					if (_act1[GNNEActivation.InputA] is Call call3 && call3.Target is GNNELoad)
					{
						Call call4 = (Call)_act1[GNNEActivation.InputA];
						if (call4[GNNELoad.Input] is Var && sliceInfo.ContainsKey(call4))
						{
							_if2BufIdx = sliceInfo[call4].Nb.OfBufferIndex;
							_src2ItemName = ItemName.Ifmap2;
							_ifmap2 = sliceInfo[call4].Ofmap;
							_ifmap2Offset = sliceInfo[call4].Nb.OfmapOffset;
							_if2BufIdx = sliceInfo[call4].Nb.OfBufferIndex;
							_if2Type = call4.CheckedDataType;
						}
						else
						{
							_lif2 = call4;
							_ifmap2 = _ofmap;
							_ifmap2Offset = nodeInfo.Nb.Ifmap2Offset;
							_src2ItemName = ItemName.Ifmap2;
							_if2Type = call4.CheckedDataType;
						}
					}
					else if (_act1[GNNEActivation.InputB] is Call call5 && call5.Target is GNNELoad)
					{
						Call call6 = (Call)_act1[GNNEActivation.InputB];
						if (call6[GNNELoad.Input] is Var && sliceInfo.ContainsKey(call6))
						{
							_if2BufIdx = sliceInfo[call6].Nb.OfBufferIndex;
							_src2ItemName = ItemName.Ifmap2;
							_ifmap2 = sliceInfo[call6].Ofmap;
							_ifmap2Offset = sliceInfo[call6].Nb.OfmapOffset;
							_if2BufIdx = sliceInfo[call6].Nb.OfBufferIndex;
							_if2Type = call6.CheckedDataType;
						}
						else
						{
							_lif2 = call6;
							_ifmap2 = _ofmap;
							_ifmap2Offset = nodeInfo.Nb.Ifmap2Offset;
							_src2ItemName = ItemName.Ifmap2;
							_if2Type = _act1[GNNEActivation.InputB].CheckedDataType;
						}
					}
					else
					{
						_src2ItemName = ItemName.Ifmap2;
						if (((GNNEActivation)_act1.Target).InputFromL1[0])
						{
							NodeInfo nodeInfo2 = sliceInfo[(Call)_act1[GNNEActivation.InputB]];
							_ifmap2 = nodeInfo2.Ofmap;
							_ifmap2Offset = nodeInfo2.Nb.OfmapOffset;
							_if2BufIdx = nodeInfo2.Nb.OfBufferIndex;
							_if2Type = _act1[GNNEActivation.InputB].CheckedDataType;
						}
						else
						{
							if (!((GNNEActivation)_act1.Target).InputFromL1[1])
							{
								throw new NotSupportedException("not support conv_act1 fuse type!");
							}
							NodeInfo nodeInfo3 = sliceInfo[(Call)_act1[GNNEActivation.InputA]];
							_ifmap2 = nodeInfo3.Ofmap;
							_ifmap2Offset = nodeInfo3.Nb.OfmapOffset;
							_if2BufIdx = nodeInfo3.Nb.OfBufferIndex;
							_if2Type = _act1[GNNEActivation.InputA].CheckedDataType;
							_swapAB = true;
						}
					}
				}
				_l1FuseNi = sliceInfo[_act1];
				_l1Fused = true;
				_l1FusedInfos[curNode.Op] = L1FusedType.FusedAct1;
			}
			_convOutputShape[2] = TileUtilities.GetInputRowSegment(0, _outputShape[2], _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH).Length;
			_convOutputShape[3] = TileUtilities.GetInputColumnSegment(0, _outputShape[3], _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW).Length;
			_weightGroup = _weightGroups[_conv];
			_weight = new SegmentND(new Segment1D(.._weightsShape[0], Padding.Zero()), new Segment1D(.._weightsShape[1], Padding.Zero()), new Segment1D(.._weightsShape[2], Padding.Zero()), new Segment1D(.._weightsShape[3], Padding.Zero()));
			_if1BufIdx = sliceInfo[key].Nb.OfBufferIndex;
			_ofBufIdx = (_l1Fused ? _l1FuseNi.Nb.OfBufferIndex : _ni.Nb.OfBufferIndex);
			_weightBufIdx = ((_ni.Nb.WeightPreloadOffset != -1) ? (-1) : ((_ni.Nb.WeightOffset != 0) ? 1 : 0));
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEActivation)
		{
			_act1 = curNode.Op;
			_lact = (Call)_act1[GNNEActivation.Act];
			_outputShape = _act1.CheckedShape.ToValueArray();
			_outputType = _act1.CheckedDataType;
			_ofmap = _ni.Ofmap;
			_ofmapOffset = _ni.Nb.OfmapOffset;
			if (!(_act1[GNNEActivation.InputB] == None.Default))
			{
				(_ifmapA, _ifmapB) = GetAct1InputsShape(_act1[GNNEActivation.InputA].CheckedShape.ToValueArray(), _act1[GNNEActivation.InputB].CheckedShape.ToValueArray(), _act1.CheckedShape.ToValueArray(), _ofmap);
				if (_act1[GNNEActivation.InputA] is Call call7 && call7.Target is GNNELoad && _act1[GNNEActivation.InputB] is Call call8 && call8.Target is GNNELoad)
				{
					Call call9 = _act1[GNNEActivation.InputA] as Call;
					Call call10 = _act1[GNNEActivation.InputB] as Call;
					_src2ItemName = ItemName.Ifmap2;
					_ifmap2Offset = _ni.Nb.Ifmap2Offset;
					if (sliceInfo.ContainsKey(call9))
					{
						_ifmap = sliceInfo[call9].Ofmap;
						_ifmapOffset = sliceInfo[call9].Nb.OfmapOffset;
						_lif2 = call10;
						_ifmap2 = _ifmapB;
						_if1BufIdx = sliceInfo[call9].Nb.OfBufferIndex;
						_if2BufIdx = -1;
						_ofBufIdx = _ni.Nb.OfBufferIndex;
						_if2Type = call10.CheckedDataType;
						_inputType = call10.CheckedDataType;
						TileUtilities.Assert(_ifmap == _ifmapA, "_ifmap == _ifmapA", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3242);
					}
					else
					{
						TileUtilities.Assert(sliceInfo.ContainsKey(call10), "sliceInfo.ContainsKey(act1LifB!)", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3246);
						_ifmap = sliceInfo[call10].Ofmap;
						_ifmapOffset = sliceInfo[call10].Nb.OfmapOffset;
						_lif2 = call9;
						_ifmap2 = _ifmapA;
						_if1BufIdx = sliceInfo[call10].Nb.OfBufferIndex;
						_if2BufIdx = -1;
						_ofBufIdx = _ni.Nb.OfBufferIndex;
						_if2Type = call9.CheckedDataType;
						_inputType = call10.CheckedDataType;
						TileUtilities.Assert(_ifmap == _ifmapB, "_ifmap == _ifmapB", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3256);
					}
				}
				else if (_act1[GNNEActivation.InputA] is Call call11 && call11.Target is GNNELoad && !sliceInfo.ContainsKey((Call)_act1[GNNEActivation.InputA]))
				{
					Call call12 = _act1[GNNEActivation.InputA] as Call;
					Call call13 = _act1[GNNEActivation.InputB] as Call;
					_ifmap = sliceInfo[call13].Ofmap;
					_ifmapOffset = sliceInfo[call13].Nb.OfmapOffset;
					_ifmap2 = _ifmapA;
					_src2ItemName = ItemName.Ifmap2;
					_ifmap2Offset = _ni.Nb.Ifmap2Offset;
					_lif2 = call12;
					_if1BufIdx = sliceInfo[call13].Nb.OfBufferIndex;
					_if2BufIdx = -1;
					_ofBufIdx = _ni.Nb.OfBufferIndex;
					_if2Type = call12.CheckedDataType;
					_inputType = call13.CheckedDataType;
					_swapAB = true;
					TileUtilities.Assert(_ifmap == _ifmapB, "_ifmap == _ifmapB", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3275);
				}
				else if (_act1[GNNEActivation.InputB] is Call call14 && call14.Target is GNNELoad && !sliceInfo.ContainsKey((Call)_act1[GNNEActivation.InputB]))
				{
					Call call15 = _act1[GNNEActivation.InputB] as Call;
					Call call16 = _act1[GNNEActivation.InputA] as Call;
					_ifmapOffset = sliceInfo[call16].Nb.OfmapOffset;
					_ifmap = sliceInfo[call16].Ofmap;
					_ifmap2 = _ifmapB;
					_src2ItemName = ItemName.Ifmap2;
					_ifmap2Offset = _ni.Nb.Ifmap2Offset;
					_lif2 = call15;
					_if1BufIdx = sliceInfo[call16].Nb.OfBufferIndex;
					_if2BufIdx = -1;
					_ofBufIdx = _ni.Nb.OfBufferIndex;
					_if2Type = call15.CheckedDataType;
					_inputType = call16.CheckedDataType;
					TileUtilities.Assert(_ifmap == _ifmapA, "_ifmap == _ifmapA", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3292);
				}
				else
				{
					Call call17 = _act1[GNNEActivation.InputA] as Call;
					Call call18 = _act1[GNNEActivation.InputB] as Call;
					_ifmap = sliceInfo[call17].Ofmap;
					_ifmapOffset = sliceInfo[call17].Nb.OfmapOffset;
					_ifmap2 = sliceInfo[call18].Ofmap;
					_ifmap2Offset = sliceInfo[call18].Nb.OfmapOffset;
					_src2ItemName = ItemName.Ifmap2;
					_if1BufIdx = sliceInfo[call17].Nb.OfBufferIndex;
					_if2BufIdx = sliceInfo[call18].Nb.OfBufferIndex;
					_ofBufIdx = _ni.Nb.OfBufferIndex;
					_if2Type = call18.CheckedDataType;
					_inputType = call17.CheckedDataType;
					TileUtilities.Assert(_ifmap2.Shape_size > 0, "_ifmap2.Shape_size > 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3311);
				}
			}
			else
			{
				Call call19 = _act1[GNNEActivation.InputA] as Call;
				_ifmap = sliceInfo[call19].Ofmap;
				_ifmapOffset = sliceInfo[call19].Nb.OfmapOffset;
				_if1BufIdx = sliceInfo[call19].Nb.OfBufferIndex;
				_if2BufIdx = -1;
				_ofBufIdx = _ni.Nb.OfBufferIndex;
				_inputType = call19.CheckedDataType;
				_if2Type = _inputType;
			}
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEPdp1)
		{
			_pdp1 = curNode.Op;
			Call call20 = _pdp1[GNNEPdp1.Input] as Call;
			_ifmap = sliceInfo[call20].Ofmap;
			_ifmapOffset = sliceInfo[call20].Nb.OfmapOffset;
			_inputType = call20.CheckedDataType;
			_outputType = _pdp1.CheckedDataType;
			_ofmap = _ni.Ofmap;
			_ofmapOffset = _ni.Nb.OfmapOffset;
			_inputShape = _pdp1[GNNEPdp1.Input].CheckedShape.ToValueArray();
			_outputShape = _pdp1.CheckedShape.ToValueArray();
			_if1BufIdx = sliceInfo[call20].Nb.OfBufferIndex;
			_if2BufIdx = -1;
			_ofBufIdx = _ni.Nb.OfBufferIndex;
			int num5 = ((TensorConst)_pdp1[GNNEPdp1.Filter]).Value.ToArray<int>()[0];
			int num6 = ((TensorConst)_pdp1[GNNEPdp1.Filter]).Value.ToArray<int>()[1];
			if (_inputShape[2] == num5 && _inputShape[3] == num6 && (_inputShape[2] > 16 || _inputShape[3] > 64 || _inputShape[2] * _inputShape[3] > 256))
			{
				_isGlobalPdp = true;
			}
			else
			{
				_isGlobalPdp = false;
			}
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNETranspose)
		{
			_transpose = curNode.Op;
			Call call21 = _transpose[GNNETranspose.Input] as Call;
			_ifmap = sliceInfo[call21].Ofmap;
			_ifmapOffset = sliceInfo[call21].Nb.OfmapOffset;
			_inputType = call21.CheckedDataType;
			_outputType = _transpose.CheckedDataType;
			_ofmap = _ni.Ofmap;
			_ofmapOffset = _ni.Nb.OfmapOffset;
			_inputShape = call21.CheckedShape.ToValueArray();
			_outputShape = _transpose.CheckedShape.ToValueArray();
			_if1BufIdx = sliceInfo[call21].Nb.OfBufferIndex;
			_if2BufIdx = -1;
			_ofBufIdx = _ni.Nb.OfBufferIndex;
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is Concat)
		{
			_cat = curNode.Op;
			_outputType = _cat.CheckedDataType;
			Call key2 = _cat[Concat.Input][1] as Call;
			_ifmap = sliceInfo[key2].Ofmap;
			_ifmapOffset = sliceInfo[key2].Nb.OfmapOffset;
			_if1BufIdx = sliceInfo[key2].Nb.OfBufferIndex;
			key2 = _cat[Concat.Input][0] as Call;
			_ifmap2 = sliceInfo[key2].Ofmap;
			_ifmap2Offset = sliceInfo[key2].Nb.OfmapOffset;
			_if2BufIdx = sliceInfo[key2].Nb.OfBufferIndex;
			_ofmap = _ni.Ofmap;
			_ofmapOffset = _ni.Nb.OfmapOffset;
			_ofBufIdx = _ni.Nb.OfBufferIndex;
			TileUtilities.Assert(_if1BufIdx != _ofBufIdx, "_if1BufIdx != _ofBufIdx", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3405);
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is Ai2dResize)
		{
			_resize = curNode.Op;
			Call call22 = _resize[Ai2dResize.Input] as Call;
			_ifmap = sliceInfo[call22].Ofmap;
			_ifmapOffset = sliceInfo[call22].Nb.OfmapOffset;
			_inputType = call22.CheckedDataType;
			_outputType = call22.CheckedDataType;
			_ofmap = _ni.Ofmap;
			_ofmapOffset = _ni.Nb.OfmapOffset;
			_inputShape = call22.CheckedShape.ToValueArray();
			_outputShape = _resize.CheckedShape.ToValueArray();
			_if1BufIdx = sliceInfo[call22].Nb.OfBufferIndex;
			_if2BufIdx = -1;
			_ofBufIdx = _ni.Nb.OfBufferIndex;
		}
		_ofmapSt = _ofmap;
		Call call23 = (((object)_conv != null) ? ((Call)_conv[GNNEConv2D.Input]) : null);
		_ifmapLd = (((object)call23 != null) ? sliceInfo[call23].Ofmap : _ifmap);
		if (ReshapeConv(_conv, ref _inputShape, ref _outputShape, ref _convOutputShape, ref _weightsShape))
		{
			int num7 = ((_conv[GNNEConv2D.Weights].CheckedShape.ToValueArray()[1] % 24 == 0) ? 24 : ((_conv[GNNEConv2D.Weights].CheckedShape.ToValueArray()[1] % 20 == 0) ? 20 : 16));
			_ifmap = new SegmentND(new Segment1D(.._ifmap[0].Length, Padding.Zero()), new Segment1D(..num7, Padding.Zero()), new Segment1D(..(_ifmap[1].Length / num7), Padding.Zero()), new Segment1D(..(_ifmap[2].Length * _ifmap[3].Length), Padding.Zero()));
			_ofmap = new SegmentND(new Segment1D(.._ofmap[0].Length, Padding.Zero()), new Segment1D(.._ofmap[1].Length, Padding.Zero()), new Segment1D(..1, Padding.Zero()), new Segment1D(..(_ofmap[2].Length * _ofmap[3].Length), Padding.Zero()));
			_convOutputShape[2] = TileUtilities.GetInputRowSegment(0, _outputShape[2], _convOutputShape[2], _fusedKernelH, _fusedStrideH, _fusedDilationH, in _fusedPaddingH).Length;
			_convOutputShape[3] = TileUtilities.GetInputColumnSegment(0, _outputShape[3], _convOutputShape[3], _fusedKernelW, _fusedStrideW, _fusedDilationW, in _fusedPaddingW).Length;
			_weight = new SegmentND(new Segment1D(.._weightsShape[0], Padding.Zero()), new Segment1D(.._weightsShape[1], Padding.Zero()), new Segment1D(.._weightsShape[2], Padding.Zero()), new Segment1D(.._weightsShape[3], Padding.Zero()));
		}
		if (!weightGroupOnly)
		{
			GetGlbLayouts(fusionInfo, curNode, glb, sliceInfo);
		}
	}

	private bool ReshapeConv(Call conv, ref int[] inputShape, ref int[] outputShape, ref int[] convOutputShape, ref int[] weightsShape)
	{
		if (Conv1X1(conv))
		{
			TileUtilities.Assert(conv[GNNEConv2D.Weights].CheckedShape.ToValueList()[1] % 24 == 0, "conv[GNNEConv2D.Weights].CheckedShape.ToValueList()[1] % 24 == 0", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 3468);
			int num = 24;
			while (conv[GNNEConv2D.Weights].CheckedShape.ToValueList()[1] % num != 0)
			{
				num--;
			}
			inputShape = new int[4]
			{
				conv[GNNEConv2D.Input].CheckedShape.ToValueList()[0],
				num,
				conv[GNNEConv2D.Weights].CheckedShape.ToValueList()[1] / num,
				conv[GNNEConv2D.Input].CheckedShape.ToValueList()[2] * conv[GNNEConv2D.Input].CheckedShape.ToValueList()[3]
			};
			weightsShape = new int[4]
			{
				conv[GNNEConv2D.Weights].CheckedShape.ToValueList()[0],
				num,
				conv[GNNEConv2D.Weights].CheckedShape.ToValueList()[1] / num,
				1
			};
			outputShape = new int[4]
			{
				conv.CheckedShape.ToValueList()[0],
				conv.CheckedShape.ToValueList()[1],
				1,
				conv.CheckedShape.ToValueList()[2] * conv.CheckedShape.ToValueList()[3]
			};
			convOutputShape = new int[4]
			{
				conv.CheckedShape.ToValueList()[0],
				conv.CheckedShape.ToValueList()[1],
				1,
				conv.CheckedShape.ToValueList()[2] * conv.CheckedShape.ToValueList()[3]
			};
			if ((object)_dw != null)
			{
				outputShape = new int[4]
				{
					_dw.CheckedShape.ToValueList()[0],
					_dw.CheckedShape.ToValueList()[1],
					1,
					_dw.CheckedShape.ToValueList()[2] * _dw.CheckedShape.ToValueList()[3]
				};
			}
			if ((object)_pool != null)
			{
				outputShape = new int[4]
				{
					_pool.CheckedShape.ToValueList()[0],
					_pool.CheckedShape.ToValueList()[1],
					1,
					_pool.CheckedShape.ToValueList()[2] * _pool.CheckedShape.ToValueList()[3]
				};
			}
			if ((object)_act1 != null)
			{
				outputShape = new int[4]
				{
					_act1.CheckedShape.ToValueList()[0],
					_act1.CheckedShape.ToValueList()[1],
					1,
					_act1.CheckedShape.ToValueList()[2] * _act1.CheckedShape.ToValueList()[3]
				};
			}
			return true;
		}
		return false;
	}

	private void GetGlbLayouts(FusionInfo fusionInfo, NodeInfo curNode, TiledGlb glb, Dictionary<Call, NodeInfo> sliceInfo)
	{
		if (_003C_003Ec._003C_003E9__116_0 == null)
		{
			_003C_003Ec._003C_003E9__116_0 = delegate(int a, int b, int alignElement)
			{
				int item = a;
				int item2 = b;
				if (a * b % alignElement == 0)
				{
					return new Tuple<int, int>(item, item2);
				}
				int num7 = a + alignElement - 1;
				int num8 = b + alignElement - 1;
				long num9 = 4294836225L;
				for (int i = a; i < num7; i++)
				{
					for (int j = b; j < num8; j++)
					{
						int num10 = i * j;
						if (num10 % alignElement == 0 && num10 < num9)
						{
							num9 = num10;
							item = i;
							item2 = j;
						}
					}
				}
				return new Tuple<int, int>(item, item2);
			};
		}
		MmuItem mmu = glb.GlbMap[ItemName.Ofmap].Mmu;
		NodeInfo nodeInfo = curNode;
		Call op = curNode.Op;
		if ((object)op != null && op.Target is GNNEConv2D && (object)_act1 != null)
		{
			nodeInfo = curNode.Children[0];
		}
		switch (nodeInfo.Nb.AlignType)
		{
		case AlignedType.FAligned:
		{
			int length2 = _ofmapSt[3].Length;
			int alignmentFactor2 = ((TileUtilities.GetBytesPerElement(_outputType) == 1) ? 32 : 16);
			int alignedNum2 = TileUtilities.GetAlignedNum(length2, alignmentFactor2);
			glb.GlbMap[ItemName.Ofmap] = new TensorOnGlb(new int[4]
			{
				_ofmapSt[0].Length,
				_ofmapSt[1].Length,
				_ofmapSt[2].Length,
				alignedNum2
			}, _outputType, 0, mmu);
			break;
		}
		case AlignedType.EAligned:
		{
			int length = _ofmapSt[2].Length;
			int alignmentFactor = ((TileUtilities.GetBytesPerElement(_outputType) == 1) ? 32 : 16);
			int alignedNum = TileUtilities.GetAlignedNum(length, alignmentFactor);
			glb.GlbMap[ItemName.Ofmap] = new TensorOnGlb(new int[4]
			{
				_ofmapSt[0].Length,
				_ofmapSt[1].Length,
				alignedNum,
				_ofmapSt[3].Length
			}, _outputType, 0, mmu);
			break;
		}
		default:
			glb.GlbMap[ItemName.Ofmap] = new TensorOnGlb(new int[4]
			{
				_ofmapSt[0].Length,
				_ofmapSt[1].Length,
				_ofmapSt[2].Length,
				_ofmapSt[3].Length
			}, _outputType, 0, mmu);
			break;
		}
		int index = 0;
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEActivation && curNode.Op[GNNEActivation.InputB] != None.Default && sliceInfo.ContainsKey((Call)curNode.Op[GNNEActivation.InputB]) && !sliceInfo.ContainsKey((Call)curNode.Op[GNNEActivation.InputA]))
		{
			index = 1;
		}
		mmu = glb.GlbMap[ItemName.Ofmap].Mmu;
		op = curNode.Op;
		if ((object)op == null || !(op.Target is GNNELoad))
		{
			if (sliceInfo[(Call)curNode.Op.Arguments[index]].Nb.AlignType == AlignedType.FAligned)
			{
				int length3 = _ifmapLd[2].Length;
				int length4 = _ifmapLd[3].Length;
				int num = length3;
				int alignmentFactor3 = ((TileUtilities.GetBytesPerElement(_inputType) == 1) ? 32 : 16);
				int alignedNum3 = TileUtilities.GetAlignedNum(length4, alignmentFactor3);
				glb.GlbMap[ItemName.Ifmap] = new TensorOnGlb(new int[4]
				{
					_ifmapLd[0].Length,
					_ifmapLd[1].Length,
					num,
					alignedNum3
				}, _inputType, 0, mmu);
			}
			else if (sliceInfo[(Call)curNode.Op.Arguments[index]].Nb.AlignType == AlignedType.EAligned)
			{
				int length5 = _ifmapLd[2].Length;
				int length6 = _ifmapLd[3].Length;
				int alignmentFactor4 = ((TileUtilities.GetBytesPerElement(_inputType) == 1) ? 32 : 16);
				int alignedNum4 = TileUtilities.GetAlignedNum(length5, alignmentFactor4);
				int num2 = length6;
				glb.GlbMap[ItemName.Ifmap] = new TensorOnGlb(new int[4]
				{
					_ifmapLd[0].Length,
					_ifmapLd[1].Length,
					alignedNum4,
					num2
				}, _inputType, 0, mmu);
			}
			else
			{
				DataType inputType = _inputType;
				glb.GlbMap[ItemName.Ifmap] = new TensorOnGlb(new int[4]
				{
					_ifmapLd[0].Length,
					_ifmapLd[1].Length,
					_ifmapLd[2].Length,
					_ifmapLd[3].Length
				}, inputType, 0, mmu);
			}
		}
		MmuItem mmu2 = (fusionInfo.Mmu.ContainsKey(ItemName.Ifmap2) ? fusionInfo.Mmu[ItemName.Ifmap2] : glb.GlbMap[ItemName.Ifmap2].Mmu);
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEActivation && curNode.Op[GNNEActivation.InputB] != None.Default)
		{
			index = 1;
			if (sliceInfo.ContainsKey((Call)curNode.Op[GNNEActivation.InputB]) && !sliceInfo.ContainsKey((Call)curNode.Op[GNNEActivation.InputA]))
			{
				index = 0;
			}
			int length7 = _ifmap2[2].Length;
			int length8 = _ifmap2[3].Length;
			int alignmentFactor5 = 32 / TileUtilities.GetBytesPerElement(_if2Type);
			int num3;
			int num4;
			if (!sliceInfo.ContainsKey((Call)curNode.Op.Arguments[index]))
			{
				num3 = length7;
				num4 = length8;
			}
			else
			{
				mmu2 = glb.GlbMap[ItemName.Ofmap].Mmu;
				switch (sliceInfo[(Call)curNode.Op.Arguments[index]].Nb.AlignType)
				{
				case AlignedType.FAligned:
					num3 = length7;
					num4 = TileUtilities.GetAlignedNum(length8, alignmentFactor5);
					break;
				case AlignedType.EAligned:
					num3 = TileUtilities.GetAlignedNum(length7, alignmentFactor5);
					num4 = length8;
					break;
				default:
					num3 = length7;
					num4 = length8;
					break;
				}
			}
			glb.GlbMap[ItemName.Ifmap2] = new TensorOnGlb(new int[4]
			{
				_ifmap2[0].Length,
				_ifmap2[1].Length,
				num3,
				num4
			}, _if2Type, 0, mmu2);
		}
		else
		{
			glb.GlbMap[ItemName.Ifmap2] = new TensorOnGlb(new int[4]
			{
				_ifmap2[0].Length,
				_ifmap2[1].Length,
				_ifmap2[2].Length,
				_ifmap2[3].Length
			}, _if2Type, 0, mmu2);
		}
		op = curNode.Op;
		if ((object)op != null && op.Target is GNNEConv2D && (object)_act1 != null && _act1[GNNEActivation.InputB] != None.Default)
		{
			index = (((GNNEActivation)_act1.Target).InputFromL1[0] ? 1 : 0);
			int length9 = _ifmap2[2].Length;
			int length10 = _ifmap2[3].Length;
			int alignmentFactor6 = 32 / TileUtilities.GetBytesPerElement(_if2Type);
			int num5;
			int num6;
			if (!sliceInfo.ContainsKey((Call)_act1.Arguments[index]))
			{
				num5 = TileUtilities.GetAlignedNum(length9, alignmentFactor6);
				num6 = length10;
			}
			else
			{
				mmu2 = glb.GlbMap[ItemName.Ofmap].Mmu;
				switch (sliceInfo[(Call)_act1.Arguments[index]].Nb.AlignType)
				{
				case AlignedType.FAligned:
					num5 = length9;
					num6 = TileUtilities.GetAlignedNum(length10, alignmentFactor6);
					break;
				case AlignedType.EAligned:
					num5 = TileUtilities.GetAlignedNum(length9, alignmentFactor6);
					num6 = length10;
					break;
				default:
					num5 = length9;
					num6 = length10;
					break;
				}
			}
			glb.GlbMap[ItemName.Ifmap2] = new TensorOnGlb(new int[4]
			{
				_ifmap2[0].Length,
				_ifmap2[1].Length,
				num5,
				num6
			}, _if2Type, 0, mmu2);
		}
		if (_h2C)
		{
			if ((object)_lif != null)
			{
				int[] array = ((TensorConst)curNode.Children[0].Op[GNNEConv2D.Padding]).Value.ToArray<int>();
				MmuItem mmu3 = glb.GlbMap[ItemName.Ofmap].Mmu;
				glb.GlbMap[ItemName.Ofmap] = new TensorOnGlb(new int[4]
				{
					_ofmapSt[0].Length,
					_ofmapSt[1].Length,
					_ofmapSt[2].Length + array[0] + array[1],
					_ofmapSt[3].Length
				}, _outputType, 0, mmu3);
				_ofmapSt[2].Padding = _ofmap[2].Padding;
				_ofmapSt[3].Padding = _ofmap[3].Padding;
				glb.GlbMap[ItemName.Ifmap] = glb.GlbMap[ItemName.Ofmap];
			}
			else
			{
				int[] array2 = ((TensorConst)_conv[GNNEConv2D.Padding]).Value.ToArray<int>();
				MmuItem mmu4 = glb.GlbMap[ItemName.Ifmap].Mmu;
				glb.GlbMap[ItemName.Ifmap] = new TensorOnGlb(new int[4]
				{
					_ifmapLd[0].Length,
					_ifmapLd[1].Length,
					_ifmapLd[2].Length + array2[0] + array2[1],
					_ifmapLd[3].Length
				}, _inputType, 0, mmu4);
				_ifmapLd[2].Padding = _ifmap[2].Padding;
				_ifmapLd[3].Padding = _ifmap[3].Padding;
			}
		}
	}

	private void ItemRecStatusUpdate()
	{
		if ((object)_conv != null)
		{
			if (!_nodesWeightRec.ContainsKey(_conv))
			{
				_nodesWeightRec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (!_nodesOfmapRec.ContainsKey(_conv))
			{
				_nodesOfmapRec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (!_nodesG2LIfRec.ContainsKey(_conv))
			{
				_nodesG2LIfRec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (!_nodesG2RWRec.ContainsKey(_conv))
			{
				_nodesG2RWRec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (!_nodesL2GOfRec.ContainsKey(_conv))
			{
				_nodesL2GOfRec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (!_nodesL2RIf2Rec.ContainsKey(_conv))
			{
				_nodesL2RIf2Rec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (!_nodesG2RWSliceRec.ContainsKey(_conv))
			{
				_nodesG2RWSliceRec.Add(_conv, new List<List<Tuple<SegmentND, TensorStat>>>
				{
					new List<Tuple<SegmentND, TensorStat>>(),
					new List<Tuple<SegmentND, TensorStat>>()
				});
			}
			if (_weightBufIdx != -1)
			{
				_nodesQuenesAsW[_weightBufIdx].Add(new Tuple<Call, int>(_conv, 0));
			}
		}
		if ((object)_resize != null)
		{
			if (!_nodesOfmapRec.ContainsKey(_resize))
			{
				_nodesOfmapRec.Add(_resize, new List<List<Tuple<SegmentND, TensorStat>>>());
			}
			if (_nodesAi2dIfRec.ContainsKey(_resize))
			{
				_nodesAi2dIfRec.Add(_resize, new List<List<Tuple<SegmentND, TensorStat>>>());
			}
			if (_nodesAi2dOfRec.ContainsKey(_resize))
			{
				_nodesAi2dOfRec.Add(_resize, new List<List<Tuple<SegmentND, TensorStat>>>());
			}
		}
	}

	private void UpdateCcrRecStat()
	{
		foreach (List<Tuple<SegmentND, TensorStat>> item in _nodesG2LIfRec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter))
		{
			for (int i = 0; i < item.Count; i++)
			{
				if (i == 0)
				{
					item[i].Item2.IsFirstSlice = true;
				}
				if (i == item.Count - 1)
				{
					item[i].Item2.IsLastSlice = true;
				}
				if (i < item.Count - 1 && item[i].Item1 != item[i + 1].Item1)
				{
					item[i].Item2.IsLastSlice = true;
					item[i + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item2 in _nodesG2RWRec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter))
		{
			for (int j = 0; j < item2.Count; j++)
			{
				if (j == 0)
				{
					item2[j].Item2.IsFirstSlice = true;
				}
				if (j == item2.Count - 1)
				{
					item2[j].Item2.IsLastSlice = true;
				}
				if (j < item2.Count - 1 && item2[j].Item1 != item2[j + 1].Item1)
				{
					item2[j].Item2.IsLastSlice = true;
					item2[j + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item3 in _nodesL2GOfRec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter))
		{
			for (int k = 0; k < item3.Count; k++)
			{
				if (k == 0)
				{
					item3[k].Item2.IsFirstSlice = true;
				}
				if (k == item3.Count - 1)
				{
					item3[k].Item2.IsLastSlice = true;
				}
				if (k < item3.Count - 1 && item3[k].Item1 != item3[k + 1].Item1)
				{
					item3[k].Item2.IsLastSlice = true;
					item3[k + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item4 in _nodesL2RIf2Rec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter))
		{
			for (int l = 0; l < item4.Count; l++)
			{
				if (l == 0)
				{
					item4[l].Item2.IsFirstSlice = true;
				}
				if (l == item4.Count - 1)
				{
					item4[l].Item2.IsLastSlice = true;
				}
				if (l < item4.Count - 1 && item4[l].Item1 != item4[l + 1].Item1)
				{
					item4[l].Item2.IsLastSlice = true;
					item4[l + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item5 in _nodesWeightRec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter.Where((List<Tuple<SegmentND, TensorStat>> t) => t.Count > 0)))
		{
			item5[0].Item2.IsFirstSlice = true;
			item5[item5.Count - 1].Item2.IsLastSlice = true;
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item6 in from iter in _nodesOfmapRec.Values
			from t in iter
			where t.Count > 0
			select t)
		{
			item6[0].Item2.IsFirstSlice = true;
			item6[item6.Count - 1].Item2.IsLastSlice = true;
		}
		foreach (KeyValuePair<Call, List<List<Tuple<SegmentND, TensorStat>>>> item7 in _nodesG2RWSliceRec)
		{
			List<List<Tuple<SegmentND, TensorStat>>> list = _nodesG2RWRec[item7.Key];
			for (int m = 0; m < item7.Value.Count; m++)
			{
				List<SegmentND> list2 = new List<SegmentND>();
				List<SegmentND> list3 = new List<SegmentND>();
				List<Tuple<SegmentND, TensorStat>> list4 = new List<Tuple<SegmentND, TensorStat>>();
				for (int n = 0; n < list[m].Count; n++)
				{
					if (list[m][n].Item2.IsFirstSlice)
					{
						list2.Clear();
						list3.Clear();
						list4.Clear();
					}
					if (!list2.Contains(item7.Value[m][n].Item1))
					{
						list2.Add(item7.Value[m][n].Item1);
					}
					list3.Add(item7.Value[m][n].Item1);
					list4.Add(item7.Value[m][n]);
					if (!list[m][n].Item2.IsLastSlice)
					{
						continue;
					}
					foreach (SegmentND item8 in list2)
					{
						list4[list3.IndexOf(item8)].Item2.IsFirstSlice = true;
						list4[list4.Count - 1 - list3.IndexOf(item8)].Item2.IsLastSlice = true;
					}
					for (int num = 0; num < list3.Count; num++)
					{
						list4[num].Item2.SliceIdx = list2.IndexOf(list3[num]);
						item7.Value[m][n - list3.Count + 1 + num] = list4[num];
					}
				}
				Call key = item7.Key;
				if ((object)key == null || !(key.Target is GNNEConv2D) || key[GNNEConv2D.Weights].CheckedDataType != DataTypes.Int16)
				{
					continue;
				}
				for (int num2 = 1; num2 < item7.Value[m].Count; num2++)
				{
					if (item7.Value[m][item7.Value[m].Count - num2 - 1].Item2.IsFirstSlice)
					{
						item7.Value[m][item7.Value[m].Count - num2].Item2.IsFirstSlice = true;
					}
					if (item7.Value[m][num2].Item2.IsLastSlice)
					{
						item7.Value[m][num2 - 1].Item2.IsLastSlice = true;
					}
				}
				for (int num3 = 0; num3 < item7.Value[m].Count; num3++)
				{
					item7.Value[m][num3].Item2.SliceIdx = item7.Value[m][num3].Item2.SliceIdx * 2 + (num3 & 1);
				}
			}
		}
		foreach (List<Tuple<Call, int>> item9 in _nodesQuenesAsW)
		{
			for (int num4 = 0; num4 < item9.Count; num4++)
			{
				item9[num4] = new Tuple<Call, int>(item9[num4].Item1, num4);
			}
		}
	}

	private void UpdateAi2dCcrRecStat()
	{
		foreach (List<Tuple<SegmentND, TensorStat>> item in _nodesAi2dIfRec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter))
		{
			for (int i = 0; i < item.Count; i++)
			{
				if (i == 0)
				{
					item[i].Item2.IsFirstSlice = true;
				}
				if (i == item.Count - 1)
				{
					item[i].Item2.IsLastSlice = true;
				}
				if (i < item.Count - 1 && !(item[i].Item1 == item[i + 1].Item1))
				{
					item[i].Item2.IsLastSlice = true;
					item[i + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item2 in _nodesAi2dOfRec.Values.SelectMany((List<List<Tuple<SegmentND, TensorStat>>> iter) => iter))
		{
			for (int j = 0; j < item2.Count; j++)
			{
				if (j == 0)
				{
					item2[j].Item2.IsFirstSlice = true;
				}
				if (j == item2.Count - 1)
				{
					item2[j].Item2.IsLastSlice = true;
				}
				if (j < item2.Count - 1 && !(item2[j].Item1 == item2[j + 1].Item1))
				{
					item2[j].Item2.IsLastSlice = true;
					item2[j + 1].Item2.IsFirstSlice = true;
				}
			}
		}
		foreach (List<Tuple<SegmentND, TensorStat>> item3 in from iter in _nodesOfmapRec.Values
			from t in iter
			where t.Count > 0
			select t)
		{
			item3[0].Item2.IsFirstSlice = true;
			item3[item3.Count - 1].Item2.IsLastSlice = true;
		}
	}

	private Tuple<List<CcrSet>, List<CcrClr>> GetCcrSetAndClrVec(NodeInfo currNode)
	{
		List<CcrSet> list = new List<CcrSet>();
		List<CcrClr> list2 = new List<CcrClr>();
		if (!GNNEEnv.UseCcr)
		{
			return new Tuple<List<CcrSet>, List<CcrClr>>(list, list2);
		}
		Call op = currNode.Op;
		TileUtilities.Assert((object)op == null || !(op.Target is GNNEConv2D), "currNode.Op is not { Target: GNNEConv2D }", "/home/gitlab-runner/builds/zaC7hZ1H/0/maix2-ai-sw/k510-gnne-compiler/modules/Nncase.Modules.K230/Transform/Rules/Tile/TileLayerGroup.cs", 4064);
		op = currNode.Op;
		if ((object)op != null && op.Target is GNNEStore)
		{
			if (_nodesQueNeedClearFake.Count > 0)
			{
				list.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.OfmapFake, _ofBufIdx)), 1));
			}
		}
		else
		{
			int ccrSetAccordingPostNodes = GetCcrSetAccordingPostNodes(currNode);
			if (ccrSetAccordingPostNodes != 0)
			{
				list.Add(new CcrSet(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _ofBufIdx)), ccrSetAccordingPostNodes));
			}
		}
		op = currNode.Op;
		if ((object)op == null || !(op.Target is GNNELoad))
		{
			op = currNode.Op;
			if ((object)op == null || !(op.Target is GNNEStore))
			{
				if (_if1BufIdx != -1)
				{
					list2.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _if1BufIdx))));
				}
				if (_if2BufIdx != -1)
				{
					op = currNode.Op;
					if ((object)op == null || !(op.Target is Concat))
					{
						list2.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _if2BufIdx))));
					}
				}
				goto IL_01c0;
			}
		}
		op = currNode.Op;
		if ((object)op != null && op.Target is GNNEStore)
		{
			list2.Add(new CcrClr(_ccrHandler.GetCcrItem(_ccrHandler.GetName(ItemName.Ofmap, _if1BufIdx))));
		}
		goto IL_01c0;
		IL_01c0:
		return new Tuple<List<CcrSet>, List<CcrClr>>(list, list2);
	}
}
