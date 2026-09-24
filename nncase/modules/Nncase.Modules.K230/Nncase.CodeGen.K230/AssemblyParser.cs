using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Nncase.IR;
using Nncase.TIR;
using Nncase.TIR.Builders;

namespace Nncase.CodeGen.K230;

public sealed class AssemblyParser
{
	public string AsmOutPath;

	public Nncase.TIR.Buffer[]? Params;

	public readonly Regex InstFinder = new Regex("^(\\d{8})\\s\\s(I\\.(\\w+)\\((.*?)\\))", RegexOptions.Multiline);

	public AssemblyParser(string asm_out_path, CallableType? signature)
	{
		AsmOutPath = asm_out_path;
		Params = (((object)signature == null) ? null : CodeGenUtil.ToPrimFuncParameters(signature).ToArray());
	}

	public void OverrideGlbParams(IReadOnlyList<int> args)
	{
		string[] array = File.ReadAllLines(AsmOutPath);
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].StartsWith("gnne_start:"))
			{
				num = i;
				break;
			}
		}
		num += 6;
		int num2 = 1;
		Regex regex = new Regex("(^\\d{8}\\s\\sI.ADDI\\(R.rax,R.r0,)(0)(\\))");
		foreach (byte[] item in args.Select(BitConverter.GetBytes))
		{
			for (int num3 = 3; num3 >= 0; num3--)
			{
				byte value = item[num3];
				array[num + num2] = regex.Replace(array[num + num2], $"${{1}}{value}$3");
				num2 += 2;
			}
		}
		File.WriteAllLines(AsmOutPath, array);
	}

	public IEnumerable<Expr> ParserInst()
	{
		MatchCollection source = InstFinder.Matches(File.ReadAllText(AsmOutPath));
		new Dictionary<string, MethodCallExpression>();
		return source.OfType<Match>().Select(delegate(Match m)
		{
			string value = m.Groups[3].Value;
			string[] array = (from s in m.Groups[4].Value.Split(',')
				where s != string.Empty
				select s).ToArray();
			MethodInfo method = typeof(I).GetMethod(value);
			IEnumerable<Expression> arguments = array.Zip(method.GetParameters()).Select<(string, System.Reflection.ParameterInfo), Expression>(delegate((string, System.Reflection.ParameterInfo) t)
			{
				string text = t.Item1.Trim();
				if (text.StartsWith("R."))
				{
					Type? typeFromHandle = typeof(R);
					string text2 = text;
					return Expression.Field(null, typeFromHandle.GetField(text2.Substring(2, text2.Length - 2)));
				}
				string value2 = text;
				if (t.Item2.ParameterType == typeof(Expr))
				{
					return Expression.Convert(Expression.Constant(Convert.ToInt32(value2), typeof(int)), typeof(Expr));
				}
				string value3 = text;
				if (!t.Item2.ParameterType.IsEnum)
				{
					throw new ArgumentOutOfRangeException();
				}
				return Expression.Constant(Enum.Parse(t.Item2.ParameterType, value3), t.Item2.ParameterType);
			}).Concat(from p in method.GetParameters().Skip(array.Length)
				select (p.DefaultValue is DBNull) ? Expression.Constant(null, p.ParameterType) : Expression.Constant(p.DefaultValue, p.ParameterType));
			return Expression.Lambda<Func<Expr>>(Expression.Call(method, arguments), Array.Empty<ParameterExpression>()).Compile()();
		});
	}

	public PrimFunction ParserFunction()
	{
		ISequentialBuilder<PrimFunction> sequentialBuilder = T.PrimFunc("main", "k230", Params ?? new Nncase.TIR.Buffer[0]);
		object[] exprOrBuilders = ParserInst().ToArray();
		return sequentialBuilder.Body(exprOrBuilders).Build();
	}

	public int ParserStackSize()
	{
		using StreamReader streamReader = new StreamReader(File.OpenRead(AsmOutPath));
		return Convert.ToInt32(Regex.Match(streamReader.ReadLine(), "^\\s\\s.stack_size\\s(\\d+)").Groups[1].Value);
	}
}
