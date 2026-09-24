using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_PDP1_CONF2 : InstructionOp, ISerializeInst, IEquatable<MFU_PDP1_CONF2?>
{
	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rcount_w { get; }

	public GP_REGISTER rcount_h { get; }

	public GP_REGISTER rpe_h { get; }

	public GP_REGISTER rpe_last_h { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rcount_w, 5);
		bitWriter.Write(rcount_h, 5);
		bitWriter.Write(rpe_h, 5);
		bitWriter.Write(rpe_last_h, 5);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_PDP1_CONF2(MFU_CONF_FUNCTION funct5, GP_REGISTER rcountW, GP_REGISTER rcountH, GP_REGISTER rpeH, GP_REGISTER rpeLastH)
	{
		this.funct5 = funct5;
		rcount_w = rcountW;
		rcount_h = rcountH;
		rpe_h = rpeH;
		rpe_last_h = rpeLastH;
	}

	public MFU_PDP1_CONF2 With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rcountW = null, GP_REGISTER? rcountH = null, GP_REGISTER? rpeH = null, GP_REGISTER? rpeLastH = null)
	{
		return new MFU_PDP1_CONF2(funct5 ?? this.funct5, rcountW ?? rcount_w, rcountH ?? rcount_h, rpeH ?? rpe_h, rpeLastH ?? rpe_last_h);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_PDP1_CONF2);
	}

	public bool Equals(MFU_PDP1_CONF2? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rcount_w.Equals(other.rcount_w) && rcount_h.Equals(other.rcount_h) && rpe_h.Equals(other.rpe_h))
		{
			return rpe_last_h.Equals(other.rpe_last_h);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rcount_w, rcount_h, rpe_h, rpe_last_h));
	}
}
