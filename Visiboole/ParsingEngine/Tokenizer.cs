using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VisiBoole.ParsingEngine
{
    public class Tokenizer
    {
        /// <summary>
        /// Statement type proposed by the lexer.
        /// </summary>
        protected enum StatementType
        {
            Boolean,
            Clock,
            Comment,
            Empty,
            FormatSpecifier,
            Library,
            Module,
            Submodule,
            VariableList
        }

        protected enum TokenType
        {
            Variable,
            Constant,
            Comment,
            Assignment,
            Clock,
            Asterick,
            NegationOperator,
            OrOperator,
            ExclusiveOrOperator,
            EqualToOperator,
            MathOperator,
            Formatter,
            Declaration,
            Instantiation,
            Whitespace,
            Newline,
            Semicolon,
            Colon,
            Comma,
            OpenParenthesis,
            CloseParenthesis,
            OpenBrace,
            CloseBrace
        }

        protected class Token
        {
            public TokenType Type { get; private set; }

            public string Text { get; private set; }

            public Token(string text, TokenType type)
            {
                Text = text;
                Type = type;
            }
        }

        /// <summary>
        /// List of separators.
        /// </summary>
        private static readonly IList<char> SeperatorsList = new ReadOnlyCollection<char>(new List<char> { ' ', '\n', '(', ')', '{', '}', ',', ':' });

        /// <summary>
        /// List of operators.
        /// </summary>
        private static readonly IList<string> OperatorsList = new ReadOnlyCollection<string>(new List<string> { "^", "|", "+", "-", "==", " " });

        /// <summary>
        /// List of exclusive operators.
        /// </summary>
        private static readonly IList<string> ExclusiveOperatorsList = new ReadOnlyCollection<string>(new List<string> { "^", "+", "-", "==" });

        /// <summary>
        /// List of errors.
        /// </summary>
        protected List<string> ErrorLog;

        #region Token Identification

        /// <summary>
        /// Returns whether a lexeme is a scalar.
        /// </summary>
        /// <param name="lexeme">Lexeme to interpret</param>
        /// <returns>Whether the lexeme is a scalar or an error message</returns>
        private VerificationResponse IsScalar(string lexeme)
        {
            // Try to match lexeme as a scalar
            Match scalarMatch = ScalarRegex.Match(lexeme);
            // If lexeme is a scalar
            if (scalarMatch.Success)
            {
                // Get scalar name and bit
                string name = scalarMatch.Groups["Name"].Value;
                string bitString = string.Concat(name.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse());
                int bit = string.IsNullOrEmpty(bitString) ? -1 : Convert.ToInt32(bitString);
                if (bit != -1)
                {
                    name = name.Substring(0, name.Length - bitString.Length);
                }

                // If scalar bit is larger than 31
                if (bit > 31)
                {
                    return new VerificationResponse($"Bit count of '{lexeme}' must be between 0 and 31.");
                }

                return new VerificationResponse(true);
            }
            // If lexeme is not a scalar
            else
            {
                return new VerificationResponse(false);
            }
        }

        /// <summary>
        /// Returns whether a lexeme is a vector. (If so, initializes it)
        /// </summary>
        /// <param name="lexeme">Lexeme to interpret</param>
        /// <returns>Whether the lexeme is a vector</returns>
        private bool IsVector(string lexeme)
        {
            // Try to match lexeme as a vector
            Match vectorMatch = VectorRegex.Match(lexeme);
            // If lexeme is a vector
            if (vectorMatch.Success)
            {
                // Get vector name
                string name = vectorMatch.Groups["Name"].Value;

                // If vector name ends in a number
                if (char.IsDigit(name[name.Length - 1]))
                {
                    // Add vector name error to error log
                    ErrorLog.Add($"{LineNumber}: Vector name '{name}' cannot end in a number.");
                    return false;
                }

                // Get vector bounds and step
                int leftBound = string.IsNullOrEmpty(vectorMatch.Groups["LeftBound"].Value) ? -1 : Convert.ToInt32(vectorMatch.Groups["LeftBound"].Value);
                int step = string.IsNullOrEmpty(vectorMatch.Groups["Step"].Value) ? -1 : Convert.ToInt32(vectorMatch.Groups["Step"].Value);
                int rightBound = string.IsNullOrEmpty(vectorMatch.Groups["RightBound"].Value) ? -1 : Convert.ToInt32(vectorMatch.Groups["RightBound"].Value);

                // If left bound or right bound is greater than 31
                if (leftBound > 31 || rightBound > 31)
                {
                    // Add vector bounds error to error log
                    ErrorLog.Add($"{LineNumber}: Vector bounds of '{lexeme}' must be between 0 and 31.");
                    return false;
                }
                // If step is not between 1 and 31
                else if (step == 0 || step > 31)
                {
                    // Add vector step error to error log
                    ErrorLog.Add($"{LineNumber}: Vector step of '{lexeme}' must be between 1 and 31.");
                    return false;
                }

                return true;
            }
            // If lexeme is not a vector
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns whether a lexeme is a constant.
        /// </summary>
        /// <param name="lexeme">Lexeme to interpret</param>
        /// <returns>Whether the lexeme is a constant</returns>
        private bool IsConstant(string lexeme)
        {
            // Try to match lexeme as a constant
            Match constantMatch = ConstantRegex.Match(lexeme);
            // If lexeme is a constant
            if (constantMatch.Success)
            {
                // If the provided bit count is greater than 32 bits
                if (!string.IsNullOrEmpty(constantMatch.Groups["BitCount"].Value) && Convert.ToInt32(constantMatch.Groups["BitCount"].Value) > 32)
                {
                    // Add constant bit count error to error log
                    ErrorLog.Add($"{LineNumber}: Constant '{lexeme}' can    have at most 32 bits.");
                    return false;
                }

                return true;
            }
            // If lexeme is not a constant
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the token type of the provided lexeme or seperator
        /// </summary>
        /// <param name="lexeme">Lexeme</param>
        /// <param name="seperator">Seperator</param>
        /// <returns>Token type of the provided lexeme or seperator</returns>
        private TokenType? GetTokenType(string lexeme, char seperator)
        {
            if (lexeme.Length > 0)
            {
                VerificationResponse response = IsScalar(lexeme);
                if (response.Error != null)
            }
            else
            {
                if (seperator == ' ')
                {
                    return TokenType.Whitespace;
                }
                else if (seperator == '\n')
                {
                    return TokenType.Newline;
                }
                else if (seperator == '(')
                {
                    return TokenType.OpenParenthesis;
                }
                else if (seperator == ')')
                {
                    return TokenType.CloseParenthesis;
                }
                else if (seperator == '{')
                {
                    return TokenType.OpenBrace;
                }
                else if (seperator == '}')
                {
                    return TokenType.CloseBrace;
                }
                else if (seperator == ',')
                {
                    return TokenType.Comma;
                }
                else if (seperator == ':')
                {
                    return TokenType.Colon;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region Token Validation

        protected bool IsTokenValid(Token token, StatementType statementType)
        {
            if (token.Type == TokenType.Whitespace || token.Type == TokenType.Newline)
            {
                return true;
            }
            else if (token.Type == TokenType.Variable || token.Type == TokenType.Constant)
            {
                return statementType != StatementType.Comment && statementType != StatementType.Library;
            }
            else if (token.Type == TokenType.EqualToOperator || token.Type == TokenType.ExclusiveOrOperator
                || token.Type == TokenType.MathOperator || token.Type == TokenType.NegationOperator
                || token.Type == TokenType.OrOperator)
            {
                return statementType == StatementType.Boolean || statementType == StatementType.Clock;
            }
            else if (token.Type == TokenType.CloseParenthesis || token.Type == TokenType.OpenParenthesis)
            {
                return statementType == StatementType.Boolean || statementType == StatementType.Clock
                    || statementType == StatementType.Module || statementType == StatementType.Submodule;
            }
            else if (token.Type == TokenType.CloseBrace || token.Type == TokenType.OpenBrace)
            {
                return statementType != StatementType.Comment && statementType != StatementType.Library;
            }
            else if (token.Type == TokenType.Assignment)
            {
                return statementType == StatementType.Boolean;
            }
            else if (token.Type == TokenType.Clock)
            {
                return statementType == StatementType.Clock;
            }
            else if (token.Type == TokenType.Formatter)
            {
                return statementType == StatementType.FormatSpecifier;
            }
            else if (token.Type == TokenType.Asterick)
            {
                return statementType == StatementType.VariableList;
            }
            else if (token.Type == TokenType.Comment)
            {
                return statementType == StatementType.Comment;
            }
            else if (token.Type == TokenType.Colon || token.Type == TokenType.Comma)
            {
                return statementType == StatementType.Module || statementType == StatementType.Submodule;
            }
            else if (token.Type == TokenType.Instantiation)
            {
                return statementType == StatementType.Submodule;
            }
            else if (token.Type == TokenType.Declaration)
            {
                return statementType == StatementType.Module;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}