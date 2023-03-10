/*
  Copyright (C) 2002, 2003, 2004, 2005, 2006 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System.Reflection;
using System.Runtime.CompilerServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("IKVM.NET Runtime")]
[assembly: AssemblyDescription("JVM for Mono and .NET")]

[assembly: System.Security.AllowPartiallyTrustedCallers]

#if SIGNCODE
[assembly: InternalsVisibleTo("IKVM.Runtime.JNI, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Core, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Util, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Security, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Management, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Media, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Misc, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Remoting, PublicKey=")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.SwingAWT, PublicKey=")]
#else
[assembly: InternalsVisibleTo("IKVM.Runtime.JNI")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Core")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Util")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Security")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Management")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Media")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Misc")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Remoting")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.SwingAWT")]
#endif
