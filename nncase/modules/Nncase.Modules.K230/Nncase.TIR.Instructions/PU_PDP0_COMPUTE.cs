using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_PDP0_COMPUTE : InstructionOp, ISerializeInst, IEquatable<PU_PDP0_COMPUTE?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_PDP0_COMPUTE), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_PDP0_COMPUTE), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_s { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_pdp0_compute, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 1);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_PDP0_COMPUTE(GP_REGISTER raddrS)
	{
		raddr_s = raddrS;
	}

	public PU_PDP0_COMPUTE With(GP_REGISTER? raddrS = null)
	{
		return new PU_PDP0_COMPUTE(raddrS ?? raddr_s);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_PDP0_COMPUTE);
	}

	public bool Equals(PU_PDP0_COMPUTE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return raddr_s.Equals(other.raddr_s);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_s));
	}
}
