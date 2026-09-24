namespace Nncase.Passes.Rules.K230;

public static class GNNEEnv
{
	public static bool UseCcr { get; } = true;


	public static int GprNum { get; } = 31;


	public static int SsrNum { get; } = 8;


	public static int PuHeight { get; } = 24;


	public static int PuWidth { get; } = 32;


	public static int WAlignNum { get; } = PuHeight;


	public static int ActNumPerChan { get; } = 7;


	public static int TcuActNum { get; } = 1;


	public static int MfuPuHeight { get; } = 16;


	public static int PuKernelSpad { get; } = 8;


	public static int GlbBankWidth { get; } = 32;


	public static int GlbBankHeight { get; } = 4096;


	public static int GlbWidth { get; } = 1;


	public static int GlbHeight { get; } = 32;


	public static int GlbBankSize { get; } = GlbBankWidth * GlbBankHeight;


	public static int GlbDepth { get; } = GlbHeight * GlbBankHeight;


	public static int GlbSize { get; } = GlbBankSize * GlbWidth * GlbHeight;


	public static bool BatchInference { get; }

	public static int NPingPongSplit { get; } = 2;


	public static int IfmapBankWidth { get; } = GlbWidth;


	public static int WBankWidth { get; } = GlbWidth;


	public static int OfmapBankWidth { get; } = GlbWidth;


	public static int PsumBankWidth { get; } = GlbWidth;


	public static int ActBankWidth { get; } = GlbWidth;


	public static int IfQargBankWidth { get; } = GlbWidth;


	public static int ResInQargBankWidth { get; } = GlbWidth;


	public static int WQargBankWidth { get; } = GlbWidth;


	public static int StQargBankWidth { get; } = GlbWidth;


	public static int IfL1SizePerChan { get; } = 1024;


	public static int IfL1Size { get; } = PuHeight * IfL1SizePerChan;


	public static int PsumL1ElePerChan { get; } = 1024;


	public static int PsumL1Size { get; } = 4 * PuWidth * PsumL1ElePerChan;


	public static int MultiLayerTilingH { get; }

	public static int MultiLayerTilingW { get; }

	public static int Ai2dSramLen { get; } = 256;


	public static int Ai2dSramSize { get; } = Ai2dSramLen * Ai2dSramLen;

}
