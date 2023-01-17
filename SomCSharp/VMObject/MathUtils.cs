namespace Som.VMObject;

public static class MathUtils
{
    public static bool AddExact(int var0, int var1, out int var2)
    {
        var2 = var0 + var1;
        return !(((var0 ^ var2) & (var1 ^ var2)) < 0);
    }
    public static bool AddExact(long var0, long var2, out long var4)
    {
        var4 = var0 + var2;
        return !(((var0 ^ var4) & (var2 ^ var4)) < 0);
    }
    public static bool SubtractExact(int var0, int var1, out int var2)
    {
        var2 = var0 - var1;
        return !(((var0 ^ var1) & (var0 ^ var2)) < 0);
    }
    public static bool SubtractExact(long var0, long var2, out long var4)
    {
        var4 = var0 - var2;
        return !(((var0 ^ var2) & (var0 ^ var4)) < 0L);
    }
    public static bool MultiplyExact(int var0, int var1, out long var2)
    {
        var2 = (long)var0 * (long)var1;
        return !((long)((int)var2) != var2);
    }
    public static bool MultiplyExact(long var0, long var2, out long var4)
    {
        var4 = var0 * var2;
        long var6 = Math.Abs(var0);
        long var8 = Math.Abs(var2);
        return ((var6 | var8) >> 31 == 0L || (var2 == 0L || var4 / var2 == var0) && (var0 != -9223372036854775808L || var2 != -1L));
    }
}
