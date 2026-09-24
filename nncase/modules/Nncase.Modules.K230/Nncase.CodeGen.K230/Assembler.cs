using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nncase.CodeGen.K230;

internal sealed class Assembler
{
	private abstract class AsmLine
	{
		public string Line;

		public string Update;

		public int Pc;

		public AsmLine(string line, int pc)
		{
			Line = line;
			Pc = pc;
			Update = string.Empty;
		}
	}

	private sealed class AsmInst : AsmLine
	{
		public string InstName;

		public AsmInst(string inst_name, string line, int pc)
			: base(line, pc)
		{
			InstName = inst_name;
		}
	}

	private sealed class AsmSymobl : AsmLine
	{
		public string SymbolName;

		public AsmSymobl(string symbol_name, string line, int pc)
			: base(line, pc)
		{
			SymbolName = symbol_name;
		}
	}

	private sealed class AsmLabel : AsmLine
	{
		public string LabelName;

		public AsmLabel(string label_name, string line, int pc)
			: base(line, pc)
		{
			LabelName = label_name;
		}
	}

	private sealed class AsmOther : AsmLine
	{
		public AsmOther(string line, int pc)
			: base(line, pc)
		{
		}
	}

	public string AsmPath;

	private readonly List<AsmLine> AsmLines;

	private readonly Dictionary<string, int> SymobelMap;

	private readonly Dictionary<string, int> LabelMap;

	public Assembler(string asm_path)
	{
		AsmPath = asm_path;
		AsmLines = new List<AsmLine>();
		SymobelMap = new Dictionary<string, int>();
		LabelMap = new Dictionary<string, int>();
	}

	public void Assemble()
	{
		collect(AsmPath);
		int num = 0;
		bool flag = false;
		do
		{
			if (flag)
			{
				relocate();
				dump_relocate(Path.Combine(Path.GetDirectoryName(AsmPath) ?? "", Path.GetFileNameWithoutExtension(AsmPath) + "_" + num++ + ".s"));
				flag = false;
			}
			foreach (var (inst, index) in from t in AsmLines.Select((AsmLine l, int i) => (l: l, i: i))
				where t.l is AsmInst
				select ((AsmInst)t.l, i: t.i))
			{
				if (replaceSymobl(inst, index))
				{
					flag = true;
					break;
				}
				if (replaceLabel(inst, index))
				{
					flag = true;
					break;
				}
			}
		}
		while (flag);
	}

	private void collect(string asm_path)
	{
		using StreamReader streamReader = new StreamReader(File.OpenRead(asm_path));
		int num = 0;
		while (true)
		{
			string text = streamReader.ReadLine();
			if (text == null)
			{
				break;
			}
			string text2 = text;
			if (text2.StartsWith(".L."))
			{
				string text3 = text2;
				AsmLabel asmLabel = new AsmLabel(text3.Substring(0, text3.Length - 1), text2, num);
				AsmLines.Add(asmLabel);
				LabelMap.Add(asmLabel.LabelName, num);
				continue;
			}
			string text4 = text2;
			if (!text4.StartsWith("  ") && text4.EndsWith(":"))
			{
				string text3 = text4;
				AsmSymobl asmSymobl = new AsmSymobl(text3.Substring(0, text3.Length - 1), text4, num);
				AsmLines.Add(asmSymobl);
				SymobelMap.Add(asmSymobl.SymbolName, num);
				continue;
			}
			string text5 = text2;
			if (text5.StartsWith("  I."))
			{
				AsmInst asmInst = new AsmInst(text5.Substring(4, text5.IndexOf("(") - 4), text5, num);
				AsmLines.Add(asmInst);
				num += CodeGenUtil.InstLength(asmInst.InstName);
				continue;
			}
			if (text2.StartsWith("  ."))
			{
				AsmLines.Add(new AsmOther(text, num));
				continue;
			}
			throw new ArgumentOutOfRangeException(text);
		}
	}

	private bool is_overflow(int imm, int bits = 12)
	{
		if (imm < 0)
		{
			int num = ~((1 << bits - 1) - 1);
			if (num == (imm & num))
			{
				return false;
			}
			return true;
		}
		return imm > 1 << bits - 1;
	}

	private (uint high, uint low) split(int imm)
	{
		int item = ((imm >> 12) + ((imm >> 11) & 1)) & 0xFFFFF;
		uint item2 = (uint)imm & 0xFFFu;
		return (high: (uint)item, low: item2);
	}

	private void relocate()
	{
		SymobelMap.Clear();
		LabelMap.Clear();
		int num = 0;
		foreach (AsmLine asmLine in AsmLines)
		{
			if (!(asmLine is AsmLabel asmLabel))
			{
				if (!(asmLine is AsmSymobl asmSymobl))
				{
					if (asmLine is AsmInst asmInst)
					{
						asmInst.Pc = num;
						num += CodeGenUtil.InstLength(asmInst.InstName);
					}
					else
					{
						asmLine.Pc = num;
					}
				}
				else
				{
					asmSymobl.Pc = num;
					SymobelMap[asmSymobl.SymbolName] = num;
				}
			}
			else
			{
				asmLabel.Pc = num;
				LabelMap[asmLabel.LabelName] = num;
			}
		}
	}

	private bool replaceLabel(AsmInst inst, int index)
	{
		Match match = new Regex("  I.(\\w+)\\(.*(\\.L\\..*)\\)").Match(inst.Line);
		if (match == Match.Empty)
		{
			return false;
		}
		string value = match.Groups[2].Value;
		int num;
		if (value.EndsWith(".high"))
		{
			Dictionary<string, int> labelMap = LabelMap;
			string text = value;
			num = labelMap[text.Substring(0, text.Length - 5)] - inst.Pc;
			inst.Update = inst.Line.Replace(value, split(num).high.ToString());
		}
		else if (value.EndsWith(".low"))
		{
			Dictionary<string, int> labelMap2 = LabelMap;
			string text = value;
			num = labelMap2[text.Substring(0, text.Length - 4)] - inst.Pc;
			inst.Update = inst.Line.Replace(value, split(num + 4).low.ToString());
		}
		else
		{
			num = LabelMap[value] - inst.Pc;
			int bits = 12;
			if (inst.InstName == "JAL")
			{
				bits = 20;
			}
			if (is_overflow(num, bits))
			{
				if (inst.InstName == "JAL")
				{
					throw new NotSupportedException("overflow imm!");
				}
				inst.Line = inst.Line.Replace(value, "8");
				AsmLines.InsertRange(index + 1, new AsmInst[4]
				{
					new AsmInst("JAL", "  I.JAL(R.r0,16)", 0),
					new AsmInst("AUIPC", "  I.AUIPC(R.r10," + value + ".high)", 0),
					new AsmInst("ADDI", "  I.ADDI(R.r10,R.r10," + value + ".low)", 0),
					new AsmInst("JALR", "  I.JALR(R.r0,R.r10,0) ", 0)
				});
				return true;
			}
			inst.Update = inst.Line.Replace(value, num.ToString());
		}
		inst.Update += $" # {value} = {inst.Pc} + {num}";
		return false;
	}

	private bool replaceSymobl(AsmInst inst, int index)
	{
		Match match = new Regex("\\(.*,?.*(@[a-zA-Z\\d_]+\\.(\\w+)).*,?.*\\)").Match(inst.Line);
		if (match == Match.Empty)
		{
			return false;
		}
		string text = match.Groups[1].Value.Trim();
		Dictionary<string, int> symobelMap = SymobelMap;
		string text2 = text;
		int num = match.Groups[2].Length + 1;
		int num2 = symobelMap[text2.Substring(1, text2.Length - num - 1)] - inst.Pc;
		uint num3;
		if (match.Groups[2].Value == "high")
		{
			num3 = split(num2).high;
		}
		else
		{
			if (!(match.Groups[2].Value == "low"))
			{
				throw new ArgumentOutOfRangeException();
			}
			num3 = split(num2 + 4).low;
		}
		inst.Update = inst.Line.Replace(text, num3.ToString());
		inst.Update += $" # {text} = {inst.Pc} + {num2}";
		return false;
	}

	public void dump_relocate(string output_path)
	{
		using StreamWriter streamWriter = new StreamWriter(File.Open(output_path, FileMode.Create));
		foreach (AsmLine asmLine in AsmLines)
		{
			streamWriter.WriteLine(asmLine.Line);
		}
	}

	public void Dump(string output_path)
	{
		using StreamWriter streamWriter = new StreamWriter(File.Open(output_path, FileMode.Create));
		foreach (AsmLine asmLine in AsmLines)
		{
			if (asmLine is AsmInst asmInst)
			{
				streamWriter.Write($"{asmInst.Pc,8:D8}");
			}
			streamWriter.WriteLine((asmLine.Update == string.Empty) ? asmLine.Line : asmLine.Update);
		}
	}
}
