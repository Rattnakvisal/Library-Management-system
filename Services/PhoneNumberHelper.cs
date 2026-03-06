using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Library_Management_system.Services;

public static class PhoneNumberHelper
{
    public static string NormalizeDigits(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(phoneNumber.Length);
        foreach (var ch in phoneNumber)
        {
            if (!char.IsDigit(ch))
            {
                continue;
            }

            if (char.GetUnicodeCategory(ch) is not UnicodeCategory.DecimalDigitNumber)
            {
                continue;
            }

            var numericValue = char.GetNumericValue(ch);
            if (numericValue is >= 0 and <= 9)
            {
                builder.Append((char)('0' + (int)numericValue));
            }
        }

        return builder.ToString();
    }

    public static string NormalizeForCambodia(string? phoneNumber)
    {
        var digits = NormalizeDigits(phoneNumber);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        if (digits.StartsWith("00855", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        // Keep international format without the local leading zero.
        if (digits.StartsWith("8550", StringComparison.Ordinal))
        {
            return $"855{digits[4..]}";
        }

        if (digits.StartsWith("855", StringComparison.Ordinal))
        {
            return digits;
        }

        if (digits.StartsWith('0') && digits.Length >= 8)
        {
            return $"855{digits[1..]}";
        }

        return digits;
    }

    public static IReadOnlyCollection<string> BuildMatchCandidates(string? phoneNumber)
    {
        var candidates = new HashSet<string>(StringComparer.Ordinal);
        var digits = NormalizeDigits(phoneNumber);
        var kh = NormalizeForCambodia(phoneNumber);

        AddCandidate(candidates, digits);
        AddCandidate(candidates, kh);

        if (!string.IsNullOrWhiteSpace(kh) && kh.StartsWith("855", StringComparison.Ordinal) && kh.Length > 3)
        {
            AddCandidate(candidates, $"0{kh[3..]}");
        }

        return candidates;
    }

    public static bool AreEquivalent(string? leftPhoneNumber, string? rightPhoneNumber)
    {
        var left = BuildMatchCandidates(leftPhoneNumber);
        var right = BuildMatchCandidates(rightPhoneNumber);

        return left.Any(right.Contains);
    }

    public static bool LooksLikePhone(string? value)
    {
        var digits = NormalizeDigits(value);
        return digits.Length is >= 8 and <= 15;
    }

    private static void AddCandidate(ISet<string> candidates, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        candidates.Add(value);
    }
}
