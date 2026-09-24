using System;
using System.Collections.Generic;
using System.Reactive;
using Nncase.IR;
using Nncase.IR.Buffers;
using Nncase.TIR;

namespace Nncase.Mutators.K230;

internal sealed class FoldBufferSlot : ExprRewriter
{
	private Dictionary<Nncase.TIR.Buffer, int> _bufferIndexMap = new Dictionary<Nncase.TIR.Buffer, int>(ReferenceEqualityComparer.Instance);

	protected override Expr VisitPrimFunction(PrimFunction expr, Unit context)
	{
		if (expr.SchedResult.IsScheduled)
		{
			int num = 0;
			int num2 = 0;
			ReadOnlySpan<Nncase.TIR.Buffer> parameters = expr.Parameters;
			for (int i = 0; i < parameters.Length; i++)
			{
				Nncase.TIR.Buffer buffer = parameters[i];
				switch (buffer.MemSpan.Location)
				{
				case MemoryLocation.Input:
					_bufferIndexMap.Add(buffer, num++);
					break;
				case MemoryLocation.Output:
					_bufferIndexMap.Add(buffer, num + num2++);
					break;
				default:
					throw new NotSupportedException();
				}
			}
			return base.VisitPrimFunction(expr, context);
		}
		return expr;
	}

	protected override Expr RewriteLeafCall(Call expr)
	{
		if (expr.Target is BufferIndexOf)
		{
			Nncase.TIR.Buffer buffer = (Nncase.TIR.Buffer)expr.Arguments[0];
			MemoryLocation location = buffer.MemSpan.Location;
			return location switch
			{
				MemoryLocation.Input => _bufferIndexMap[buffer], 
				MemoryLocation.Output => _bufferIndexMap[buffer], 
				MemoryLocation.Rdata => _bufferIndexMap.Count, 
				MemoryLocation.Data => _bufferIndexMap.Count + 1, 
				_ => throw new ArgumentOutOfRangeException($"You Can't Assgin The BaseMent For {location}!"), 
			};
		}
		if (expr.Target is DDrOf && expr.Arguments[0] is MemSpan memSpan)
		{
			return memSpan.Start;
		}
		return expr;
	}
}
