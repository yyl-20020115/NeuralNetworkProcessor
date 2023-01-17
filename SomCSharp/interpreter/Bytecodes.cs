/**
 * Copyright (c) 2017 Michael Haupt, github@haupz.de
 * Copyright (c) 2009 Michael Haupt, michael.haupt@hpi.uni-potsdam.de
 * Software Architecture Group, Hasso Plattner Institute, Potsdam, Germany
 * http://www.hpi.uni-potsdam.de/swa/
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

namespace Som.Interpreter;

public static class Bytecodes
{

    // Bytecodes used by the simple object machine
    public const byte HALT = 0;
    public const byte DUP = 1;
    public const byte PUSH_LOCAL = 2;
    public const byte PUSH_ARGUMENT = 3;
    public const byte PUSH_FIELD = 4;
    public const byte PUSH_BLOCK = 5;
    public const byte PUSH_CONSTANT = 6;
    public const byte PUSH_GLOBAL = 7;
    public const byte POP = 8;
    public const byte POP_LOCAL = 9;
    public const byte POP_ARGUMENT = 10;
    public const byte POP_FIELD = 11;
    public const byte SEND = 12;
    public const byte SUPER_SEND = 13;
    public const byte RETURN_LOCAL = 14;
    public const byte RETURN_NON_LOCAL = 15;

    private static readonly string[] PADDED_BYTECODE_NAMES = new string[] {
          "HALT            ", "DUP             ", "PUSH_LOCAL      ",
          "PUSH_ARGUMENT   ", "PUSH_FIELD      ", "PUSH_BLOCK      ",
          "PUSH_CONSTANT   ", "PUSH_GLOBAL     ", "POP             ",
          "POP_LOCAL       ", "POP_ARGUMENT    ", "POP_FIELD       ",
          "SEND            ", "SUPER_SEND      ", "RETURN_LOCAL    ",
          "RETURN_NON_LOCAL"
    };

    private static readonly string[] BYTECODE_NAMES = PADDED_BYTECODE_NAMES.Select(n => n.Trim()).ToArray();

    private static readonly byte NUM_BYTECODES = (byte)BYTECODE_NAMES.Length;

    private static void CheckBytecodeIndex(byte bytecode)
    {
        if (bytecode < 0 || bytecode >= NUM_BYTECODES)
        {
            throw new ArgumentException("illegal bytecode: " + bytecode);
        }
    }

    public static string GetBytecodeName(byte bytecode)
    {
        CheckBytecodeIndex(bytecode);
        return BYTECODE_NAMES[bytecode];
    }

    public static string GetPaddedBytecodeName(byte bytecode)
    {
        CheckBytecodeIndex(bytecode);
        return PADDED_BYTECODE_NAMES[bytecode];
    }

    public static int GetBytecodeLength(byte bytecode) =>
        // Return the length of the given bytecode
        BYTECODE_LENGTH[bytecode];

    // Static array holding lengths of each bytecode
    private static readonly int[] BYTECODE_LENGTH = new int[] {
      1, // HALT
      1, // DUP
      3, // PUSH_LOCAL
      3, // PUSH_ARGUMENT
      2, // PUSH_FIELD
      2, // PUSH_BLOCK
      2, // PUSH_CONSTANT
      2, // PUSH_GLOBAL
      1, // POP
      3, // POP_LOCAL
      3, // POP_ARGUMENT
      2, // POP_FIELD
      2, // SEND
      2, // SUPER_SEND
      1, // RETURN_LOCAL
      1 // RETURN_NON_LOCAL
    };
}