using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_ACT1_CONF_DEQ : InstructionOp, ISerializeInst, IEquatable<MFU_ACT1_CONF_DEQ?>
{
	public static readonly ParameterInfo sid = new ParameterInfo(typeof(MFU_ACT1_CONF_DEQ), 0, "sid", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo rshift_bits = new ParameterInfo(typeof(MFU_ACT1_CONF_DEQ), 1, "rshift_bits", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_ACT1_CONF_DEQ), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rscale { get; }

	public GP_REGISTER rbias { get; }

	public QUANT_TYPE quant_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rscale, 5);
		bitWriter.Write(rbias, 5);
		bitWriter.Write(quant_type, 2);
		bitWriter.Write(((TensorConst)call[sid]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[rshift_bits]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_ACT1_CONF_DEQ(MFU_CONF_FUNCTION funct5, GP_REGISTER rscale, GP_REGISTER rbias, QUANT_TYPE quantType)
	{
		this.funct5 = funct5;
		this.rscale = rscale;
		this.rbias = rbias;
		quant_type = quantType;
	}

	public MFU_ACT1_CONF_DEQ With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rscale = null, GP_REGISTER? rbias = null, QUANT_TYPE? quantType = null)
	{
		return new MFU_ACT1_CONF_DEQ(funct5 ?? this.funct5, rscale ?? this.rscale, rbias ?? this.rbias, quantType ?? quant_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_ACT1_CONF_DEQ);
	}

	public bool Equals(MFU_ACT1_CONF_DEQ? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rscale.Equals(other.rscale) && rbias.Equals(other.rbias))
		{
			return quant_type.Equals(other.quant_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rscale, rbias, quant_type));
	}
}
