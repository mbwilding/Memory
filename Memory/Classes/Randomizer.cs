using System;
using System.Linq;

namespace Memory.Classes
{
    // ReSharper disable StringLiteralTypo
    // ReSharper disable IdentifierTypo
    public static class Randomizer
    {
        public static string RandomStr(int length)
        {
            Random rand = new();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }
    }
}
