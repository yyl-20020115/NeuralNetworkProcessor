﻿#region Copyright and License
/*
 * SharpAssembler
 * Library for .NET that assembles a predetermined list of
 * instructions into machine code.
 * 
 * Copyright (C) 2011 Daniël Pelsmaeker
 * 
 * This file is part of SharpAssembler.
 * 
 * SharpAssembler is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * SharpAssembler is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with SharpAssembler.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion
using SharpAssembler.Instructions;

using System.Linq;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpAssembler.Core.Tests.Instructions
{
	/// <summary>
	/// Tests the <see cref="DeclareData"/> class.
	/// </summary>
	[TestClass]
	public class DeclareDataTests : InstructionTestsBase
	{
		/// <summary>
		/// Tests whether the <see cref="DeclareData"/> instruction emits the result of the expression.
		/// </summary>
		[TestMethod]
		public void EmitsData()
		{
			Func<Context, SimpleExpression> expression = (context) => new SimpleExpression(context.Address);
			var size = DataSize.Bit32;
			var instr = new DeclareData(expression, size);
			Assert.AreEqual(expression, instr.Expression);
			Assert.AreEqual(size, instr.Size);

			Int128 value = 0xDEADBEEF;
			Context.Address = value;

			var emittable = instr.Construct(Context).First() as ExpressionEmittable;
			Assert.AreEqual(value, emittable.Expression.Constant);
			Assert.IsNull(emittable.Expression.Reference);
			Assert.AreEqual(size, emittable.Size);
		}
	}
}
