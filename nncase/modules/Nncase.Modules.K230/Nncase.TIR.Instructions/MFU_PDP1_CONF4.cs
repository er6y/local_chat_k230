using System;
using System.IO;
using Nncase.IO;
using Nncase.IR;

namespace Nncase.TIR.Instructions;

public sealed class MFU_PDP1_CONF4 : InstructionOp, ISerializeInst, IEquatable<MFU_PDP1_CONF4?>
{
	public static readonly ParameterInfo enable_h2c = new ParameterInfo(typeof(MFU_PDP1_CONF4), 0, "enable_h2c", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo enable_bw = new ParameterInfo(typeof(MFU_PDP1_CONF4), 1, "enable_bw", TypePatternUtility.IsIntegralScalar());

	public static readonly ParameterInfo reserved0 = new ParameterInfo(typeof(MFU_PDP1_CONF4), 2, "reserved0", TypePatternUtility.IsIntegralScalar());

	public MFU_CONF_FUNCTION funct5 { get; }

	public GP_REGISTER rwindow_w { get; }

	public GP_REGISTER rwindow_h { get; }

	public GP_REGISTER rscale { get; }

	public void Serialize(BinaryWriter writer, Call call)
	{
		byte[] array = new byte[4];
		BitWriter bitWriter = new BitWriter(array, 0uL);
		bitWriter.Write(OPCODE.mfu_conf, 7);
		bitWriter.Write(funct5, 5);
		bitWriter.Write(rwindow_w, 5);
		bitWriter.Write(rwindow_h, 5);
		bitWriter.Write(rscale, 5);
		bitWriter.Write(((TensorConst)call[enable_h2c]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[enable_bw]).Value.ToScalar<uint>(), 1);
		bitWriter.Write(((TensorConst)call[reserved0]).Value.ToScalar<uint>(), 3);
		bitWriter.Flush();
		writer.Write(array);
	}

	public MFU_PDP1_CONF4(MFU_CONF_FUNCTION funct5, GP_REGISTER rwindowW, GP_REGISTER rwindowH, GP_REGISTER rscale)
	{
		this.funct5 = funct5;
		rwindow_w = rwindowW;
		rwindow_h = rwindowH;
		this.rscale = rscale;
	}

	public MFU_PDP1_CONF4 With(MFU_CONF_FUNCTION? funct5 = null, GP_REGISTER? rwindowW = null, GP_REGISTER? rwindowH = null, GP_REGISTER? rscale = null)
	{
		return new MFU_PDP1_CONF4(funct5 ?? this.funct5, rwindowW ?? rwindow_w, rwindowH ?? rwindow_h, rscale ?? this.rscale);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as MFU_PDP1_CONF4);
	}

	public bool Equals(MFU_PDP1_CONF4? other)
	{
		if ((object)this == other)
		{
			return true;
		}
		if ((object)other != null && Equals((InstructionOp?)other) && funct5.Equals(other.funct5) && rwindow_w.Equals(other.rwindow_w) && rwindow_h.Equals(other.rwindow_h))
		{
			return rscale.Equals(other.rscale);
		}
		return false;
	}

	protected override int GetHashCodeCore()
	{
		return HashCode.Combine(base.GetHashCodeCore(), HashCode.Combine(funct5, rwindow_w, rwindow_h, rscale));
	}
}
