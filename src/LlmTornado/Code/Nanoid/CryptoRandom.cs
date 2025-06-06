using System;
using System.Security.Cryptography;

namespace LlmTornado.Code;

/*
 *  With minor modifications and removed shims for old .NET runtimes taken from
 *  https://github.com/codeyu/nanoid-net
 *  MIT licensed
 */

/// <summary>
/// Implementation of <see cref="System.Random"></see> using <see cref="System.Security.Cryptography.RandomNumberGenerator"></see>.
/// </summary>
internal class CryptoRandom : Random, IDisposable
{
    private readonly RandomNumberGenerator r;

    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public CryptoRandom()
    {
        r = RandomNumberGenerator.Create();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="buffer"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public override void NextBytes(byte[] buffer)
    {
        r.GetBytes(buffer);
    }
    
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override double NextDouble()
    {
#if MODERN
        Span<byte> uint32Buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(uint32Buffer);
        return BitConverter.ToUInt32(uint32Buffer) / (1.0 + uint.MaxValue);
#else
        byte[] uint32Buffer = new byte[4];
        using (RandomNumberGenerator? rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(uint32Buffer);
        }
        return BitConverter.ToUInt32(uint32Buffer, 0) / (1.0 + uint.MaxValue);
#endif

    }

    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
    public override int Next(int minValue, int maxValue)
    {
        if (minValue == maxValue) return minValue;
        long range = (long)maxValue - minValue;
        return (int)((long)Math.Floor(NextDouble() * range) + minValue);
    }

    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int Next()
    {
        return Next(0, int.MaxValue);
    }

    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException"></exception>
    public override int Next(int maxValue)
    {
        return Next(0, maxValue);
    }

    public void Dispose()
    {
        r.Dispose();
    }
}