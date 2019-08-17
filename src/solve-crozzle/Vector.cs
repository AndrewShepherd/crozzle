﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public class Vector
	{
		public readonly int Dx;
		public readonly int Dy;
		
		public Vector(int dx, int dy)
		{
			Dx = dx;
			Dy = dy;
		}

		public override bool Equals(object obj) =>
			(obj is Vector v)
			&& v.Dx == this.Dx
			&& v.Dy == this.Dy;

		public override int GetHashCode() =>
			(Dx << 16) ^ Dy;
	}
}
