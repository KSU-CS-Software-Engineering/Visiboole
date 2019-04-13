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

            BrowserTabControl = new NewTabControl();
            BrowserTabControl.Font = new Font("Segoe UI", 10.75F);
            BrowserTabControl.SelectedTabColor = Color.DodgerBlue;
            BrowserTabControl.TabBoundaryColor = Color.Black;
            BrowserTabControl.SelectedTabTextColor = Color.White;
            BrowserTabControl.MouseDown += new MouseEventHandler(TabMouseDownEvent);
        }

        /// <summary>
        /// Checks whether the user is trying to close a tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabMouseDownEvent(object sender, MouseEventArgs e)
        {
            if (BrowserTabControl.SelectedIndex != -1)
            {
                Rectangle current = BrowserTabControl.GetTabRect(BrowserTabControl.SelectedIndex);
                Rectangle close = new Rectangle(current.Right - 18, current.Height - 16, 16, 16);
                TabPage tab = BrowserTabControl.SelectedTab;
                if (close.Contains(e.Location))
                {
                    if (BrowserTabControl.TabPages.Count > 1)
                    {
                        if (BrowserTabControl.SelectedIndex != 0)
                        {
                            BrowserTabControl.SelectedIndex -= 1;
                        }
                        else
                        {
                            BrowserTabControl.SelectedIndex += 1;
                        }

                    }
                    BrowserTabControl.TabPages.Remove(tab); // Remove tab page
                }
            }

            if (BrowserTabControl.TabPages.Count == 0)
            {
                Controller.SwitchDisplay();
            }
        }

        /// <summary>
        /// Saves the handle to the controller for this display
        /// </summary>
        /// <param name="controller">The handle to the controller to save</param>
        public void AttachController(IDisplayController controller)
		{
			this.Controller = controller;
		}

        /// <summary>
        /// Loads the given tabcontrol into this display
        /// </summary>
        /// <param name="tc">The tabcontrol that will be loaded by this display</param>
        public void AddTabControl(TabControl tc)
		{
            pnlMain.Controls.Add(pnlOutputControls, 0, 0);
            pnlMain.Controls.Add(BrowserTabControl, 0, 1);
            BrowserTabControl.Dock = DockStyle.Fill;
            BrowserTabControl.TabPages.Clear();
        }

        /// <summary>
        /// Loads the given web browser into this display
        /// </summary>
        /// <param name="designName">Name of the design represented by the browser</param>
        /// <param name="browser">The browser that will be loaded by this display</param>
        public void AddBrowser(string designName, WebBrowser browser)
		{
            TabPage newTab = new TabPage(designName);
            newTab.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;

            BrowserTabControl.TabPages.Add(newTab);
            BrowserTabControl.SelectedTab = newTab;

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