using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisiBoole.Models;
using VisiBoole.ParsingEngine.ObjectCode;
using VisiBoole.Views;

namespace VisiBoole.ParsingEngine
{
    public class Lexer
    {
        #region Lexer Types

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

        #endregion

        #region Token Class and Types

        private enum TokenType
        {
            Scalar,
            Vector,
            Constant,
            Assignment,
            Clock,
            NegationOperator,
            BooleanOperator,
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

        private class Token
        {
            public TokenType Type { get; private set; }

            public string Text { get; private set; }

            public int Index { get; private set; }

            public Token(string text, TokenType type, int index)
            {
                Text = text;
                Type = type;
                Index = index;
            }
        }

        private class VariableToken
        {
            public string Text { get; private set; }

            public bool PartOfGrouping { get; private set; }

            public VariableToken(string text, bool partOfGrouping)
            {
                Text = text;
                PartOfGrouping = partOfGrouping;
            }
        }

        #endregion

        #region Lexer Patterns and Regular Expressions

        /// <summary>
        /// Pattern for identifying names.
        /// </summary>
        private static readonly string NamePattern = @"(?<Name>[a-zA-Z][[a-zA-Z0-9]*(?<!\d))";

        /// <summary>
        /// Pattern for identifying scalars. (No ~ or *)
        /// </summary>
        public static readonly string ScalarPattern = $@"({NamePattern}(?<Bit>\d+)?)";

        /// <summary>
        /// Pattern for identfying indexes.
        /// </summary>
        private static readonly string IndexPattern = @"(\[(?<LeftBound>\d+)\.(?<Step>\d+)?\.(?<RightBound>\d+)\]|\[\])";

        /// <summary>
        /// Pattern for identifying vectors. (No ~ or *)
        /// </summary>
        protected static readonly string VectorPattern = $@"({NamePattern}{IndexPattern})";

        /// <summary>
        /// Pattern for identifying binary constants.
        /// </summary>
        private static readonly string BinaryConstantPattern = @"((?<BitCount>\d+)?'(?<Format>[bB])(?<Value>[0-1]+))";

        /// <summary>
        /// Pattern for identifying hex constants.
        /// </summary>
        private static readonly string HexConstantPattern = @"((?<BitCount>\d+)?'(?<Format>[hH])(?<Value>[a-fA-F0-9]+))";

        /// <summary>
        /// Pattern for identifying decimal constants.
        /// </summary>
        private static readonly string DecimalConstantPattern = @"(((?<BitCount>\d+)?'(?<Format>[dD])?)?(?<Value>[0-9]+))";

        /// <summary>
        /// Pattern for identifying constants. (No ~)
        /// </summary>
        public static readonly string ConstantPattern = $@"({BinaryConstantPattern}|{HexConstantPattern}|{DecimalConstantPattern})";

        /// <summary>
        /// Pattern for identifying scalars, vectors and constants. (No ~ or *)
        /// </summary>
        protected static readonly string VariablePattern = $@"({VectorPattern}|{ConstantPattern}|{ScalarPattern})";

        /// <summary>
        /// Pattern for identifying variable lists.
        /// </summary>
        protected static readonly string VariableListPattern = $@"(?<Vars>{VariablePattern}(\s+{VariablePattern})*)";

        /// <summary>
        /// Pattern for identifying concatenations.
        /// </summary>
        public static readonly string ConcatPattern = $@"(\{{{VariableListPattern}\}})";

        /// <summary>
        /// Pattern for identifying concatenations of any type or any type.
        /// </summary>
        public static readonly string AnyTypePattern = $@"({ConcatPattern}|{VariablePattern})";

        /// <summary>
        /// Pattern for identifying formatters.
        /// </summary>
        protected static readonly string FormatterPattern = @"(%(?<Format>[ubhdUBHD]))";

        /// <summary>
        /// Pattern for identifying submodule instantiations.
        /// </summary>
        public static readonly string InstantiationPattern = @"(?<Design>\w+)\.(?<Name>[a-zA-Z0-9]+)";

        /// <summary>
        /// Pattern for identifying components (inputs or outputs) in a module notation.
        /// </summary>
        private static readonly string ModuleComponentPattern = $@"(({AnyTypePattern}|{VariableListPattern})(,\s+({AnyTypePattern}|{VariableListPattern}))*)";

        /// <summary>
        /// Pattern for identifying modules.
        /// </summary>
        public static readonly string ModulePattern = $@"(?<Components>(?<Inputs>{ModuleComponentPattern})\s+:\s+(?<Outputs>{ModuleComponentPattern}))";

        /// <summary>
        /// Pattern for identifying operators.
        /// </summary>
        private static readonly string OperatorPattern = $@"^(([=+^|-])|(<=(@{ScalarPattern})?)|(~+)|(==))$";

        /// <summary>
        /// Pattern for identifying seperators.
        /// </summary>
        private static readonly string SeperatorPattern = @"[\s{}():,;]";

        /// <summary>
        /// Pattern for identifying invalid characters.
        /// </summary>
        private static readonly string InvalidPattern = @"[^\s_a-zA-Z0-9~@%^*()=+[\]{}<|:;',.-]";

        /// <summary>
        /// Regex for identifying scalars. (Optional ~ or *)
        /// </summary>
        private static Regex ScalarRegex { get; } = new Regex($"^[~*]*{ScalarPattern}$", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying vectors. (Optional ~ or *)
        /// </summary>
        private static Regex VectorRegex { get; } = new Regex($"^[~*]*{VectorPattern}$", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying constants. (Optional ~)
        /// </summary>
        public static Regex ConstantRegex { get; } = new Regex($"^~*{ConstantPattern}$", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying formatters.
        /// </summary>
        private static Regex FormatterRegex = new Regex($"^{FormatterPattern}$", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying submodule instantiations.
        /// </summary>
        private static Regex InstantiationRegex = new Regex($"^{InstantiationPattern}$");

        /// <summary>
        /// Regex for identifying operators.
        /// </summary>
        private static Regex OperatorRegex = new Regex(OperatorPattern, RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying seperators.
        /// </summary>
        private static Regex SeperatorRegex = new Regex(SeperatorPattern, RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying invalid characters.
        /// </summary>
        private static Regex InvalidRegex = new Regex(InvalidPattern, RegexOptions.Compiled);

        #endregion

        /// <summary>
        /// Line number of the design being parsed.
        /// </summary>
        protected int LineNumber;

        /// <summary>
        /// List of errors.
        /// </summary>
        protected List<string> ErrorLog;

        /// <summary>
        /// The design being parsed.
        /// </summary>
        protected Design Design;

        /// <summary>
        /// List of libraries included for this instance.
        /// </summary>
        protected List<string> Libraries;

        /// <summary>
        /// Dictionary of submodules for this instance.
        /// </summary>
        public Dictionary<string, string> Subdesigns;

        /// <summary>
        /// Dictionary of instantiations for this instance.
        /// </summary>
        public Dictionary<string, string> Instantiations;

        /// <summary>
        /// Memo for vector expansions.
        /// </summary>
        protected static Dictionary<string, IEnumerable<string>> ExpansionMemo = new Dictionary<string, IEnumerable<string>>();

        /// <summary>
        /// Constructs a lexer to verify the design.
        /// </summary>
        /// <param name="design">Design to parse</param>
        protected Lexer(Design design)
        {
            ErrorLog = new List<string>();
            Design = design;
            Libraries = new List<string>();
            Subdesigns = new Dictionary<string, string>();
            Instantiations = new Dictionary<string, string>();
        }

        // To Do: Format Specifiers not inside each other.
        //        All variables in this kind of statement must be in a formatter.

        /// <summary>
        /// Returns the token type of the provided lexeme.
        /// </summary>
        /// <param name="lexeme">Lexeme</param>
        /// <param name="seperatorChar">Seperator character after lexeme</param>
        /// <returns>Token type of lexeme</returns>
        private TokenType? GetTokenType(string lexeme, char seperatorChar)
        {
            if (lexeme == Design.FileName && seperatorChar == '(')
            {
                return TokenType.Declaration;
            }
            else if (IsScalar(lexeme))
            {
                return TokenType.Scalar;
            }
            else if (IsVector(lexeme))
            {
                return TokenType.Vector;
            }
            else if (OperatorRegex.IsMatch(lexeme))
            {
                if (lexeme == "|" || lexeme == "^" || lexeme == "==")
                {
                    return TokenType.BooleanOperator;
                }
                else if (lexeme.Contains('~'))
                {
                    return TokenType.NegationOperator;
                }
                else if (lexeme == "+" || lexeme == "-")
                {
                    return TokenType.MathOperator;
                }
                else if (lexeme == "=")
                {
                    return TokenType.Assignment;
                }
                else
                {
                    return TokenType.Clock;
                }
            }
            else if (IsConstant(lexeme))
            {
                return TokenType.Constant;
            }
            else if (FormatterRegex.IsMatch(lexeme))
            {
                return TokenType.Formatter;
            }
            else if (InstantiationRegex.IsMatch(lexeme) && seperatorChar == '(')
            {
                return TokenType.Instantiation;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a list of tokens from the provided line
        /// </summary>
        /// <param name="line">Line to generate tokens</param>
        /// <returns>List of tokens from the line</returns>
        private List<Token> GetTokens(string line)
        {
            // Create tokens list
            List<Token> tokens = new List<Token>();
            // Create string builder for current lexeme
            StringBuilder lexeme = new StringBuilder();
            // Index of lexeme
            int lexemeIndex = 0;
            // Create groupings stack
            Stack<char> groupings = new Stack<char>();
            // Save current line number
            int lineNumber = LineNumber;

            for (int i = 0; i < line.Length; i++)
            {
                // Get current character
                char c = line[i];
                // Get character as a string
                string newChar = c.ToString();
                // Get current lexme
                string currentLexeme = lexeme.ToString();

                // If the character is an invalid character
                if (InvalidRegex.IsMatch(newChar))
                {
                    // Add invalid character error to error log
                    ErrorLog.Add($"{lineNumber}: Invalid character '{c}'.");
                    // Return null
                    return null;
                }
                // If the character is a seperator character
                else if (SeperatorRegex.IsMatch(newChar))
                {
                    if (currentLexeme.Length > 0)
                    {
                        TokenType? tokenType = GetTokenType(currentLexeme, c);
                        if (tokenType == null)
                        {
                            return null;
                        }

                        // Add current token to tokens list
                        tokens.Add(new Token(currentLexeme, (TokenType)tokenType, lexemeIndex));
                        // Clear current lexeme
                        lexeme = lexeme.Clear();
                    }

                    if (c == ' ')
                    {
                        tokens.Add(new Token(newChar, TokenType.Whitespace, i));
                    }
                    else if (c == '\n')
                    {
                        lineNumber++;
                        tokens.Add(new Token(newChar, TokenType.Newline, i));
                    }
                    else if (c == ';')
                    {
                        if (tokens.Count == 0 || tokens.Any(token => token.Type == TokenType.Semicolon))
                        {
                            ErrorLog.Add($"{LineNumber}: ';' can only be used to end a statement.");
                            return null;
                        }

                        tokens.Add(new Token(newChar, TokenType.Semicolon, i));
                    }
                    else if (c == ':')
                    {
                        tokens.Add(new Token(newChar, TokenType.Colon, i));
                    }
                    else if (c == ',')
                    {
                        tokens.Add(new Token(newChar, TokenType.Comma, i));
                    }
                    else if (c == '(' || c == '{')
                    {
                        if (groupings.Count > 0 && groupings.Peek() == '{')
                        {
                            if (c == '{')
                            {
                                // Concatenation inside concatenation error
                                ErrorLog.Add($"{lineNumber}: Concatenations can't be used inside other concatenations.");
                            }
                            else
                            {
                                // Parenthesis inside concatenation error
                                ErrorLog.Add($"{lineNumber}: Parenthesis can't be used inside concatenations.");
                            }
                            return null;
                        }

                        if (c == '(')
                        {
                            tokens.Add(new Token(newChar, TokenType.OpenParenthesis, i));
                        }
                        else
                        {
                            tokens.Add(new Token(newChar, TokenType.OpenBrace, i));
                        }
                        groupings.Push(c);
                    }
                    else if (c == ')' || c == '}')
                    {
                        char top = groupings.Count > 0 ? groupings.Pop() : '\0';
                        if ((c == ')' && top != '(') || (c == '}' && top != '{'))
                        {
                            if (top == '\0')
                            {
                                // New grouping error
                                ErrorLog.Add($"{lineNumber}: '{c}' cannot be matched.");
                            }
                            else
                            {
                                // Unmatched grouping error
                                ErrorLog.Add($"{lineNumber}: '{c}' cannot be matched. '{top}' must be matched first.");
                            }
                            return null;
                        }

                        if (c == ')')
                        {
                            tokens.Add(new Token(newChar, TokenType.CloseParenthesis, i));
                        }
                        else
                        {
                            tokens.Add(new Token(newChar, TokenType.CloseBrace, i));
                        }
                    }

                    // Advance lexeme index
                    lexemeIndex = i + 1;
                }
                // If the character is not a seperator character
                else
                {
                    // Append new character to the current lexeme
                    lexeme.Append(c);
                }
            }

            if (groupings.Count > 0)
            {
                // Unmatched grouping error
                ErrorLog.Add($"{lineNumber}: '{groupings.Peek()}' was not matched.");
                return null;
            }

            return tokens;
        }

        protected StatementType? GetStatementType(string line)
        {
            // Create statement type
            StatementType? statementType = null;
            // Create variable token list
            List<VariableToken> variables = new List<VariableToken>();
            // Create open parenthesis stack
            Stack<int> openParenthesesIndexes = new Stack<int>();
            // Whether the current token is inside a module
            bool insideModule = false;
            // Whether the current token is inside a formatter
            bool insideFormat = false;
            // Whether the current token is inside a concat
            bool insideConcat = false;

            // Get tokens list
            List<Token> tokens = GetTokens(line);
            // If tokens list is empty
            if (tokens == null)
            {
                return statementType;
            }

            Token lastToken = null;
            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                List<Token> previousTokens = i != 0 ? tokens.GetRange(0, i) : new List<Token>();

                if (token.Type == TokenType.Newline)
                {
                    // Increment line number
                    LineNumber++;
                }
                else if (token.Type == TokenType.OpenParenthesis)
                {
                    if (statementType == StatementType.FormatSpecifier || statementType == null)
                    {
                        ErrorLog.Add($"{LineNumber}: '(' can't be used in a format specifier or variable list statement.");
                        return null;
                    }

                    insideModule = lastToken != null && (lastToken.Type == TokenType.Declaration || lastToken.Type == TokenType.Instantiation);
                    openParenthesesIndexes.Push(token.Index);
                }
                else if (token.Type == TokenType.CloseParenthesis)
                {
                    if (statementType == StatementType.FormatSpecifier || statementType == null)
                    {
                        ErrorLog.Add($"{LineNumber}: ')' can't be used in a format specifier or variable list statement.");
                        return null;
                    }

                    insideModule = false;
                    // Check parenthesis

                }
                else if (token.Type == TokenType.OpenBrace)
                {
                    insideConcat = true;
                    insideFormat = lastToken != null && tokens[i - 1].Type == TokenType.Formatter;
                }
                else if (token.Type == TokenType.CloseBrace)
                {
                    insideConcat = false;
                    insideFormat = false;
                }
                else if (token.Type == TokenType.Comma)
                {
                    if (!insideModule)
                    {
                        ErrorLog.Add($"{LineNumber}: ',' can only be used inside the () in a submodule or module statement.");
                        return null;
                    }
                }
                else if (token.Type == TokenType.Colon)
                {
                    if (!insideModule)
                    {
                        ErrorLog.Add($"{LineNumber}: ':' can only be used to seperate input and output variables in a module or submodule statement.");
                        return null;
                    }

                    if (previousTokens.Any(t => t.Type == TokenType.Colon))
                    {
                        ErrorLog.Add($"{LineNumber}: ':' can only be used once in a module or submodule statement.");
                        return null;
                    }
                }
                else if (token.Type == TokenType.Declaration)
                {
                    if (statementType != null)
                    {
                        ErrorLog.Add($"{LineNumber}: Invalid module declaration.");
                        return null;
                    }

                    statementType = StatementType.Module;
                    insideModule = true;
                }
                else if (token.Type == TokenType.Instantiation)
                {
                    if (statementType != null)
                    {
                        ErrorLog.Add($"{LineNumber}: Invalid module instantiation.");
                        return null;
                    }

                    if (previousTokens.Count(t => t.Type != TokenType.Whitespace && t.Type != TokenType.Newline) > 0)
                    {
                        /*
                        ErrorLog.Add($"{LineNumber}: Module instantiations must o.");
                        */
                        return null;
                    }

                    if (!ValidateInstantiation(InstantiationRegex.Match(token.Text), line))
                    {
                        return null;
                    }

                    statementType = StatementType.Submodule;
                    insideModule = true;
                }
                else if (token.Type == TokenType.Scalar || token.Type == TokenType.Vector || token.Type == TokenType.Constant)
                {
                    if (statementType == StatementType.Boolean || statementType == StatementType.Clock)
                    {
                        if (token.Text.Contains('*'))
                        {
                            ErrorLog.Add($"{LineNumber}: '*' cannot be used on the right side of a boolean or clock statement.");
                            return null;
                        }

                        if (insideConcat && token.Text.Contains('~'))
                        {
                            ErrorLog.Add($"{LineNumber}: '~' can't be used inside a concatenation.");
                            return null;
                        }
                    }
                    else
                    {
                        if (token.Text.Contains('~'))
                        {
                            ErrorLog.Add($"{LineNumber}: '~' can only be used on the right side of a boolean or clock statement.");
                            return null;
                        }

                        if (statementType == StatementType.FormatSpecifier)
                        {
                            if (!insideFormat)
                            {
                                ErrorLog.Add($"{LineNumber}: Variables or constants in a format specifier statement must be inside a format specifier.");
                                return null;
                            }
                        }
                        else if (statementType == StatementType.Module || statementType == StatementType.Submodule)
                        {
                            if (!insideModule)
                            {
                                ErrorLog.Add($"{LineNumber}: Variables or constants in module or submodule statements must be inside an instantiation or declaration.");
                                return null;
                            }
                        }
                    }

                    if (token.Type == TokenType.Constant && insideConcat && !(char.IsDigit(token.Text.TrimStart('~')[0]) && token.Text.Contains('\'')))
                    {
                        ErrorLog.Add($"{LineNumber}: Constants in concatenations must specify a bit count.");
                        return null;
                    }

                    if (token.Type != TokenType.Constant && !variables.Any(v => v.Text == token.Text))
                    {
                        variables.Add(new VariableToken(token.Text, insideConcat));
                    }
                }
                else if (token.Type == TokenType.Assignment || token.Type == TokenType.Clock)
                {
                    if (statementType != null)
                    {
                        ErrorLog.Add($"{LineNumber}: '{token.Text}' can only precede dependent(s) once in a statement.");
                        return null;
                    }
                    else
                    {
                        statementType = token.Type == TokenType.Assignment ? StatementType.Boolean : StatementType.Clock;
                    }

                    if (previousTokens.Any(t => t.Text.Contains('*')))
                    {
                        ErrorLog.Add($"{LineNumber}: '{token.Text}' cannot be used with '*'.");
                        return null;
                    }

                    if (previousTokens.Any(t => t.Type == TokenType.Constant))
                    {
                        ErrorLog.Add($"{LineNumber}: Constants can't be used on the left side of a boolean statement.");
                        return null;
                    }

                    if (variables.Count == 0)
                    {
                        ErrorLog.Add($"{LineNumber}: '{token.Text}' can only be used after a dependent in boolean or clock statements.");
                        return null;
                    }

                    if (variables.Count > 1 && variables.Count(v => !v.PartOfGrouping) > 0)
                    {
                        ErrorLog.Add($"{LineNumber}: In order to use multiple variables for a dependent, you must place the variables inside a concatenation.");
                        return null;
                    }
                }
                else if (token.Type == TokenType.BooleanOperator || token.Type == TokenType.NegationOperator || token.Type == TokenType.MathOperator)
                {
                    if (insideConcat)
                    {
                        ErrorLog.Add($"{LineNumber}: '{token.Text}' can't be used inside a concatenation.");
                        return null;
                    }

                    if (statementType != StatementType.Boolean && statementType != StatementType.Clock)
                    {
                        ErrorLog.Add($"{LineNumber}: '{token.Text}' operator can only be used in a boolean or clock statement.");
                        return null;
                    }

                    if (token.Type == TokenType.BooleanOperator || token.Type == TokenType.NegationOperator)
                    {
                        if (tokens.Any(t => t.Type == TokenType.MathOperator))
                        {
                            ErrorLog.Add($"{LineNumber}: Math operators (+ and -) cannot be used with boolean operators in a boolean or clock statement.");
                            return null;
                        }

                        if (token.Type == TokenType.NegationOperator && (i + 1 < tokens.Count))
                        {
                            ErrorLog.Add($"{LineNumber}: '~' must be attached to a scalar, vector, constant, parenthesis or concatenation.");
                            return null;
                        }
                    }
                    else
                    {
                        if (tokens.Any(t => token.Type == TokenType.BooleanOperator || token.Type == TokenType.NegationOperator))
                        {
                            ErrorLog.Add($"{LineNumber}: Math operators (+ and -) cannot be used with boolean operators in a boolean or clock statement.");
                            return null;
                        }
                    }
                }
                else if (token.Type == TokenType.Formatter)
                {
                    if (insideFormat)
                    {
                        ErrorLog.Add($"{LineNumber}: Formatters can't be used inside other formatters.");
                        return null;
                    }

                    if (statementType != null && statementType != StatementType.FormatSpecifier)
                    {
                        ErrorLog.Add($"{LineNumber}: '{token.Text}' can only be used in a format specifier statement.");
                        return null;
                    }
                    else if (statementType == null)
                    {
                        if (previousTokens.Any(t => token.Type == TokenType.Scalar || token.Type == TokenType.Vector || token.Type == TokenType.Constant))
                        {
                            ErrorLog.Add($"{LineNumber}: All variables or constants in a format specifier statement must be inside a format specifier.");
                            return null;
                        }

                        statementType = StatementType.FormatSpecifier;
                    }
                }

                if (token.Type != TokenType.Whitespace && token.Type != TokenType.Newline)
                {
                    lastToken = token;
                }
            }

            // If there are no errors and the statement type is still null
            if (statementType == null)
            {
                // Set statement type to variable list
                statementType = StatementType.VariableList;
            }

            // Return statement type
            return statementType;
        }

        #region Token Verifications

        /// <summary>
        /// Returns whether the provided lexeme is a recognized token.
        /// </summary>
        /// <param name="lexeme">Lexeme to interpret</param>
        /// <param name="seperatorChar">Character seperator</param>
        /// <returns>Whether the provided lexeme is a token</returns>
        private bool IsToken(string lexeme, char seperatorChar)
        {
            if (lexeme == Design.FileName && seperatorChar == '(')
            {
                return true;
            }
            else if (IsScalar(lexeme))
            {
                return true;
            }
            else if (IsVector(lexeme))
            {
                return true;
            }
            else if (OperatorRegex.IsMatch(lexeme))
            {
                return true;
            }
            else if (IsConstant(lexeme))
            {
                return true;
            }
            else if (FormatterRegex.IsMatch(lexeme))
            {
                return true;
            }
            else if (InstantiationRegex.IsMatch(lexeme) && seperatorChar == '(')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns whether a lexeme is a scalar.
        /// </summary>
        /// <param name="lexeme">Lexeme to interpret</param>
        /// <returns>Whether the lexeme is a scalar</returns>
        private bool IsScalar(string lexeme)
        {
            // Try to match lexeme as a scalar
            Match scalarMatch = ScalarRegex.Match(lexeme);
            // If lexeme is a scalar
            if (scalarMatch.Success)
            {
                // Get scalar name and bit
                string name = scalarMatch.Groups["Name"].Value;
                int bit = string.IsNullOrEmpty(scalarMatch.Groups["Bit"].Value) ? -1 : Convert.ToInt32(scalarMatch.Groups["Bit"].Value);

                // If scalar bit is larger than 31
                if (bit > 31)
                {
                    ErrorLog.Add($"{LineNumber}: Bit count of '{lexeme}' must be between 0 and 31.");
                    return false;
                }

                // If scalar doesn't contain a bit
                if (bit == -1)
                {
                    // If namespace belongs to a vector
                    if (Design.Database.NamespaceBelongsToVector(name))
                    {
                        // Add namespace error to error log
                        ErrorLog.Add($"{LineNumber}: Namespace '{name}' is already being used by a vector.");
                        return false;
                    }
                    // If namespace doesn't exist
                    else if (!Design.Database.NamespaceExists(name))
                    {
                        // Update namespace with no bit
                        Design.Database.UpdateNamespace(name, bit);
                    }
                }
                // If scalar does contain a bit
                else
                {
                    // If namespace exists and doesn't belong to a vector
                    if (Design.Database.NamespaceExists(name) && !Design.Database.NamespaceBelongsToVector(name))
                    {
                        // Add namespace error to error log
                        ErrorLog.Add($"{LineNumber}: Namespace '{name}' is already being used by a scalar.");
                        return false;
                    }
                    // If namespace doesn't exist or belongs to a vector
                    else
                    {
                        // Update/add namespace with bit
                        Design.Database.UpdateNamespace(name, bit);
                    }
                }

                return true;
            }
            // If lexeme is not a scalar
            else
            {
                return false;
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

                // Check for invalid [] notation
                /*
                if (!Design.Database.HasVectorNamespace(name) && leftBound == -1)
                {
                    ErrorLog.Add($"{LineNumber}: '{name}[]' cannot be used without an explicit dimension somewhere.");
                    return false;
                }
                */

                // If namespace exists and doesn't belong to a vector
                if (Design.Database.NamespaceExists(name) && !Design.Database.NamespaceBelongsToVector(name))
                {
                    // Add namespace error to error log
                    ErrorLog.Add($"{LineNumber}: Namespace '{name}' is already being used by a scalar.");
                    return false;
                }

                // If vector is explicit
                if (leftBound != -1)
                {
                    // If left bound is least significant bit
                    if (leftBound < rightBound)
                    {
                        // Flips bounds so left bound is most significant bit
                        leftBound = leftBound + rightBound;
                        rightBound = leftBound - rightBound;
                        leftBound = leftBound - rightBound;
                    }

                    // For each bit in the vector bounds
                    for (int i = leftBound; i >= rightBound; i--)
                    {
                        // Update/add bit to namespace
                        Design.Database.UpdateNamespace(name, i);
                    }
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
                    ErrorLog.Add($"{LineNumber}: Constant '{lexeme}' can only have at most 32 bits.");
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

        #endregion

        #region Token Validation

        /// <summary>
        /// Validates the provided token with the current character, statement type and the grouping stack.
        /// </summary>
        /// <param name="line">Current line</param>
        /// <param name="currentChar">Character being appended to the current lexeme</param>
        /// <param name="currentLexeme">Current lexeme</param>
        /// <param name="type">Statement type of the line</param>
        /// <param name="groupings">Groupings stack</param>
        /// <returns>Whether the token is validate in its current context</returns>
        private bool ValidateCurrentToken(string line, char currentChar, string currentLexeme, ref StatementType? type, Stack<char> groupings)
        {
            // Check for invalid tokens with current statement type
            if (currentLexeme == "=")
            {
                if (type != null)
                {
                    ErrorLog.Add($"{LineNumber}: '{currentLexeme}' can only be used after the dependent in a boolean statement.");
                    return false;
                }
                else
                {
                    type = StatementType.Boolean;
                }

                if (line.Substring(0, line.IndexOf(currentLexeme)).Contains("*"))
                {
                    ErrorLog.Add($"{LineNumber}: '*' can only be used in a variable list statement.");
                    return false;
                }

                if (line.Substring(0, line.IndexOf(currentLexeme)).Contains("'"))
                {
                    ErrorLog.Add($"{LineNumber}: Constants can't be used on the left side of a boolean statement.");
                    return false;
                }
            }
            else if (currentLexeme.Contains("<="))
            {
                if (type != null)
                {
                    ErrorLog.Add($"{LineNumber}: '{currentLexeme}' can only be used after the dependent in a clock statement.");
                    return false;
                }
                else
                {
                    type = StatementType.Clock;
                }

                if (line.Substring(0, line.IndexOf(currentLexeme)).Contains("*"))
                {
                    ErrorLog.Add($"{LineNumber}: '*' can only be used in a variable list statement.");
                    return false;
                }

                if (line.Substring(0, line.IndexOf(currentLexeme)).Contains("'"))
                {
                    ErrorLog.Add($"{LineNumber}: Constants can't be used on the left side of a clock statement.");
                    return false;
                }
            }
            else if (Regex.IsMatch(currentLexeme, @"^([+|^-])|(==)$"))
            {
                if (!(type == StatementType.Boolean || type == StatementType.Clock))
                {
                    ErrorLog.Add($"{LineNumber}: '{currentLexeme}' operator can only be used in a boolean or clock statement.");
                    return false;
                }
            }
            else if (currentLexeme.Contains("%"))
            {
                if (type != null && type != StatementType.FormatSpecifier)
                {
                    ErrorLog.Add($"{LineNumber}: '{currentLexeme}' can only be used in a format specifier statement.");
                    return false;
                }
                else if (type == null)
                {
                    type = StatementType.FormatSpecifier;
                }
            }
            else if (currentLexeme.Contains("~"))
            {
                if (currentLexeme == "~" && currentChar != '(' && currentChar != '{')
                {
                    ErrorLog.Add($"{LineNumber}: '~' must be attached to a scalar, vector, constant, parenthesis or concatenation.");
                    return false;
                }

                if (!(type == StatementType.Boolean || type == StatementType.Clock))
                {
                    ErrorLog.Add($"{LineNumber}: '~' can only be used in on the right side of a boolean or clock statement.");
                    return false;
                }

                if (groupings.Count > 0 && groupings.Peek() == '{')
                {
                    ErrorLog.Add($"{LineNumber}: '~' can't be used inside a concatenation field.");
                    return false;
                }
            }
            else if (currentLexeme.Contains("*"))
            {
                if (type != null)
                {
                    ErrorLog.Add($"{LineNumber}: '*' can only be used in a variable list statement.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates the seperator token with the current lexeme, statement type, groupings stack and the previous tokens.
        /// </summary>
        /// <param name="line">Current line</param>
        /// <param name="seperatorChar">Character that is ending the current lexeme</param>
        /// <param name="currentLexeme">Current lexeme</param>
        /// <param name="type">Statement type of the line</param>
        /// <param name="groupings">Groupings stack</param>
        /// <param name="tokens">Previous tokens</param>
        /// <returns></returns>
        private bool ValidateSeperatorToken(string line, char seperatorChar, string currentLexeme, ref StatementType? type, Stack<char> groupings, List<string> tokens)
        {
            if (seperatorChar == '{' || seperatorChar == '}')
            {
                if (seperatorChar == '{' && groupings.Count > 0 && groupings.Peek() == '{')
                {
                    ErrorLog.Add($"{LineNumber}: Concatenations can't be used inside of other concatenations.");
                    return false;
                }
            }
            else if (seperatorChar == '(' || seperatorChar == ')')
            {
                if (groupings.Count > 0 && groupings.Peek() == '{')
                {
                    ErrorLog.Add($"{LineNumber}: '{seperatorChar}' can't be used in a concatenation.");
                    return false;
                }

                if (currentLexeme == Design.FileName)
                {
                    type = StatementType.Module;
                }
                else if (InstantiationRegex.IsMatch(currentLexeme))
                {
                    if (!ValidateInstantiation(InstantiationRegex.Match(currentLexeme), line))
                    {
                        return false;
                    }

                    type = StatementType.Submodule;
                }

                if (type == StatementType.FormatSpecifier || type == null)
                {
                    ErrorLog.Add($"{LineNumber}: '{seperatorChar}' can't be used in a format specifier or variable list statement.");
                    return false;
                }
            }
            else if (seperatorChar == ';')
            {
                if (tokens.Count == 0 || tokens.Contains(";"))
                {
                    ErrorLog.Add($"{LineNumber}: ';' can only be used to end a statement.");
                    return false;
                }
            }
            else if (seperatorChar == ',')
            {
                // Check for misplaced comma
                if (groupings.Count == 0 || groupings.Peek() != '(')
                {
                    ErrorLog.Add($"{LineNumber}: ',' can only be used inside the () in a submodule or module statement.");
                    return false;
                }
                else
                {
                    if (!(type == StatementType.Submodule || type == StatementType.Module))
                    {
                        ErrorLog.Add($"{LineNumber}: ',' can only be used inside the () in a submodule or module statement.");
                        return false;
                    }
                }
            }
            else if (seperatorChar == ':')
            {
                if (groupings.Count == 0 || groupings.Peek() != '(')
                {
                    ErrorLog.Add($"{LineNumber}: ':' can only be used to seperate input and output variables in a module or submodule statement.");
                    return false;
                }
                else
                {
                    if (!(type == StatementType.Module || type == StatementType.Submodule))
                    {
                        ErrorLog.Add($"{LineNumber}: ':' can only be used to seperate input and output variables in a module or submodule statement.");
                        return false;
                    }
                    else
                    {
                        if (tokens.Contains(":"))
                        {
                            ErrorLog.Add($"{LineNumber}: ':' can only be used once in a module or submodule statement.");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool ValidateParentheses(List<Token> tokens)
        {
            // Remove embedded parenthesis
            int parenthesesCount = tokens.Count(t => t.Type == TokenType.OpenParenthesis);
            for (int i = 1; i < parenthesesCount; i++)
            {
                // Start last open parenthesis index
                int lastOpenParenthesisIndex = -1;
                // Find last open parenthesis index
                for (int j = 0; j < tokens.Count; j++)
                {
                    if (tokens[j].Type == TokenType.OpenParenthesis)
                    {
                        lastOpenParenthesisIndex = j;
                    }
                }

                // Start matching closing parenthesis index
                int matchingCloseParenthesisIndex = -1;
                // Find matching closing parenthesis
                for (int j = lastOpenParenthesisIndex; j < tokens.Count; j++)
                {
                    if (tokens[j].Type == TokenType.CloseParenthesis)
                    {
                        matchingCloseParenthesisIndex = j;
                        break;
                    }
                }

                // Remove everthing inside parentheses
                for (int j = lastOpenParenthesisIndex; j <= matchingCloseParenthesisIndex; j++)
                {
                    tokens.RemoveAt(j);
                }
                tokens.Insert(lastOpenParenthesisIndex, new Token("()", TokenType.Scalar, -1));
            }



            List<string> expressionOperators = new List<string>();
            List<string> expressionExclusiveOperators = new List<string>();
            for (int i = 1; i < tokens.Count - 1; i++)
            {
                Token currentToken = tokens[i];
                Token previousToken = i != 1 ? tokens[i - 1] : null;
                Token nextToken = i != tokens.Count - 2 ? tokens[i + 1] : null;

                if (currentToken.Type == TokenType.MathOperator || currentToken.Type == TokenType.BooleanOperator)
                {

                }
            }
        }

        /// <summary>
        /// Validates a submodule instantiation.
        /// </summary>
        /// <param name="instantiationMatch">Instantiation</param>
        /// <param name="line">Line of instantiation</param>
        /// <returns>Whether the instantiation is valid</returns>
        private bool ValidateInstantiation(Match instantiationMatch, string line)
        {
            string designName = instantiationMatch.Groups["Design"].Value;
            string instantiationName = instantiationMatch.Groups["Name"].Value;

            if (designName == Design.FileName)
            {
                ErrorLog.Add($"{LineNumber}: You cannot instantiate from the current design.");
                return false;
            }

            if (Instantiations.ContainsKey(instantiationName))
            {
                ErrorLog.Add($"{LineNumber}: Instantiation name '{instantiationName}' is already being used.");
                return false;
            }
            else
            {
                try
                {
                    if (!Subdesigns.ContainsKey(designName))
                    {
                        string file = null;
                        string[] files = Directory.GetFiles(Design.FileSource.DirectoryName, String.Concat(designName, ".vbi"));
                        if (files.Length > 0)
                        {
                            // Check for module Declaration
                            if (DesignHasModuleDeclaration(files[0]))
                            {
                                file = files[0];
                            }
                        }

                        if (file == null)
                        {
                            for (int i = 0; i < Libraries.Count; i++)
                            {
                                files = Directory.GetFiles(Libraries[i], String.Concat(designName, ".vbi"));
                                if (files.Length > 0)
                                {
                                    // Check for module Declaration
                                    if (DesignHasModuleDeclaration(files[0]))
                                    {
                                        file = files[0];
                                        break;
                                    }
                                }
                            }

                            // If file is not found
                            if (file == null)
                            {
                                // Add file not found to error log
                                ErrorLog.Add($"{LineNumber}: Unable to find a design named '{designName}' with a module declaration.");
                                return false;
                            }
                        }

                        Subdesigns.Add(designName, file);
                        Instantiations.Add(instantiationName, line);
                    }
                    else
                    {
                        Instantiations.Add(instantiationName, line);
                    }

                    return true;
                }
                catch (Exception)
                {
                    ErrorLog.Add($"{LineNumber}: Error locating '{designName}'.");
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns whether the design has a module declaration.
        /// </summary>
        /// <param name="designPath">Path to design</param>
        /// <returns>Whether the design has a module declaration</returns>
        private bool DesignHasModuleDeclaration(string designPath)
        {
            FileInfo fileInfo = new FileInfo(designPath);
            string name = fileInfo.Name.Split('.')[0];
            using (StreamReader reader = fileInfo.OpenText())
            {
                string nextLine = string.Empty;
                while ((nextLine = reader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(nextLine, $@"^\s*{name}\({ModulePattern}\);$"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Expansion Methods

        /// <summary>
        /// Expands a vector into its components.
        /// </summary>
        /// <param name="vector">Vector to expand</param>
        /// <returns>List of vector components</returns>
        protected List<string> ExpandVector(Match vector)
        {
            // Create expansion return list
            List<string> expansion = new List<string>();

            // Get vector name, bounds and step
            string name = vector.Value.Contains("*")
                ? string.Concat(vector.Value[0], vector.Groups["Name"].Value)
                : vector.Groups["Name"].Value;
            int leftBound = Convert.ToInt32(vector.Groups["LeftBound"].Value);
            int rightBound = Convert.ToInt32(vector.Groups["RightBound"].Value);

            // If left bound is least significant bit
            if (leftBound < rightBound)
            {
                // Get vector step
                int step = string.IsNullOrEmpty(vector.Groups["Step"].Value) ? 1 : Convert.ToInt32(vector.Groups["Step"].Value);

                // For each bit in the step
                for (int i = leftBound; i <= rightBound; i += step)
                {
                    // Add bit to expansion
                    expansion.Add(string.Concat(name, i));
                }
            }
            // If right bound is least significant bit
            else
            {
                // Get vector step
                int step = string.IsNullOrEmpty(vector.Groups["Step"].Value) ? -1 : -Convert.ToInt32(vector.Groups["Step"].Value);

                // For each bit in the step
                for (int i = leftBound; i >= rightBound; i += step)
                {
                    // Add bit to expansion
                    expansion.Add(string.Concat(name, i));
                }
            }

            // If expansion hasn't been done before
            if (!ExpansionMemo.ContainsKey(vector.Value))
            {
                // Save expansion value in expansion memo
                ExpansionMemo.Add(vector.Value, expansion);
            }

            return expansion;
        }

        /// <summary>
        /// Expands a constant into its bit components.
        /// </summary>
        /// <param name="constant">Constant to expand</param>
        /// <returns>List of constant bits</returns>
        protected List<string> ExpandConstant(Match constant)
        {
            // Create expansion return list
            List<string> expansion = new List<string>();
            // Init output binary string
            string outputBinary;
            // Init char bits
            char[] charBits;

            // If constant format is hex
            if (constant.Groups["Format"].Value == "h" || constant.Groups["Format"].Value == "H")
            {
                // Get output binary in hex format
                outputBinary = Convert.ToString(Convert.ToInt32(constant.Groups["Value"].Value, 16), 2);
                // Convert binary to char bits
                charBits = outputBinary.ToCharArray();
            }
            // If constant format is decimal
            else if (constant.Groups["Format"].Value == "d" || constant.Groups["Format"].Value == "D")
            {
                // Get output binary in decimal format
                outputBinary = Convert.ToString(Convert.ToInt32(constant.Groups["Value"].Value, 10), 2);
                // Convert binary to char bits
                charBits = outputBinary.ToCharArray();
            }
            // If constant format is binary
            else if (constant.Groups["Format"].Value == "b" || constant.Groups["Format"].Value == "B")
            {
                // Convert binary to char bits
                charBits = constant.Groups["Value"].Value.ToCharArray();
            }
            // If no constant format is specified
            else
            {
                // Get output binary in decimal format
                outputBinary = Convert.ToString(Convert.ToInt32(constant.Groups["Value"].Value, 10), 2);
                // Convert binary to char bits
                charBits = outputBinary.ToCharArray();
            }

            // Get binary bits
            int[] bits = Array.ConvertAll(charBits, bit => (int)char.GetNumericValue(bit));
            // Get bit count
            int bitCount = String.IsNullOrEmpty(constant.Groups["BitCount"].Value)
                ? bits.Length
                : Convert.ToInt32(constant.Groups["BitCount"].Value);

            // If padding is necessary
            if (bitCount > bits.Length)
            {
                // Get padding count
                int padding = bitCount - bits.Length;
                // For each padding
                for (int i = 0; i < padding; i++)
                {
                    // Add padding of 0
                    expansion.Add("0");
                }
                // Remove padding count from bit count
                bitCount -= padding;
            }

            // For each specified bit
            for (int i = bits.Length - bitCount; i < bits.Length; i++)
            {
                // Add bit to expansion
                expansion.Add(bits[i].ToString());
            }

            // If expansion hasn't been done before
            if (!ExpansionMemo.ContainsKey(constant.Value))
            {
                // Save expansion value in expansion memo
                ExpansionMemo.Add(constant.Value, expansion);
            }

            return expansion;
        }

        #endregion
    }
}