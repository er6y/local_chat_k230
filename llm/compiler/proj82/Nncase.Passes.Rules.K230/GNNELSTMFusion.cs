using Nncase.IR;
using Nncase.IR.K230;
using Nncase.Passes.Rules.Neutral;
using Nncase.PatternMatch;

namespace Nncase.Passes.Rules.K230;

internal sealed class GNNELSTMFusion : ComplexFusion<GNNELSTM, GNNELoad, GNNEStore>
{
	public override (ParameterInfo, CallPattern)[] InputPatterns { get; } = ComplexFusion<GNNELSTM, GNNELoad, GNNEStore>.GenerateInputPatterns(GNNELSTM.Input, GNNELSTM.InitialH, GNNELSTM.InitialC);


	public override string Name { get; } = "TileLSTMCase";


	public override string ModuleKind { get; } = "k230";

}
