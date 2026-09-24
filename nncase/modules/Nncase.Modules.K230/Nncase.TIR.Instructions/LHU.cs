using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class LHU : InstructionOp, ISerializeInst, IEquatable<LHU?>
{
	public static readonly ParameterInfo offset = new ParameterInfo(typeof(LHU), 0, "offset", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rd { get; }

	public GP_REGISTER rs { get; }

	public LOAD_FUNCTION funct3 { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.load, 7);
		bitWriter.Write(rd, 5);
		bitWriter.Write(rs, 5);
		bitWriter.Write(funct3, 3);
		bitWriter.Write(((TensorConst)call[offset]).Value.ToScalar<uint>(), 12);
		bitWriter.Flush();
		writer.Write(array);
	}

	public LHU(GP_REGISTER rd, GP_REGISTER rs, LOAD_FUNCTION funct3)
	{
		this.rd = rd;
		this.rs = rs;
		this.funct3 = funct3;
	}

	public LHU With(GP_REGISTER? rd = null, GP_REGISTER? rs = null, LOAD_FUNCTION? funct3 = null)
	{
		return new LHU(rd ?? this.rd, rs ?? this.rs, funct3 ?? this.funct3);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as LHU);
	}

	public bool Equals(LHU? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rd.Equals(other.rd) && rs.Equals(other.rs))
		{
			return funct3.Equals(other.funct3);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rd, rs, funct3));
	}
}
