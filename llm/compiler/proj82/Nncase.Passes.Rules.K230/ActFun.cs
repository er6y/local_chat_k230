using System;
using System.Collections.Generic;

namespace Nncase.Passes.Rules.K230;

public class ActFun
{
	public delegate float ActFuncType(float x);

	private ActFuncType _func;

	private float _splitPoint0;

	private float _splitPoint14;

	private float _splitPointCenter;

	private int _centerPoint;

	private List<float> _minParam;

	private List<float> _maxParam;

	public ActFuncType Func
	{
		get
		{
			return _func;
		}
		set
		{
			_func = value ?? throw new ArgumentNullException("value");
		}
	}

	public float SplitPoint0
	{
		get
		{
			return _splitPoint0;
		}
		set
		{
			_splitPoint0 = value;
		}
	}

	public float SplitPoint14
	{
		get
		{
			return _splitPoint14;
		}
		set
		{
			_splitPoint14 = value;
		}
	}

	public float SplitPointCenter
	{
		get
		{
			return _splitPointCenter;
		}
		set
		{
			_splitPointCenter = value;
		}
	}

	public int CenterPoint
	{
		get
		{
			return _centerPoint;
		}
		set
		{
			_centerPoint = value;
		}
	}

	public List<float> MinParam
	{
		get
		{
			return _minParam;
		}
		set
		{
			_minParam = value ?? throw new ArgumentNullException("value");
		}
	}

	public List<float> MaxParam
	{
		get
		{
			return _maxParam;
		}
		set
		{
			_maxParam = value ?? throw new ArgumentNullException("value");
		}
	}
}
