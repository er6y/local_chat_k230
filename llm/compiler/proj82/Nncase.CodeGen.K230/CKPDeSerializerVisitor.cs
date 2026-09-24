using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nncase.IO;
using Nncase.TIR.Instructions;

namespace Nncase.CodeGen.K230;

public class CKPDeSerializerVisitor
{
	private string de_stride(ref BitReader br)
	{
		uint value = br.Read<uint>(16uL);
		uint value2 = br.Read<uint>(16uL);
		uint value3 = br.Read<uint>(16uL);
		return $"({value3}, {value2}, {value})";
	}

	private string de_shape(ref BitReader br)
	{
		uint value = br.Read<uint>(16uL);
		uint value2 = br.Read<uint>(16uL);
		uint value3 = br.Read<uint>(16uL);
		uint value4 = br.Read<uint>(16uL);
		return $"({value4}, {value3}, {value2}, {value})";
	}

	private string de_ddr_addr(ref BitReader br, ulong length = 32uL)
	{
		return br.Read<uint>(length).ToString();
	}

	private string de_glb_addr(ref BitReader br, ulong length = 22uL)
	{
		return br.Read<uint>(length).ToString();
	}

	private string de_serialize_l2_load_conf(ref BitReader reader)
	{
		string value = de_stride(ref reader);
		string value2 = de_stride(ref reader);
		L2_DATATYPE value3 = (L2_DATATYPE)reader.Read<uint>(2uL);
		DDR_DATATYPE value4 = (DDR_DATATYPE)reader.Read<uint>(3uL);
		return $"I.L2_LOAD_CONF({value}, {value2}, {value3}, {value4})";
	}

	private string de_serialize_l2_load(ref BitReader reader)
	{
		string value = de_glb_addr(ref reader, 22uL);
		string value2 = de_ddr_addr(ref reader, 32uL);
		string value3 = de_shape(ref reader);
		return $"I.L2_LOAD({value}, {value2}, {value3})";
	}

	private string de_serialize_l2_load_w_conf(ref BitReader reader)
	{
		uint value = reader.Read<uint>(21uL);
		uint value2 = reader.Read<uint>(21uL);
		L2_DATATYPE value3 = (L2_DATATYPE)reader.Read<uint>(2uL);
		DDR_DATATYPE value4 = (DDR_DATATYPE)reader.Read<uint>(3uL);
		int value5 = reader.Read<int>(1uL);
		return $"I.L2_LOAD_W_CONF({value}, {value2}, {value3}, {value4}, {value5})";
	}

	private string de_serialize_l2_load_w(ref BitReader reader)
	{
		string value = de_glb_addr(ref reader, 22uL);
		string value2 = de_ddr_addr(ref reader, 32uL);
		uint value3 = reader.Read<uint>(5uL);
		return $"I.L2_LOAD_W({value}, {value2}, {value3})";
	}

	private string de_serialize_l2_store_conf(ref BitReader reader)
	{
		string value = de_stride(ref reader);
		string value2 = de_stride(ref reader);
		L2_DATATYPE value3 = (L2_DATATYPE)reader.Read<uint>(2uL);
		DDR_DATATYPE value4 = (DDR_DATATYPE)reader.Read<uint>(3uL);
		return $"I.L2_STORE_CONF({value}, {value2}, {value3}, {value4})";
	}

	private string de_serialize_l2_store(ref BitReader reader)
	{
		string value = de_ddr_addr(ref reader, 32uL);
		string value2 = de_glb_addr(ref reader, 22uL);
		string value3 = de_shape(ref reader);
		return $"I.L2_STORE({value}, {value2}, {value3})";
	}

	private string de_serialize_pu_conf(ref BitReader reader)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		switch ((PU_CONF_FUNCTION)reader.Read<uint>(4uL))
		{
		case PU_CONF_FUNCTION.pu_fetchif_conf1:
		{
			int value30 = reader.Read<int>(5uL);
			int value31 = reader.Read<int>(5uL);
			int value32 = reader.Read<int>(11uL);
			int value33 = reader.Read<int>(11uL);
			return $"I.PU_FETCHIF_CONF1({value}, {value2}, {value30}, {value31}, ({value32},{value33}))";
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf2:
		{
			uint value28 = reader.Read<uint>(5uL);
			uint value29 = reader.Read<uint>(5uL);
			return $"I.PU_FETCHIF_CONF2({value}, {value2}, {value28}, {value29})";
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf3:
		{
			uint value24 = reader.Read<uint>(11uL);
			uint value25 = reader.Read<uint>(5uL);
			uint value26 = reader.Read<uint>(11uL);
			uint value27 = reader.Read<uint>(11uL);
			return $"I.PU_FETCHIF_CONF3({value}, {value2}, {value24}, {value25}, ({value26}, {value27}))";
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf4:
		{
			uint value19 = reader.Read<uint>(9uL);
			uint value20 = reader.Read<uint>(11uL);
			uint value21 = reader.Read<uint>(11uL);
			uint value22 = reader.Read<uint>(11uL);
			uint value23 = reader.Read<uint>(11uL);
			return $"I.PU_FETCHIF_CONF4({value}, {value2}, {value19}, (({value20}, {value21}), ({value22}, {value23})))";
		}
		case PU_CONF_FUNCTION.pu_fetchif_conf_deq:
		{
			uint value16 = reader.Read<uint>(5uL);
			uint value17 = reader.Read<uint>(8uL);
			QUANT_TYPE value18 = (QUANT_TYPE)reader.Read<uint>(2uL);
			return $"I.PU_FETCHIF_CONF_DEQ({value}, {value2}, {value16}, {value17}, {value18})";
		}
		case PU_CONF_FUNCTION.pu_w_conf:
		{
			int value14 = reader.Read<int>(5uL);
			int value15 = reader.Read<int>(5uL);
			return $"I.PU_W_CONF({value}, {value2}, {value14}, {value15})";
		}
		case PU_CONF_FUNCTION.pu_of_conf1:
		{
			uint value11 = reader.Read<uint>(5uL);
			uint value12 = reader.Read<uint>(11uL);
			uint value13 = reader.Read<uint>(11uL);
			return $"I.PU_OF_CONF1({value}, {value2}, {value11}, NULL, ({value12}, {value13}))";
		}
		case PU_CONF_FUNCTION.pu_of_conf2:
		{
			uint value8 = reader.Read<uint>(12uL);
			uint value9 = reader.Read<uint>(11uL);
			uint value10 = reader.Read<uint>(11uL);
			return $"I.PU_OF_CONF2({value}, {value2}, {value8}, ({value9},{value10}))";
		}
		case PU_CONF_FUNCTION.pu_compute_conf:
		{
			int value3 = reader.Read<int>(1uL);
			int value4 = reader.Read<int>(1uL);
			PU_OUTPUT_DEST value5 = (PU_OUTPUT_DEST)reader.Read<uint>(1uL);
			int value6 = reader.Read<int>(1uL);
			PU_COMPUTE_MODE value7 = (PU_COMPUTE_MODE)reader.Read<uint>(1uL);
			return $"I.PU_COMPUTE_CONF({value}, {value2}, {value3}, {value4}, {value5}, {value6}, {value7})";
		}
		default:
			throw new ArgumentOutOfRangeException("Bad Funct");
		}
	}

	private string de_serialize_pu_compute(ref BitReader reader)
	{
		int value = reader.Read<int>(3uL);
		PU_OF_SHIFT_MODE value2 = (PU_OF_SHIFT_MODE)reader.Read<uint>(2uL);
		return $"I.PU_COMPUTE({value}, {value2})";
	}

	private string de_serialize_pu_forward_psum(ref BitReader reader)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		uint value3 = reader.Read<uint>(12uL);
		uint value4 = reader.Read<uint>(11uL);
		return $"I.PU_FORWARD_PSUM({value}, {value2}, {value3}, {value4})";
	}

	private string de_serialize_act0_conf(ref BitReader reader)
	{
		int value = reader.Read<int>(3uL);
		int value2 = reader.Read<int>(3uL);
		reader.Read<uint>(3uL);
		ACT0_CHANNEL value3 = (ACT0_CHANNEL)reader.Read<uint>(1uL);
		uint value4 = reader.Read<uint>(16uL);
		uint value5 = reader.Read<uint>(16uL);
		uint value6 = reader.Read<uint>(6uL);
		uint value7 = reader.Read<uint>(5uL);
		return $"I.ACT0_SRC1_CONF(tcu_id: {value}, pu_id: {value2}, channel: {value3}, act_shape: ({value6}, {value5}, {value4}), rshift_bits: {value7})";
	}

	private string de_serialize_act0_compute(ref BitReader reader)
	{
		reader.Read<int>(5uL);
		uint value = reader.Read<uint>(12uL);
		int value2 = reader.Read<int>(3uL);
		ACT0_CHANNEL value3 = (ACT0_CHANNEL)reader.Read<uint>(1uL);
		ACT0_OUTPUT_DEST value4 = (ACT0_OUTPUT_DEST)reader.Read<uint>(2uL);
		ACT_OUTPUT_TYPE value5 = (ACT_OUTPUT_TYPE)reader.Read<uint>(2uL);
		int value6 = reader.Read<int>(1uL);
		return $"I.ACT0_COMPUTE(raddr_d: {value}, tcu_id: {value2}, channel: {value3}, target: {value4}, dest_datatype: {value5}, is_by_channel: {value6})";
	}

	public string DeSerialize(IEnumerable<byte[]> inputs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringWriter stringWriter = new StringWriter(stringBuilder);
		foreach (byte[] input in inputs)
		{
			BitReader reader = new BitReader(input);
			switch (reader.Read<OPCODE>(7uL))
			{
			case OPCODE.l2_load_conf:
				stringWriter.WriteLine(de_serialize_l2_load_conf(ref reader));
				break;
			case OPCODE.l2_load_w_conf:
				stringWriter.WriteLine(de_serialize_l2_load_w_conf(ref reader));
				break;
			case OPCODE.l2_load_w:
				stringWriter.WriteLine(de_serialize_l2_load_w(ref reader));
				break;
			case OPCODE.l2_store_conf:
				stringWriter.WriteLine(de_serialize_l2_store_conf(ref reader));
				break;
			case OPCODE.l2_load:
				stringWriter.WriteLine(de_serialize_l2_load(ref reader));
				break;
			case OPCODE.l2_store:
				stringWriter.WriteLine(de_serialize_l2_store(ref reader));
				break;
			case OPCODE.pu_conf:
				stringWriter.WriteLine(de_serialize_pu_conf(ref reader));
				break;
			case OPCODE.pu_compute:
				stringWriter.WriteLine(de_serialize_pu_compute(ref reader));
				break;
			case OPCODE.pu_forward_psum:
				stringWriter.WriteLine(de_serialize_pu_forward_psum(ref reader));
				break;
			case OPCODE.act0_conf:
				stringWriter.WriteLine(de_serialize_act0_conf(ref reader));
				break;
			case OPCODE.act0_compute:
				stringWriter.WriteLine(de_serialize_act0_compute(ref reader));
				break;
			default:
				throw new ArgumentOutOfRangeException("BadInstruction OpCode");
			}
		}
		return stringBuilder.ToString();
	}
}
