using System;
using System.Collections.Generic;
using Nncase.IR;
using Nncase.TIR;

namespace Nncase.Passes.Rules.Tile;

internal sealed class CheckedConvertMutator : ExprRewriter
{
	private readonly Dictionary<Fusion, BaseFunction> _fusionConertedCache;

	private readonly Dictionary<string, RoofLineInfo> _fusionMacsMap;

	private readonly IDictionary<Fusion, IFusionChecker> _fusionCheckerCache;

	private readonly RunPassContext _passOptions;

	public CheckedConvertMutator(Dictionary<Fusion, BaseFunction> fusionConvertedCache, Dictionary<string, RoofLineInfo> fusionMacsMap, IDictionary<Fusion, IFusionChecker> fusionchecker_cache, RunPassContext passOptions)
	{
		_fusionConertedCache = fusionConvertedCache;
		_fusionMacsMap = fusionMacsMap;
		_fusionCheckerCache = fusionchecker_cache;
		_passOptions = passOptions;
	}

	protected override Expr RewriteLeafFusion(Fusion expr)
	{
		if ((object)expr != null && expr.ModuleKind == "k230")
		{
			if (!_fusionConertedCache.TryGetValue(expr, out BaseFunction _))
			{
				PrimFunction primFunction;
				if (_fusionCheckerCache.TryGetValue(expr, out IFusionChecker value2))
				{
					if (value2 is L1FusionChecker)
					{
						MultiFusionChecker multiFusionChecker = new MultiFusionChecker();
						if (multiFusionChecker.Check(expr, _passOptions))
						{
							_fusionCheckerCache[expr] = multiFusionChecker;
							value2 = multiFusionChecker;
						}
					}
					primFunction = value2.Convert();
				}
				else
				{
					primFunction = (PrimFunction)new K230FusionConvertVisitor(_passOptions).Rewrite(expr.Clone());
				}
				BaseFunction baseFunction = primFunction;
				_fusionConertedCache.Add(expr, baseFunction);
				new DDrMacCalcVisitor(_fusionMacsMap, baseFunction.Name).Visit(expr);
			}
		}
		return expr;
	}

	protected override Expr RewriteLeafCall(Call expr)
	{
		if (expr.Target is Fusion { ModuleKind: "k230" } fusion)
		{
			BaseFunction baseFunction = _fusionConertedCache[fusion];
			PrimFunctionWrapper primFunctionWrapper;
			if (baseFunction is PrimFunction primFunction)
			{
				bool flag = true;
				int num = 0;
				ReadOnlySpan<Nncase.TIR.Buffer> parameters = primFunction.Parameters;
				for (int i = 0; i < parameters.Length; i++)
				{
					if (parameters[i].MemSpan.Location == MemoryLocation.Input)
					{
						if (!flag)
						{
							throw new InvalidOperationException("The output buffer must behind the input buffer");
						}
						num++;
					}
					else
					{
						flag = false;
					}
				}
				primFunctionWrapper = new PrimFunctionWrapper(primFunction, num);
				_fusionConertedCache[fusion] = primFunctionWrapper;
			}
			else
			{
				primFunctionWrapper = (PrimFunctionWrapper)baseFunction;
			}
			return expr.With(primFunctionWrapper);
		}
		return expr;
	}
}
