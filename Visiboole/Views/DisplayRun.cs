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

using CustomTabControl;
using System;
using System.Drawing;
using System.Windows.Forms;
using VisiBoole.Controllers;

namespace VisiBoole.Views
{
    /// <summary>
    /// The horizontally-split display that is hosted by the MainWindow
    /// </summary>
    public partial class DisplayRun : UserControl, IDisplay
    {
        /// <summary>
        /// Event that occurs when the display tab is changed.
        /// </summary>
        public event DisplayTabChangeEventHandler DisplayTabChanged;

        /// <summary>
        /// Event that occurs when the display tab is closed.
        /// </summary>
        public event DisplayTabChangeEventHandler DisplayTabClosed;

        /// <summary>
        /// Controller for this display.
        /// </summary>
        private IDisplayController Controller;

        /// <summary>
        /// Tab control for this display.
        /// </summary>
        private NewTabControl TabControl;

        /// <summary>
        /// Type of this display.
        /// </summary>
        public DisplayType DisplayType { get { return DisplayType.RUN; } }

        /// <summary>
        /// Constucts an instance of DisplaySingleOutput
        /// </summary>
        public DisplayRun()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Saves the handle to the controller for this display
        /// </summary>
        /// <param name="controller">The handle to the controller to save</param>
        public void AttachController(IDisplayController controller)
        {
            Controller = controller;
        }

        /// <summary>
		/// Loads the given tab control into this display.
		/// </summary>
		/// <param name="tabControl">The tabcontrol that will be loaded by this display</param>
		public void AttachTabControl(NewTabControl tabControl)
        {
            TabControl = tabControl;
            tabControl.SelectedIndexChanged += (sender, eventArgs) => {
                string tabName = tabControl.SelectedIndex != -1 ? tabControl.TabPages[TabControl.SelectedIndex].Text : null;
                DisplayTabChanged?.Invoke(tabName);
            };
            tabControl.TabClosed += (sender, eventArgs) => {
                DisplayTabClosed?.Invoke(((TabPage) sender).Text);
            };
            pnlMain.Controls.Add(pnlOutputControls, 0, 0);
            pnlMain.Controls.Add(TabControl, 0, 1);
            TabControl.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Selects the tab with the provided name if present.
        /// </summary>
        /// <param name="name">Name of tab to select</param>
        public void SelectTab(string name)
        {
            for (int i = 0; i < TabControl.TabPages.Count; i++)
            {
                TabPage tabPage = TabControl.TabPages[i];
                if (tabPage.Text == name)
                {
                    TabControl.SelectTab(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Closes the tab with the provided name if present.
        /// </summary>
        /// <param name="name"></param>
        public void CloseTab(string name)
        {
            for (int i = 0; i < TabControl.TabPages.Count; i++)
            {
                TabPage tabPage = TabControl.TabPages[i];
                if (tabPage.Text == name)
                {
                    TabControl.TabPages.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Closes all tabs.
        /// </summary>
        public void CloseTabs()
        {
            for (int i = 0; i < TabControl.TabCount; i++)
            {
                TabControl.TabPages.RemoveAt(0);
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
            TabControl.Refresh();
        }

        /// <summary>
        /// Creates the document text from the body html.
        /// </summary>
        /// <param name="html">Body html of the document</param>
        /// <returns>Document text</returns>
        private string CreateDocumentText(string html)
        {
            string falseColor = Properties.Settings.Default.Colorblind ? "royalblue" : "green";
            html = html.Replace("\"@TRUE@\"", "\"crimson\"").Replace("\"@FALSE@\"", $"\"{falseColor}\"").Replace("@SIZE@pt", $"{Properties.Settings.Default.FontSize}pt");
            return "<html><head><meta name=\"viewport\" content=\"initial-scale=1.0, user-scalable=no\" />"
            + "<style type=\"text/css\"> p {{ margin: 0; font-family: consolas; } </style>"
            + "<script>function KeyPress(e) { var evtobj = window.event? event : e; if (evtobj.ctrlKey &&"
            + "(evtobj.keyCode == 187 || evtobj.keyCode == 189 || evtobj.keyCode == 107 || evtobj.keyCode == 109"
            + "|| evtobj.keyCode == 61 || evtobj.keyCode == 173)) { e.preventDefault(); } }</script></head><body>"
            + html + "</body></html>";
        }

        /// <summary>
        /// Adds/updates a tab page with the provided name and the provided component.
        /// </summary>
        /// <param name="name">Name of the tab page to add or update</param>
        /// <param name="component">Component to add or update</param>
        /// <param name="swap">Whether to swap to the new component</param>
        public void AddTabComponent(string name, object component, bool swap = true)
        {
            string html = (string)component;

            TabPage existingTabPage = null;
            foreach (TabPage tabPage in TabControl.TabPages)
            {
                if (tabPage.Text == name)
                {
                    existingTabPage = tabPage;
                    break;
                }
            }

            if (existingTabPage == null)
            {
                TabPage newTabPage = new TabPage(name);
                newTabPage.Text = name;
                newTabPage.ToolTipText = name;

                // Init browser
                WebBrowser browser = new WebBrowser();
                browser.IsWebBrowserContextMenuEnabled = false;
                browser.AllowWebBrowserDrop = false;
                browser.WebBrowserShortcutsEnabled = false;
                browser.ObjectForScripting = Controller;
                // Create browser with empty body
                browser.DocumentText = CreateDocumentText(html);
                browser.PreviewKeyDown += (sender, eventArgs) => {
                    if (eventArgs.Control)
                    {
                        if (eventArgs.KeyCode == Keys.E)
                        {
                            CloseTabs();
                        }
                        else if (eventArgs.KeyCode == Keys.Add || eventArgs.KeyCode == Keys.Oemplus)
                        {
                            Properties.Settings.Default.FontSize += 2;
                            Controller.RefreshOutput();
                        }
                        else if (eventArgs.KeyCode == Keys.Subtract || eventArgs.KeyCode == Keys.OemMinus)
                        {
                            if (Properties.Settings.Default.FontSize > 9)
                            {
                                Properties.Settings.Default.FontSize -= 2;
                                Controller.RefreshOutput();
                            }
                        }
                    }
                };
                browser.DocumentCompleted += (sender, eventArgs) => {
                    browser.Document.Body.DoubleClick += (sender2, eventArgs2) => {
                        Controller.RetrieveFocus();
                    };
                };

                newTabPage.Controls.Add(browser);
                browser.Dock = DockStyle.Fill;
                TabControl.TabPages.Add(newTabPage);
                if (swap)
                {
                    TabControl.SelectedTab = newTabPage;
                }
            }
            else
            {
                WebBrowser browser = ((WebBrowser)existingTabPage.Controls[0]);
                browser.Document.Body.InnerHtml = html;
                if (swap)
                {
                    TabControl.SelectedTab = existingTabPage;
                }
            }

            pnlMain.Focus();
        }

        /// <summary>
        /// Handles the event when the tick button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTick_Click(object sender, System.EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            Controller.Tick(1);
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Handles the event when the multi tick button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMultiTick_Click(object sender, System.EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            Controller.Tick(Convert.ToInt32(numericUpDown1.Value));
            Cursor = Cursors.Default;
        }
    }
}