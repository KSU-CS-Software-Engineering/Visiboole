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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VisiBoole.Models
{
	/// <summary>
	/// Parses the VisiBoole source code input by the user
	/// </summary>
	public class InputParser
	{
        SubDesign subDesign;
		/// <summary>
		/// The current dependent variable
		/// </summary>
		public string currentDependent;

		/// <summary>
		/// A list of any format specifiers that were parsed from the input
		/// </summary>
		private static List<FormatSpecifier> _formatSpecifiers = new List<FormatSpecifier>();

		/// <summary>
		/// Constructs an instance of InputParser
		/// </summary>
		public InputParser(SubDesign sub)//string[] codeText, string fileName)
		{
            this.subDesign = sub;
		}

		/// <summary>
		/// Parses the VisiBoole source code from the user into independent/dependent variables and their associated expressions
		/// </summary>
		/// <param name="variableClicked">The variable that was last clicked by the user, if any</param>
		public void ParseInput(string variableClicked)
		{
			if (String.IsNullOrEmpty(variableClicked))
            {
                string txt = subDesign.Text;
                byte[] byteArr = Encoding.UTF8.GetBytes(txt);
                MemoryStream stream = new MemoryStream(byteArr);
                using (StreamReader reader = new StreamReader(stream))
                {
                    string text = "";
                    int lineNumber = 1;

                    while ((text = reader.ReadLine()) != null)
                    {
                        if (!text.Contains(';'))
                        {

                        }
                        else
                        {
                            ContainsVariable(text.Substring(0, text.Length - 1), lineNumber);
                        }
                        lineNumber++;
                    }
                }
			}
			else
			{
				/*Globals.CurrentTab = subDesign.FileSourceName;
                
                
				int newValue = Negate(subDesign.Variables[variableClicked]);
				subDesign.Variables[variableClicked] = newValue;
                */

				//build list of all dependent variables based on user click
				List<string> totalVariables = new List<string>();

                /*
				foreach (string dependentVariable in subDesign.Dependencies[variableClicked])
				{
					totalVariables.Add(dependentVariable);
				}
                */

				int count = 0;
				int end = totalVariables.Count;

				while (count != end)
				{
                    /*
					for (int i = count; i < end; i++)
					{
						foreach (string dependentVariable in subDesign.Dependencies[totalVariables[i]])
						{
							totalVariables.Add(dependentVariable);
						}
					}
					count = end;
					end = totalVariables.Count;
                    */
				}

				foreach (string dependentVariable in totalVariables)
				{
					//currentDependent is used in SolveExpression()
                    
					//currentDependent = dependentVariable;
					//int updatedVariable = SolveExpression(subDesign.Expressions[dependentVariable], -1);
					//subDesign.Variables[dependentVariable] = updatedVariable;
                    
				}

				// TODO: HACK -> FIX THIS (_formatSpecifiers is static)
				_formatSpecifiers.Clear();
				foreach (var pair in hackList)
				{
					var lineNumber = pair.Item1;
					var lineOfCode = pair.Item2;
					Tuple<string, List<int>> data = ParseFormatSpecifier(lineOfCode, lineNumber);
					_formatSpecifiers.Add(new FormatSpecifier(lineNumber, data.Item1, data.Item2));
				}
				foreach (FormatSpecifier oSpcfr in _formatSpecifiers)
				{
					changeLine(subDesign, oSpcfr.LineNumber - 1, oSpcfr.Calculate());
				}
			}
			foreach (FormatSpecifier oSpcfr in _formatSpecifiers)
			{
				changeLine(subDesign, oSpcfr.LineNumber-1, oSpcfr.Calculate());
			}
		}

		private void changeLine(SubDesign rtb, int line, string text)
		{
			int s1 = rtb.GetFirstCharIndexFromLine(line);
			int s2 = line < rtb.Lines.Count() - 1 ?
					  rtb.GetFirstCharIndexFromLine(line + 1) - 1 :
					  rtb.Text.Length;
			rtb.Select(s1, s2 - s1);
			rtb.SelectedText = text;
		}

        //TODO Error Handling
		private Tuple<string, List<int>> ParseFormatSpecifier(string txt, int lnNum)
		{
			try
			{
                #region Regex for formatting
                // obtain the format specifier token
                Regex regex = new Regex(@"([ubhd])", RegexOptions.None);
				string format = regex.Match(txt).Value;

				// strip the surrounding specifier and brackets to get the content
				string content = regex.Replace(txt, string.Empty, 1);
				regex = new Regex(@"[%{};]", RegexOptions.None);
				content = regex.Replace(content, string.Empty);

				// obtain the variables within the content. First search for pattern A[N..n]
				List<int> elems = new List<int>();
				regex = new Regex(@"[a-zA-Z0-9_]+\[\d+\.\.\d\]", RegexOptions.None);
				string match = regex.Match(content).Value;
                #endregion

                if (!string.IsNullOrEmpty(match))
				{
                    #region first pattern found. Expand the expression and add the ordered variables to the list 
                    regex = new Regex(@"[a-zA-Z0-9_]+", RegexOptions.None);
					string var = regex.Match(match).Value;
					regex = new Regex(@"\d");
					MatchCollection matches = regex.Matches(match);
					int beg = Convert.ToInt32(matches[0].Value);
					int end = Convert.ToInt32(matches[1].Value);
                    

                    // arrange beg and end from smallest to largest
                    if (end < beg)
					{
						int temp = beg;
						beg = end;
						end = temp;
					}

					// add to our list the value of each variable
					for (int i = beg; i < end; i++)
					{
                        /*
						string key = string.Concat(var, i);
						if (subDesign.Variables.ContainsKey(key))
						{
							elems.Add(subDesign.Variables[key]);
						}
						else
						{
							// if a variable wasn't found then the given data is erroneous
							// TODO: throw a proper error with metadata
							throw new Exception();
						}
                        */
					}
                    #endregion
                }
                else
				{
					// first pattern was not found. Search the content for the second pattern: A1 A2 An
					regex = new Regex(@"[a-zA-Z0-9_]{1,20}", RegexOptions.None);
					MatchCollection matches = regex.Matches(content);
					foreach (Match m in matches)
					{
						// add to our list the value of each variable
                        /*
						if (subDesign.Variables.ContainsKey(m.Value))
						{
							elems.Add(subDesign.Variables[m.Value]);
						}
						else
						{
							// if a variable wasn't found then the given data is erroneous
							// TODO: throw a proper error with metadata
							throw new Exception();
						}
                        */
					}
				}

				// if no values have been gathered, then there was a user syntax error
				if (elems.Count == 0)
				{
					// TODO: throw a proper error with metadata
					throw new Exception();
				}

				return new Tuple<string, List<int>>(format, elems);
			}
			catch (Exception ex)
			{
				// TODO: proper exception handling
				Globals.DisplayException(ex);
				return null;
			}			
		}

		// TODO: HACK -> FIX THIS
		private static List<Tuple<int, string>> hackList = new List<Tuple<int, string>>();

		/// <summary>
		/// Checks to see if the line of code contains variables; if so, splits them into independent/dependent variable expressions
		/// </summary>
		/// <param name="lineOfCode">The line of code to check</param>
		/// <param name="lineNumber">The line number of the line of code to check</param>
		/// <returns>Returns the expression or the line given to it, depending on whether variables were found</returns>
		public string ContainsVariable(string lineOfCode, int lineNumber)
		{
			if (lineOfCode.Contains("%"))
			{
				hackList.Add(new Tuple<int, string>(lineNumber, lineOfCode));
				Tuple<string, List<int>> data = ParseFormatSpecifier(lineOfCode, lineNumber);
				_formatSpecifiers.Add(new FormatSpecifier(lineNumber, data.Item1, data.Item2));
			}
			else if (!lineOfCode.Contains('='))
			{
				string[] independent = lineOfCode.Split(' ');
				foreach (string s in independent)
				{
					if (s.Contains('*'))
					{
						if (!subDesign.Database.AllVars.ContainsKey(s.Substring(1)))
						{
                            
							//subDesign.Database.AllVars.Add(s.Substring(1), 1);
							//subDesign.Database.Dependencies[s.Substring(1)] = new List<string>();
                            
						}
					}
					else
					{
						if (!subDesign.Database.AllVars.ContainsKey(s))
						{
                            
							//subDesign.Variables.Add(s, 0);
							//subDesign.Dependencies[s] = new List<string>();
                            
						}
					}
				}
			}
			else
			{
				string dependent = lineOfCode.Substring(0, lineOfCode.IndexOf('='));
				currentDependent = dependent.Trim();
				if (!subDesign.Database.Dependencies.ContainsKey(currentDependent))
				{
					subDesign.Database.Dependencies.Add(currentDependent, new List<string>());
				}
				string expression = lineOfCode.Substring(lineOfCode.IndexOf('=') + 1).Trim();
				if (!subDesign.Database.Expressions.ContainsKey(currentDependent))
				{
					subDesign.Database.Expressions.Add(currentDependent, expression);
				}
				int x = SolveExpression(expression, lineNumber);
				if (!subDesign.Database.AllVars.ContainsKey(dependent.Trim()))
				{
                    
					//subDesign.Database.AllVars.Add(dependent.Trim(), x);
                    
				}
				return expression;
			}
			return lineOfCode;
		}

        #region Solves the expression from the innermost to the outermost - WELL DOCUMENTED
        /// <summary>
        /// Solves the given expression
        /// </summary>
        /// <param name="expression">The expression to solve</param>
        /// <param name="lineNumber">The line number of the expression to solve</param>
        /// <returns>Returns the line number of the expression that is solved</returns>
        public int SolveExpression(string expression, int lineNumber)
        {
            string fullExp = expression;
            string exp = "";
            string value = "";
            while(!GetInnerMostExpression(fullExp).Equals(fullExp))
            {
                exp = GetInnerMostExpression(fullExp);
                value = SolveBasicExpression(exp);
                exp = "(" + exp + ")";
                fullExp = fullExp.Replace(exp, value);
            }
            fullExp = SolveBasicExpression(fullExp);
            if(fullExp.Equals("TRUE"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public string GetInnerMostExpression(string expression)
        {
            // this variable keeps track of the ('s in the expression.
            int innerStart;
            // this variable makes sure to keep the farthest inward  (  before hitting a  )  .
            int lastStart = 0;
            // this variable finds the index innermost  )  .
            int innerEnd = expression.IndexOf(')');
            // this will be the final expression if there are  ()  within the starting expression.
            string exp;
            // check to see if any  )'s  were found.
            if (innerEnd != -1)
            {
                // chop off the right side of the expression where the  )  starts.
                exp = expression.Substring(0, innerEnd);
                // chop off all  ('s  until there is only one left.
                do
                {
                    innerStart = exp.IndexOf('(');
                    // if there was a  (  found chop off the left side of expression where the  ( starts.
                    if (innerStart != -1)
                    {
                        lastStart = innerStart;
                        exp = exp.Substring(lastStart + 1);
                    }
                } while (innerStart != -1);
                // now return the inner most expression with no  ()'s  .
                return exp;
            }
            return expression;
        }

        public string SolveBasicExpression(string expression)
        {
            // set basicExpression variable
            string basicExpression = expression;

            //
            // look for [not] gates
            //
            int notGate = basicExpression.IndexOf('~');

            // found a [not] gate
            while (notGate != -1)
            {
                // eleminating everything but the varible
                string oldVariable = basicExpression.Substring(notGate);
                if(!oldVariable.IndexOf(' ').Equals(-1))
                {
                    oldVariable = oldVariable.Substring(0, oldVariable.IndexOf(' '));
                }

                // get rid of the ~ so we can check for the variable in the dictionary
                string newVariable = oldVariable.Substring(1);

                // check to see variable is in subdesign
                /*
                if (subDesign.Variables.ContainsKey(newVariable))
                {
                    // applies [not] gate to the variable
                    if (Negate(subDesign.Variables[newVariable]) == 1)
                    {
                        // replace variable with TRUE
                        basicExpression = basicExpression.Replace(oldVariable, "TRUE");
                    }
                    else
                    {
                        // replace variable with FALSE
                        basicExpression = basicExpression.Replace(oldVariable, "FALSE");
                    }
                    // adds the current dependent variable to the dependencies of this variable
                    if (!subDesign.Dependencies[newVariable].Contains(currentDependent))
                    {
                        subDesign.Dependencies[newVariable].Add(currentDependent);
                    }
                }
                notGate = basicExpression.IndexOf('~');
                */
            }

            //
            // look for [and] gates
            // 

            // start by spliting the expression by [or] sign
            string[] andExpression = basicExpression.Split('+');

            // format the expression
            for(int i=0; i<andExpression.Length; i++)
            {
                andExpression[i] = andExpression[i].Trim();
            }

            // loop through each element
            foreach (string exp in andExpression)
            {
                // break element up to see if it has multiple variables
                string[] elements = exp.Split(' ');

                // make a new array to store int's instead of string's
                int[] inputs = new int[elements.Length];

                // loop through each element to get their boolean value
                for (int i=0; i<elements.Length; i++)
                {
                    // check for TRUE
                    if (elements[i].Equals("TRUE"))
                    {
                        inputs[i] = 1;
                    }
                    // check for FALSE
                    else if (elements[i].Equals("FALSE"))
                    {
                        inputs[i] = 0;
                    }
                    // check to see variable is in subdesign
                    /*
                    if (subDesign.Variables.ContainsKey(elements[i]))
                    {
                        //set input
                        inputs[i] = subDesign.Variables[elements[i]];
                        // adds the current dependent variable to the dependencies of this variable
                        if (!subDesign.Dependencies[elements[i]].Contains(currentDependent))
                        {
                            subDesign.Dependencies[elements[i]].Add(currentDependent);
                        }
                    }
                    */
                }

                // applies [and] gate to each input/expression
                if (And(inputs) == 1)
                {
                    // replace variable with TRUE
                    basicExpression = basicExpression.Replace(exp, "TRUE");
                }
                else
                {
                    // replace variable with FALSE
                    basicExpression = basicExpression.Replace(exp, "FALSE");
                }
            }


            //
            // look for [or] gates
            //
            string[] orExpression = basicExpression.Split('+');

            // format the expression
            for (int i = 0; i < orExpression.Length; i++)
            {
                orExpression[i] = orExpression[i].Trim();
            }

            // make a new array to store int's instead of string's
            int[] values = new int[orExpression.Length];

            // loop through each element of get their boolean value
            for (int i=0; i<orExpression.Length; i++)
            {
                // check for TRUE
                if (orExpression[i].Equals("TRUE"))
                {
                    values[i] = 1;
                }
                // check for FALSE
                else if (orExpression[i].Equals("FALSE"))
                {
                    values[i] = 0;
                }
                // it must be a variable
                else
                {
                    // check to see variable is in subdesign
                    /*
                    if (subDesign.Variables.ContainsKey(orExpression[i]))
                    {
                        // get the boolean value of the variable
                        if (subDesign.Variables[orExpression[i]] == 1)
                        {
                            //set value
                            values[i] = 1;
                        }
                        else
                        {
                            //set value
                            values[i] = 0;
                        }
                    }
                    if (!subDesign.Dependencies[orExpression[i]].Contains(currentDependent))
                    {
                        subDesign.Dependencies[orExpression[i]].Add(currentDependent);
                    }
                    */
                }
            }

            //
            // now see what the final (potential) [or] gate is equal too and return "TRUE" or "FALSE"
            //

            if(Or(values) == 1)
            {
                return "TRUE";
            }
            else
            {
                return "FALSE";
            }
        }
        #endregion

        #region Negate, And, Or function - simple logic, returns binary
        /// <summary>
        /// Negates the given value
        /// </summary>
        /// <param name="value">The value to negate</param>
        /// <returns>Returns the opposite of the given value</returns>
        public int Negate(int value)
		{
			if (value == 0)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

        public int And(int[] values)
        {
            foreach(int value in values)
            {
                if(value == 0)
                {
                    return 0;
                }
            }
            return 1;
        }

        public int Or(int[] values)
        {
            foreach(int value in values)
            {
                if(value == 1)
                {
                    return 1;
                }
            }
            return 0;
        }
        #endregion

        #region Potentially obsolete binary conversion
        /// <summary>
        /// Converts binary to decimal
        /// </summary>
        /// <param name="binary">The binary to convert to decimal</param>
        /// <returns>Returns the converted decimal</returns>
        public int BinaryToDecimal(string binary)
		{
			int dec = 0;

			for (int i = 0; i < binary.Length; i++)
			{
				if (binary[binary.Length - i - 1] == '0') continue;
				dec += (int)Math.Pow(2, i);
			}
			return dec;
		}
        #endregion
    }
}