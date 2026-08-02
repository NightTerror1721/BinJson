#nullable enable

using System.Text;
using Krampus.BinJson.SourceGenerators.Models;

namespace Krampus.BinJson.SourceGenerators.Utilities
{
    /// <summary>
    /// Transforms member names according to naming policies
    /// </summary>
    internal static class NamingPolicyTransformer
    {
        /// <summary>
        /// Apply naming policy to a member name
        /// </summary>
        /// <param name="memberName">Original CLR member name (e.g., "PlayerName")</param>
        /// <param name="policy">Naming policy to apply</param>
        /// <returns>Transformed name according to policy</returns>
        public static string Transform(string memberName, NamingPolicy policy)
        {
            if (string.IsNullOrEmpty(memberName))
                return memberName;

            return policy switch
            {
                NamingPolicy.Default => memberName,
                NamingPolicy.CamelCase => ToCamelCase(memberName),
                NamingPolicy.SnakeCase => ToSnakeCase(memberName),
                NamingPolicy.KebabCase => ToKebabCase(memberName),
                _ => memberName
            };
        }

        /// <summary>
        /// Convert to camelCase: "PlayerName" -> "playerName"
        /// </summary>
        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // If first character is already lowercase, return as-is
            if (char.IsLower(name[0]))
                return name;

            // Handle sequences of uppercase letters (e.g., "HTTPSConnection" -> "httpsConnection")
            var chars = name.ToCharArray();
            int i = 0;

            // Lowercase leading uppercase characters until we hit a lowercase or end
            while (i < chars.Length && char.IsUpper(chars[i]))
            {
                // If this is not the last uppercase in a sequence and next is lowercase,
                // keep this one uppercase (e.g., "XMLParser" -> "xmlParser", not "xmLParser")
                if (i + 1 < chars.Length && char.IsLower(chars[i + 1]))
                    break;

                chars[i] = char.ToLowerInvariant(chars[i]);
                i++;
            }

            return new string(chars);
        }

        /// <summary>
        /// Convert to snake_case: "PlayerName" -> "player_name"
        /// </summary>
        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var builder = new StringBuilder();
            bool previousWasUnderscore = false;

            for (int i = 0; i < name.Length; i++)
            {
                char current = name[i];

                // If uppercase and not first character, add underscore before it
                if (char.IsUpper(current))
                {
                    if (i > 0 && !previousWasUnderscore)
                    {
                        // Don't add underscore if previous char was also uppercase and next is lowercase
                        // (e.g., "HTTPSConnection" -> "https_connection", not "h_t_t_p_s_connection")
                        char previous = name[i - 1];
                        bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                        if (!char.IsUpper(previous) || nextIsLower)
                        {
                            builder.Append('_');
                            previousWasUnderscore = true;
                        }
                    }

                    builder.Append(char.ToLowerInvariant(current));
                }
                else if (current == '_')
                {
                    builder.Append(current);
                    previousWasUnderscore = true;
                }
                else
                {
                    builder.Append(current);
                    previousWasUnderscore = false;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Convert to kebab-case: "PlayerName" -> "player-name"
        /// </summary>
        private static string ToKebabCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var builder = new StringBuilder();
            bool previousWasHyphen = false;

            for (int i = 0; i < name.Length; i++)
            {
                char current = name[i];

                // If uppercase and not first character, add hyphen before it
                if (char.IsUpper(current))
                {
                    if (i > 0 && !previousWasHyphen)
                    {
                        // Don't add hyphen if previous char was also uppercase and next is lowercase
                        char previous = name[i - 1];
                        bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                        if (!char.IsUpper(previous) || nextIsLower)
                        {
                            builder.Append('-');
                            previousWasHyphen = true;
                        }
                    }

                    builder.Append(char.ToLowerInvariant(current));
                }
                else if (current == '-' || current == '_')
                {
                    builder.Append('-');
                    previousWasHyphen = true;
                }
                else
                {
                    builder.Append(current);
                    previousWasHyphen = false;
                }
            }

            return builder.ToString();
        }
    }
}
