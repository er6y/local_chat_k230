using Nncase.IR;
using Nncase.IR.K230;
using Nncase.Passes.Rules.Neutral;
using Nncase.PatternMatch;

namespace Nncase.Passes.Rules.K230;

public static class FusionPattern
{
	public static Pattern IsGNNEFusion<T>(string midName = "call", string beginName = "ld", string endName = "st", string fusionName = "fusion") where T : Op
	{
		return Nncase.PatternMatch.Utility.IsFusion<T, GNNELoad, GNNEStore>(fusionName, "k230", endName, midName, beginName, "input");
	}

	public static Pattern IsL1Fusion<T>() where T : Op
	{
		return Nncase.PatternMatch.Utility.IsPairLayerFusion<GNNEConv2D, T, GNNELoad, GNNEStore>("k230", "call");
	}

	public static Pattern IsGNNEFusion(Pattern pattern)
	{
		return Nncase.PatternMatch.Utility.IsFusion("k230", pattern);
	}

	public static Pattern IsGNNEConvFusion()
	{
		return IsGNNEFusion<GNNEConv2D>();
	}

	public static Pattern IsGNNEPdp0DwFusion()
	{
		return IsGNNEFusion<GNNEPdp0DW>();
	}

	public static Pattern IsGNNEPdp0ReduceFusion()
	{
		return IsGNNEFusion<GNNEPdp0Reduce>();
	}

	public static Pattern IsGNNETransposeFusion()
	{
		return IsGNNEFusion<GNNETranspose>();
	}

	public static Pattern IsGNNEPDP1Fusion()
	{
		return IsGNNEFusion<GNNEPdp1>();
	}

	public static Pattern IsGNNEActivationFusion()
	{
		return IsGNNEFusion<GNNEActivation>();
	}

	public static Pattern IsL1Pdp0DwFusion()
	{
		return IsL1Fusion<GNNEPdp0DW>();
	}

	public static Pattern IsL1Pdp0ReduceFusion()
	{
		return IsL1Fusion<GNNEPdp0Reduce>();
	}

	public static Pattern IsL1Act1Fusion()
	{
		return IsL1Fusion<GNNEActivation>();
	}

	public static Pattern IsGNNEConv2DTransposeFusion()
	{
		return IsGNNEFusion<GNNEConv2DTranspose>();
	}

	public static Pattern IsGNNEPadFusion()
	{
		return IsGNNEFusion<GNNEPad>();
	}

	public static Pattern IsGNNEMatMulFusion()
	{
		return IsGNNEFusion<GNNEMatMul>();
	}

	public static Pattern IsGNNELSTMFusion()
	{
		return IsGNNEFusion<GNNELSTM>();
	}

	public static Pattern IsGNNELoadStoreFusion()
	{
		return IsGNNEFusion(new DataTransferFusion<GNNELoad, GNNEStore>().Pattern);
	}

	public static Pattern IsAi2DResizeFusion()
	{
		return IsGNNEFusion<Ai2dResize>();
	}
}
