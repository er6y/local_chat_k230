using System;
using Nncase.IR;
using Nncase.Mutators.K230;

namespace Nncase.Transform;

public static class K230Mutator
{
	public static Func<ExprRewriter> FoldBufferSlot()
	{
		return () => new FoldBufferSlot();
	}
}
