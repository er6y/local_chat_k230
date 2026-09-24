using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class CCR_DECL : InstructionOp, ISerializeInst, IEquatable<CCR_DECL?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(CCR_DECL), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rnum { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.ccr_decl, 7);
		bitWriter.Write(rnum, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public CCR_DECL(GP_REGISTER rnum)
	{
		this.rnum = rnum;
	}

	public CCR_DECL With(GP_REGISTER? rnum = null)
	{
		return new CCR_DECL(rnum ?? this.rnum);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as CCR_DECL);
	}

	public bool Equals(CCR_DECL? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return rnum.Equals(other.rnum);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rnum));
	}
}
