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

using System;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpAssembler.Core.Tests.Instructions
{
	/// <summary>
	/// Tests the <see cref="Define"/> class.
	/// </summary>
	[TestClass]
	public class DefineTests : InstructionTestsBase
	{
		/// <summary>
		/// Tests whether the <see cref="Define"/> instruction correctly adds a symbol to the context.
		/// </summary>
		[TestMethod]
		public void AddsSymbol()
		{
			Func<Context, SimpleExpression> expression = (context) => new SimpleExpression(context.Address + 3);

			var instr = new Define("test", expression);
			Assert.AreEqual("test", instr.DefinedSymbol.Identifier);
			Assert.AreEqual(LabelType.Private, instr.DefinedSymbol.SymbolType);
			Assert.AreEqual(expression, instr.Expression);

			Context.Address = 5;

			Assert.AreEqual(instr.Construct(Context).ToList().Count,0);
			Assert.AreEqual((Int128)8, Context.SymbolTable["test"].Value);
		}
	}
}
