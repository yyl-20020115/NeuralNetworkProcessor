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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpAssembler.Core.Tests
{
	/// <summary>
	/// Tests for the <see cref="ObjectFile"/> class.
	/// </summary>
	[TestClass]
	public class ObjectFileTests
	{
		/// <summary>
		/// Tests the <see cref="ObjectFile.AddNewSection"/> method.
		/// </summary>
		[TestMethod]
		public void AddNewSectionTest()
		{
			ObjectFile objectfile = new ObjectFileMock();

			objectfile.Sections.AddNew(SectionType.Program, ".text");
			Assert.IsTrue(objectfile.Sections[0].Allocate);
			Assert.IsTrue(objectfile.Sections[0].Executable);
			Assert.IsFalse(objectfile.Sections[0].Writable);
			Assert.IsFalse(objectfile.Sections[0].NoBits);

			objectfile.Sections.AddNew(SectionType.Data, ".data");
			Assert.IsTrue(objectfile.Sections[1].Allocate);
			Assert.IsFalse(objectfile.Sections[1].Executable);
			Assert.IsTrue(objectfile.Sections[1].Writable);
			Assert.IsFalse(objectfile.Sections[1].NoBits);

			objectfile.Sections.AddNew(SectionType.Bss, ".bss");
			Assert.IsTrue(objectfile.Sections[2].Allocate);
			Assert.IsFalse(objectfile.Sections[2].Executable);
			Assert.IsTrue(objectfile.Sections[2].Writable);
			Assert.IsTrue(objectfile.Sections[2].NoBits);
		}
	}
}
