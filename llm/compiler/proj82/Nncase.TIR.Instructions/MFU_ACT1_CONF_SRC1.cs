using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF_SRC1 : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF_SRC1?>
{
	public static readonly ParameterInfo sid = new ParameterInfo(typeof(MFU_ACT1_CONF_SRC1), 0, "sid", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF_SRC1), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rslice { get; }

	public GP_REGISTER rright_repeats { get; }

	public GP_REGISTER rslice_repeats { get; }

	public SLICE_LOCATION slice_loc { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rslice, 5);
		bitWriter.Write(rright_repeats, 5);
		bitWriter.Write(rslice_repeats, 5);
		bitWriter.Write(((TensorConst)call[sid]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(slice_loc, 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 3);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF_SRC1(MFU_CONF_FUNCTION funct5, GP_REGISTER rslice, GP_REGISTER rrightRepeats, GP_REGISTER rsliceRepeats, SLICE_LOCATION sliceLoc)
	{
		this.funct5 = funct5;
		this.rslice = rslice;
		rright_repeats = rrightRepeats;
		rslice_repeats = rsliceRepeats;
		slice_loc = sliceLoc;
	}

	public MFU_ACT1_CONF_SRC1 With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rslice = null, GP_REGISTER? rrightRepeats = null, GP_REGISTER? rsliceRepeats = null, SLICE_LOCATION? sliceLoc = null)
	{
		return new MFU_ACT1_CONF_SRC1(funct5 ?? this.funct5, rslice ?? this.rslice, rrightRepeats ?? rright_repeats, rsliceRepeats ?? rslice_repeats, sliceLoc ?? slice_loc);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF_SRC1);
	}

	public bool Equals(MFU_ACT1_CONF_SRC1? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rslice.Equals(other.rslice) && rright_repeats.Equals(other.rright_repeats) && rslice_repeats.Equals(other.rslice_repeats))
		{
			return slice_loc.Equals(other.slice_loc);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rslice, rright_repeats, rslice_repeats, slice_loc));
	}
}
