using System.Text.RegularExpressions;

namespace Drongo.Routing.Telco;

public class AddressTransformer
{
    public string Transform(string input, string pattern, string transform)
    {
        var normalizedInput = NormalizeInput(input);
        
        if (!MatchesPattern(pattern, normalizedInput))
        {
            return input;
        }

        var inputDigits = normalizedInput;
        
        var result = "";
        var digitIndex = 0;
        
        var i = 0;
        while (i < transform.Length)
        {
            if (transform[i] == '$')
            {
                var j = i;
                while (j < transform.Length && transform[j] == '$')
                {
                    j++;
                }
                var placeholderSize = j - i;
                
                var digitsToTake = Math.Min(placeholderSize, inputDigits.Length - digitIndex);
                if (digitsToTake > 0)
                {
                    result += inputDigits.Substring(digitIndex, digitsToTake);
                    digitIndex += digitsToTake;
                }
                
                i = j;
            }
            else if (transform[i] == 'Z' || transform[i] == 'z')
            {
                if (digitIndex < inputDigits.Length)
                {
                    result += inputDigits[digitIndex..];
                    digitIndex = inputDigits.Length;
                }
                i++;
            }
            else
            {
                result += transform[i];
                i++;
            }
        }

        return result;
    }

    private bool MatchesPattern(string pattern, string input)
    {
        var regex = ConvertToRegex(pattern);
        
        if (Regex.IsMatch(input, $"^{regex}$"))
        {
            return true;
        }
        
        if (Regex.IsMatch(input, $"^{regex}"))
        {
            return true;
        }
        
        return false;
    }

    private string ConvertToRegex(string pattern)
    {
        var cleanedPattern = pattern.Replace("-", "").Replace(" ", "");
        var result = "";
        var i = 0;
        var nextIsWildcard = false;

        var WildcardChars = new HashSet<char> { 'N', 'n', 'X', 'x', 'Z', 'z' };

        while (i < cleanedPattern.Length)
        {
            var c = cleanedPattern[i];

            if (c == '(')
            {
                var endIdx = FindMatchingParen(cleanedPattern, i);
                var inner = cleanedPattern[(i + 1)..endIdx];
                result += $"\\({ConvertGroupContent(inner)}\\)";
                i = endIdx + 1;
                nextIsWildcard = false;
            }
            else if (c == '[')
            {
                var endIdx = cleanedPattern.IndexOf(']', i);
                var inner = cleanedPattern[(i + 1)..endIdx];
                result += $"({ConvertChoiceContent(inner)})";
                i = endIdx + 1;
                nextIsWildcard = false;
            }
            else if (c == '+' || c == '*' || c == '?')
            {
                if (result.Length > 0 && result[^1] == '\\')
                {
                    result += c;
                }
                else if (result.Length > 1 && result.EndsWith(@"\d"))
                {
                    result = result[..^2] + c switch
                    {
                        '+' => @"\d+",
                        '*' => @"\d*",
                        '?' => @"\d?",
                        _ => @"\d"
                    };
                }
                else if (result.Length > 0)
                {
                    var lastChar = result[^1];
                    result = result[..^1];
                    result += c switch
                    {
                        '+' => $"{lastChar}+",
                        '*' => $"{lastChar}*",
                        '?' => $"{lastChar}?",
                        _ => lastChar.ToString()
                    };
                }
                i++;
            }
            else if (char.IsDigit(c))
            {
                if (nextIsWildcard)
                {
                    result += @"\d";
                }
                else
                {
                    result += c;
                }
                nextIsWildcard = false;
                i++;
            }
            else if (WildcardChars.Contains(c))
            {
                result += c switch
                {
                    'N' or 'n' => "[2-9]",
                    'X' or 'x' or 'Z' or 'z' => @"\d",
                    _ => "."
                };
                nextIsWildcard = true;
                i++;
            }
            else if (c == '.')
            {
                result += ".";
                nextIsWildcard = true;
                i++;
            }
            else
            {
                result += c;
                nextIsWildcard = false;
                i++;
            }
        }

        return result;
    }

    private int FindMatchingParen(string pattern, int start)
    {
        var depth = 1;
        for (var i = start + 1; i < pattern.Length; i++)
        {
            if (pattern[i] == '(') depth++;
            else if (pattern[i] == ')')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return pattern.Length - 1;
    }

    private string ConvertGroupContent(string content)
    {
        var result = "";
        var nextIsWildcard = false;
        var WildcardChars = new HashSet<char> { 'N', 'n', 'X', 'x', 'Z', 'z' };

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (c == '(')
            {
                var endIdx = FindMatchingParen(content, i);
                var inner = content[(i + 1)..endIdx];
                result += $"\\({ConvertGroupContent(inner)}\\)";
                i = endIdx;
                nextIsWildcard = false;
            }
            else if (char.IsDigit(c))
            {
                if (nextIsWildcard)
                {
                    result += @"\d";
                }
                else
                {
                    result += c;
                }
                nextIsWildcard = false;
            }
            else if (WildcardChars.Contains(c))
            {
                result += c switch
                {
                    'N' or 'n' => "[2-9]",
                    'X' or 'x' or 'Z' or 'z' => @"\d",
                    _ => "."
                };
                nextIsWildcard = true;
            }
            else if (c == '.')
            {
                result += ".";
                nextIsWildcard = true;
            }
            else
            {
                result += c;
                nextIsWildcard = false;
            }
        }

        return result;
    }

    private string ConvertChoiceContent(string content)
    {
        var options = content.Split('|');
        var result = string.Join("|", options.Select(o => ConvertChoiceOption(o.Trim('(', ')'))));
        return result;
    }

    private string ConvertChoiceOption(string option)
    {
        var result = "";
        var WildcardChars = new HashSet<char> { 'N', 'n', 'X', 'x', 'Z', 'z' };

        foreach (var c in option)
        {
            if (c == '(')
            {
                var endIdx = FindMatchingParen(option, option.IndexOf(c));
                var inner = option[(option.IndexOf(c) + 1)..endIdx];
                result += $"\\({ConvertGroupContent(inner)}\\)";
            }
            else if (WildcardChars.Contains(c))
            {
                result += c switch
                {
                    'N' or 'n' => "[2-9]",
                    _ => @"\d"
                };
            }
            else if (c == '.')
            {
                result += ".";
            }
            else
            {
                result += c;
            }
        }

        return result;
    }

    private string NormalizeInput(string input)
    {
        return input.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
    }
}
