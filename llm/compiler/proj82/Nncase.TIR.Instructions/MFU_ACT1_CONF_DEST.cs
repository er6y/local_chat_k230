using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF_DEST : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF_DEST?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF_DEST), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rlen { get; }

	public SHAPE_REGISTER rshape { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rlen, 5);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF_DEST(MFU_CONF_FUNCTION funct5, GP_REGISTER rlen, SHAPE_REGISTER rshape)
	{
		this.funct5 = funct5;
		this.rlen = rlen;
		this.rshape = rshape;
	}

	public MFU_ACT1_CONF_DEST With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rlen = null, SHAPE_REGISTER? rshape = null)
	{
		return new MFU_ACT1_CONF_DEST(funct5 ?? this.funct5, rlen ?? this.rlen, rshape ?? this.rshape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF_DEST);
	}

	public bool Equals(MFU_ACT1_CONF_DEST? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rlen.Equals(other.rlen))
		{
			return rshape.Equals(other.rshape);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rlen, rshape));
	}
}
