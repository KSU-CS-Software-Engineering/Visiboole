/*
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

using System.Collections.Generic;
using System.Security.Permissions;
using System.Windows.Forms;
using VisiBoole.Models;
using VisiBoole.ParsingEngine;
using VisiBoole.Views;
using VisiBoole.ParsingEngine.ObjectCode;
using System.Drawing;
using System.Threading;
using System;
using CustomTabControl;

namespace VisiBoole.Controllers
{
    /// <summary>
    /// The different display types for the UserControl displays that are hosted by the MainWindow
    /// </summary>
    public enum DisplayType
    {
        EDIT,
        RUN,
        NONE
    }

    /// <summary>
    /// Handles the logic, and communication with other objects for the displays hosted by the MainWindow
    /// </summary>
	[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	public class DisplayController : IDisplayController
	{
		/// <summary>
		/// No-split input view that is hosted by the MainWindow
		/// </summary>
		private IDisplay EditDisplay;

		/// <summary>
		/// Horizontal-split view that is hosted by the MainWindow
		/// </summary>
		private IDisplay RunDisplay;

		/// <summary>
		/// Handle to the controller for the MainWindow
		/// </summary>
		private IMainWindowController MainWindowController;

        /// <summary>
        /// HTMLBuilder for the web browser.
        /// </summary>
        private HtmlBuilder HtmlBuilder;

		/// <summary>
		/// The WebBrowser that shows the output that is shared amongst the displays that are hosted by the MainWindow
		/// </summary>
		private WebBrowser Browser;

        /// <summary>
        /// Html output template for the browser
        /// </summary>
        private string OutputTemplate = "<html><head><style type=\"text/css\"> p { margin: 0;} </style></head><body>{0}</body></html>";

        /// <summary>
        /// Last output of the browser.
        /// </summary>
        private List<IObjectCodeElement> LastOutput;

        private TreeNode InstantiationClicks;

        /// <summary>
        /// The display that was hosted by the MainWindow before the current one
        /// </summary>
        public IDisplay PreviousDisplay { get; set; }

		/// <summary>
		/// The display that is currently hosted by the MainWindow
		/// </summary>
		private IDisplay currentDisplay;

		/// <summary>
		/// The display that is currently hosted by the MainWindow
		/// </summary>
		public IDisplay CurrentDisplay
		{
			get
			{
				return currentDisplay;
			}
			set
			{
				value.AddTabControl(TabControl);
                string currentDesign = TabControl.SelectedTab != null ? TabControl.SelectedTab.Text.TrimStart('*') : "";
				value.AddTabComponent(currentDesign, Browser);
				currentDisplay = value;
			}
		}

        /// <summary>
        /// Constructs an instance of DisplayController with a handle to the two displays.
        /// </summary>
        /// <param name="editDisplay">Handle to the edit display hosted by the MainWindow</param>
        /// <param name="runDisplay">Handle to the run display hosted by the MainWindow</param>
        public DisplayController(IDisplay editDisplay, IDisplay runDisplay)
		{
            // Init tab control
			var designTabControl = new NewTabControl();
            designTabControl.Font = new Font("Segoe UI", 10.75F);
            designTabControl.SelectedTabColor = Color.DodgerBlue;
            designTabControl.TabBoundaryColor = Color.Black;
            designTabControl.SelectedTabTextColor = Color.White;
            designTabControl.SelectedIndexChanged += (sender, e) => {
                string fileSelection = designTabControl.SelectedIndex != -1 ? designTabControl.SelectedTab.Text.TrimStart('*') : null;
                MainWindowController.SelectFile(fileSelection);
                MainWindowController.LoadDisplay(DisplayType.EDIT);
            };
            designTabControl.TabClosing += (sender) => {
                MainWindowController.CloseActiveFile();
            };
            designTabControl.TabSwap += (sender, e) => {
                MainWindowController.SwapDesignNodes(e.SourceTabPageIndex, e.DestinationTabPageIndex);
            };

            var browserTabControl = new NewTabControl();
            browserTabControl.Font = new Font("Segoe UI", 10.75F);
            browserTabControl.SelectedTabColor = Color.DodgerBlue;
            browserTabControl.TabBoundaryColor = Color.Black;
            browserTabControl.SelectedTabTextColor = Color.White;
            browserTabControl.SelectedIndexChanged += (sender, e) => {
                if (browserTabControl.SelectedIndex != -1)
                {
                    MainWindowController.SelectParser(browserTabControl.TabPages[browserTabControl.SelectedIndex].Name);
                }
            };
            browserTabControl.TabClosing += (sender) => {
                MainWindowController.CloseParser(((TabPage)sender).Name);
            };
            browserTabControl.TabClosed += (sender, e) => {
                if (e.TabPagesCount == 0)
                {
                    SwitchDisplay();
                }
            };

            // Init browser
            Browser = new WebBrowser();
            Browser.IsWebBrowserContextMenuEnabled = false;
            Browser.AllowWebBrowserDrop = false;
            Browser.WebBrowserShortcutsEnabled = false;
            Browser.ObjectForScripting = this;
            // Create browser with empty body
            Browser.DocumentText = OutputTemplate.Replace("{0}", "");
            Browser.PreviewKeyDown += (sender, eventArgs) => {
                if (eventArgs.Control)
                {
                    if (eventArgs.KeyCode == Keys.E)
                    {
                        MainWindowController.LoadDisplay(DisplayType.EDIT);
                    }
                    else if (eventArgs.KeyCode == Keys.Add || eventArgs.KeyCode == Keys.Oemplus)
                    {
                        Properties.Settings.Default.FontSize += 2;
                        MainWindowController.SetFontSize();
                        MainWindowController.RefreshOutput();
                    }
                    else if (eventArgs.KeyCode == Keys.Subtract || eventArgs.KeyCode == Keys.OemMinus)
                    {
                        if (Properties.Settings.Default.FontSize > 9)
                        {
                            Properties.Settings.Default.FontSize -= 2;
                            MainWindowController.SetFontSize();
                            RefreshOutput();
                        }
                    }
                }
            };

            InstantiationClicks = new TreeNode();

            // Create html builder
            HtmlBuilder = new HtmlBuilder();

            // Init displays
            EditDisplay = editDisplay;
            EditDisplay.AddTabControl(designTabControl);

			RunDisplay = runDisplay;
            RunDisplay.AddTabControl(browserTabControl);

			CurrentDisplay = editDisplay;
        }

        /// <summary>
        /// Saves the handle to the controller for the MainWindow
        /// </summary>
        /// <param name="mainWindowController"></param>
        public void AttachMainWindowController(IMainWindowController mainWindowController)
		{
			MainWindowController = mainWindowController;
        }

        /// <summary>
		/// Returns a handle to the display of the matching type
		/// </summary>
		/// <param name="dType">The type of the display to return</param>
		/// <returns>Returns a handle to the display of the matching type</returns>
		public IDisplay GetDisplayOfType(DisplayType dType)
        {
            switch (dType)
            {
                case DisplayType.EDIT:
                    return EditDisplay;
                case DisplayType.RUN:
                    return RunDisplay;
                default: return null;
            }
        }

        /// <summary>
		/// Selects the tab page with the provided name.
		/// </summary>
		/// <param name="name">Name of tabpage to select</param>
		public void SelectTabPage(string name)
        {
            CurrentDisplay.SelectTab(name);
        }

        /// <summary>
		/// Creates a new tab on the design tab control.
		/// </summary>
		/// <param name="design">The Design that is displayed in the new tab</param>
		/// <returns>Returns true if a new tab was successfully created</returns>
		public void CreateNewTab(Design design)
        {
            CurrentDisplay.AddTabComponent(design.FileName, design);
        }

        /// <summary>
        /// Closes a specific tab in the tab control.
        /// </summary>
        /// <param name="designName">Name of the design being closed</param>
        /// <returns>Whether the operation was successful</returns>
        public bool CloseTab(string designName)
        {
            TabPage tab = TabControl.TabPages[GetDesignTabIndex(designName)];

            if (tab != null)
            {
                if (TabControl.SelectedIndex != 0)
                {
                    TabControl.SelectedIndex -= 1;
                }
                else
                {
                    TabControl.SelectedIndex += 1;
                }

                TabControl.TabPages.Remove(tab); // Remove tab page

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the provided tab with a new design. (Used for SaveAs operations)
        /// </summary>
        /// <param name="tabName">Name of tab</param>
        /// <param name="newDesign">Design to add</param>
        public void UpdateTab(string tabName, Design newDesign)
        {
            // Get index of tab
            int designTabIndex = GetDesignTabIndex(tabName);
            // Get tab from tab control
            TabPage tab = TabControl.TabPages[designTabIndex];
            // Remove design from tab's controls
            tab.Controls.Clear();
            // Update tab text
            tab.Text = newDesign.FileName;
            // Update tab tool tip text
            tab.ToolTipText = $"{tab.Text}.vbi";
            // Add new design to tab
            tab.Controls.Add(newDesign);
            // Fill tab with new design
            newDesign.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Updates the tab text to include or remove the dirty indicator.
        /// </summary>
        /// <param name="designName">Design name of the tab to update</param>
        /// <param name="isDirty">Whether the design has unsaved changes</param>
        public void UpdateTabText(string designName, bool isDirty)
        {
            int designTabIndex = GetDesignTabIndex(designName);
            if (designTabIndex != -1)
            {
                TabControl.TabPages[designTabIndex].Text = isDirty ? $"*{designName}" : designName;
            }
        }

        /// <summary>
        /// Sets the theme of edit and run tab control
        /// </summary>
        public void SetTheme()
        {
            TabControl.BackgroundColor = Properties.Settings.Default.Theme == "Light" ? Color.AliceBlue : Color.FromArgb(66, 66, 66);
            TabControl.TabColor = Properties.Settings.Default.Theme == "Light" ? Color.White : Color.FromArgb(66, 66, 66);
            TabControl.TabTextColor = Properties.Settings.Default.Theme == "Light" ? Color.Black : Color.White;

            if (CurrentDisplay is DisplayEdit)
            {
                TabControl.Refresh();
            }
            else
            {
                
            }
        }

        /// <summary>
        /// Displays the provided output to the Browser.
        /// </summary>
        /// <param name="output">Output of the parsed design</param>
        /// <param name="position">Scroll position of the Browser</param>
		public void DisplayOutput(List<IObjectCodeElement> output, int position = 0)
        {
            //Browser.ObjectForScripting = this;
            Browser.Document.Body.ScrollTop = position;
            Browser.Document.Body.InnerHtml = HtmlBuilder.GetHTML(output);

            if (CurrentDisplay is DisplayEdit)
            {
                MainWindowController.LoadDisplay(DisplayType.RUN);
                InstantiationClicks = new TreeNode();
            }

            LastOutput = output;
        }

        /// <summary>
        /// Handles the event that occurs when the Browser needs to be refreshed.
        /// </summary>
        public void RefreshOutput()
        {
            //Browser.ObjectForScripting = this;
            DisplayOutput(LastOutput, Browser.Document.Body.ScrollTop);
        }

        /// <summary>
        /// Switches the display to the edit mode.
        /// </summary>
        public void SwitchDisplay()
        {
            MainWindowController.LoadDisplay(DisplayType.EDIT);
        }

        /// <summary>
        /// Handles the event that occurs when the user ticks.
        /// </summary>
        /// <param name="count">Number of times to tick</param>
        public void Tick(int count)
        {
            //Browser.ObjectForScripting = this;
            int position = Browser.Document.Body.ScrollTop;

            for (int i = 0; i < count; i++)
            {
                DisplayOutput(MainWindowController.Tick(), position);
            }
        }

        /// <summary>
        /// Handles the event that occurs when the user clicks on an independent variable.
        /// </summary>
        /// <param name="variableName">The name of the variable that was clicked by the user</param>
        /// <param name="value">Value for formatter click</param>
        public void Variable_Click(string variableName, string value = null)
        {
            //Browser.ObjectForScripting = this;
            DisplayOutput(MainWindowController.Variable_Click(variableName, value), Browser.Document.Body.ScrollTop);
            if (InstantiationClicks.Nodes.Count > 0)
            {
                foreach (TreeNode node in InstantiationClicks.Nodes)
                {
                    Instantiation_Click(node.Text, false);
                }
            }
            MainWindowController.RetrieveFocus();
        }

        /// <summary>
        /// Handles the event that occurs when the user clicks on an instantiation.
        /// </summary>
        /// <param name="instantiation">The instantiation that was clicked by the user</param>
        public void Instantiation_Click(string instantiation, bool addNode = true)
        {
            if (addNode)
            {
                if (InstantiationClicks.Nodes.Count != 0)
                {
                    foreach (TreeNode node in InstantiationClicks.Nodes)
                    {
                        if (node.Text.Split('.')[0] == DesignController.ActiveDesign.FileName)
                        {
                            node.Nodes.Add(instantiation);
                        }
                    }
                }
                else
                {
                    InstantiationClicks.Nodes.Add(instantiation);
                }
            }

            List<IObjectCodeElement> output = MainWindowController.RunSubdesign(instantiation);
            if (output == null)
            {
                return;
            }

            WebBrowser subBrowser = new WebBrowser();
            subBrowser.IsWebBrowserContextMenuEnabled = false;
            subBrowser.AllowWebBrowserDrop = false;
            subBrowser.WebBrowserShortcutsEnabled = false;
            subBrowser.ObjectForScripting = this;
            subBrowser.DocumentText = OutputTemplate.Replace("{0}", HtmlBuilder.GetHTML(output, true));
            subBrowser.PreviewKeyDown += (sender, eventArgs) => {
                if (eventArgs.Control)
                {
                    if (eventArgs.KeyCode == Keys.E)
                    {
                        MainWindowController.LoadDisplay(DisplayType.EDIT);
                    }
                    else if (eventArgs.KeyCode == Keys.Add || eventArgs.KeyCode == Keys.Oemplus)
                    {
                        Properties.Settings.Default.FontSize += 2;
                        MainWindowController.SetFontSize();
                        MainWindowController.RefreshOutput();
                    }
                    else if (eventArgs.KeyCode == Keys.Subtract || eventArgs.KeyCode == Keys.OemMinus)
                    {
                        if (Properties.Settings.Default.FontSize > 9)
                        {
                            Properties.Settings.Default.FontSize -= 2;
                            MainWindowController.SetFontSize();
                            RefreshOutput();
                        }
                    }
                }
            };

            CurrentDisplay.AddTabComponent(instantiation.Split('.')[0], subBrowser);
        }
    }
}