﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisiBoole.ParsingEngine.ObjectCode;

namespace VisiBoole.ParsingEngine.Statements
{
    public class ConcatStmt : Statement
    {
        /// <summary>
	    /// The identifying pattern that can be used to identify and extract this statement from raw text
	    /// </summary>
        public static Regex Pattern { get; } 
            = new Regex(@"^(([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\]))\s*\=\s*\{(([a-zA-Z]+)|([a-zA-Z]+[0-9]+)|([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\]))(\,\s*(([a-zA-Z]+)|([a-zA-Z]+[0-9]+)|([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\])))*\}\;$");

        /// <summary>
        /// Constructs an instance of CommentStmt
        /// </summary>
        /// <param name="lnNum">The line number that this statement is located on within edit mode - not simulation mode</param>
        /// <param name="txt">The raw, unparsed text of this statement</param>
        public ConcatStmt(int lnNum, string txt) : base(lnNum, txt)
        {
        }

        /// <summary>
        /// Parses the Text of this statement into a list of discrete IObjectCodeElement elements
        /// to be used by the html parser to generate formatted output to be displayed in simulation mode.
        /// </summary>
        public override void Parse()
        {
            try
            {
                Regex regex = new Regex(@"^(([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\]))", RegexOptions.None); // Get left side
                string leftSide = regex.Match(Text).Value; // Left side of equal sign
                List<string> leftVars = Expand(leftSide); // Expand left side to get all left variables

                regex = new Regex(@"\{(([a-zA-Z]+)|([a-zA-Z]+[0-9]+)|([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\]))(\,\s*(([a-zA-Z]+)|([a-zA-Z]+[0-9]+)|([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\])))*\}", RegexOptions.None); // Get everything inside braces
                string rightSide = regex.Match(Text).Value; // Right side of equal sign

                regex = new Regex(@"[{\s*}]", RegexOptions.None); // Remove whitespace and braces
                rightSide = regex.Replace(rightSide, string.Empty);

                string[] parts = rightSide.Split(','); // Split variables by commas
                if (!rightSide.Contains(",")) parts[0] = rightSide;

                List<string> rightVars = new List<string>();
                foreach (string s in parts)
                {
                    regex = new Regex(@"(([a-zA-Z]+\[[0-9]+\.\.[0-9]+\])|([a-zA-Z]+\[[0-9]+\.[0-9]+\.[0-9]+\]))", RegexOptions.None);
                    if (regex.Match(s).Success)
                    {
                        List<string> expand = Expand(s);
                        foreach (string v in expand) rightVars.Add(v);
                    }
                    else rightVars.Add(s);
                }

                if (leftVars.Count != rightVars.Count) Globals.DisplayException(new Exception());

                /* Creates new variables and assings values */
                foreach (string var in leftVars)
                {
                    int value = Database.TryGetValue(var);
                    if (value != -1)
                    {
                        Output.Add((DependentVariable)Database.TryGetVariable<DependentVariable>(var)); // Output left side variable
                        Operator sign = new Operator("=");
                        Output.Add(sign); // Output =

                        value = Database.TryGetValue(rightVars[leftVars.IndexOf(var)]);
                        if (value != 1)
                        {
                            Database.SetValue(var, (value == 1));
                            IndependentVariable indVar = (IndependentVariable)Database.TryGetVariable<IndependentVariable>(rightVars[leftVars.IndexOf(var)]);
                            DependentVariable depVar = (DependentVariable)Database.TryGetVariable<DependentVariable>(rightVars[leftVars.IndexOf(var)]);

                            if (indVar != null) Output.Add(indVar); // Output right side variable
                            else Output.Add(depVar); // Output right side variable
                        }
                        else
                        {
                            // Create new variable
                            IndependentVariable indVar = new IndependentVariable(rightVars[leftVars.IndexOf(var)], false);
                            Database.AddVariable<IndependentVariable>(indVar);
                            Database.SetValue(var, false);
                            Output.Add(indVar); // Output right side variable
                        }
                    }
                    else
                    {
                        value = Database.TryGetValue(rightVars[leftVars.IndexOf(var)]);
                        if (value != -1)
                        {
                            /* Create left side variable and output it */
                            DependentVariable newVar = new DependentVariable(var, (value == 1));
                            Database.AddVariable<DependentVariable>(newVar);
                            Output.Add(newVar); // Output left side variable
                            Operator sign = new Operator("=");
                            Output.Add(sign); // Output =

                            /* Get right side variable and output it */
                            IndependentVariable indVar = (IndependentVariable)Database.TryGetVariable<IndependentVariable>(rightVars[leftVars.IndexOf(var)]);
                            DependentVariable depVar = (DependentVariable)Database.TryGetVariable<DependentVariable>(rightVars[leftVars.IndexOf(var)]);
                            if (indVar != null) Output.Add(indVar); // Output right side variable
                            else Output.Add(depVar); // Output right side variable
                        }
                        else
                        {
                            /* Create left and ride side variables and output them */
                            IndependentVariable indVar = new IndependentVariable(rightVars[leftVars.IndexOf(var)], false);
                            DependentVariable depVar = new DependentVariable(var, false);
                            Database.AddVariable<IndependentVariable>(indVar);
                            Database.AddVariable<DependentVariable>(depVar);
                            Output.Add(depVar); // Output left side variable
                            Operator sign = new Operator("=");
                            Output.Add(sign); // Output =
                            Output.Add(indVar); // Output right side variable
                        }
                    }

                    LineFeed lf = new LineFeed();
                    Output.Add(lf);
                }
            }
            catch (Exception ex)
            {
                Globals.DisplayException(ex);
            }
        }

        /// <summary>
        /// Expands an array of variables
        /// </summary>
        /// <param name="exp">Expression to expand</param>
        /// <returns>A list of all variables</returns>
        private List<string> Expand(string exp)
        {
            Regex regex = new Regex(@"[a-zA-Z]+", RegexOptions.None);
            string var = regex.Match(exp).Value;
            regex = new Regex(@"[0-9]+", RegexOptions.None);
            MatchCollection matches = regex.Matches(exp);

            int start = Convert.ToInt32(matches[0].Value);
            int step = (matches.Count == 2) ? 1 : Convert.ToInt32(matches[1].Value);
            int end = (matches.Count == 2) ? Convert.ToInt32(matches[1].Value) : Convert.ToInt32(matches[2].Value);

            List<string> vars = new List<string>();
            if (start < end)
            {
                for (int i = start; i <= end; i += step)
                    vars.Add(String.Concat(var, i.ToString()));
            }
            else
            {
                for (int i = start; i >= end; i -= step)
                    vars.Add(String.Concat(var, i.ToString()));
            }
            return vars;
        }
    }
}