using Nncase.IR;
using Nncase.IR.K230;
using Nncase.IR.K230.F;
using Nncase.PatternMatch;
using Nncase.PatternMatch.F;

namespace Nncase.Passes.Rules.K230;

[RuleGenerator]
public sealed class FoldPadFakePdp : IRewriteRule
{
	public IPattern Pattern { get; } = Nncase.PatternMatch.F.K230.IsGNNEStore("gnneStore", "call", (GNNEStore _) => true, Nncase.PatternMatch.F.K230.IsGNNEPad("gnnePad", (GNNEPad _) => true, Nncase.PatternMatch.F.K230.IsGNNELoad("gnneLoad", (GNNELoad _) => true, Nncase.PatternMatch.F.K230.IsFakePdp("fakePdp", (FakePdp _) => true, Nncase.PatternMatch.Utility.IsWildcard("input"), Nncase.PatternMatch.Utility.IsTensorConst(), Nncase.PatternMatch.Utility.IsTensorConst("filter"), Nncase.PatternMatch.Utility.IsTensorConst("stride"), Nncase.PatternMatch.Utility.IsTensorConst("padding1"), Nncase.PatternMatch.Utility.IsTensorConst("countIncludePad"))), Nncase.PatternMatch.Utility.IsTensorConst("padding2"), Nncase.PatternMatch.Utility.IsTensorConst("padValue")), Nncase.PatternMatch.Utility.IsTensorConst());


	public Expr? GetReplace(GNNEStore gnneStore, Call call, FakePdp fakePdp, Expr input, Expr filter, Expr stride, Expr countIncludePad, TensorConst padding1, TensorConst padding2, Tensor<float> padValue)
	{
		if (padding1.Value.ToArray<int>()[0] == 0 && padding1.Value.ToArray<int>()[1] == 0 && padding1.Value.ToArray<int>()[2] == 0 && padding1.Value.ToArray<int>()[3] == 0 && padding2.Value.ToArray<int>()[0] == 0 && padding2.Value.ToArray<int>()[1] == 0 && padding2.Value.ToArray<int>()[2] == 0 && padding2.Value.ToArray<int>()[3] == 0)
		{
			return Nncase.IR.K230.F.Tensors.FakePdp(fakePdp.ReduceOp, input, Const.FromTensor(padValue), filter, stride, padding2, countIncludePad);
		}
		return null;
	}

	public Expr? GetReplace(IMatchResult __result, RunPassContext __context)
	{
		GNNEStore gnneStore = (GNNEStore)__result["gnneStore"];
		Call call = (Call)__result["call"];
		FakePdp fakePdp = (FakePdp)__result["fakePdp"];
		Expr input = (Expr)__result["input"];
		Expr filter = (Expr)__result["filter"];
		Expr stride = (Expr)__result["stride"];
		Expr countIncludePad = (Expr)__result["countIncludePad"];
		TensorConst padding = (TensorConst)__result["padding1"];
		TensorConst padding2 = (TensorConst)__result["padding2"];
		Tensor<float> padValue = ((TensorConst)__result["padValue"]).Value.Cast<float>();
		return GetReplace(gnneStore, call, fakePdp, input, filter, stride, countIncludePad, padding, padding2, padValue);
	}
}
