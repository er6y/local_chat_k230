using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_COMPUTE : InstructionOp, ISerializeInst, IEquatable<PU_COMPUTE?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_COMPUTE), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_COMPUTE), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_OF_SHIFT_MODE of_shift_mode { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[2];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_compute, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(of_shift_mode, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 4);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_COMPUTE(PU_OF_SHIFT_MODE ofShiftMode)
	{
		of_shift_mode = ofShiftMode;
	}

	public PU_COMPUTE With(PU_OF_SHIFT_MODE? ofShiftMode = null)
	{
		return new PU_COMPUTE(ofShiftMode ?? of_shift_mode);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_COMPUTE);
	}

	public bool Equals(PU_COMPUTE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other))
		{
			return of_shift_mode.Equals(other.of_shift_mode);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(of_shift_mode));
	}
}
