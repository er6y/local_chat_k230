using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class DM_LOAD_W_CONF_DEQ : InstructionOp, ISerializeInst, IEquatable<DM_LOAD_W_CONF_DEQ?>
{
	public static readonly ParameterInfo tcu_id = new ParameterInfo(typeof(DM_LOAD_W_CONF_DEQ), 0, "tcu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo pu_id = new ParameterInfo(typeof(DM_LOAD_W_CONF_DEQ), 1, "pu_id", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(DM_LOAD_W_CONF_DEQ), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public DM_CONF_FUNCTION funct4 { get; }

	public QUANT_TYPE quant_type { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.dm_conf, 7);
		bitWriter.Write(((TensorConst)call[tcu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(((TensorConst)call[pu_id]).Value.ToScalar<uint>(), 3);
		bitWriter.Write(funct4, 4);
		bitWriter.Write(quant_type, 2);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 13);
		bitWriter.Flush();
		writer.Write(array);
	}

	public DM_LOAD_W_CONF_DEQ(DM_CONF_FUNCTION funct4, QUANT_TYPE quantType)
	{
		this.funct4 = funct4;
		quant_type = quantType;
	}

	public DM_LOAD_W_CONF_DEQ With(DM_CONF_FUNCTION? funct4 = null, QUANT_TYPE? quantType = null)
	{
		return new DM_LOAD_W_CONF_DEQ(funct4 ?? this.funct4, quantType ?? quant_type);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as DM_LOAD_W_CONF_DEQ);
	}

	public bool Equals(DM_LOAD_W_CONF_DEQ? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct4.Equals(other.funct4))
		{
			return quant_type.Equals(other.quant_type);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct4, quant_type));
	}
}
