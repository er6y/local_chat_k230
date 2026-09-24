using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_TRANSPOSE : InstructionOp, ISerializeInst, IEquatable<MFU_TRANSPOSE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_TRANSPOSE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public GP_REGISTER raddr_s { get; }

	public SHAPE_REGISTER rshape { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_transpose, 7);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_TRANSPOSE(GP_REGISTER raddrD, GP_REGISTER raddrS, SHAPE_REGISTER rshape)
	{
		raddr_d = raddrD;
		raddr_s = raddrS;
		this.rshape = rshape;
	}

	public MFU_TRANSPOSE With(GP_REGISTER? raddrD = null, GP_REGISTER? raddrS = null, SHAPE_REGISTER? rshape = null)
	{
		return new MFU_TRANSPOSE(raddrD ?? raddr_d, raddrS ?? raddr_s, rshape ?? this.rshape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_TRANSPOSE);
	}

	public bool Equals(MFU_TRANSPOSE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d.Equals(other.raddr_d) && raddr_s.Equals(other.raddr_s))
		{
			return rshape.Equals(other.rshape);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d, raddr_s, rshape));
	}
}
