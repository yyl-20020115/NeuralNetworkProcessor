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
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace SharpAssembler
{
	// TODO: Add an optional reference to the Constructable from which the exception was thrown.
	/// <summary>
	/// An exception which is thrown by the assembler while assembling an object file.
	/// </summary>
	[Serializable]
	public class AssemblerException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblerException"/> class.
		/// </summary>
		public AssemblerException()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblerException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public AssemblerException(string message)
			: base(message)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblerException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception; or <see langword="null"/> if
		/// no inner exception is specified.</param>
		public AssemblerException(string message, Exception inner)
			: base(message, inner)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblerException"/> class.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the
		/// exception being thrown.</param>
		/// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the
		/// source or destination.</param>
		protected AssemblerException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }

		/// <summary>
		/// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
		/// <param name="context">The destination for this serialization.</param>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
		}
	}
}
