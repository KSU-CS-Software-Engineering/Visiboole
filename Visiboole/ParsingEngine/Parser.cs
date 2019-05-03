﻿/*
 * Copyright (C) 2019 John Devore
 * Copyright (C) 2019 Chance Henney, Juwan Moore, William Van Cleve
 * Copyright (C) 2017 Matthew Segraves, Zachary Terwort, Zachary Cleary
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program located at "\Visiboole\license.txt".
 * If not, see <http://www.gnu.org/licenses/>
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VisiBoole.Models;
using VisiBoole.ParsingEngine.ObjectCode;
using VisiBoole.ParsingEngine.Statements;
using VisiBoole.Views;

namespace VisiBoole.ParsingEngine
{
    /// <summary>
    /// The main class of the parsing engine. This class is the brains of the parsing engine and 
    /// communicates with the calling classes.
    /// </summary>
	public class Parser : Lexer
	{
        private class SourceCode
        {
            public string Text { get; private set; }

            public StatementType Type { get; private set; }

            public SourceCode(string text, StatementType type)
            {
                Text = text;
                Type = type;
            }
        }


        #region Parsing Patterns & Regular Expressions

        /// <summary>
        /// Pattern for identifying scalars. (Optional *)
        /// </summary>
        private static readonly string ScalarPattern2 = $@"(?<!([%.']))(\*?{ScalarPattern})(?![.(])";

        /// <summary>
        /// Pattern for identifying vectors. (Optional *)
        /// </summary>
        private static readonly string VectorPattern2 = $@"\*?{VectorPattern}";

        /// <summary>
        /// Pattern for identifying scalars, vectors and constants. (Optional *)
        /// </summary>
        private static readonly string VariablePattern2 = $@"({VectorPattern2}|{ConstantPattern}|{ScalarPattern2})";

        /// <summary>
        /// Pattern for identifying format specifiers.
        /// </summary>
        public static readonly string FormatSpecifierPattern = $@"({FormatterPattern}{ConcatPattern})";

        /// <summary>
        /// Pattern for identifying module instantiations.
        /// </summary>
        public static readonly string ModuleInstantiationPattern = $@"((?<Padding>\s*)?(?<Instantiation>{InstantiationPattern})\({ModulePattern}\))";

        /// <summary>
        /// Pattern for identifying whitespace.
        /// </summary>
        public static Regex WhitespaceRegex { get; } = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying scalars. (Optional *)
        /// </summary>
        public static Regex ScalarRegex { get; } = new Regex(ScalarPattern2, RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying vectors that need to be expanded.
        /// </summary>
        private static Regex VectorRegex2 { get; } = new Regex(VectorPattern2, RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying constants that need to be expanded.
        /// </summary>
        private static Regex ConstantRegex2 { get; } = new Regex($@"((?<=\W){ConstantPattern})", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying scalars, vectors and constants.
        /// </summary>
        public static Regex VariableRegex { get; } = new Regex(VariablePattern2, RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying concatenations.
        /// </summary>
        private static Regex ConcatRegex = new Regex($@"((?<!{FormatterPattern}){ConcatPattern})", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying concatenations of any type or any type.
        /// </summary>
        private static Regex AnyTypeRegex = new Regex(AnyTypePattern, RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying module instantiations.
        /// </summary>
        private static Regex ModuleInstantiationRegex = new Regex(ModuleInstantiationPattern);

        /// <summary>
        /// Regex for determining whether expansion is required.
        /// </summary>
        private static Regex ExpansionRegex { get; } = new Regex($@"((?<!{FormatterPattern}){ConcatPattern})|{VectorPattern}|((?<=\W){ConstantPattern})", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying comment statements.
        /// </summary>
        public static Regex CommentStmtRegex = new Regex(@"^(?<FrontSpacing>\s*)(?<DoInclude>[+-])?""(?<Comment>.*)""\s*;$", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying library statements.
        /// </summary>
        public static Regex LibraryStmtRegex = new Regex(@"^\s*#library\s+(?<Name>\S+)\s*;$", RegexOptions.Compiled);

        /// <summary>
        /// Regex for identifying module declarations.
        /// </summary>
        private Regex ModuleRegex;

        /// <summary>
        /// Regex for identifying submodule statements.
        /// </summary>
        private static Regex SubmoduleStmtRegex = new Regex($@"\s*(?<Instantiation>{InstantiationPattern})\({ModulePattern}\)\s*;", RegexOptions.Compiled);

        #endregion

        /// <summary>
        /// List of operators.
        /// </summary>
        public static readonly IList<string> OperatorsList = new ReadOnlyCollection<string>(new List<string> { "^", "|", "+", "-", "==", " ", "~" });
        public static readonly IList<string> ExclusiveOperatorsList = new ReadOnlyCollection<string>(new List<string>{"^", "+", "-", "=="});

        /// <summary>
        /// Statements generated by the parser.
        /// </summary>
        private List<Statement> Statements;

        /// <summary>
        /// Constructs a parser to parse designs.
        /// </summary>
        /// <param name="design">Design to parse</param>
        public Parser(Design design) : base(design)
        {
            ModuleRegex = new Regex($@"^\s*{Design.FileName}\({ModulePattern}\)\s*;$");
        }

        #region Design Helper Methods

        /// <summary>
        /// Exports the independent variables of the design.
        /// </summary>
        /// <returns></returns>
        public List<Variable> ExportState()
        {
            List<Variable> variables = new List<Variable>();
            foreach (Variable var in Design.Database.AllVars.Values)
            {
                if (var.GetType() == typeof(IndependentVariable))
                {
                    variables.Add(var);
                }
            }
            return variables;
        }

        /// <summary>
        /// Returns a list of inputs from the specified instantiation and module.
        /// </summary>
        /// <param name="instantiationName">Instantiation of module</param>
        /// <param name="moduleDeclaration">Module declaration</param>
        /// <returns>List of input variables</returns>
        public List<Variable> GetModuleInputs(string instantiationName, string moduleDeclaration)
        {
            string instantiation = Instantiations[instantiationName];
            string inputLists = Regex.Match(instantiation, ModuleInstantiationPattern).Groups["Inputs"].Value;

            // Get input values
            List<bool> inputValues = new List<bool>();
            foreach (string input in Regex.Split(inputLists, @",\s+"))
            {
                string[] vars = GetExpansion(AnyTypeRegex.Match(input)).ToArray();
                foreach (string var in vars)
                {
                    inputValues.Add(Design.Database.TryGetValue(var) == 1);
                }
            }

            // Get input variables
            List<string> inputNames = new List<string>();
            foreach (string input in Regex.Split(Regex.Match(moduleDeclaration, ModulePattern).Groups["Inputs"].Value, @",\s+"))
            {
                inputNames.AddRange(GetExpansion(AnyTypeRegex.Match(input)));
            }

            List<Variable> inputVariables = new List<Variable>();
            for (int i = 0; i < inputValues.Count; i++)
            {
                inputVariables.Add(new IndependentVariable(inputNames[i], inputValues[i]));
            }

            return inputVariables;
        }

        #endregion

        #region Parsing Methods

        #region Alternate Clock Methods

        /// <summary>
        /// Updates the stored values of all alternate clocks.
        /// </summary>
        private void UpdateAltClocks()
        {
            if (Design.Database.AltClocks.Count > 0)
            {
                // Update alt clock values
                foreach (KeyValuePair<string, AltClock> kv in Design.Database.AltClocks)
                {
                    kv.Value.UpdateValue(Design.Database.TryGetValue(kv.Key) == 1);
                }
            }
        }

        /// <summary>
        /// Ticks statements with alt clocks that go from off to on.
        /// </summary>
        /// <returns>Whether an alternate clock was ticked</returns>
        private bool TickAltClocks()
        {
            bool wasClockTicked = false;
            if (Design.Database.AltClocks.Count > 0)
            {
                foreach (KeyValuePair<string, AltClock> kv in Design.Database.AltClocks)
                {
                    if ((Design.Database.TryGetValue(kv.Key) == 1) && !(bool)kv.Value.ObjCodeValue)
                    {
                        foreach (Statement stmt in Statements)
                        {
                            if (stmt.GetType() == typeof(DffClockStmt))
                            {
                                DffClockStmt clockStmt = ((DffClockStmt)stmt);
                                if (clockStmt.AltClock == kv.Key)
                                {
                                    clockStmt.Tick();
                                    wasClockTicked = true;
                                }
                            }
                        }
                    }
                }
            }
            return wasClockTicked;
        }

        #endregion

        /// <summary>
        /// Gets the parsed output from the statement list.
        /// </summary>
        /// <returns>Parsed output</returns>
        private List<IObjectCodeElement> GetParsedOutput()
        {
            // Parse statements for output
            List<IObjectCodeElement> output = new List<IObjectCodeElement>();
            foreach (Statement statement in Statements)
            {
                statement.Parse(); // Parse output
                output.AddRange(statement.Output); // Add output
                statement.Output = new List<IObjectCodeElement>(); // Clear output
            }

            return output;
        }

        /// <summary>
        /// Runs any present submodules. Returns whether there was an error.
        /// </summary>
        /// <returns>Whether there was an error</returns>
        private bool TryRunSubmodules()
        {
            bool ranASubmodule = false;
            foreach (Statement statement in Statements)
            {
                if (statement.GetType() == typeof(SubmoduleInstantiationStmt))
                {
                    SubmoduleInstantiationStmt submodule = (SubmoduleInstantiationStmt)statement;
                    if (!submodule.TryRunInstance())
                    {
                        return false;
                    }

                    if (!ranASubmodule)
                    {
                        ranASubmodule = true;
                    }
                }
            }

            if (ranASubmodule)
            {
                Design.Database.ReevaluateExpressions();
            }
            return true;
        }

        /// <summary>
        /// Parsers the current design text into output.
        /// </summary>
        public List<IObjectCodeElement> Parse()
        {
            // Get statements for parsing
            Design.Database = new Database();
            Statements = ParseStatements();
            if (Statements == null)
            {
                ErrorListBox.Display(ErrorLog);
                return null;
            }
            if (!TryRunSubmodules())
            {
                ErrorListBox.Display(ErrorLog);
                return null;
            }

            // Get output
            UpdateAltClocks();
            return GetParsedOutput();
        }

        /// <summary>
        /// Parses the current design text and clicks the provided variable.
        /// </summary>
        /// <param name="variableName">Variable clicked</param>
        /// <returns>Parsed output</returns>
        public List<IObjectCodeElement> ParseClick(string variableName)
        {
            // Flip value of variable clicked and reevlaute expressions
            Design.Database.FlipValue(variableName);
            Design.Database.ReevaluateExpressions();
            bool didAnAltClockTick = TickAltClocks();
            UpdateAltClocks();
            if (didAnAltClockTick)
            {
                Design.Database.ReevaluateExpressions();
            }
            TryRunSubmodules();

            // Get output
            return GetParsedOutput();
        }

        /// <summary>
        /// Parsers the provided design text and ticks.
        /// </summary>
        /// <returns>Parsed output</returns>
        public List<IObjectCodeElement> ParseTick()
        {
            // Tick clock statements and reevaluate expressions
            foreach (Statement stmt in Statements)
            {
                if (stmt.GetType() == typeof(DffClockStmt))
                {
                    DffClockStmt clockStmt = ((DffClockStmt)stmt);
                    if (clockStmt.AltClock == null)
                    {
                        clockStmt.Tick();
                    }
                }
            }
            Design.Database.ReevaluateExpressions();
            bool didAnAltClockTick = TickAltClocks();
            UpdateAltClocks();
            if (didAnAltClockTick)
            {
                Design.Database.ReevaluateExpressions();
            }
            TryRunSubmodules();

            // Get output
            return GetParsedOutput();
        }

        /// <summary>
        /// Parsers the current design text with input variables.
        /// </summary>
        /// <param name="inputs">Input variables</param>
        /// <returns>Parsed output</returns>
        public List<IObjectCodeElement> ParseWithInput(List<Variable> inputs)
        {
            // Get statements for parsing
            Design.Database = new Database();
            foreach (Variable input in inputs)
            {
                Design.Database.AddVariable(new IndependentVariable(input.Name, input.Value));
            }

            Statements = ParseStatements();
            if (Statements == null)
            {
                ErrorListBox.Display(ErrorLog);
                return null;
            }
            if (!TryRunSubmodules())
            {
                ErrorListBox.Display(ErrorLog);
                return null;
            }
            UpdateAltClocks();

            // Get output
            return GetParsedOutput();
        }

        /// <summary>
        /// Parses the design as a module with the provided input values.
        /// </summary>
        /// <param name="inputValues">Values of input variables in the module declaration</param>
        /// <returns>Values of output variables in the module declaration</returns>
        public List<bool> ParseAsModule(List<bool> inputValues)
        {
            // Init database and get module match
            Design.Database = new Database();

            // Check design for valid module declaration
            if (Design.ModuleDeclaration == null || !Regex.IsMatch(Design.ModuleDeclaration, ModulePattern))
            {
                ErrorListBox.Display(new List<string>(new string[] { $"Unable to locate a valid module declaration inside design '{Design.FileName}'. Please check your source file for errors." }));
                return null;
            }

            Match moduleMatch = Regex.Match(Design.ModuleDeclaration, ModulePattern);

            // Set input values
            int inputValuesIndex = 0;
            foreach (string inputList in Regex.Split(moduleMatch.Groups["Inputs"].Value, @",\s+"))
            {
                foreach (string input in WhitespaceRegex.Split(inputList))
                {
                    foreach (string inputVar in GetExpansion(AnyTypeRegex.Match(input)))
                    {
                        Design.Database.AddVariable(new IndependentVariable(inputVar, inputValues[inputValuesIndex++]));
                    }
                }
            }

            // Parse statements
            Statements = ParseStatements();
            if (Statements == null)
            {
                ErrorListBox.Display(new List<string>(new string[] { $"Error parsing design '{Design.FileName}'. Please check/run your source file for errors." }));
                return null;
            }
            if (!TryRunSubmodules())
            {
                ErrorListBox.Display(ErrorLog);
                return null;
            }
            UpdateAltClocks();

            // Get output values
            List<bool> outputValues = new List<bool>();
            foreach (string outputList in Regex.Split(moduleMatch.Groups["Outputs"].Value, @",\s+"))
            {
                // Output each output var in the output list
                foreach (string output in WhitespaceRegex.Split(outputList))
                {
                    foreach (string outputVar in GetExpansion(AnyTypeRegex.Match(output)))
                    {
                        outputValues.Add(Design.Database.TryGetValue(outputVar) == 1);
                    }
                }
            }
            return outputValues;
        }

        #endregion

        #region Statement Creation

        /// <summary>
        /// Returns a string enumerable with the statement lines from the stream reader.
        /// </summary>
        /// <param name="streamReader">Stream reader that contains the lines</param>
        /// <returns>String enumerable containing the lines from the stream reader</returns>
        private List<string> ReadLines(StreamReader streamReader)
        {
            // Create list of lines to return
            List<string> lines = new List<string>();
            // Create current statement
            string currentStatement = "";
            int currentStatementLineNumber = 1;

            string line;
            // While the reader reads a line
            while ((line = streamReader.ReadLine()) != null)
            {
                // If line is empty
                if (line.Length == 0)
                {
                    // Add line to lines list
                    lines.Add(line);
                    // Increment statement line number
                    currentStatementLineNumber++;
                }
                // If line doesn't contain a semicolon
                else if (!line.Contains(';'))
                {
                    // If the current statement is an on going statement
                    if (currentStatement.Length > 0)
                    {
                        // Add a newline seperator
                        currentStatement += '\n';
                    }
                    // Add line to current statement
                    currentStatement += line;
                }
                // If line does contain a semicolon
                else
                {
                    // Get semicolon index
                    int semicolonIndex = line.IndexOf(';');
                    // For all characteres after the semicolon index
                    for (int i = semicolonIndex + 1; i < line.Length; i++)
                    {
                        // If the character is not an empty space
                        if (line[i] != ' ')
                        {
                            // Add multiple statements on line error to error log
                            ErrorLog.Add($"{currentStatementLineNumber}: Only one statement can appear on a line.");
                            return null;
                        }
                    }

                    // If current statement is empty
                    if (currentStatement.Length == 0)
                    {
                        // Add line to lines list
                        lines.Add(line);
                    }
                    // If current statement is not empty
                    else
                    {
                        // Add the current statement with the current line to the list of lines
                        lines.Add(string.Concat(currentStatement, "\n", line));
                        // Reset current statement
                        currentStatement = "";
                        // Increment statement line number by the number of new lines characters in the previous on going statement
                        currentStatementLineNumber += lines.Last().Count(c => c == '\n');
                    }
                    // Increment statement line number
                    currentStatementLineNumber++;
                }
            }

            // If the current statement is not empty
            if (currentStatement.Length > 0)
            {
                // Add unfinished statement error to error log
                ErrorLog.Add($"{currentStatementLineNumber}: '{currentStatement.Replace('\n', ' ')}' is missing an ending semicolon.");
                return null;
            }

            return lines;
        }

        /// <summary>
        /// Validates, expands and initializes the provided source code.
        /// </summary>
        /// <param name="sourceCode">Source code</param>
        /// <returns>List of expanded source if operations were successful</returns>
        private List<SourceCode> GetExpandedSourceCode(List<string> statementText)
        {
            // Create source code list to return
            List<SourceCode> sourceCode = new List<SourceCode>();
            // Create valid bool
            bool valid = true;
            // Start module declaration string
            Design.ModuleDeclaration = null;
            // Start line number counter
            LineNumber = 0;
            // Declare statement type
            StatementType? type = StatementType.Empty;

            // For each statement in the statement text list
            foreach (string statement in statementText)
            {
                // Increment line number counter
                LineNumber++;
                // If source is not only whitespace
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    // Get statement type
                    type = GetStatementType(statement);
                }
                // If source is only whitespace
                else
                {
                    // Set statement type to empty
                    type = StatementType.Empty;
                }

                // If statement type is null
                if (type == null)
                {
                    // If current execution is valid
                    if (valid)
                    {
                        // Set valid to false
                        valid = false;
                    }
                }
                // If statement type is library
                else if (type == StatementType.Library)
                {
                    // If library isn't valid and the current execution is valid
                    if (!VerifyLibraryStatement(statement) && valid)
                    {
                        // Set valid to false
                        valid = false;
                    }
                }
                // If statement type is submodule
                else if (type == StatementType.Submodule)
                {
                    // If submodule instantiation isn't valid and the current execution is valid
                    if (!VerifySubmoduleStatement(statement) && valid)
                    {
                        // Set valid to false
                        valid = false;
                    }
                }
                // If statement type is module
                else if (type == StatementType.Module)
                {
                    // If design doesn't have a module declaration
                    if (Design.ModuleDeclaration == null)
                    {
                        // Set the design's module declaration to the source
                        Design.ModuleDeclaration = statement;
                    }
                    // If design has a module module declaration
                    else
                    {
                        // Add invalid module statement error to error list
                        ErrorLog.Add($"{LineNumber}: Designs can only have one module declaration statement.");
                        // If current execution is valid
                        if (valid)
                        {
                            // Set valid to false
                            valid = false;
                        }
                    }
                }

                // If current execution is valid
                if (valid)
                {
                    // Add statement and its type to the list of source code
                    sourceCode.Add(new SourceCode(statement, (StatementType)type));
                }
            }

            // If not valid
            if (!valid)
            {
                return null;
            }

            // Reset line number
            LineNumber = 1;
            // For each source code in the source code list
            for (int i = 0; i < sourceCode.Count; i++)
            {
                // Get source from source code list
                SourceCode source = sourceCode[i];
                // Get whether the source can be expanded
                bool canExpand = source.Type != StatementType.Empty && source.Type != StatementType.Comment && source.Type != StatementType.Library;
                
                // Declare expanded text
                string expandedText;
                // If source can expand
                if (canExpand)
                {
                    // Get expanded text of source
                    expandedText = ExpandSource(source.Text.Replace("**", "").Replace("~~", ""));
                }
                // If source can't expand
                else
                {
                    // Set expanded text equal to the source text
                    expandedText = source.Text.Replace("**", "").Replace("~~", "");
                }
                // If expanded text is null
                if (expandedText == null)
                {
                    // If current execution is valid
                    if (valid)
                    {
                        // Set valid to false
                        valid = false;
                    }
                    // Continue to next source
                    continue;
                }

                // If current execution is valid and not empty
                if (valid && expandedText.Length != 0)
                {
                    // Remove current source code
                    sourceCode.RemoveAt(i);

                    // Get expanded source text array
                    string[] expandedSourceText = expandedText.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    // For each expanded source text in reverse
                    for (int j = expandedSourceText.Length - 1; j >= 0; j--)
                    {
                        // Get line of the expanded source text
                        string line = expandedSourceText[j];
                        // Get whether the line needs to be initialized
                        bool needsInit = source.Type != StatementType.Comment && source.Type != StatementType.Empty && source.Type != StatementType.Library;
                        // If line needs to be initialized and we are unable to init the line
                        if (needsInit && !InitSource(line, source.Type))
                        {
                            // Set valid to false
                            valid = false;
                            // End expanded source iterations
                            break;
                        }
                        // If able to init line
                        else
                        {
                            // Add line to expanded source code list
                            sourceCode.Insert(i, new SourceCode($"{line};", source.Type));
                        }
                    }
                    // Increment i by one less the number of added lines
                    i += expandedSourceText.Length - 1;
                }

                LineNumber += source.Text.Count(c => c == '\n') + 1;
            }

            // If there is a module declaration and it isn't valid
            if (!string.IsNullOrEmpty(Design.ModuleDeclaration) && !VerifyModuleDeclarationStatement())
            {
                // Set current execution to invalid
                valid = false;
            }

            // If execution is valid: return expanded source code list
            // Otherwise: return null
            return valid ? sourceCode : null;
        }

        /// <summary>
        /// Creates a list of statements from parsed source code.
        /// </summary>
        /// <returns>List of statements</returns>
        private List<Statement> ParseStatements()
        {
            // Create statement list to return
            List<Statement> statements = new List<Statement>();
            // Create statement text list
            List<string> statementText;

            // Get design text as bytes
            byte[] bytes = Encoding.UTF8.GetBytes(Design.Text);
            // Create memory stream of design bytes
            MemoryStream stream = new MemoryStream(bytes);
            // With a stream reader
            using (StreamReader reader = new StreamReader(stream))
            {
                // Read the statement text from the bytes in the stream
                statementText = ReadLines(reader);
            }
            // If statement text is null
            if (statementText == null)
            {
                return null;
            }

            // Get expanded source code from the statement text
            List<SourceCode> expandedSourceCode = GetExpandedSourceCode(statementText);
            // If expanded source code is null
            if (expandedSourceCode == null)
            {
                return null;
            }
            // For each source in the expanded source code
            foreach (SourceCode source in expandedSourceCode)
            {
                // If the source statement type is a library statement
                if (source.Type == StatementType.Library)
                {
                    // Skip library statement
                    continue;
                }
                // If the source statement type is an empty statement
                else if (source.Type == StatementType.Empty)
                {
                    // Add empty statement to statement list
                    statements.Add(new EmptyStmt(source.Text));
                }
                // If the source statement type is a comment statement
                else if (source.Type == StatementType.Comment)
                {
                    // Get comment match
                    Match commentMatch = CommentStmtRegex.Match(source.Text);
                    // If comment should be displayed
                    if (commentMatch.Groups["DoInclude"].Value != "-" && (Properties.Settings.Default.SimulationComments || commentMatch.Groups["DoInclude"].Value == "+"))
                    {
                        // Get comment to display
                        string comment = $"{commentMatch.Groups["FrontSpacing"].Value}{commentMatch.Groups["Comment"].Value}";
                        // Add comment statement to statement list
                        statements.Add(new CommentStmt(comment));
                    }
                }
                // If the source statement type is a boolean statement
                else if (source.Type == StatementType.Boolean)
                {
                    // Add boolean statement to statement list
                    statements.Add(new BooleanAssignmentStmt(source.Text));
                }
                // If the source statement type is a clock statement
                else if (source.Type == StatementType.Clock)
                {
                    // Add clock statement to statement list
                    statements.Add(new DffClockStmt(source.Text));
                }
                // If the source statement type is a variable list statement
                else if (source.Type == StatementType.VariableList)
                {
                    // Add variable list statement to statement list
                    statements.Add(new VariableListStmt(source.Text));
                }
                // If the source statement type is a format specifier statement
                else if (source.Type == StatementType.FormatSpecifier)
                {
                    // Add format specifier statement to statement list
                    statements.Add(new FormatSpecifierStmt(source.Text));
                }
                // If the source statement type is a module statement
                else if (source.Type == StatementType.Module)
                {
                    // Add module declaration statement to statement list
                    statements.Add(new ModuleDeclarationStmt(source.Text));
                }
                // If the source statement type is a submodule statement
                else if (source.Type == StatementType.Submodule)
                {
                    // Get module instantiation match
                    Match match = ModuleInstantiationRegex.Match(source.Text);
                    // Add submodule instantiation statement to statement list
                    statements.Add(new SubmoduleInstantiationStmt(source.Text, Subdesigns[match.Groups["Design"].Value]));
                }
            }

            // Return statement list
            return statements;
        }

        #endregion

        #region Statement Verifications

        /// <summary>
        /// Verifies a library statement
        /// </summary>
        /// <param name="line">Line to verify</param>
        /// <returns>Whether the line is valid or not</returns>
        private bool VerifyLibraryStatement(string line)
        {
            string library = LibraryStmtRegex.Match(line).Groups["Name"].Value;
            try
            {
                // Insert slash if not present
                /*
                if (library[0] != '.' && library[0] != '\\' && library[0] != '/')
                {
                    library = library.Insert(0, "\\");
                }
                */

                string path = Path.GetFullPath(Design.FileSource.DirectoryName + library);
                if (Directory.Exists(path))
                {
                    Libraries.Add(path);
                    return true;
                }
                else
                {
                    ErrorLog.Add($"{LineNumber}: Library '{path}' doesn't exist or is invalid.");
                    return false;
                }
            }
            catch (Exception)
            {
                ErrorLog.Add($"{LineNumber}: Invalid library name '{library}'.");
                return false;
            }
        }

        /// <summary>
        /// Verifies a module declaration statement
        /// </summary>
        /// <param name="declaration">Declaration to verifiy</param>
        /// <returns>Whether the declaration is valid or not</returns>
        private bool VerifyModuleDeclarationStatement()
        {
            // Get input and output variables
            Match module = ModuleRegex.Match(Design.ModuleDeclaration);

            // Check input variables
            foreach (string input in Regex.Split(module.Groups["Inputs"].Value, @",\s+"))
            {
                List<string> vars;
                if (!input.Contains("{") && !input.Contains("["))
                {
                    vars = new List<string>();
                    vars.Add(input);
                }
                else
                {
                    vars = GetExpansion(AnyTypeRegex.Match(input));
                }

                foreach (string var in vars)
                {
                    if (Design.Database.TryGetVariable<IndependentVariable>(var) == null)
                    {
                        ErrorLog.Insert(0, $"'{var}' must be an independent variable to be used as an input in a module declaration statement.");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Verifies the instantiation with its matching declaration.
        /// </summary>
        /// <param name="instantiation">Instantiation to verify</param>
        /// <returns>Whether the instantiation is valid</returns>
        private bool VerifySubmoduleStatement(string instantiation)
        {
            Match instantiationMatch = ModuleInstantiationRegex.Match(instantiation);
            string[] instantiationInputVars = Regex.Split(instantiationMatch.Groups["Inputs"].Value, @",\s+");
            string[] instantiationOutputVars = Regex.Split(instantiationMatch.Groups["Outputs"].Value, @",\s+");
            List<List<string>> instantiationVars = new List<List<string>>();
            foreach (string var in instantiationInputVars)
            {
                instantiationVars.Add(GetExpansion(AnyTypeRegex.Match(var)));
            }
            foreach (string var in instantiationOutputVars)
            {
                instantiationVars.Add(GetExpansion(AnyTypeRegex.Match(var)));
            }

            FileInfo fileInfo = new FileInfo(Subdesigns[instantiationMatch.Groups["Design"].Value]);
            string name = fileInfo.Name.Split('.')[0];
            Regex declarationRegex = new Regex($@"^\s*{name}\({ModulePattern}\);$");
            using (StreamReader reader = fileInfo.OpenText())
            {
                string nextLine = string.Empty;
                while ((nextLine = reader.ReadLine()) != null)
                {
                    Match match = declarationRegex.Match(nextLine);
                    if (match.Success)
                    {
                        int i = 0;

                        string[] declarationInputVars = Regex.Split(match.Groups["Inputs"].Value, @",\s+");
                        if (instantiationInputVars.Length != declarationInputVars.Length)
                        {
                            ErrorLog.Add($"{LineNumber}: Instantiation '{instantiation}' doesn't have the same number of input variables as the matching module declaration.");
                            return false;
                        }
                        string[] declarationOutputVars = Regex.Split(match.Groups["Outputs"].Value, @",\s+");
                        if (instantiationOutputVars.Length != declarationOutputVars.Length)
                        {
                            ErrorLog.Add($"{LineNumber}: Instantiation '{instantiation}' doesn't have the same number of output variables as the matching module declaration.");
                            return false;
                        }

                        foreach (string inputVar in declarationInputVars)
                        {
                            if (instantiationVars[i++].Count != GetExpansion(AnyTypeRegex.Match(inputVar)).Count)
                            {
                                ErrorLog.Add($"{LineNumber}: Instantiation '{instantiation}' doesn't have the same number of input variables as the matching module declaration.");
                                return false;
                            }
                        }

                        foreach (string outputVar in declarationOutputVars)
                        {
                            if (instantiationVars[i++].Count != GetExpansion(AnyTypeRegex.Match(outputVar)).Count)
                            {
                                ErrorLog.Add($"{LineNumber}: Instantiation '{instantiation}' doesn't have the same number of output variables as the matching module declaration.");
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Returns what line number the current index is on.
        /// </summary>
        /// <param name="source">Source text</param>
        /// <param name="index">Current index in the source text</param>
        /// <returns>Returns what line number the current index is on</returns>
        private int GetLineNumber(string source, int index)
        {
            int currentLineNumber = LineNumber;
            for (int i = 0; i < index; i++)
            {
                if (source[i] == '\n')
                {
                    currentLineNumber++;
                }
            }
            return currentLineNumber;
        }

        /// <summary>
        /// Initializes variables and dependencies in the provided source.
        /// </summary>
        /// <param name="source">Source to init</param>
        /// <param name="type">Statement type of source</param>
        /// <returns>Whether the source was initialized</returns>
        private bool InitSource(string source, StatementType? type)
        {
            // Init dependents dictionary
            List<string> dependents = new List<string>();
            // Init dependencies list
            List<string> dependencies = new List<string>();
            // Init dependent seperator index
            int dependentSeperatorIndex = source.IndexOf('=');

            // Get variables in the statement
            MatchCollection variableMatches = ScalarRegex.Matches(source);
            // Iterate through all variables in the statement
            foreach (Match variableMatch in variableMatches)
            {
                // Get variable
                string variable = variableMatch.Value;
                // Get value of variable
                bool value = variable.Contains("*");
                // If value is true
                if (value)
                {
                    // Remove * from variable
                    variable = variable.TrimStart('*');
                }
                // Get whether the variable is a dependent
                bool isDependent = variableMatch.Index < dependentSeperatorIndex;

                // If statement type is not boolean
                if (type != StatementType.Boolean)
                {
                    // If variable isn't in the database
                    if (Design.Database.TryGetVariable<Variable>(variable) == null)
                    {
                        // Add variable to the database
                        Design.Database.AddVariable(new IndependentVariable(variable, value));
                    }

                    // If variable is a dependent
                    if (isDependent)
                    {
                        // Get dependent name
                        string dependent = $"{variable}.d";
                        // If dependent isn't in the database
                        if (Design.Database.TryGetVariable<Variable>(dependent) == null)
                        {
                            // Add dependent to the database
                            Design.Database.AddVariable(new DependentVariable(dependent, value));
                        }
                    }
                }
                // If statement type is boolean
                else
                {
                    // If variable isn't dependent
                    if (!isDependent)
                    {
                        // If dependents contains the variable
                        if (dependents.Contains(variable))
                        {
                            // Circular dependency error
                            ErrorLog.Add($"{GetLineNumber(source, variableMatch.Index)}: {variable} cannot depend on itself.");
                            return false;
                        }

                        // Add variable to dependencies list
                        if (!dependencies.Contains(variable))
                        {
                            dependencies.Add(variable);
                        }
                    }
                    // If variable is dependent
                    else
                    {
                        // Add variable to dependents list
                        if (!dependents.Contains(variable))
                        {
                            dependents.Add(variable);
                        }
                    }

                    // If variable isn't in the database
                    if (Design.Database.TryGetVariable<Variable>(variable) == null)
                    {
                        // If variable isn't dependent
                        if (!isDependent)
                        {
                            // Add variable to the database
                            Design.Database.AddVariable(new IndependentVariable(variable, value));
                        }
                        // If variable is dependent
                        else
                        {
                            // Add variable to the database
                            Design.Database.AddVariable(new DependentVariable(variable, value));
                        }
                    }
                    // If variable is in the database
                    else
                    {
                        // If variable is dependent, in the database not as a dependent
                        if (isDependent && Design.Database.TryGetVariable<DependentVariable>(variable) == null)
                        {
                            // Make variable in database a dependent
                            Design.Database.MakeDependent(variable);
                        }
                    }
                }
            }

            // For each dependent in depedents list
            foreach (string dependent in dependents)
            {
                // If unable to add dependency list to database
                if (!Design.Database.TryAddDependencyList(dependent, dependencies))
                {
                    // Circular dependency error
                    ErrorLog.Add($"{GetLineNumber(source, source.IndexOf(dependent))}: {dependent} cannot depend on itself.");
                    return false;
                }
            }

            return true;
        }

        #region Expansion Methods

        /// <summary>
        /// Exapnds a single token into its components.
        /// </summary>
        /// <param name="token">Token to expand</param>
        /// <returns>List of expansion components</returns>
        protected List<string> ExpandToken(Match token)
        {
            if (token.Value.Contains("[") && string.IsNullOrEmpty(token.Groups["LeftBound"].Value))
            {
                List<string> components = Design.Database.GetComponents(token.Groups["Name"].Value);
                if (components == null)
                {
                    return null;
                }

                if (token.Value.Contains("~") && !components[0].Contains("~"))
                {
                    for (int i = 0; i < components.Count; i++)
                    {
                        components[i] = string.Concat("~", components[i]);
                    }
                }
                return components;
            }
            else
            {
                if (ExpansionMemo.ContainsKey(token.Value))
                {
                    return ExpansionMemo[token.Value].ToList();
                }
                else
                {
                    if (token.Value.Contains("["))
                    {
                        return ExpandVector(token);
                    }
                    else
                    {
                        return ExpandConstant(token);
                    }
                }
            }
        }

        /// <summary>
        /// Expands token into components.
        /// </summary>
        /// <param name="token">Token to expand</param>
        /// <returns>List of expansion components</returns>
        protected List<string> GetExpansion(Match token)
        {
            List<string> expansion = new List<string>();

            // Get token's variables
            string[] vars;
            if (token.Value.Contains("{"))
            {
                vars = WhitespaceRegex.Split(token.Value);
            }
            else
            {
                vars = new string[] { token.Value };
            }

            // Expand each variable
            foreach (string var in vars)
            {
                Match match = VariableRegex.Match(var);

                if (match.Value.Contains("[") || match.Value.Contains("'") || match.Value.All(char.IsDigit))
                {
                    List<string> tokenExpansion = ExpandToken(match);
                    if (tokenExpansion == null)
                    {
                        return null;
                    }

                    expansion.AddRange(tokenExpansion);
                }
                else
                {
                    expansion.Add(match.Value);
                }
            }

            return expansion;
        }

        /// <summary>
        /// Expands the provided source.
        /// </summary>
        /// <param name="source">Source to expand</param>
        /// <returns>Expanded source</returns>
        private string ExpandSource(string source)
        {
            // Get line count inside source
            int lineCount = source.Count(c => c == '\n');
            // Get whether the source needs to be expanded
            bool needsExpansion = ExpansionRegex.IsMatch(source) || source.Contains(':');
            // If source needs to be expanded, is an expression statement and is not a mathematical expression
            if (needsExpansion && source.Contains("=") && !source.Contains("+") && !source.Contains("-"))
            {
                // Vertical expansion needed
                source = ExpandVertically(source);
            }
            // If source needs to be expanded
            else if (needsExpansion)
            {
                // Horizontal expansion needed
                if (!source.Contains(':'))
                {
                    source = ExpandHorizontally(source);
                }
                else
                {
                    // Get text that shouldn't be expanded
                    string frontText = source.Substring(0, source.IndexOf("(") + 1);
                    // Get text that needs to be expanded
                    string restOfText = source.Substring(frontText.Length);
                    // Combine front text with the expanded form of the rest of the text
                    source = $"{frontText}{ExpandHorizontally(restOfText)}";
                }
            }

            // Increment line number by the line count
            LineNumber += lineCount;
            return source;
        }

        /// <summary>
        /// Expands all concatenations and vectors in a line.
        /// </summary>
        /// <param name="line">Line to expand</param>
        /// <returns>Expanded line</returns>
        private string ExpandHorizontally(string line)
        {
            string expandedLine = line;
            Match match;

            while ((match = VectorRegex2.Match(expandedLine)).Success)
            {
                List<string> expansion = GetExpansion(match);
                if (expansion == null)
                {
                    ErrorLog.Add($"{GetLineNumber(line, match.Index)}: '{match.Value}' is missing an explicit dimension.");
                    return null;
                }
                // Replace matched vector with its components
                expandedLine = expandedLine.Substring(0, match.Index) + string.Join(" ", expansion) + expandedLine.Substring(match.Index + match.Length);
            }

            while ((match = ConstantRegex2.Match(expandedLine)).Success && match.Value != "1" && match.Value != "0")
            {
                // Replace matched constants with its components
                expandedLine = expandedLine.Substring(0, match.Index) + string.Join(" ", GetExpansion(match)) + expandedLine.Substring(match.Index + match.Length);
            }

            if (line.Contains('='))
            {
                Regex variableListRegex = new Regex($@"{VariableListPattern}(?![^{{}}]*\}})"); // Variable lists not inside {}
                while ((match = variableListRegex.Match(expandedLine)).Success)
                {
                    // Add { } to the matched variable list
                    expandedLine = expandedLine.Substring(0, match.Index) + string.Concat("{", match.Value, "}") + expandedLine.Substring(match.Index + match.Length);
                }
            }
            else if (!line.Contains(':'))
            {
                while ((match = ConcatRegex.Match(expandedLine)).Success)
                {
                    // Replace matched concat with its components
                    expandedLine = expandedLine.Substring(0, match.Index) + string.Join(" ", GetExpansion(match)) + expandedLine.Substring(match.Index + match.Length);
                }
            }

            return expandedLine;
        }

        /// <summary>
        /// Expands a line into lines.
        /// </summary>
        /// <param name="line">Line to expand</param>
        /// <returns>Expanded lines</returns>
        protected string ExpandVertically(string line)
        {
            string expanded = string.Empty;

            // Get dependent and expression
            int start = line.ToList().FindIndex(c => char.IsWhiteSpace(c) == false); // First non whitespace character
            string dependent = line.Contains("<")
                ? line.Substring(start, line.IndexOf("<") - start).TrimEnd()
                : line.Substring(start, line.IndexOf("=") - start).TrimEnd();
            string expression = line.Substring(line.IndexOf("=") + 1).TrimStart();
            int expressionIndex = line.LastIndexOf(expression);

            // Expand dependent
            List<string> dependentExpansion = new List<string>();
            Match dependentMatch = AnyTypeRegex.Match(dependent);
            LineNumber = GetLineNumber(line, dependentMatch.Index);
            if (dependentMatch.Value.Contains("{"))
            {
                dependentExpansion = GetExpansion(dependentMatch);
                // If expansion fails
                if (dependentExpansion == null)
                {
                    ErrorLog.Add($"{GetLineNumber(line, dependentMatch.Index)}: '{dependent}' contains a [] notation that is missing an explicit dimension somewhere.");
                    return null;
                }
            }
            else if (dependentMatch.Value.Contains("["))
            {
                dependentExpansion = ExpandToken(dependentMatch);
                // If expansion fails
                if (dependentExpansion == null)
                {
                    ErrorLog.Add($"{GetLineNumber(line, dependentMatch.Index)}: '{dependentMatch.Groups["Name"].Value}[]' notation can't be used without an explicit dimension somewhere.");
                    return null;
                }
            }
            else
            {
                dependentExpansion.Add(dependent);
            }

            // Expand expression
            List<List<string>> expressionExpansions = new List<List<string>>();
            MatchCollection matches = ExpansionRegex.Matches(expression);
            foreach (Match match in matches)
            {
                LineNumber = GetLineNumber(line, match.Index);
                List<string> expansion;
                bool canPad;
                if (!match.Value.Contains("{"))
                {
                    canPad = !match.Value.Contains("[") && string.IsNullOrEmpty(match.Groups["BitCount"].Value);
                    expansion = ExpandToken(match);
                }
                else
                {
                    canPad = false;
                    expansion = GetExpansion(match);
                }

                // If expansion fails
                if (expansion == null)
                {
                    return null;
                }

                if (canPad)
                {
                    int paddingCount = dependentExpansion.Count - expansion.Count;
                    for (int i = 0; i < paddingCount; i++)
                    {
                        expansion.Insert(0, "0");
                    }
                }

                expressionExpansions.Add(expansion);
            }

            // Verify expansions
            for (int i = 0; i < expressionExpansions.Count; i++)
            {
                List<string> expressionExpansion = expressionExpansions[i];
                if (dependentExpansion.Count != expressionExpansion.Count)
                {
                    ErrorLog.Add($"{GetLineNumber(line, matches[i].Index)}: Expansion count of '{matches[i].Value}' doesn't match the expansion count of '{dependent}'.");
                    return null;
                }
            }

            // Combine expansions
            List<List<string>> expansions = new List<List<string>>();
            expansions.Add(dependentExpansion);
            expansions.AddRange(expressionExpansions);

            // Expand line into lines
            for (int i = 0; i < dependentExpansion.Count; i++)
            {
                string newLine = line;

                for (int j = matches.Count - 1; j >= 0; j--)
                {
                    Match match = matches[j];
                    string beforeMatch = newLine.Substring(0, match.Index + expressionIndex);
                    string afterMatch = newLine.Substring(match.Index + expressionIndex + match.Length);
                    newLine = string.Concat(beforeMatch, expansions[j + 1][i], afterMatch);
                }

                string beforeDependent = newLine.Substring(0, start);
                string afterDependent = newLine.Substring(start + dependent.Length);
                newLine = string.Concat(beforeDependent, expansions[0][i], afterDependent);

                expanded += newLine;
            }

            return expanded;
        }

        #endregion
    }
}