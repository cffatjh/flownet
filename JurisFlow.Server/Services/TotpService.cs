using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JurisFlow.Server.Services
{
    public static class TotpService
    {
        private const int TimeStepSeconds = 30;
        private const int CodeDigits = 6;
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string GenerateSecret(int length = 20)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return Base32Encode(bytes);
        }

        public static string BuildOtpauthUri(string issuer, string accountName, string secret)
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedAccount = Uri.EscapeDataString(accountName);
            return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secret}&issuer={encodedIssuer}&digits={CodeDigits}";
        }

        public static bool VerifyCode(string secret, string code, int allowedDriftSteps = 1)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalized = new string(code.Where(char.IsDigit).ToArray());
            if (normalized.Length != CodeDigits)
            {
                return false;
            }

            var key = Base32Decode(secret);
            var timestep = GetCurrentTimeStep();

            for (var offset = -allowedDriftSteps; offset <= allowedDriftSteps; offset++)
            {
                var candidate = GenerateTotp(key, timestep + offset);
                if (candidate == normalized)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<string> GenerateBackupCodes(int count = 8)
        {
            var codes = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                codes.Add(GenerateBackupCode());
            }
            return codes;
        }

        private static string GenerateBackupCode()
        {
            var value = RandomNumberGenerator.GetInt32(0, 100000000);
            return value.ToString("D8");
        }

        private static long GetCurrentTimeStep()
        {
            var unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixSeconds / TimeStepSeconds;
        }

        private static string GenerateTotp(byte[] key, long timestep)
        {
            var timestepBytes = BitConverter.GetBytes(timestep);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestepBytes);
            }

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(timestepBytes);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24)
                         | ((hash[offset + 1] & 0xFF) << 16)
                         | ((hash[offset + 2] & 0xFF) << 8)
                         | (hash[offset + 3] & 0xFF);

            var otp = binary % (int)Math.Pow(10, CodeDigits);
            return otp.ToString(new string('0', CodeDigits));
        }

        private static string Base32Encode(byte[] data)
        {
            var result = new StringBuilder();
            var buffer = 0;
            var bitsLeft = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    var index = (buffer >> (bitsLeft - 5)) & 31;
                    bitsLeft -= 5;
                    result.Append(Base32Alphabet[index]);
                }
            }

            if (bitsLeft > 0)
            {
                var index = (buffer << (5 - bitsLeft)) & 31;
                result.Append(Base32Alphabet[index]);
            }

            return result.ToString();
        }

        private static byte[] Base32Decode(string input)
        {
            var normalized = input.Trim().TrimEnd('=').ToUpperInvariant();
            var bytes = new List<byte>();
            var buffer = 0;
            var bitsLeft = 0;

            foreach (var c in normalized)
            {
                var index = Base32Alphabet.IndexOf(c);
                if (index < 0)
                {
                    continue;
                }

                buffer = (buffer << 5) | index;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                    bitsLeft -= 8;
                }
            }

            return bytes.ToArray();
        }
    }
}
