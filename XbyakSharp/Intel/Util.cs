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
 * Util.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */

namespace XbyakSharp.Intel;
static class Util
{
    public const int AlignPageSize = 4096;

    private static readonly string[] ErrorMessage = new string[]
        {
                "none",
                "bad addressing",
                "code is too big",
                "bad scale",
                "esp can't be index",
                "bad combination",
                "bad size of register",
                "imm is too big",
                "bad align",
                "label is redefined",
                "label is too far",
                "label is not found",
                "code is not copyable",
                "bad parameter",
                "can't protect",
                "can't use 64bit disp(use (void*))",
                "offset is too big",
                "MEM size is not specified",
                "bad mem size",
                "bad st combination",
                "over local label",
                "under local label",
                "can't alloc",
                "T_SHORT is not supported in AutoGrow",
                "bad protect mode",
                "internal error",
        };

    public static string ConvertErrorToString(Error error) => ErrorMessage[(int)error];

    public static bool IsInDisp8(uint x) => 0xFFFFFF80 <= x || x <= 0x7F;

    public static bool IsInInt32(ulong x) => ~0x7fffffffUL <= x || x <= 0x7FFFFFFFU;

    public static uint VerifyInInt32(ulong x) => Environment.Is64BitProcess && !IsInInt32(x) ? throw new ErrorException(Error.ErrOffsetIsTooBig) : (uint)x;

    public static void Swap<T>(ref T t1, ref T t2)
    {
        T tmp = t1;
        t1 = t2;
        t2 = tmp;
    }

    public static ulong ToUInt64(this IntPtr ptr) => (ulong)ptr;
}