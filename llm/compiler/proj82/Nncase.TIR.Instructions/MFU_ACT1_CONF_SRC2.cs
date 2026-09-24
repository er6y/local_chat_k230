using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF_SRC2 : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF_SRC2?>
{
	public static readonly ParameterInfo sid = new ParameterInfo(typeof(MFU_ACT1_CONF_SRC2), 0, "sid", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF_SRC2), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rleft_repeats { get; }

	public SHAPE_REGISTER rshape { get; }

	public ACT1_SOURCE_TYPE source_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rleft_repeats, 5);
		bitWriter.Write(rshape, 3);
		bitWriter.Write(((TensorConst)call[sid]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(source_type, 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF_SRC2(MFU_CONF_FUNCTION funct5, GP_REGISTER rleftRepeats, SHAPE_REGISTER rshape, ACT1_SOURCE_TYPE sourceType)
	{
		this.funct5 = funct5;
		rleft_repeats = rleftRepeats;
		this.rshape = rshape;
		source_type = sourceType;
	}

	public MFU_ACT1_CONF_SRC2 With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rleftRepeats = null, SHAPE_REGISTER? rshape = null, ACT1_SOURCE_TYPE? sourceType = null)
	{
		return new MFU_ACT1_CONF_SRC2(funct5 ?? this.funct5, rleftRepeats ?? rleft_repeats, rshape ?? this.rshape, sourceType ?? source_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF_SRC2);
	}

	public bool Equals(MFU_ACT1_CONF_SRC2? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rleft_repeats.Equals(other.rleft_repeats) && rshape.Equals(other.rshape))
		{
			return source_type.Equals(other.source_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rleft_repeats, rshape, source_type));
	}
}
