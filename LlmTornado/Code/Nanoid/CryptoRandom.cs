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
internal class CryptoRandom : Random
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
        ArgumentNullException.ThrowIfNull(buffer);
        r.GetBytes(buffer);
    }
    
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override double NextDouble()
    {
        Span<byte> uint32Buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(uint32Buffer);
        return BitConverter.ToUInt32(uint32Buffer) / (1.0 + UInt32.MaxValue);
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
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
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
        ArgumentOutOfRangeException.ThrowIfNegative(maxValue);
        return Next(0, maxValue);
    }
}