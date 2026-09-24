using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class L2_LOAD_W : InstructionOp, ISerializeInst, IEquatable<L2_LOAD_W?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(L2_LOAD_W), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER raddr_d { get; }

	public GP_REGISTER raddr_s { get; }

	public GP_REGISTER rvalid_c_num { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.l2_load_w, 7);
		bitWriter.Write(raddr_d, 5);
		bitWriter.Write(raddr_s, 5);
		bitWriter.Write(rvalid_c_num, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 10);
		bitWriter.Flush();
		writer.Write(array);
	}

	public L2_LOAD_W(GP_REGISTER raddrD, GP_REGISTER raddrS, GP_REGISTER rvalidCNum)
	{
		raddr_d = raddrD;
		raddr_s = raddrS;
		rvalid_c_num = rvalidCNum;
	}

	public L2_LOAD_W With(GP_REGISTER? raddrD = null, GP_REGISTER? raddrS = null, GP_REGISTER? rvalidCNum = null)
	{
		return new L2_LOAD_W(raddrD ?? raddr_d, raddrS ?? raddr_s, rvalidCNum ?? rvalid_c_num);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as L2_LOAD_W);
	}

	public bool Equals(L2_LOAD_W? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && raddr_d.Equals(other.raddr_d) && raddr_s.Equals(other.raddr_s))
		{
			return rvalid_c_num.Equals(other.rvalid_c_num);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(raddr_d, raddr_s, rvalid_c_num));
	}
}
