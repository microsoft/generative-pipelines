// Copyright (c) Microsoft. All rights reserved.

using System.Security.Cryptography;

namespace Aspire.AppHost.Internals;

internal static class Utils
{
    public static string GenerateSecret(int length)
    {
        const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        int charSetLength = AllowedChars.Length;

        if (length is <= 8 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 8 and 255.");
        }

        if (charSetLength is 0 or > 256)
        {
            throw new ArgumentException("AllowedChars.Length must be between 1 and 256.");
        }

        int maxRandom = 256 - (256 % charSetLength);
        Span<char> result = length <= 128 ? stackalloc char[length] : new char[length];
        using var rng = RandomNumberGenerator.Create();
        Span<byte> buffer = stackalloc byte[1];

        int i = 0;
        while (i < length)
        {
            rng.GetBytes(buffer);
            int value = buffer[0];
            if (value < maxRandom)
            {
                result[i++] = AllowedChars[value % charSetLength];
            }
        }

        return new string(result);
    }
}
