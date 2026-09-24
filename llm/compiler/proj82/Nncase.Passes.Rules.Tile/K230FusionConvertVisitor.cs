using System;
using System.Linq;
using Nncase.IR;
using Nncase.IR.K230;
using Nncase.Passes.Rules.K230;
using Nncase.PatternMatch;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

public sealed class K230FusionConvertVisitor : ExprRewriter
{
	private readonly RunPassContext _options;

	public K230FusionConvertVisitor(RunPassContext options)
	{
		_options = options;
	}

	protected override Expr RewriteLeafFusion(Fusion expr)
	{
		return Process(expr);
	}

	private PrimFunction Process(Fusion fusion)
	{
		IRewriteRule rewriteRule;
		if (fusion.Name.StartsWith("TileTransposeCase"))
		{
			rewriteRule = new TileTranspose();
		}
		else if (fusion.Name.StartsWith("TilePdp1Case"))
		{
			rewriteRule = new TilePdp1();
		}
		else if (fusion.Name.StartsWith("TileAct1Case"))
		{
			rewriteRule = new TileAct1();
		}
		else if (fusion.Name.StartsWith("TileConv2dCase"))
		{
			Call obj = ((Call)fusion.Body)[GNNEStore.Input] as Call;
			int[] array = obj[GNNEConv2D.Input].CheckedShape.ToValueArray();
			int[] array2 = obj.CheckedShape.ToValueArray();
			int[] array3 = obj[GNNEConv2D.Weights].CheckedShape.ToValueArray();
			int[] source = ((TensorConst)obj[GNNEConv2D.Padding]).Value.ToArray<int>();
			int num = ((TensorConst)obj[GNNEConv2D.Stride]).Value.ToArray<int>()[0];
			int num2 = ((TensorConst)obj[GNNEConv2D.Stride]).Value.ToArray<int>()[1];
			int num3 = ((TensorConst)obj[GNNEConv2D.Dilation]).Value.ToArray<int>()[0];
			int num4 = ((TensorConst)obj[GNNEConv2D.Dilation]).Value.ToArray<int>()[1];
			int num5 = ((TensorConst)obj[GNNEConv2D.Groups]).Value.ToScalar<int>();
			rewriteRule = ((array[0] <= 1 || array2[2] != 1 || array2[3] != 1 || array[2] != 1 || array[3] != 1 || array3[2] != 1 || array3[3] != 1 || num != 1 || num2 != 1 || num3 != 1 || num4 != 1 || source.Sum() != 0 || num5 != 1) ? ((RewriteRule<Pattern>)new TileConv2D()) : ((RewriteRule<Pattern>)new TileTransposeMatmul()));
		}
		else if (fusion.Name.StartsWith("TileConv2dTransposeCase"))
		{
			rewriteRule = new TileConv2dTranspose();
		}
		else if (fusion.Name.StartsWith("TilePadCase"))
		{
			rewriteRule = new TilePad();
		}
		else if (fusion.Name.StartsWith("TileResizeCase"))
		{
			rewriteRule = new TileResize();
		}
		else if (fusion.Name.StartsWith("TileMatMulCase"))
		{
			rewriteRule = new TileMatMul();
		}
		else if (fusion.Name.StartsWith("TileLSTMCase"))
		{
			rewriteRule = new TileLSTM();
		}
		else
		{
			if (!fusion.Name.StartsWith("TileLoadStoreCase"))
			{
				throw new NotImplementedException();
			}
			rewriteRule = new TileLoadStore();
		}
		if (!CompilerServices.TryMatchRoot(fusion, rewriteRule.Pattern, out IMatchResult result))
		{
			throw new NotImplementedException();
		}
		Expr replace = rewriteRule.GetReplace(result, _options);
		if ((object)replace == null)
		{
			return (PrimFunction)new TileConv2D().GetReplace(result, _options);
		}
		return (PrimFunction)replace;
	}
}
