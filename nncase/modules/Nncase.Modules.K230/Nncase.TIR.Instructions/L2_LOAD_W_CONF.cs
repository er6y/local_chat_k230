using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class L2_LOAD_W_CONF : InstructionOp, ISerializeInst, IEquatable<L2_LOAD_W_CONF?>
{
	public static readonly ParameterInfo enable_decompress = new ParameterInfo(typeof(L2_LOAD_W_CONF), 0, "enable_decompress", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(L2_LOAD_W_CONF), 1, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rlen_compressed { get; }

	public GP_REGISTER rlen_decompressed { get; }

	public L2_DATATYPE l2_datatype { get; }

	public DDR_DATATYPE ddr_datatype { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.l2_load_w_conf, 7);
		bitWriter.Write(rlen_compressed, 5);
		bitWriter.Write(rlen_decompressed, 5);
		bitWriter.Write(l2_datatype, 2);
		bitWriter.Write(ddr_datatype, 3);
		bitWriter.Write(((TensorConst)call[enable_decompress]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 9);
		bitWriter.Flush();
		writer.Write(array);
	}

	public L2_LOAD_W_CONF(GP_REGISTER rlenCompressed, GP_REGISTER rlenDecompressed, L2_DATATYPE l2Datatype, DDR_DATATYPE ddrDatatype)
	{
		rlen_compressed = rlenCompressed;
		rlen_decompressed = rlenDecompressed;
		l2_datatype = l2Datatype;
		ddr_datatype = ddrDatatype;
	}

	public L2_LOAD_W_CONF With(GP_REGISTER? rlenCompressed = null, GP_REGISTER? rlenDecompressed = null, L2_DATATYPE? l2Datatype = null, DDR_DATATYPE? ddrDatatype = null)
	{
		return new L2_LOAD_W_CONF(rlenCompressed ?? rlen_compressed, rlenDecompressed ?? rlen_decompressed, l2Datatype ?? l2_datatype, ddrDatatype ?? ddr_datatype);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as L2_LOAD_W_CONF);
	}

	public bool Equals(L2_LOAD_W_CONF? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rlen_compressed.Equals(other.rlen_compressed) && rlen_decompressed.Equals(other.rlen_decompressed) && l2_datatype.Equals(other.l2_datatype))
		{
			return ddr_datatype.Equals(other.ddr_datatype);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rlen_compressed, rlen_decompressed, l2_datatype, ddr_datatype));
	}
}
