using System;
using System.IO;
using Nncase.IR;
using Nncase.TIR;
using Nncase.TIR.Instructions;

namespace Nncase.CodeGen.K230;

// Direct-order serializer: walks the Sequential's expression list and
// serializes each instruction Call exactly once, in list order.
// The previous ExprVisitor-based version memoizes expressions, which silently
// drops or relocates repeated instructions (JAL subroutine bodies built by
// GnneLoopify re-use target/operand objects and are structurally equal to
// their originals) - the emitted .text then no longer matches the byte
// accounting used to compute jump offsets. Instruction operands here are
// TensorConst immediates only, so plain recursion is exactly faithful.
internal sealed class InstSerializeVisitor
{
	private readonly BinaryWriter Writer;

	public InstSerializeVisitor(BinaryWriter binaryWriter)
	{
		Writer = binaryWriter;
	}

	public bool Visit(Expr expr)
	{
		if (expr is Sequential seq)
		{
			foreach (var e in seq.Fields)
			{
				Visit(e);
			}
			return true;
		}
		if (expr is Call call)
		{
			if (call.Target is ISerializeInst serializeInst)
			{
				serializeInst.Serialize(Writer, call);
				return true;
			}
			throw new InvalidOperationException("The " + call.Target.GetType().Name + " is invalid in here!");
		}
		return true;
	}
}
