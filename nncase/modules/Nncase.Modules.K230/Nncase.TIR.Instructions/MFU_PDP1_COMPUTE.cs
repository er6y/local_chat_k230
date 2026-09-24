using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_PDP1_COMPUTE : InstructionOp, ISerializeInst, IEquatable<MFU_PDP1_COMPUTE?>
{
	public static readonly ParameterInfo reserved1 = new ParameterInfo(typeof(MFU_PDP1_COMPUTE), 0, "reserved1", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public GP_REGISTER raddr_s { get; }

	public SHAPE_REGISTER rshape { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_pdp1_compute, 7);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[reserved1]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_PDP1_COMPUTE(GP_REGISTER raddrD, GP_REGISTER raddrS, SHAPE_REGISTER rshape)
	{
		raddr_d = raddrD;
		raddr_s = raddrS;
		this.rshape = rshape;
	}

	public MFU_PDP1_COMPUTE With(GP_REGISTER? raddrD = null, GP_REGISTER? raddrS = null, SHAPE_REGISTER? rshape = null)
	{
		return new MFU_PDP1_COMPUTE(raddrD ?? raddr_d, raddrS ?? raddr_s, rshape ?? this.rshape);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_PDP1_COMPUTE);
	}

	public bool Equals(MFU_PDP1_COMPUTE? other)
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
