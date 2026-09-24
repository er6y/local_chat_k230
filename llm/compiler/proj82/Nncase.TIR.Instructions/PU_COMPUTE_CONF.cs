using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class PU_COMPUTE_CONF : InstructionOp, ISerializeInst, IEquatable<PU_COMPUTE_CONF?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(PU_COMPUTE_CONF), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(PU_COMPUTE_CONF), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo load_psum = new ParameterInfo(typeof(PU_COMPUTE_CONF), 2, "load_psum", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo clr_psum = new ParameterInfo(typeof(PU_COMPUTE_CONF), 3, "clr_psum", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo release_if = new ParameterInfo(typeof(PU_COMPUTE_CONF), 4, "release_if", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(PU_COMPUTE_CONF), 5, "reserved0", TypePatternUtility.IsIntegralScalar());

	public PU_CONF_FUNCTION funct4 { get; }

	public PU_OUTPUT_DEST dest_target { get; }

	public PU_COMPUTE_MODE mode { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.pu_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(((TensorConst)call[load_psum]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[clr_psum]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(dest_target, 1);
		bitWriter.Write(((TensorConst)call[release_if]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(mode, 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public PU_COMPUTE_CONF(PU_CONF_FUNCTION funct4, PU_OUTPUT_DEST destTarget, PU_COMPUTE_MODE mode)
	{
		this.funct4 = funct4;
		dest_target = destTarget;
		this.mode = mode;
	}

	public PU_COMPUTE_CONF With(PU_CONF_FUNCTION? funct4 = null, PU_OUTPUT_DEST? destTarget = null, PU_COMPUTE_MODE? mode = null)
	{
		return new PU_COMPUTE_CONF(funct4 ?? this.funct4, destTarget ?? dest_target, mode ?? this.mode);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as PU_COMPUTE_CONF);
	}

	public bool Equals(PU_COMPUTE_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4) && dest_target.Equals(other.dest_target))
		{
			return mode.Equals(other.mode);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, dest_target, mode));
	}
}
