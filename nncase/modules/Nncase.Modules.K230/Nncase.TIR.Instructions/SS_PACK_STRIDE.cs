using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class SS_PACK_STRIDE : InstructionOp, ISerializeInst, IEquatable<SS_PACK_STRIDE?>
{
	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(SS_PACK_STRIDE), 0, "reserved0", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved1 = new ParameterInfo(typeof(SS_PACK_STRIDE), 1, "reserved1", TypePatternUtility.IsIntegralScalar());

	public GP_REGISTER rn { get; }

	public GP_REGISTER rc { get; }

	public GP_REGISTER rh { get; }

	public SHAPE_REGISTER rss { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.ss_pack_stride, 7);
		bitWriter.Write(rn, 5);
		bitWriter.Write(rc, 5);
		bitWriter.Write(rh, 5);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 5);
		bitWriter.Write(rss, 3);
		bitWriter.Write(((TensorConst)call[reserved1]).Value.ToScalar<uint>(), 2);
		bitWriter.Flush();
		writer.Write(array);
	}

	public SS_PACK_STRIDE(GP_REGISTER rn, GP_REGISTER rc, GP_REGISTER rh, SHAPE_REGISTER rss)
	{
		this.rn = rn;
		this.rc = rc;
		this.rh = rh;
		this.rss = rss;
	}

	public SS_PACK_STRIDE With(GP_REGISTER? rn = null, GP_REGISTER? rc = null, GP_REGISTER? rh = null, SHAPE_REGISTER? rss = null)
	{
		return new SS_PACK_STRIDE(rn ?? this.rn, rc ?? this.rc, rh ?? this.rh, rss ?? this.rss);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as SS_PACK_STRIDE);
	}

	public bool Equals(SS_PACK_STRIDE? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && rn.Equals(other.rn) && rc.Equals(other.rc) && rh.Equals(other.rh))
		{
			return rss.Equals(other.rss);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(rn, rc, rh, rss));
	}
}
