using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_COMPUTE : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_COMPUTE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_COMPUTE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d1 { get; }

	public GP_REGISTER raddr_s1 { get; }

	public GP_REGISTER raddr_s2 { get; }

	public GP_REGISTER raddr_arg { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_act1_compute, 7);
		bitWriter.Write(raddr_d1, 5);
		bitWriter.Write(raddr_s1, 5);
		bitWriter.Write(raddr_s2, 5);
		bitWriter.Write(raddr_arg, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_COMPUTE(GP_REGISTER raddrD1, GP_REGISTER raddrS1, GP_REGISTER raddrS2, GP_REGISTER raddrArg)
	{
		raddr_d1 = raddrD1;
		raddr_s1 = raddrS1;
		raddr_s2 = raddrS2;
		raddr_arg = raddrArg;
	}

	public MFU_ACT1_COMPUTE With(GP_REGISTER? raddrD1 = null, GP_REGISTER? raddrS1 = null, GP_REGISTER? raddrS2 = null, GP_REGISTER? raddrArg = null)
	{
		return new MFU_ACT1_COMPUTE(raddrD1 ?? raddr_d1, raddrS1 ?? raddr_s1, raddrS2 ?? raddr_s2, raddrArg ?? raddr_arg);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_COMPUTE);
	}

	public bool Equals(MFU_ACT1_COMPUTE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d1.Equals(other.raddr_d1) && raddr_s1.Equals(other.raddr_s1) && raddr_s2.Equals(other.raddr_s2))
		{
			return raddr_arg.Equals(other.raddr_arg);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d1, raddr_s1, raddr_s2, raddr_arg));
	}
}
