﻿/*!
 * --- original library license
 * 
	@file xbyak.h
	@brief Xbyak ; JIT assembler for x86(IA32)/x64 by C++
	@author herumi
	@url https://github.com/herumi/xbyak, http://homepage1.nifty.com/herumi/soft/xbyak_e.html
	@note modified new BSD license
	http://opensource.org/licenses/BSD-3-Clause
*/

/*
 * Exceptions.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */

namespace XbyakSharp.Intel;
public class ErrorException : Exception
{
    public ErrorException(Error error) => Error = error;

    public Error Error { get; private set; }

    public override string Message => Util.ConvertErrorToString(Error);
}