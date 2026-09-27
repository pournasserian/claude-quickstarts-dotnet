using System.Text;

namespace AutonomousCoding;

/// <summary>
/// A POSIX-mode shlex.split() equivalent (.NET has no built-in one). Used only for security
/// validation of bash commands - never for actually building a command to execute, so failing
/// safe (throwing on anything ambiguous) is the right default. Matches Python's shlex rules:
/// single quotes are fully literal; double quotes allow backslash to escape $ ` " \ and newline;
/// outside quotes, backslash escapes the next character.
///
/// Deliberately does NOT treat ; | &amp; as separator characters, matching shlex's actual default
/// behavior (they're ordinary word characters unless whitespace-separated). This matters for
/// security, not just fidelity: e.g. "./init.sh;rm -rf /" must tokenize as a single dirty token
/// "./init.sh;rm" that fails the "== ./init.sh" check in Security.ValidateInitScript, the same way
/// it does in the original Python. Splitting into separate "./init.sh" and "rm" tokens here would
/// let the injected command hide behind a clean-looking first token.
/// </summary>
public static class ShellTokenizer
{
    public static string[] Split(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inToken = false;
        var i = 0;

        while (i < input.Length)
        {
            var c = input[i];

            if (char.IsWhiteSpace(c))
            {
                if (inToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    inToken = false;
                }
                i++;
                continue;
            }

            inToken = true;

            switch (c)
            {
                case '\'':
                    i++;
                    var closedSingle = false;
                    while (i < input.Length)
                    {
                        if (input[i] == '\'')
                        {
                            closedSingle = true;
                            i++;
                            break;
                        }
                        current.Append(input[i]);
                        i++;
                    }
                    if (!closedSingle)
                    {
                        throw new FormatException("Unclosed single quote");
                    }
                    break;

                case '"':
                    i++;
                    var closedDouble = false;
                    while (i < input.Length)
                    {
                        if (input[i] == '"')
                        {
                            closedDouble = true;
                            i++;
                            break;
                        }
                        if (input[i] == '\\' && i + 1 < input.Length && "$`\"\\\n".Contains(input[i + 1]))
                        {
                            current.Append(input[i + 1]);
                            i += 2;
                            continue;
                        }
                        current.Append(input[i]);
                        i++;
                    }
                    if (!closedDouble)
                    {
                        throw new FormatException("Unclosed double quote");
                    }
                    break;

                case '\\':
                    if (i + 1 >= input.Length)
                    {
                        throw new FormatException("Trailing unescaped backslash");
                    }
                    current.Append(input[i + 1]);
                    i += 2;
                    break;

                default:
                    current.Append(c);
                    i++;
                    break;
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }
}
