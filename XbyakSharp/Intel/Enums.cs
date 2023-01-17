/*!
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
* Enums.cs
* Author: mes
* License: new BSD license http://opensource.org/licenses/BSD-3-Clause
*/

namespace XbyakSharp.Intel;
public enum Error : uint
{
    ErrNone = 0,
    ErrBadAddressing,
    ErrCodeIsTooBig,
    ErrBadScale,
    ErrEspCantBeIndex,
    ErrBadCombination,
    ErrBadSizeOfRegister,
    ErrImmIsTooBig,
    ErrBadAlign,
    ErrLabelIsRedefined,
    ErrLabelIsTooFar,
    ErrLabelIsNotFound,
    ErrCodeIsnotCopyable,
    ErrBadParameter,
    ErrCantProtect,
    ErrCantUse64BitDisp,
    ErrOffsetIsTooBig,
    ErrMemSizeIsNotSpecified,
    ErrBadMemSize,
    ErrBadStCombination,
    ErrOverLocalLabel,
    ErrUnderLocalLabel,
    ErrCantAlloc,
    ErrOnlyTNearIsSupportedInAutoGrow,
    ErrBadProtectMode,
    ErrInternal
}

public enum LabelMode :uint
{
    LasIs = 0,
    Labs,
    LaddTop
}
