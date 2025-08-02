using System;

namespace LlmTornado.Code;

internal static class Runtime
{
    #if MODERN
    public static bool IsBrowser => OperatingSystem.IsBrowser();
    #else
    public static bool IsBrowser => false;
    #endif
}