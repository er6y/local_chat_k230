using System;
using System.IO;
using System.Text;
using Nncase.IO;
using Nncase.TIR.Instructions;

namespace Nncase.CodeGen;

public class K230DeSerializerVisitor
{
	private uint[] _gp_regs;

	private ulong[,] _shape_regs;

	private ulong[] _if_bw;

	private ulong[] _w_bw;

	private ulong[] _of_bw;

	private ulong[] _other_bw;

	private DDR_DATATYPE _l2_load_ddr_type;

	private DDR_DATATYPE _l2_store_ddr_type;

	public K230DeSerializerVisitor()
	{
		_gp_regs = new uint[32];
		_shape_regs = new ulong[32, 4];
		_if_bw = new ulong[2];
		_w_bw = new ulong[2];
		_of_bw = new ulong[2];
		_other_bw = new ulong[2];
	}

	private ulong DDrTypeSizeInByte(DDR_DATATYPE dtype)
	{
		switch (dtype)
		{
		case DDR_DATATYPE.i8:
			return 1uL;
		case DDR_DATATYPE.fp16:
			return 2uL;
		case DDR_DATATYPE.fp32:
			return 3uL;
		case DDR_DATATYPE.i4:
			return 1uL;
		case DDR_DATATYPE.i6:
			return 1uL;
		case DDR_DATATYPE.i16:
			return 2uL;
		default:
		{
			throw new System.Runtime.CompilerServices.SwitchExpressionException(dtype);
		}
		}
	}

	private ulong GetProduct(SHAPE_REGISTER reg, DDR_DATATYPE dtype)
	{
		return (ulong)TensorUtilities.GetProduct(new int[4]
		{
			(int)_shape_regs[(uint)reg, 0],
			(int)_shape_regs[(uint)reg, 1],
			(int)_shape_regs[(uint)reg, 2],
			(int)_shape_regs[(uint)reg, 3]
		}) * DDrTypeSizeInByte(dtype);
	}

	private void AppendRValue(StringBuilder sb, string field, GP_REGISTER reg)
	{
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, sb);
		handler.AppendLiteral("    ");
		handler.AppendFormatted(field);
		handler.AppendLiteral("_val: ");
		handler.AppendFormatted(_gp_regs[(uint)reg]);
		sb.AppendLine(ref handler);
	}

	private void AppendSValue(StringBuilder sb, string field, SHAPE_REGISTER reg)
	{
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 5, sb);
		handler.AppendLiteral("    ");
		handler.AppendFormatted(field);
		handler.AppendLiteral("_val: ");
		handler.AppendFormatted(_shape_regs[(uint)reg, 0]);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(_shape_regs[(uint)reg, 1]);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(_shape_regs[(uint)reg, 2]);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(_shape_regs[(uint)reg, 3]);
		sb.AppendLine(ref handler);
	}

	private void WriteBandWidth(TextWriter bwWriter, string funcName)
	{
		ulong[] array = new ulong[2]
		{
			_if_bw[0] + _w_bw[0] + _of_bw[0] + _other_bw[0],
			_if_bw[1] + _w_bw[1] + _of_bw[1] + _other_bw[1]
		};
		bwWriter.WriteLine($"ddr bandwidth(total if w of other): {array[0]}\t{_if_bw[0]}\t{_w_bw[0]}\t{_of_bw[0]}\t{_other_bw[0]}");
		bwWriter.WriteLine($"effctive ddr bandwidth(total if w of other): {array[1]}\t{_if_bw[1]}\t{_w_bw[1]}\t{_of_bw[1]}\t{_other_bw[1]}");
	}

	private string visit_lui(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER gP_REGISTER = (GP_REGISTER)reader.Read<uint>(5uL);
		int num = reader.Read<int>(20uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst lui");
		stringBuilder.AppendLine("    opcode : lui");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rd : ");
		handler.AppendFormatted(gP_REGISTER);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    imm : ");
		handler.AppendFormatted(num);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 32uL;
		_gp_regs[(uint)gP_REGISTER] = (uint)(num << 12) | 0u;
		return stringBuilder.ToString();
	}

	private string visit_auipc(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		int value2 = reader.Read<int>(20uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst auipc");
		stringBuilder.AppendLine("    opcode : auipc");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rd : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    imm : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_arithm_imm(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER gP_REGISTER = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER2 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<uint>(3uL);
		int num = reader.Read<int>(12uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst addi");
		stringBuilder.AppendLine("    opcode : arithm_imm");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rd : ");
		handler.AppendFormatted(gP_REGISTER);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rs : ");
		handler.AppendFormatted(gP_REGISTER2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    imm : ");
		handler.AppendFormatted(num);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		uint num2 = _gp_regs[(uint)gP_REGISTER2];
		int num3 = ((((num >> 11) & 1) == 1) ? (num - 4096) : num);
		_gp_regs[(uint)gP_REGISTER] = (uint)num3 + num2;
		return stringBuilder.ToString();
	}

	private string visit_arithm(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER gP_REGISTER = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER2 = (GP_REGISTER)reader.Read<uint>(5uL);
		switch ((ARITHMETIC_FUNCTION)reader.Read<uint>(5uL))
		{
		case ARITHMETIC_FUNCTION.add:
		{
			GP_REGISTER gP_REGISTER9 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder26 = new StringBuilder();
			stringBuilder26.AppendLine("inst add");
			stringBuilder26.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder27 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder27.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder28 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder28.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder29 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER9);
			stringBuilder29.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num13 = _gp_regs[(uint)gP_REGISTER2];
			uint num14 = _gp_regs[(uint)gP_REGISTER9];
			_gp_regs[(uint)gP_REGISTER] = num13 + num14;
			return stringBuilder26.ToString();
		}
		case ARITHMETIC_FUNCTION.sub:
		{
			GP_REGISTER gP_REGISTER8 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder22 = new StringBuilder();
			stringBuilder22.AppendLine("inst sub");
			stringBuilder22.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder23 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER8);
			stringBuilder25.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num11 = _gp_regs[(uint)gP_REGISTER2];
			uint num12 = _gp_regs[(uint)gP_REGISTER8];
			_gp_regs[(uint)gP_REGISTER] = num11 - num12;
			return stringBuilder22.ToString();
		}
		case ARITHMETIC_FUNCTION.mul:
		{
			GP_REGISTER gP_REGISTER7 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder18 = new StringBuilder();
			stringBuilder18.AppendLine("inst mul");
			stringBuilder18.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder19 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder20.AppendLine(ref handler);
			stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder21 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER7);
			stringBuilder21.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num9 = _gp_regs[(uint)gP_REGISTER2];
			uint num10 = _gp_regs[(uint)gP_REGISTER7];
			_gp_regs[(uint)gP_REGISTER] = num9 * num10;
			return stringBuilder18.ToString();
		}
		case ARITHMETIC_FUNCTION.div:
		{
			GP_REGISTER gP_REGISTER6 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder14 = new StringBuilder();
			stringBuilder14.AppendLine("inst div");
			stringBuilder14.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder15 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder15.AppendLine(ref handler);
			stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder16 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder16.AppendLine(ref handler);
			stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder17 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER6);
			stringBuilder17.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num7 = _gp_regs[(uint)gP_REGISTER2];
			uint num8 = _gp_regs[(uint)gP_REGISTER6];
			_gp_regs[(uint)gP_REGISTER] = (uint)((int)num7 / (int)num8);
			return stringBuilder14.ToString();
		}
		case ARITHMETIC_FUNCTION.divu:
		{
			GP_REGISTER gP_REGISTER5 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder10 = new StringBuilder();
			stringBuilder10.AppendLine("inst divu");
			stringBuilder10.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder11 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER5);
			stringBuilder13.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num5 = _gp_regs[(uint)gP_REGISTER2];
			uint num6 = _gp_regs[(uint)gP_REGISTER5];
			_gp_regs[(uint)gP_REGISTER] = num5 / num6;
			return stringBuilder10.ToString();
		}
		case ARITHMETIC_FUNCTION.rem:
		{
			GP_REGISTER gP_REGISTER4 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder6 = new StringBuilder();
			stringBuilder6.AppendLine("inst rem");
			stringBuilder6.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder7 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER4);
			stringBuilder9.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num3 = _gp_regs[(uint)gP_REGISTER2];
			uint num4 = _gp_regs[(uint)gP_REGISTER4];
			_gp_regs[(uint)gP_REGISTER] = (uint)((int)num3 % (int)num4);
			return stringBuilder6.ToString();
		}
		case ARITHMETIC_FUNCTION.remu:
		{
			GP_REGISTER gP_REGISTER3 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst remu");
			stringBuilder.AppendLine("    opcode : arithm");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(gP_REGISTER);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(gP_REGISTER2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(gP_REGISTER3);
			stringBuilder5.AppendLine(ref handler);
			pc_offset += 32uL;
			uint num = _gp_regs[(uint)gP_REGISTER2];
			uint num2 = _gp_regs[(uint)gP_REGISTER3];
			_gp_regs[(uint)gP_REGISTER] = num % num2;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_load(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		switch ((LOAD_FUNCTION)reader.Read<uint>(3uL))
		{
		case LOAD_FUNCTION.lw:
		{
			int value7 = reader.Read<int>(12uL);
			StringBuilder stringBuilder18 = new StringBuilder();
			stringBuilder18.AppendLine("inst lw");
			stringBuilder18.AppendLine("    opcode : load");
			StringBuilder stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder19 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder20.AppendLine(ref handler);
			stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder21 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value7);
			stringBuilder21.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder18.ToString();
		}
		case LOAD_FUNCTION.lh:
		{
			int value6 = reader.Read<int>(12uL);
			StringBuilder stringBuilder14 = new StringBuilder();
			stringBuilder14.AppendLine("inst lh");
			stringBuilder14.AppendLine("    opcode : load");
			StringBuilder stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder15 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder15.AppendLine(ref handler);
			stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder16 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder16.AppendLine(ref handler);
			stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder17 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value6);
			stringBuilder17.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder14.ToString();
		}
		case LOAD_FUNCTION.lhu:
		{
			int value5 = reader.Read<int>(12uL);
			StringBuilder stringBuilder10 = new StringBuilder();
			stringBuilder10.AppendLine("inst lhu");
			stringBuilder10.AppendLine("    opcode : load");
			StringBuilder stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder11 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value5);
			stringBuilder13.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder10.ToString();
		}
		case LOAD_FUNCTION.lb:
		{
			int value4 = reader.Read<int>(12uL);
			StringBuilder stringBuilder6 = new StringBuilder();
			stringBuilder6.AppendLine("inst lb");
			stringBuilder6.AppendLine("    opcode : load");
			StringBuilder stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder7 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value4);
			stringBuilder9.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder6.ToString();
		}
		case LOAD_FUNCTION.lbu:
		{
			int value3 = reader.Read<int>(12uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst lbu");
			stringBuilder.AppendLine("    opcode : load");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_store(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		switch ((STORE_FUNCTION)reader.Read<uint>(3uL))
		{
		case STORE_FUNCTION.sw:
		{
			int value5 = reader.Read<int>(12uL);
			StringBuilder stringBuilder10 = new StringBuilder();
			stringBuilder10.AppendLine("inst sw");
			stringBuilder10.AppendLine("    opcode : store");
			StringBuilder stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder11 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value5);
			stringBuilder13.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder10.ToString();
		}
		case STORE_FUNCTION.sh:
		{
			int value4 = reader.Read<int>(12uL);
			StringBuilder stringBuilder6 = new StringBuilder();
			stringBuilder6.AppendLine("inst sh");
			stringBuilder6.AppendLine("    opcode : store");
			StringBuilder stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder7 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value4);
			stringBuilder9.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder6.ToString();
		}
		case STORE_FUNCTION.sb:
		{
			int value3 = reader.Read<int>(12uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst sb");
			stringBuilder.AppendLine("    opcode : store");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rd : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("    rs : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_branch(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		switch ((BRANCH_FUNCTION)reader.Read<uint>(3uL))
		{
		case BRANCH_FUNCTION.beq:
		{
			int value8 = reader.Read<int>(12uL);
			StringBuilder stringBuilder22 = new StringBuilder();
			stringBuilder22.AppendLine("inst beq");
			stringBuilder22.AppendLine("    opcode : branch");
			StringBuilder stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder23 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(value);
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(value2);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value8);
			stringBuilder25.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder22.ToString();
		}
		case BRANCH_FUNCTION.bne:
		{
			int value7 = reader.Read<int>(12uL);
			StringBuilder stringBuilder18 = new StringBuilder();
			stringBuilder18.AppendLine("inst bne");
			stringBuilder18.AppendLine("    opcode : branch");
			StringBuilder stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder19 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(value);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(value2);
			stringBuilder20.AppendLine(ref handler);
			stringBuilder2 = stringBuilder18;
			StringBuilder stringBuilder21 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value7);
			stringBuilder21.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder18.ToString();
		}
		case BRANCH_FUNCTION.blt:
		{
			int value6 = reader.Read<int>(12uL);
			StringBuilder stringBuilder14 = new StringBuilder();
			stringBuilder14.AppendLine("inst blt");
			stringBuilder14.AppendLine("    opcode : branch");
			StringBuilder stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder15 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(value);
			stringBuilder15.AppendLine(ref handler);
			stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder16 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(value2);
			stringBuilder16.AppendLine(ref handler);
			stringBuilder2 = stringBuilder14;
			StringBuilder stringBuilder17 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value6);
			stringBuilder17.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder14.ToString();
		}
		case BRANCH_FUNCTION.bltu:
		{
			int value5 = reader.Read<int>(12uL);
			StringBuilder stringBuilder10 = new StringBuilder();
			stringBuilder10.AppendLine("inst bltu");
			stringBuilder10.AppendLine("    opcode : branch");
			StringBuilder stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder11 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(value);
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(value2);
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value5);
			stringBuilder13.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder10.ToString();
		}
		case BRANCH_FUNCTION.bge:
		{
			int value4 = reader.Read<int>(12uL);
			StringBuilder stringBuilder6 = new StringBuilder();
			stringBuilder6.AppendLine("inst bge");
			stringBuilder6.AppendLine("    opcode : branch");
			StringBuilder stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder7 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(value);
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(value2);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value4);
			stringBuilder9.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder6.ToString();
		}
		case BRANCH_FUNCTION.bgeu:
		{
			int value3 = reader.Read<int>(12uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst bgeu");
			stringBuilder.AppendLine("    opcode : branch");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs1 : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rs2 : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    offset : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_jal(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		int value2 = reader.Read<int>(20uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst jal");
		stringBuilder.AppendLine("    opcode : jal");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rd : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    offset : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_jalr(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(3uL);
		int value3 = reader.Read<int>(12uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst jalr");
		stringBuilder.AppendLine("    opcode : jalr");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rd : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rs : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    offset : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_intr(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst intr");
		stringBuilder.AppendLine("    opcode : intr");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rs : ");
		handler.AppendFormatted(value);
		stringBuilder2.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_end(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst end");
		stringBuilder.AppendLine("    opcode : end");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rs : ");
		handler.AppendFormatted(value);
		stringBuilder2.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_fence(ref BitReader reader, ref ulong pc_offset)
	{
		reader.Read<int>(9uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst fence");
		stringBuilder.AppendLine("    opcode : fence");
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_fence_i(ref BitReader reader, ref ulong pc_offset)
	{
		reader.Read<int>(9uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst fence_i");
		stringBuilder.AppendLine("    opcode : fence_i");
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_extrw(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(10uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		int value3 = reader.Read<int>(10uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst extrw");
		stringBuilder.AppendLine("    opcode : extrw");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    extrd : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rs : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    imm : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_extraw(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(10uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		int value3 = reader.Read<int>(10uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst extraw");
		stringBuilder.AppendLine("    opcode : extraw");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    extrd : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rs : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    imm : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_ccr_decl(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst ccr_decl");
		stringBuilder.AppendLine("    opcode : ccr_decl");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
		handler.AppendLiteral("    rnum : ");
		handler.AppendFormatted(value);
		stringBuilder2.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_ccr_set(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(5uL);
		int value2 = reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst ccr_set");
		stringBuilder.AppendLine("    opcode : ccr_set");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    ccr : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    value : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_ccr_clr(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(5uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst ccr_clr");
		stringBuilder.AppendLine("    opcode : ccr_clr");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    ccr : ");
		handler.AppendFormatted(value);
		stringBuilder2.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_mmu_conf(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		int value3 = reader.Read<int>(4uL);
		reader.Read<int>(11uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mmu_conf");
		stringBuilder.AppendLine("    opcode : mmu_conf");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rstart : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rdepth : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    mmu_id : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_mmu_set_id(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		int value2 = reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mmu_setid");
		stringBuilder.AppendLine("    opcode : mmu_set_id");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rd : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    mmu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_ss_pack_shape(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER gP_REGISTER = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER2 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER3 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER4 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER sHAPE_REGISTER = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(2uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst ss_pack_shape");
		stringBuilder.AppendLine("    opcode : ss_pack_shape");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rn : ");
		handler.AppendFormatted(gP_REGISTER);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rc : ");
		handler.AppendFormatted(gP_REGISTER2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rh : ");
		handler.AppendFormatted(gP_REGISTER3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rw : ");
		handler.AppendFormatted(gP_REGISTER4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    rss : ");
		handler.AppendFormatted(sHAPE_REGISTER);
		stringBuilder7.AppendLine(ref handler);
		pc_offset += 32uL;
		_shape_regs[(uint)sHAPE_REGISTER, 0] = _gp_regs[(uint)gP_REGISTER];
		_shape_regs[(uint)sHAPE_REGISTER, 1] = _gp_regs[(uint)gP_REGISTER2];
		_shape_regs[(uint)sHAPE_REGISTER, 2] = _gp_regs[(uint)gP_REGISTER3];
		_shape_regs[(uint)sHAPE_REGISTER, 3] = _gp_regs[(uint)gP_REGISTER4];
		return stringBuilder.ToString();
	}

	private string visit_ss_pack_stride(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER gP_REGISTER = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER2 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER3 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(5uL);
		SHAPE_REGISTER sHAPE_REGISTER = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(2uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst ss_pack_stride");
		stringBuilder.AppendLine("    opcode : ss_pack_stride");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rn : ");
		handler.AppendFormatted(gP_REGISTER);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rc : ");
		handler.AppendFormatted(gP_REGISTER2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rh : ");
		handler.AppendFormatted(gP_REGISTER3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("    rss : ");
		handler.AppendFormatted(sHAPE_REGISTER);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 32uL;
		_shape_regs[(uint)sHAPE_REGISTER, 0] = 0uL;
		_shape_regs[(uint)sHAPE_REGISTER, 1] = _gp_regs[(uint)gP_REGISTER];
		_shape_regs[(uint)sHAPE_REGISTER, 2] = _gp_regs[(uint)gP_REGISTER2];
		_shape_regs[(uint)sHAPE_REGISTER, 3] = _gp_regs[(uint)gP_REGISTER3];
		return stringBuilder.ToString();
	}

	private string visit_l2_load_conf(ref BitReader reader, ref ulong pc_offset)
	{
		SHAPE_REGISTER value = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		SHAPE_REGISTER value2 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		L2_DATATYPE value3 = (L2_DATATYPE)reader.Read<uint>(2uL);
		DDR_DATATYPE dDR_DATATYPE = (DDR_DATATYPE)reader.Read<uint>(3uL);
		reader.Read<int>(14uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst l2_load_conf");
		stringBuilder.AppendLine("    opcode : l2_load_conf");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    rstride_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    rstride_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("    l2_datatype : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    ddr_datatype : ");
		handler.AppendFormatted(dDR_DATATYPE);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 32uL;
		_l2_load_ddr_type = dDR_DATATYPE;
		return stringBuilder.ToString();
	}

	private string visit_l2_load_w_conf(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER gP_REGISTER = (GP_REGISTER)reader.Read<uint>(5uL);
		L2_DATATYPE value2 = (L2_DATATYPE)reader.Read<uint>(2uL);
		DDR_DATATYPE dDR_DATATYPE = (DDR_DATATYPE)reader.Read<uint>(3uL);
		int value3 = reader.Read<int>(1uL);
		reader.Read<int>(9uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst l2_load_w_conf");
		stringBuilder.AppendLine("    opcode : l2_load_w_conf");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder2);
		handler.AppendLiteral("    rlen_compressed : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2);
		handler.AppendLiteral("    rlen_decompressed : ");
		handler.AppendFormatted(gP_REGISTER);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("    l2_datatype : ");
		handler.AppendFormatted(value2);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    ddr_datatype : ");
		handler.AppendFormatted(dDR_DATATYPE);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2);
		handler.AppendLiteral("    enable_decompress : ");
		handler.AppendFormatted(value3);
		stringBuilder7.AppendLine(ref handler);
		pc_offset += 32uL;
		_w_bw[0] += _gp_regs[(uint)gP_REGISTER] * DDrTypeSizeInByte(dDR_DATATYPE);
		return stringBuilder.ToString();
	}

	private string visit_l2_load_w(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(10uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst l2_load_w");
		stringBuilder.AppendLine("    opcode : l2_load_w");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    rvalid_c_num : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_l2_store_conf(ref BitReader reader, ref ulong pc_offset)
	{
		SHAPE_REGISTER value = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		SHAPE_REGISTER value2 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		L2_DATATYPE value3 = (L2_DATATYPE)reader.Read<uint>(2uL);
		DDR_DATATYPE dDR_DATATYPE = (DDR_DATATYPE)reader.Read<uint>(3uL);
		reader.Read<int>(14uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst l2_store_conf");
		stringBuilder.AppendLine("    opcode : l2_store_conf");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    rstride_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    rstride_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("    l2_datatype : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    ddr_datatype : ");
		handler.AppendFormatted(dDR_DATATYPE);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 32uL;
		_l2_store_ddr_type = dDR_DATATYPE;
		return stringBuilder.ToString();
	}

	private string visit_l2_load(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER sHAPE_REGISTER = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(12uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst l2_load");
		stringBuilder.AppendLine("    opcode : l2_load");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(sHAPE_REGISTER);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		_if_bw[0] += GetProduct(sHAPE_REGISTER, _l2_load_ddr_type);
		return stringBuilder.ToString();
	}

	private string visit_l2_store(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER sHAPE_REGISTER = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(12uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst l2_store");
		stringBuilder.AppendLine("    opcode : l2_store");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(sHAPE_REGISTER);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		_of_bw[0] += GetProduct(sHAPE_REGISTER, _l2_store_ddr_type);
		return stringBuilder.ToString();
	}

	private string visit_dm_conf_broadcast(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(1uL);
		int value3 = reader.Read<int>(1uL);
		int value4 = reader.Read<int>(1uL);
		reader.Read<int>(3uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst dm_conf_broadcast");
		stringBuilder.AppendLine("    opcode : dm_conf_broadcast");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    broadcast_if : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("    broadcast_w : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    psum_cascade : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_dm_conf(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		switch ((DM_CONF_FUNCTION)reader.Read<uint>(4uL))
		{
		case DM_CONF_FUNCTION.load_l1_conf:
		{
			SHAPE_REGISTER value11 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			L2_DATATYPE value12 = (L2_DATATYPE)reader.Read<uint>(2uL);
			L1_TYPE value13 = (L1_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(8uL);
			StringBuilder stringBuilder22 = new StringBuilder();
			stringBuilder22.AppendLine("inst dm_load_l1_conf");
			stringBuilder22.AppendLine("    opcode : dm_conf");
			StringBuilder stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder23 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_s : ");
			handler.AppendFormatted(value11);
			stringBuilder25.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder26 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    datatype : ");
			handler.AppendFormatted(value12);
			stringBuilder26.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder27 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("    l1_type : ");
			handler.AppendFormatted(value13);
			stringBuilder27.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder22.ToString();
		}
		case DM_CONF_FUNCTION.load_w_conf:
		{
			int value8 = reader.Read<int>(5uL);
			int value9 = reader.Read<int>(5uL);
			GP_REGISTER value10 = (GP_REGISTER)reader.Read<uint>(5uL);
			StringBuilder stringBuilder16 = new StringBuilder();
			stringBuilder16.AppendLine("inst dm_load_w_conf");
			stringBuilder16.AppendLine("    opcode : dm_conf");
			StringBuilder stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder17 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder17.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder18 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder18.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder19 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    kernel_h : ");
			handler.AppendFormatted(value8);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    kernel_w : ");
			handler.AppendFormatted(value9);
			stringBuilder20.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder21 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_oc : ");
			handler.AppendFormatted(value10);
			stringBuilder21.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder16.ToString();
		}
		case DM_CONF_FUNCTION.load_w_conf2:
		{
			GP_REGISTER value6 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value7 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder11 = new StringBuilder();
			stringBuilder11.AppendLine("inst dm_load_w_conf2");
			stringBuilder11.AppendLine("    opcode : dm_conf");
			StringBuilder stringBuilder2 = stringBuilder11;
			StringBuilder stringBuilder12 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder11;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder13.AppendLine(ref handler);
			stringBuilder2 = stringBuilder11;
			StringBuilder stringBuilder14 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("    rgroups : ");
			handler.AppendFormatted(value6);
			stringBuilder14.AppendLine(ref handler);
			stringBuilder2 = stringBuilder11;
			StringBuilder stringBuilder15 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    rgoc : ");
			handler.AppendFormatted(value7);
			stringBuilder15.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder11.ToString();
		}
		case DM_CONF_FUNCTION.load_w_conf_deq:
		{
			QUANT_TYPE value5 = (QUANT_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(13uL);
			StringBuilder stringBuilder7 = new StringBuilder();
			stringBuilder7.AppendLine("inst dm_load_w_conf_deq");
			stringBuilder7.AppendLine("    opcode : dm_conf");
			StringBuilder stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder8 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder9.AppendLine(ref handler);
			stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value5);
			stringBuilder10.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder7.ToString();
		}
		case DM_CONF_FUNCTION.store_of_conf:
		{
			SHAPE_REGISTER value3 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			L2_DATATYPE value4 = (L2_DATATYPE)reader.Read<uint>(2uL);
			reader.Read<int>(10uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst dm_store_of_conf");
			stringBuilder.AppendLine("    opcode : dm_conf");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_d : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    datatype : ");
			handler.AppendFormatted(value4);
			stringBuilder6.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_dm_load_l1(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value4 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value5 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		L1_TYPE value6 = (L1_TYPE)reader.Read<uint>(2uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst dm_load_l1");
		stringBuilder.AppendLine("    opcode : dm_load_l1");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    pu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    rhtoc_window : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    l1_type : ");
		handler.AppendFormatted(value6);
		stringBuilder8.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_dm_load_w(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value4 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value5 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		DM_LOAD_W_DEST value6 = (DM_LOAD_W_DEST)reader.Read<uint>(2uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst dm_load_w");
		stringBuilder.AppendLine("    opcode : dm_load_w");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    pu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_bw : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    r_iochannels : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    dest_type : ");
		handler.AppendFormatted(value6);
		stringBuilder8.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_dm_load_act0(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value4 = (GP_REGISTER)reader.Read<uint>(5uL);
		ACT0_CHANNEL value5 = (ACT0_CHANNEL)reader.Read<uint>(1uL);
		int value6 = reader.Read<int>(1uL);
		reader.Read<int>(7uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst dm_load_act0");
		stringBuilder.AppendLine("    opcode : dm_load_act0");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    pu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
		handler.AppendLiteral("    rlen : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
		handler.AppendLiteral("    dest_channel : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
		handler.AppendLiteral("    is_by_channel : ");
		handler.AppendFormatted(value6);
		stringBuilder8.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_dm_store_of(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value4 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		ACT0_CHANNEL value5 = (ACT0_CHANNEL)reader.Read<uint>(1uL);
		reader.Read<int>(10uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst dm_store_of");
		stringBuilder.AppendLine("    opcode : dm_store_of");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    pu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("    src_channel : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_pu_conf(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		switch ((PU_CONF_FUNCTION)reader.Read<uint>(4uL))
		{
		case PU_CONF_FUNCTION.pu_fetchif_conf1:
		{
			int value25 = reader.Read<int>(5uL);
			int value26 = reader.Read<int>(5uL);
			SHAPE_REGISTER value27 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder48 = new StringBuilder();
			stringBuilder48.AppendLine("inst pu_fetchif_conf1");
			stringBuilder48.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder48;
			StringBuilder stringBuilder49 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder49.AppendLine(ref handler);
			stringBuilder2 = stringBuilder48;
			StringBuilder stringBuilder50 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder50.AppendLine(ref handler);
			stringBuilder2 = stringBuilder48;
			StringBuilder stringBuilder51 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    stride_w : ");
			handler.AppendFormatted(value25);
			stringBuilder51.AppendLine(ref handler);
			stringBuilder2 = stringBuilder48;
			StringBuilder stringBuilder52 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    stride_h : ");
			handler.AppendFormatted(value26);
			stringBuilder52.AppendLine(ref handler);
			stringBuilder2 = stringBuilder48;
			StringBuilder stringBuilder53 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_s : ");
			handler.AppendFormatted(value27);
			stringBuilder53.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder48.ToString();
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf2:
		{
			GP_REGISTER value23 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value24 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder43 = new StringBuilder();
			stringBuilder43.AppendLine("inst pu_fetchif_conf2");
			stringBuilder43.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder43;
			StringBuilder stringBuilder44 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder44.AppendLine(ref handler);
			stringBuilder2 = stringBuilder43;
			StringBuilder stringBuilder45 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder45.AppendLine(ref handler);
			stringBuilder2 = stringBuilder43;
			StringBuilder stringBuilder46 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    rgic : ");
			handler.AppendFormatted(value23);
			stringBuilder46.AppendLine(ref handler);
			stringBuilder2 = stringBuilder43;
			StringBuilder stringBuilder47 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rgic_last : ");
			handler.AppendFormatted(value24);
			stringBuilder47.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder43.ToString();
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf3:
		{
			GP_REGISTER value20 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value21 = (GP_REGISTER)reader.Read<uint>(5uL);
			SHAPE_REGISTER value22 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder37 = new StringBuilder();
			stringBuilder37.AppendLine("inst pu_fetchif_conf3");
			stringBuilder37.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder37;
			StringBuilder stringBuilder38 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder38.AppendLine(ref handler);
			stringBuilder2 = stringBuilder37;
			StringBuilder stringBuilder39 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder39.AppendLine(ref handler);
			stringBuilder2 = stringBuilder37;
			StringBuilder stringBuilder40 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("    raddr_s : ");
			handler.AppendFormatted(value20);
			stringBuilder40.AppendLine(ref handler);
			stringBuilder2 = stringBuilder37;
			StringBuilder stringBuilder41 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("    rgroups : ");
			handler.AppendFormatted(value21);
			stringBuilder41.AppendLine(ref handler);
			stringBuilder2 = stringBuilder37;
			StringBuilder stringBuilder42 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rshape : ");
			handler.AppendFormatted(value22);
			stringBuilder42.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder37.ToString();
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf4:
		{
			GP_REGISTER value18 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			SHAPE_REGISTER value19 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder32 = new StringBuilder();
			stringBuilder32.AppendLine("inst pu_fetchif_conf4");
			stringBuilder32.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder32;
			StringBuilder stringBuilder33 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder33.AppendLine(ref handler);
			stringBuilder2 = stringBuilder32;
			StringBuilder stringBuilder34 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder34.AppendLine(ref handler);
			stringBuilder2 = stringBuilder32;
			StringBuilder stringBuilder35 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rpad_value : ");
			handler.AppendFormatted(value18);
			stringBuilder35.AppendLine(ref handler);
			stringBuilder2 = stringBuilder32;
			StringBuilder stringBuilder36 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    sspad : ");
			handler.AppendFormatted(value19);
			stringBuilder36.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder32.ToString();
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf_deq:
		{
			GP_REGISTER value15 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value16 = (GP_REGISTER)reader.Read<uint>(5uL);
			QUANT_TYPE value17 = (QUANT_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(3uL);
			StringBuilder stringBuilder26 = new StringBuilder();
			stringBuilder26.AppendLine("inst pu_fetchif_conf_deq");
			stringBuilder26.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder27 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder27.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder28 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder28.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder29 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    ric : ");
			handler.AppendFormatted(value15);
			stringBuilder29.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder30 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rbx : ");
			handler.AppendFormatted(value16);
			stringBuilder30.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder31 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value17);
			stringBuilder31.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder26.ToString();
		}
		case PU_CONF_FUNCTION.pu_w_conf:
		{
			int value13 = reader.Read<int>(5uL);
			int value14 = reader.Read<int>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder21 = new StringBuilder();
			stringBuilder21.AppendLine("inst pu_w_conf");
			stringBuilder21.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder22 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder22.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder23 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    kernel_h : ");
			handler.AppendFormatted(value13);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    kernel_w : ");
			handler.AppendFormatted(value14);
			stringBuilder25.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder21.ToString();
		}
		case PU_CONF_FUNCTION.pu_of_conf1:
		{
			GP_REGISTER value10 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value11 = (GP_REGISTER)reader.Read<uint>(5uL);
			SHAPE_REGISTER value12 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder15 = new StringBuilder();
			stringBuilder15.AppendLine("inst pu_of_conf1");
			stringBuilder15.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder15;
			StringBuilder stringBuilder16 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder16.AppendLine(ref handler);
			stringBuilder2 = stringBuilder15;
			StringBuilder stringBuilder17 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder17.AppendLine(ref handler);
			stringBuilder2 = stringBuilder15;
			StringBuilder stringBuilder18 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    rgoc : ");
			handler.AppendFormatted(value10);
			stringBuilder18.AppendLine(ref handler);
			stringBuilder2 = stringBuilder15;
			StringBuilder stringBuilder19 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rgoc_last : ");
			handler.AppendFormatted(value11);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder15;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_d : ");
			handler.AppendFormatted(value12);
			stringBuilder20.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder15.ToString();
		}
		case PU_CONF_FUNCTION.pu_of_conf2:
		{
			GP_REGISTER value8 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			SHAPE_REGISTER value9 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder10 = new StringBuilder();
			stringBuilder10.AppendLine("inst pu_of_conf2");
			stringBuilder10.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder11 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("    raddr_d : ");
			handler.AppendFormatted(value8);
			stringBuilder13.AppendLine(ref handler);
			stringBuilder2 = stringBuilder10;
			StringBuilder stringBuilder14 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    rshape_d : ");
			handler.AppendFormatted(value9);
			stringBuilder14.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder10.ToString();
		}
		case PU_CONF_FUNCTION.pu_compute_conf:
		{
			int value3 = reader.Read<int>(1uL);
			int value4 = reader.Read<int>(1uL);
			PU_OUTPUT_DEST value5 = (PU_OUTPUT_DEST)reader.Read<uint>(1uL);
			int value6 = reader.Read<int>(1uL);
			PU_COMPUTE_MODE value7 = (PU_COMPUTE_MODE)reader.Read<uint>(1uL);
			reader.Read<int>(10uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst pu_compute_conf");
			stringBuilder.AppendLine("    opcode : pu_conf");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    load_psum : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    clr_psum : ");
			handler.AppendFormatted(value4);
			stringBuilder6.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
			handler.AppendLiteral("    dest_target : ");
			handler.AppendFormatted(value5);
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    release_if : ");
			handler.AppendFormatted(value6);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    mode : ");
			handler.AppendFormatted(value7);
			stringBuilder9.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_pu_compute(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		PU_OF_SHIFT_MODE value2 = (PU_OF_SHIFT_MODE)reader.Read<uint>(2uL);
		reader.Read<int>(4uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst pu_compute");
		stringBuilder.AppendLine("    opcode : pu_compute");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
		handler.AppendLiteral("    of_shift_mode : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_pu_forward_psum(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value4 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(9uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst pu_forward_psum");
		stringBuilder.AppendLine("    opcode : pu_forward_psum");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    pu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    raddr : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
		handler.AppendLiteral("    rlen : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_pu_pdp0_conf(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		switch ((PU_PDP0_CONF_FUNCTION)reader.Read<uint>(4uL))
		{
		case PU_PDP0_CONF_FUNCTION.pdp0_mode_conf:
		{
			PU_PDP0_MODE value16 = (PU_PDP0_MODE)reader.Read<uint>(3uL);
			reader.Read<int>(12uL);
			StringBuilder stringBuilder36 = new StringBuilder();
			stringBuilder36.AppendLine("inst pu_pdp0_mode_conf");
			stringBuilder36.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder36;
			StringBuilder stringBuilder37 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder37.AppendLine(ref handler);
			stringBuilder2 = stringBuilder36;
			StringBuilder stringBuilder38 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder38.AppendLine(ref handler);
			stringBuilder2 = stringBuilder36;
			StringBuilder stringBuilder39 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    mode : ");
			handler.AppendFormatted(value16);
			stringBuilder39.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder36.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf1:
		{
			int value14 = reader.Read<int>(5uL);
			int value15 = reader.Read<int>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder31 = new StringBuilder();
			stringBuilder31.AppendLine("inst pu_pdp0_fetchif_conf1");
			stringBuilder31.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder32 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder32.AppendLine(ref handler);
			stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder33 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder33.AppendLine(ref handler);
			stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder34 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    stride_w : ");
			handler.AppendFormatted(value14);
			stringBuilder34.AppendLine(ref handler);
			stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder35 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    stride_h : ");
			handler.AppendFormatted(value15);
			stringBuilder35.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder31.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf2:
		{
			GP_REGISTER value12 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value13 = (GP_REGISTER)reader.Read<uint>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder26 = new StringBuilder();
			stringBuilder26.AppendLine("inst pu_pdp0_fetchif_conf2");
			stringBuilder26.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder27 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder27.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder28 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder28.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder29 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    rgic : ");
			handler.AppendFormatted(value12);
			stringBuilder29.AppendLine(ref handler);
			stringBuilder2 = stringBuilder26;
			StringBuilder stringBuilder30 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rgic_last : ");
			handler.AppendFormatted(value13);
			stringBuilder30.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder26.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf3:
		{
			reader.Read<int>(10uL);
			SHAPE_REGISTER value11 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder22 = new StringBuilder();
			stringBuilder22.AppendLine("inst pu_pdp0_fetchif_conf3");
			stringBuilder22.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder23 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder22;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rshape : ");
			handler.AppendFormatted(value11);
			stringBuilder25.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder22.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf4:
		{
			GP_REGISTER value9 = (GP_REGISTER)reader.Read<uint>(5uL);
			SHAPE_REGISTER value10 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(7uL);
			StringBuilder stringBuilder17 = new StringBuilder();
			stringBuilder17.AppendLine("inst pu_pdp0_fetchif_conf4");
			stringBuilder17.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder17;
			StringBuilder stringBuilder18 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder18.AppendLine(ref handler);
			stringBuilder2 = stringBuilder17;
			StringBuilder stringBuilder19 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder17;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rpad_value : ");
			handler.AppendFormatted(value9);
			stringBuilder20.AppendLine(ref handler);
			stringBuilder2 = stringBuilder17;
			StringBuilder stringBuilder21 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    sspad : ");
			handler.AppendFormatted(value10);
			stringBuilder21.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder17.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_fetchif_conf_deq:
		{
			GP_REGISTER value7 = (GP_REGISTER)reader.Read<uint>(5uL);
			QUANT_TYPE value8 = (QUANT_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(8uL);
			StringBuilder stringBuilder12 = new StringBuilder();
			stringBuilder12.AppendLine("inst pu_pdp0_conf_deq");
			stringBuilder12.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder12;
			StringBuilder stringBuilder13 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder13.AppendLine(ref handler);
			stringBuilder2 = stringBuilder12;
			StringBuilder stringBuilder14 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder14.AppendLine(ref handler);
			stringBuilder2 = stringBuilder12;
			StringBuilder stringBuilder15 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    rbx : ");
			handler.AppendFormatted(value7);
			stringBuilder15.AppendLine(ref handler);
			stringBuilder2 = stringBuilder12;
			StringBuilder stringBuilder16 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value8);
			stringBuilder16.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder12.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_w_conf:
		{
			int value5 = reader.Read<int>(5uL);
			int value6 = reader.Read<int>(5uL);
			reader.Read<int>(5uL);
			StringBuilder stringBuilder7 = new StringBuilder();
			stringBuilder7.AppendLine("inst pu_pdp0_w_conf");
			stringBuilder7.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder8 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder9.AppendLine(ref handler);
			stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    kernel_h : ");
			handler.AppendFormatted(value5);
			stringBuilder10.AppendLine(ref handler);
			stringBuilder2 = stringBuilder7;
			StringBuilder stringBuilder11 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    kernel_w : ");
			handler.AppendFormatted(value6);
			stringBuilder11.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder7.ToString();
		}
		case PU_PDP0_CONF_FUNCTION.pdp0_of_conf:
		{
			SHAPE_REGISTER value3 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			SHAPE_REGISTER value4 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(9uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst pu_pdp0_of_conf");
			stringBuilder.AppendLine("    opcode : pu_pdp0_conf");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    tcu_id : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    pu_id : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_d : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    rshape_d : ");
			handler.AppendFormatted(value4);
			stringBuilder6.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_pu_pdp0_compute(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(1uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst pu_pdp0_compute");
		stringBuilder.AppendLine("    opcode : pu_pdp0_compute");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	private string visit_act0_conf(ref BitReader reader, ref ulong pc_offset)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		reader.Read<uint>(3uL);
		ACT0_CHANNEL value3 = (ACT0_CHANNEL)reader.Read<uint>(1uL);
		SHAPE_REGISTER value4 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(5uL);
		reader.Read<int>(7uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst act0_src1_conf");
		stringBuilder.AppendLine("    opcode : act0_conf");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("    pu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    channel : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_act0_compute(ref BitReader reader, ref ulong pc_offset)
	{
		reader.Read<int>(5uL);
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		int value2 = reader.Read<int>(3uL);
		ACT0_CHANNEL value3 = (ACT0_CHANNEL)reader.Read<uint>(1uL);
		ACT0_OUTPUT_DEST value4 = (ACT0_OUTPUT_DEST)reader.Read<uint>(2uL);
		ACT_OUTPUT_TYPE value5 = (ACT_OUTPUT_TYPE)reader.Read<uint>(2uL);
		int value6 = reader.Read<int>(1uL);
		reader.Read<int>(6uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst act0_compute");
		stringBuilder.AppendLine("    opcode : act0_compute");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    tcu_id : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    channel : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    target : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
		handler.AppendLiteral("    dest_datatype : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
		handler.AppendLiteral("    is_by_channel : ");
		handler.AppendFormatted(value6);
		stringBuilder8.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_mfu_memcpy(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value3 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		SHAPE_REGISTER value4 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		SHAPE_REGISTER value5 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(6uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mfu_memcpy");
		stringBuilder.AppendLine("    opcode : mfu_memcpy");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    rstride_d : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    rstride_s : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_mfu_memset(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value3 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		SHAPE_REGISTER value4 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		L2_DATATYPE value5 = (L2_DATATYPE)reader.Read<uint>(2uL);
		reader.Read<int>(7uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mfu_memset");
		stringBuilder.AppendLine("    opcode : mfu_memset");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
		handler.AppendLiteral("    rv : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    rstride : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("    l2_datatype : ");
		handler.AppendFormatted(value5);
		stringBuilder7.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_mfu_conf(ref BitReader reader, ref ulong pc_offset)
	{
		switch ((MFU_CONF_FUNCTION)reader.Read<uint>(5uL))
		{
		case MFU_CONF_FUNCTION.transpose_conf:
		{
			SHAPE_REGISTER value47 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			SHAPE_REGISTER value48 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			L2_DATATYPE value49 = (L2_DATATYPE)reader.Read<uint>(2uL);
			MFU_TRANS_PERMUTE value50 = (MFU_TRANS_PERMUTE)reader.Read<uint>(5uL);
			reader.Read<int>(7uL);
			StringBuilder stringBuilder61 = new StringBuilder();
			stringBuilder61.AppendLine("inst mfu_transpose_conf");
			stringBuilder61.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder61;
			StringBuilder stringBuilder62 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_d : ");
			handler.AppendFormatted(value47);
			stringBuilder62.AppendLine(ref handler);
			stringBuilder2 = stringBuilder61;
			StringBuilder stringBuilder63 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_s : ");
			handler.AppendFormatted(value48);
			stringBuilder63.AppendLine(ref handler);
			stringBuilder2 = stringBuilder61;
			StringBuilder stringBuilder64 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
			handler.AppendLiteral("    l2_datatype : ");
			handler.AppendFormatted(value49);
			stringBuilder64.AppendLine(ref handler);
			stringBuilder2 = stringBuilder61;
			StringBuilder stringBuilder65 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
			handler.AppendLiteral("    permute : ");
			handler.AppendFormatted(value50);
			stringBuilder65.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder61.ToString();
		}
		case MFU_CONF_FUNCTION.pdp1_conf1:
		{
			int value42 = reader.Read<int>(5uL);
			int value43 = reader.Read<int>(5uL);
			SHAPE_REGISTER value44 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			PDP_FUNCTION value45 = (PDP_FUNCTION)reader.Read<uint>(2uL);
			SHAPE_REGISTER value46 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder55 = new StringBuilder();
			stringBuilder55.AppendLine("inst mfu_pdp1_conf1");
			stringBuilder55.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder55;
			StringBuilder stringBuilder56 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    stride_w : ");
			handler.AppendFormatted(value42);
			stringBuilder56.AppendLine(ref handler);
			stringBuilder2 = stringBuilder55;
			StringBuilder stringBuilder57 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    stride_h : ");
			handler.AppendFormatted(value43);
			stringBuilder57.AppendLine(ref handler);
			stringBuilder2 = stringBuilder55;
			StringBuilder stringBuilder58 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_s : ");
			handler.AppendFormatted(value44);
			stringBuilder58.AppendLine(ref handler);
			stringBuilder2 = stringBuilder55;
			StringBuilder stringBuilder59 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    funct2 : ");
			handler.AppendFormatted(value45);
			stringBuilder59.AppendLine(ref handler);
			stringBuilder2 = stringBuilder55;
			StringBuilder stringBuilder60 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_d : ");
			handler.AppendFormatted(value46);
			stringBuilder60.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder55.ToString();
		}
		case MFU_CONF_FUNCTION.pdp1_conf2:
		{
			GP_REGISTER value38 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value39 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value40 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value41 = (GP_REGISTER)reader.Read<uint>(5uL);
			StringBuilder stringBuilder50 = new StringBuilder();
			stringBuilder50.AppendLine("inst mfu_pdp1_conf2");
			stringBuilder50.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder50;
			StringBuilder stringBuilder51 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    rcount_w : ");
			handler.AppendFormatted(value38);
			stringBuilder51.AppendLine(ref handler);
			stringBuilder2 = stringBuilder50;
			StringBuilder stringBuilder52 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("    rcount_h : ");
			handler.AppendFormatted(value39);
			stringBuilder52.AppendLine(ref handler);
			stringBuilder2 = stringBuilder50;
			StringBuilder stringBuilder53 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    rpe_h : ");
			handler.AppendFormatted(value40);
			stringBuilder53.AppendLine(ref handler);
			stringBuilder2 = stringBuilder50;
			StringBuilder stringBuilder54 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rpe_last_h : ");
			handler.AppendFormatted(value41);
			stringBuilder54.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder50.ToString();
		}
		case MFU_CONF_FUNCTION.pdp1_conf3:
		{
			GP_REGISTER value34 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value35 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value36 = (GP_REGISTER)reader.Read<uint>(5uL);
			SHAPE_REGISTER value37 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder45 = new StringBuilder();
			stringBuilder45.AppendLine("inst mfu_pdp1_conf3");
			stringBuilder45.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder45;
			StringBuilder stringBuilder46 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
			handler.AppendLiteral("    rpe_channels : ");
			handler.AppendFormatted(value34);
			stringBuilder46.AppendLine(ref handler);
			stringBuilder2 = stringBuilder45;
			StringBuilder stringBuilder47 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2);
			handler.AppendLiteral("    rpe_last_channels : ");
			handler.AppendFormatted(value35);
			stringBuilder47.AppendLine(ref handler);
			stringBuilder2 = stringBuilder45;
			StringBuilder stringBuilder48 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rpad_value : ");
			handler.AppendFormatted(value36);
			stringBuilder48.AppendLine(ref handler);
			stringBuilder2 = stringBuilder45;
			StringBuilder stringBuilder49 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    sspad : ");
			handler.AppendFormatted(value37);
			stringBuilder49.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder45.ToString();
		}
		case MFU_CONF_FUNCTION.pdp1_conf4:
		{
			GP_REGISTER value29 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value30 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value31 = (GP_REGISTER)reader.Read<uint>(5uL);
			int value32 = reader.Read<int>(1uL);
			int value33 = reader.Read<int>(1uL);
			reader.Read<int>(3uL);
			StringBuilder stringBuilder39 = new StringBuilder();
			stringBuilder39.AppendLine("inst mfu_pdp1_conf4");
			stringBuilder39.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder39;
			StringBuilder stringBuilder40 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rwindow_w : ");
			handler.AppendFormatted(value29);
			stringBuilder40.AppendLine(ref handler);
			stringBuilder2 = stringBuilder39;
			StringBuilder stringBuilder41 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    rwindow_h : ");
			handler.AppendFormatted(value30);
			stringBuilder41.AppendLine(ref handler);
			stringBuilder2 = stringBuilder39;
			StringBuilder stringBuilder42 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rscale : ");
			handler.AppendFormatted(value31);
			stringBuilder42.AppendLine(ref handler);
			stringBuilder2 = stringBuilder39;
			StringBuilder stringBuilder43 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    enable_h2c : ");
			handler.AppendFormatted(value32);
			stringBuilder43.AppendLine(ref handler);
			stringBuilder2 = stringBuilder39;
			StringBuilder stringBuilder44 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    enable_bw : ");
			handler.AppendFormatted(value33);
			stringBuilder44.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder39.ToString();
		}
		case MFU_CONF_FUNCTION.pdp1_conf_deq:
		{
			GP_REGISTER value26 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value27 = (GP_REGISTER)reader.Read<uint>(5uL);
			QUANT_TYPE value28 = (QUANT_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(5uL);
			reader.Read<int>(3uL);
			StringBuilder stringBuilder35 = new StringBuilder();
			stringBuilder35.AppendLine("inst mfu_pdp1_conf_deq");
			stringBuilder35.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder35;
			StringBuilder stringBuilder36 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rscale : ");
			handler.AppendFormatted(value26);
			stringBuilder36.AppendLine(ref handler);
			stringBuilder2 = stringBuilder35;
			StringBuilder stringBuilder37 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    rbias : ");
			handler.AppendFormatted(value27);
			stringBuilder37.AppendLine(ref handler);
			stringBuilder2 = stringBuilder35;
			StringBuilder stringBuilder38 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value28);
			stringBuilder38.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder35.ToString();
		}
		case MFU_CONF_FUNCTION.pdp1_conf_quant:
		{
			GP_REGISTER value23 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value24 = (GP_REGISTER)reader.Read<uint>(5uL);
			QUANT_TYPE value25 = (QUANT_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(5uL);
			reader.Read<int>(3uL);
			StringBuilder stringBuilder31 = new StringBuilder();
			stringBuilder31.AppendLine("inst mfu_pdp1_conf_quant");
			stringBuilder31.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder32 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rscale : ");
			handler.AppendFormatted(value23);
			stringBuilder32.AppendLine(ref handler);
			stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder33 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    rbias : ");
			handler.AppendFormatted(value24);
			stringBuilder33.AppendLine(ref handler);
			stringBuilder2 = stringBuilder31;
			StringBuilder stringBuilder34 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value25);
			stringBuilder34.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder31.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf_stride:
		{
			SHAPE_REGISTER value20 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			SHAPE_REGISTER value21 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			SHAPE_REGISTER value22 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(11uL);
			StringBuilder stringBuilder27 = new StringBuilder();
			stringBuilder27.AppendLine("inst mfu_act1_conf_stride");
			stringBuilder27.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder27;
			StringBuilder stringBuilder28 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_s1 : ");
			handler.AppendFormatted(value20);
			stringBuilder28.AppendLine(ref handler);
			stringBuilder2 = stringBuilder27;
			StringBuilder stringBuilder29 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_s2 : ");
			handler.AppendFormatted(value21);
			stringBuilder29.AppendLine(ref handler);
			stringBuilder2 = stringBuilder27;
			StringBuilder stringBuilder30 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    rstride_d1 : ");
			handler.AppendFormatted(value22);
			stringBuilder30.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder27.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf_src1:
		{
			GP_REGISTER value15 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value16 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value17 = (GP_REGISTER)reader.Read<uint>(5uL);
			int value18 = reader.Read<int>(1uL);
			SLICE_LOCATION value19 = (SLICE_LOCATION)reader.Read<uint>(1uL);
			reader.Read<int>(3uL);
			StringBuilder stringBuilder21 = new StringBuilder();
			stringBuilder21.AppendLine("inst mfu_act1_conf_src1");
			stringBuilder21.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder22 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rslice : ");
			handler.AppendFormatted(value15);
			stringBuilder22.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder23 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("    rright_repeats : ");
			handler.AppendFormatted(value16);
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("    rslice_repeats : ");
			handler.AppendFormatted(value17);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    sid : ");
			handler.AppendFormatted(value18);
			stringBuilder25.AppendLine(ref handler);
			stringBuilder2 = stringBuilder21;
			StringBuilder stringBuilder26 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
			handler.AppendLiteral("    slice_loc : ");
			handler.AppendFormatted(value19);
			stringBuilder26.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder21.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf_src2:
		{
			GP_REGISTER value11 = (GP_REGISTER)reader.Read<uint>(5uL);
			SHAPE_REGISTER value12 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			int value13 = reader.Read<int>(1uL);
			ACT1_SOURCE_TYPE value14 = (ACT1_SOURCE_TYPE)reader.Read<uint>(1uL);
			reader.Read<int>(10uL);
			StringBuilder stringBuilder16 = new StringBuilder();
			stringBuilder16.AppendLine("inst mfu_act1_conf_src2");
			stringBuilder16.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder17 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
			handler.AppendLiteral("    rleft_repeats : ");
			handler.AppendFormatted(value11);
			stringBuilder17.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder18 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rshape : ");
			handler.AppendFormatted(value12);
			stringBuilder18.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder19 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    sid : ");
			handler.AppendFormatted(value13);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder2 = stringBuilder16;
			StringBuilder stringBuilder20 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
			handler.AppendLiteral("    source_type : ");
			handler.AppendFormatted(value14);
			stringBuilder20.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder16.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf_dest:
		{
			GP_REGISTER value9 = (GP_REGISTER)reader.Read<uint>(5uL);
			SHAPE_REGISTER value10 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
			reader.Read<int>(12uL);
			StringBuilder stringBuilder13 = new StringBuilder();
			stringBuilder13.AppendLine("inst mfu_act1_conf_dest");
			stringBuilder13.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder13;
			StringBuilder stringBuilder14 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendLiteral("    rlen : ");
			handler.AppendFormatted(value9);
			stringBuilder14.AppendLine(ref handler);
			stringBuilder2 = stringBuilder13;
			StringBuilder stringBuilder15 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rshape : ");
			handler.AppendFormatted(value10);
			stringBuilder15.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder13.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf_deq:
		{
			GP_REGISTER value5 = (GP_REGISTER)reader.Read<uint>(5uL);
			GP_REGISTER value6 = (GP_REGISTER)reader.Read<uint>(5uL);
			QUANT_TYPE value7 = (QUANT_TYPE)reader.Read<uint>(2uL);
			int value8 = reader.Read<int>(1uL);
			reader.Read<int>(5uL);
			reader.Read<int>(2uL);
			StringBuilder stringBuilder8 = new StringBuilder();
			stringBuilder8.AppendLine("inst mfu_act1_conf_deq");
			stringBuilder8.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder8;
			StringBuilder stringBuilder9 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    rscale : ");
			handler.AppendFormatted(value5);
			stringBuilder9.AppendLine(ref handler);
			stringBuilder2 = stringBuilder8;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
			handler.AppendLiteral("    rbias : ");
			handler.AppendFormatted(value6);
			stringBuilder10.AppendLine(ref handler);
			stringBuilder2 = stringBuilder8;
			StringBuilder stringBuilder11 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value7);
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder8;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("    sid : ");
			handler.AppendFormatted(value8);
			stringBuilder12.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder8.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf_quant:
		{
			QUANT_TYPE value4 = (QUANT_TYPE)reader.Read<uint>(2uL);
			reader.Read<int>(5uL);
			reader.Read<int>(13uL);
			StringBuilder stringBuilder6 = new StringBuilder();
			stringBuilder6.AppendLine("inst mfu_act1_conf_quant");
			stringBuilder6.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder6;
			StringBuilder stringBuilder7 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("    quant_type : ");
			handler.AppendFormatted(value4);
			stringBuilder7.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder6.ToString();
		}
		case MFU_CONF_FUNCTION.act1_conf:
		{
			MFU_ACT1_FUNCTION value = (MFU_ACT1_FUNCTION)reader.Read<uint>(4uL);
			int value2 = reader.Read<int>(1uL);
			int value3 = reader.Read<int>(1uL);
			reader.Read<int>(14uL);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("inst mfu_act1_conf");
			stringBuilder.AppendLine("    opcode : mfu_conf");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
			handler.AppendLiteral("    funct4 : ");
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
			handler.AppendLiteral("    is_by_channel : ");
			handler.AppendFormatted(value2);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("    is_16_segments : ");
			handler.AppendFormatted(value3);
			stringBuilder5.AppendLine(ref handler);
			pc_offset += 32uL;
			return stringBuilder.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string visit_mfu_transpose(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value3 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(12uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mfu_transpose");
		stringBuilder.AppendLine("    opcode : mfu_transpose");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_mfu_pdp1_compute(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		SHAPE_REGISTER value3 = (SHAPE_REGISTER)reader.Read<uint>(3uL);
		reader.Read<int>(12uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mfu_pdp1_compute");
		stringBuilder.AppendLine("    opcode : mfu_pdp1_compute");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("    rshape : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_mfu_act1_compute(ref BitReader reader, ref ulong pc_offset)
	{
		GP_REGISTER value = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value2 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value3 = (GP_REGISTER)reader.Read<uint>(5uL);
		GP_REGISTER value4 = (GP_REGISTER)reader.Read<uint>(5uL);
		reader.Read<int>(5uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst mfu_act1_compute");
		stringBuilder.AppendLine("    opcode : mfu_act1_compute");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_d1 : ");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s1 : ");
		handler.AppendFormatted(value2);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_s2 : ");
		handler.AppendFormatted(value3);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("    raddr_arg : ");
		handler.AppendFormatted(value4);
		stringBuilder6.AppendLine(ref handler);
		pc_offset += 32uL;
		return stringBuilder.ToString();
	}

	private string visit_ai2d_compute(ref BitReader reader, ref ulong pc_offset)
	{
		reader.Read<int>(9uL);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("inst ai2d_compute");
		stringBuilder.AppendLine("    opcode : ai2d_compute");
		pc_offset += 16uL;
		return stringBuilder.ToString();
	}

	public void DeSerialize(TextWriter asmWriter, TextWriter bwWriter, byte[] input, string functionName)
	{
		ulong pc_offset = 0uL;
		BitReader reader = new BitReader(input);
		while (reader.RemainBits != 0)
		{
			asmWriter.WriteLine($"pc offset: {pc_offset}");
			switch (reader.Read<OPCODE>(7uL))
			{
			case OPCODE.lui:
				asmWriter.Write(visit_lui(ref reader, ref pc_offset));
				break;
			case OPCODE.auipc:
				asmWriter.Write(visit_auipc(ref reader, ref pc_offset));
				break;
			case OPCODE.arithm_imm:
				asmWriter.Write(visit_arithm_imm(ref reader, ref pc_offset));
				break;
			case OPCODE.arithm:
				asmWriter.Write(visit_arithm(ref reader, ref pc_offset));
				break;
			case OPCODE.load:
				asmWriter.Write(visit_load(ref reader, ref pc_offset));
				break;
			case OPCODE.store:
				asmWriter.Write(visit_store(ref reader, ref pc_offset));
				break;
			case OPCODE.branch:
				asmWriter.Write(visit_branch(ref reader, ref pc_offset));
				break;
			case OPCODE.jal:
				asmWriter.Write(visit_jal(ref reader, ref pc_offset));
				break;
			case OPCODE.jalr:
				asmWriter.Write(visit_jalr(ref reader, ref pc_offset));
				break;
			case OPCODE.intr:
				asmWriter.Write(visit_intr(ref reader, ref pc_offset));
				break;
			case OPCODE.end:
				asmWriter.Write(visit_end(ref reader, ref pc_offset));
				break;
			case OPCODE.fence:
				asmWriter.Write(visit_fence(ref reader, ref pc_offset));
				break;
			case OPCODE.fence_i:
				asmWriter.Write(visit_fence_i(ref reader, ref pc_offset));
				break;
			case OPCODE.extrw:
				asmWriter.Write(visit_extrw(ref reader, ref pc_offset));
				break;
			case OPCODE.extraw:
				asmWriter.Write(visit_extraw(ref reader, ref pc_offset));
				break;
			case OPCODE.ccr_decl:
				asmWriter.Write(visit_ccr_decl(ref reader, ref pc_offset));
				break;
			case OPCODE.ccr_set:
				asmWriter.Write(visit_ccr_set(ref reader, ref pc_offset));
				break;
			case OPCODE.ccr_clr:
				asmWriter.Write(visit_ccr_clr(ref reader, ref pc_offset));
				break;
			case OPCODE.mmu_conf:
				asmWriter.Write(visit_mmu_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.mmu_set_id:
				asmWriter.Write(visit_mmu_set_id(ref reader, ref pc_offset));
				break;
			case OPCODE.ss_pack_shape:
				asmWriter.Write(visit_ss_pack_shape(ref reader, ref pc_offset));
				break;
			case OPCODE.ss_pack_stride:
				asmWriter.Write(visit_ss_pack_stride(ref reader, ref pc_offset));
				break;
			case OPCODE.l2_load_conf:
				asmWriter.Write(visit_l2_load_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.l2_load_w_conf:
				asmWriter.Write(visit_l2_load_w_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.l2_load_w:
				asmWriter.Write(visit_l2_load_w(ref reader, ref pc_offset));
				break;
			case OPCODE.l2_store_conf:
				asmWriter.Write(visit_l2_store_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.l2_load:
				asmWriter.Write(visit_l2_load(ref reader, ref pc_offset));
				break;
			case OPCODE.l2_store:
				asmWriter.Write(visit_l2_store(ref reader, ref pc_offset));
				break;
			case OPCODE.dm_conf_broadcast:
				asmWriter.Write(visit_dm_conf_broadcast(ref reader, ref pc_offset));
				break;
			case OPCODE.dm_conf:
				asmWriter.Write(visit_dm_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.dm_load_l1:
				asmWriter.Write(visit_dm_load_l1(ref reader, ref pc_offset));
				break;
			case OPCODE.dm_load_w:
				asmWriter.Write(visit_dm_load_w(ref reader, ref pc_offset));
				break;
			case OPCODE.dm_load_act0:
				asmWriter.Write(visit_dm_load_act0(ref reader, ref pc_offset));
				break;
			case OPCODE.dm_store_of:
				asmWriter.Write(visit_dm_store_of(ref reader, ref pc_offset));
				break;
			case OPCODE.pu_conf:
				asmWriter.Write(visit_pu_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.pu_compute:
				asmWriter.Write(visit_pu_compute(ref reader, ref pc_offset));
				break;
			case OPCODE.pu_forward_psum:
				asmWriter.Write(visit_pu_forward_psum(ref reader, ref pc_offset));
				break;
			case OPCODE.pu_pdp0_conf:
				asmWriter.Write(visit_pu_pdp0_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.pu_pdp0_compute:
				asmWriter.Write(visit_pu_pdp0_compute(ref reader, ref pc_offset));
				break;
			case OPCODE.act0_conf:
				asmWriter.Write(visit_act0_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.act0_compute:
				asmWriter.Write(visit_act0_compute(ref reader, ref pc_offset));
				break;
			case OPCODE.mfu_memcpy:
				asmWriter.Write(visit_mfu_memcpy(ref reader, ref pc_offset));
				break;
			case OPCODE.mfu_memset:
				asmWriter.Write(visit_mfu_memset(ref reader, ref pc_offset));
				break;
			case OPCODE.mfu_conf:
				asmWriter.Write(visit_mfu_conf(ref reader, ref pc_offset));
				break;
			case OPCODE.mfu_transpose:
				asmWriter.Write(visit_mfu_transpose(ref reader, ref pc_offset));
				break;
			case OPCODE.mfu_pdp1_compute:
				asmWriter.Write(visit_mfu_pdp1_compute(ref reader, ref pc_offset));
				break;
			case OPCODE.mfu_act1_compute:
				asmWriter.Write(visit_mfu_act1_compute(ref reader, ref pc_offset));
				break;
			case OPCODE.ai2d_compute:
				asmWriter.Write(visit_ai2d_compute(ref reader, ref pc_offset));
				break;
			default:
				throw new ArgumentOutOfRangeException("BadInstruction OpCode");
			}
			asmWriter.WriteLine("    binding_gnne_fusion_addr : 0");
			asmWriter.WriteLine("    binding_gnne_fusion_name : " + functionName);
		}
		WriteBandWidth(bwWriter, functionName);
	}
}
