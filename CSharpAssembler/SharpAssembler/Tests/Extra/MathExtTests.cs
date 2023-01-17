#region Copyright and License
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
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SharpAssembler.Core.Tests.Extra
{
	/// <summary>
	/// Tests the <see cref="MathExt"/> class.
	/// </summary>
	[TestClass]
	public class MathExtTests
	{
		/// <summary>
		/// Tests the <see cref="MathExt.IsPowerOfTwo"/> method.
		/// </summary>
		[TestMethod]
		public void IsPowerOfTwo()
		{
			Assert.IsFalse(MathExt.IsPowerOfTwo(-9));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-8));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-7));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-6));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-5));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-4));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-3));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-2));
			Assert.IsFalse(MathExt.IsPowerOfTwo(-1));
			Assert.IsTrue(MathExt.IsPowerOfTwo(0));
			Assert.IsTrue(MathExt.IsPowerOfTwo(1));
			Assert.IsTrue(MathExt.IsPowerOfTwo(2));
			Assert.IsFalse(MathExt.IsPowerOfTwo(3));
			Assert.IsTrue(MathExt.IsPowerOfTwo(4));
			Assert.IsFalse(MathExt.IsPowerOfTwo(5));
			Assert.IsFalse(MathExt.IsPowerOfTwo(6));
			Assert.IsFalse(MathExt.IsPowerOfTwo(7));
			Assert.IsTrue(MathExt.IsPowerOfTwo(8));
			Assert.IsFalse(MathExt.IsPowerOfTwo(15));
			Assert.IsTrue(MathExt.IsPowerOfTwo(16));
			Assert.IsFalse(MathExt.IsPowerOfTwo(17));
			Assert.IsFalse(MathExt.IsPowerOfTwo(31));
			Assert.IsTrue(MathExt.IsPowerOfTwo(32));
			Assert.IsFalse(MathExt.IsPowerOfTwo(33));
			Assert.IsTrue(MathExt.IsPowerOfTwo(64));
			Assert.IsTrue(MathExt.IsPowerOfTwo(128));
			Assert.IsTrue(MathExt.IsPowerOfTwo(256));
			Assert.IsTrue(MathExt.IsPowerOfTwo(512));
		}

		/// <summary>
		/// Tests the <see cref="MathExt.CalculatePadding"/> method.
		/// </summary>
		[TestMethod]
		public void CalculatePadding()
		{
			Assert.AreEqual(0, MathExt.CalculatePadding(0, 1024));
			Assert.AreEqual(0, MathExt.CalculatePadding(16, 4));
			Assert.AreEqual(0, MathExt.CalculatePadding(8, 8));
			Assert.AreEqual(1, MathExt.CalculatePadding(15, 8));
			Assert.AreEqual(5, MathExt.CalculatePadding(27, 16));
			Assert.AreEqual(1, MathExt.CalculatePadding(31, 4));
			Assert.AreEqual(0, MathExt.CalculatePadding(32, 4));
			Assert.AreEqual(3, MathExt.CalculatePadding(33, 4));
		}

		/// <summary>
		/// Tests the <see cref="MathExt.Align"/> method.
		/// </summary>
		[TestMethod]
		public void Align()
		{
			Assert.AreEqual(0, MathExt.Align(0, 1024));
			Assert.AreEqual(16, MathExt.Align(16, 4));
			Assert.AreEqual(8, MathExt.Align(8, 8));
			Assert.AreEqual(16, MathExt.Align(15, 8));
			Assert.AreEqual(32, MathExt.Align(27, 16));
			Assert.AreEqual(32, MathExt.Align(31, 4));
			Assert.AreEqual(32, MathExt.Align(32, 4));
			Assert.AreEqual(36, MathExt.Align(33, 4));
		}

		/// <summary>
		/// Unsigned values for the <see cref="ExtMath.GetSizeOfValue"/> test.
		/// </summary>
		private static readonly Tuple<DataSize, ulong>[] UnsignedValues = new Tuple<DataSize, ulong>[]{ 
				new Tuple<DataSize, ulong>(DataSize.Bit8, 0x00),
				new Tuple<DataSize, ulong>(DataSize.Bit8, 0x01),
				new Tuple<DataSize, ulong>(DataSize.Bit8, 0xFF),
				new Tuple<DataSize, ulong>(DataSize.Bit16, 0x100),
				new Tuple<DataSize, ulong>(DataSize.Bit16, 0xFFFF),
				new Tuple<DataSize, ulong>(DataSize.Bit32, 0x10000),
				new Tuple<DataSize, ulong>(DataSize.Bit32, 0xFFFFFFFF),
				new Tuple<DataSize, ulong>(DataSize.Bit64, 0x100000000),
				new Tuple<DataSize, ulong>(DataSize.Bit64, 0xFFFFFFFFFFFFFFFF)
			};
		/// <summary>
		/// Signed values for the <see cref="ExtMath.GetSizeOfValue"/> test.
		/// </summary>
		private static readonly Tuple<DataSize, long>[] SignedValues = new Tuple<DataSize, long>[]{ 
				new Tuple<DataSize, long>(DataSize.Bit64, -0x7FFFFFFFFFFFFFFF),
				new Tuple<DataSize, long>(DataSize.Bit64, -0x80000001),
				new Tuple<DataSize, long>(DataSize.Bit32, -0x80000000),
				new Tuple<DataSize, long>(DataSize.Bit32, -0x8001),
				new Tuple<DataSize, long>(DataSize.Bit16, -0x8000),
				new Tuple<DataSize, long>(DataSize.Bit16, -0x81),
				new Tuple<DataSize, long>(DataSize.Bit8, -0x80),
				new Tuple<DataSize, long>(DataSize.Bit8, -0x01),
				new Tuple<DataSize, long>(DataSize.Bit8, 0x00),
				new Tuple<DataSize, long>(DataSize.Bit8, 0x01),
				new Tuple<DataSize, long>(DataSize.Bit8, 0x7F),
				new Tuple<DataSize, long>(DataSize.Bit16, 0x80),
				new Tuple<DataSize, long>(DataSize.Bit16, 0x7FFF),
				new Tuple<DataSize, long>(DataSize.Bit32, 0x8000),
				new Tuple<DataSize, long>(DataSize.Bit32, 0x7FFFFFFF),
				new Tuple<DataSize, long>(DataSize.Bit64, 0x80000000),
				new Tuple<DataSize, long>(DataSize.Bit64, 0x7FFFFFFFFFFFFFFF)
			};

		/// <summary>
		/// Tests the <see cref="MathExt.GetSizeOfValue"/> method.
		/// </summary>
		[TestMethod]
		public void GetSizeOfValue()
		{
			foreach (var value in UnsignedValues)
			{
				if (value.Item2 >= 0x00 && value.Item2 <= 0xFF)
				{
					Assert.AreEqual(MathExt.GetSizeOfValue((byte)value.Item2), (value.Item1));
					Assert.AreEqual(MathExt.GetSizeOfValue((byte)value.Item2, false), (value.Item1));
				}
			}
			foreach (var value in SignedValues)
			{
				if (value.Item2 >= -0x80 && value.Item2 <= 0x7F)
				{
					Assert.AreEqual(MathExt.GetSizeOfValue((sbyte)value.Item2), (value.Item1));
					Assert.AreEqual(MathExt.GetSizeOfValue((sbyte)value.Item2, true), (value.Item1));
				}
			}

			foreach (var value in UnsignedValues)
			{
				if (value.Item2 >= 0x0000 && value.Item2 <= 0xFFFF)
				{
					Assert.AreEqual(MathExt.GetSizeOfValue((ushort)value.Item2), (value.Item1));
					Assert.AreEqual(MathExt.GetSizeOfValue((ushort)value.Item2, false), (value.Item1));
				}
			}
			foreach (var value in SignedValues)
			{
				if (value.Item2 >= -0x8000 && value.Item2 <= 0x7FFF)
				{
					Assert.AreEqual(MathExt.GetSizeOfValue((short)value.Item2), (value.Item1));
					Assert.AreEqual(MathExt.GetSizeOfValue((short)value.Item2, true), (value.Item1));
				}
			}

			foreach (var value in UnsignedValues)
			{
				if (value.Item2 >= 0x00000000 && value.Item2 <= 0xFFFFFFFF)
				{
					Assert.AreEqual(MathExt.GetSizeOfValue((uint)value.Item2), (value.Item1));
					Assert.AreEqual(MathExt.GetSizeOfValue((uint)value.Item2, false), (value.Item1));
				}
			}
			foreach (var value in SignedValues)
			{
				if (value.Item2 >= -0x80000000 && value.Item2 <= 0x7FFFFFFF)
				{
					Assert.AreEqual(MathExt.GetSizeOfValue((int)value.Item2), (value.Item1));
					Assert.AreEqual(MathExt.GetSizeOfValue((int)value.Item2, true), (value.Item1));
				}
			}

			foreach (var value in UnsignedValues)
			{
				Assert.AreEqual(MathExt.GetSizeOfValue((ulong)value.Item2), (value.Item1));
				Assert.AreEqual(MathExt.GetSizeOfValue((ulong)value.Item2, false), (value.Item1));
			}
			foreach (var value in SignedValues)
			{
				Assert.AreEqual(MathExt.GetSizeOfValue((long)value.Item2), (value.Item1));
				Assert.AreEqual(MathExt.GetSizeOfValue((long)value.Item2, true), (value.Item1));
			}
		}
	}
}
