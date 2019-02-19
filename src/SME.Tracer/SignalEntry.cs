﻿using SME;
using System;
using System.Reflection;

namespace SME.Tracer
{
	public class SignalEntry
	{
		public IRuntimeBus Bus;
		public PropertyInfo Property;
		public bool IsDriver;
		public bool IsInternal;
		public string SortKey;
	}
}

