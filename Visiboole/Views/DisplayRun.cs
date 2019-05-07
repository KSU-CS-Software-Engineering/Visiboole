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
        /// Tab control for designs in run mode.
        /// </summary>
        private NewTabControl BrowserTabControl;

		/// <summary>
		/// Handle to the controller for this display
		/// </summary>
		private IDisplayController Controller;

		/// <summary>
		/// Returns the type of this display
		/// </summary>
		public DisplayType TypeOfDisplay
		{
			get
			{
                return DisplayType.RUN;
			}
		}

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
		/// Loads the given tabcontrol into this display
		/// </summary>
		/// <param name="tabControl">The tabcontrol that will be loaded by this display</param>
		public void AddTabControl(NewTabControl tabControl)
        {
            BrowserTabControl = tabControl;

            pnlMain.Controls.Add(pnlOutputControls, 0, 0);
            pnlMain.Controls.Add(BrowserTabControl, 0, 1);
            BrowserTabControl.Dock = DockStyle.Fill;
            BrowserTabControl.TabPages.Clear();
            numericUpDown1.Value = 7; // Reset value
        }

        /// <summary>
        /// Selects the tab with the provided name if present.
        /// </summary>
        /// <param name="name">Name of tab to select</param>
        public void SelectTab(string name)
        {
            for (int i = 0; i < BrowserTabControl.TabPages.Count; i++)
            {
                TabPage tabPage = BrowserTabControl.TabPages[i];
                if (tabPage.Text == name)
                {
                    BrowserTabControl.SelectTab(i);
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
            for (int i = 0; i < BrowserTabControl.TabPages.Count; i++)
            {
                TabPage tabPage = BrowserTabControl.TabPages[i];
                if (tabPage.Text == name)
                {
                    BrowserTabControl.TabPages.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Refreshes the tab control in this display.
        /// </summary>
        public void RefreshTabControl()
        {
            BrowserTabControl.Refresh();
        }

        /// <summary>
        /// Adds/updates a tab page with the provided name and the provided component.
        /// </summary>
        /// <param name="name">Name of the tab page to add or update</param>
        /// <param name="component">Component to add or update</param>
        public void AddTabComponent(string name, object component)
        {
            string designName = name;
            WebBrowser browser = (WebBrowser)component;

            TabPage existingTabPage = null;
            foreach (TabPage tabPage in BrowserTabControl.TabPages)
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
                newTabPage.ToolTipText = $"{name}.vbi";
                newTabPage.Controls.Add(browser);
                browser.Dock = DockStyle.Fill;
                BrowserTabControl.TabPages.Add(newTabPage);
                BrowserTabControl.SelectedTab = newTabPage;
            }
            else
            {
                existingTabPage.Controls.Clear();
                existingTabPage.Controls.Add(browser);
                browser.Dock = DockStyle.Fill;
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
            Controller.Tick(1);
        }

        /// <summary>
        /// Handles the event when the multi tick button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMultiTick_Click(object sender, System.EventArgs e)
        {
            Controller.Tick(Convert.ToInt32(numericUpDown1.Value));
        }
    }
}