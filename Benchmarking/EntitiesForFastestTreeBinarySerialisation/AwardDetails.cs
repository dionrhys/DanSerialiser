﻿using System;

namespace Benchmarking.EntitiesForFastestTreeBinarySerialisation
{
	public sealed class AwardDetails
	{
		public int Key { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
		public string Image { get; set; }
		public string Type { get; set; }
		public DateTime? Date { get; set; }
	}
}