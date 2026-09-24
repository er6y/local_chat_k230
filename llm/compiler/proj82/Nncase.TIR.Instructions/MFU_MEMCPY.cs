using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_MEMCPY : InstructionOp, ISerializeInst, IEquatable<MFU_MEMCPY?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_MEMCPY), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public GP_REGISTER raddr_s { get; }

	public SHAPE_REGISTER rstride_d { get; }

	public SHAPE_REGISTER rstride_s { get; }

	public SHAPE_REGISTER rshape { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_memcpy, 7);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(rstride_d, 3);
		bitWriter.Write(rstride_s, 3);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 6);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_MEMCPY(GP_REGISTER raddrD, GP_REGISTER raddrS, SHAPE_REGISTER rstrideD, SHAPE_REGISTER rstrideS, SHAPE_REGISTER rshape)
	{
		raddr_d = raddrD;
		raddr_s = raddrS;
		rstride_d = rstrideD;
		rstride_s = rstrideS;
		this.rshape = rshape;
	}

	public MFU_MEMCPY With(GP_REGISTER? raddrD = null, GP_REGISTER? raddrS = null, SHAPE_REGISTER? rstrideD = null, SHAPE_REGISTER? rstrideS = null, SHAPE_REGISTER? rshape = null)
	{
		return new MFU_MEMCPY(raddrD ?? raddr_d, raddrS ?? raddr_s, rstrideD ?? rstride_d, rstrideS ?? rstride_s, rshape ?? this.rshape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_MEMCPY);
	}

	public bool Equals(MFU_MEMCPY? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d.Equals(other.raddr_d) && raddr_s.Equals(other.raddr_s) && rstride_d.Equals(other.rstride_d) && rstride_s.Equals(other.rstride_s))
		{
			return rshape.Equals(other.rshape);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d, raddr_s, rstride_d, rstride_s, rshape));
	}
}
