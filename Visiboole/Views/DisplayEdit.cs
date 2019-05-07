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
using System.Windows.Forms;
using VisiBoole.Controllers;
using VisiBoole.Models;

namespace VisiBoole.Views
{
	/// <summary>
	/// The no-split input display that is hosted by the MainWindow
	/// </summary>
	public partial class DisplayEdit : UserControl, IDisplay
	{
        /// <summary>
        /// Tab control for designs in edit mode.
        /// </summary>
        private NewTabControl DesignTabControl;

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
				return DisplayType.EDIT;
			}
		}

		/// <summary>
		/// Constructs an instance of DisplaySingle
		/// </summary>
		public DisplayEdit()
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
            if (!(tabControl == null))
            {
                DesignTabControl = tabControl;
                pnlMain.Controls.Add(DesignTabControl, 0, 0);
                DesignTabControl.Dock = DockStyle.Fill;
            }
        }

        /// <summary>
        /// Returns a tab page with the provided name.
        /// </summary>
        /// <param name="name">Name of tab page.</param>
        /// <returns>Tab page with the provided name</returns>
        private TabPage FindTabPage(string name)
        {
            foreach (TabPage tabPage in DesignTabControl.TabPages)
            {
                if (tabPage.Text.TrimStart('*') == name)
                {
                    return tabPage;
                }
            }

            return null;
        }

        /// <summary>
        /// Selects the tab with the provided name if present.
        /// </summary>
        /// <param name="name">Name of tab to select</param>
        public void SelectTab(string name)
        {
            for (int i = 0; i < DesignTabControl.TabPages.Count; i++)
            {
                TabPage tabPage = DesignTabControl.TabPages[i];
                if (tabPage.Text.TrimStart('*') == name)
                {
                    DesignTabControl.SelectTab(i);
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
            for (int i = 0; i < DesignTabControl.TabPages.Count; i++)
            {
                TabPage tabPage = DesignTabControl.TabPages[i];
                if (tabPage.Text.TrimStart('*') == name)
                {
                    DesignTabControl.TabPages.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Refreshes the tab control in this display.
        /// </summary>
        public void RefreshTabControl()
        {
            DesignTabControl.Refresh();
        }

        /// <summary>
        /// Adds/updates a tab page with the provided name and the provided component.
        /// </summary>
        /// <param name="name">Name of the tab page to add or update</param>
        /// <param name="component">Component to add or update</param>
        public void AddTabComponent(string name, object component)
        {
            var design = (Design)component;

            TabPage existingTabPage = null;
            foreach (TabPage tabPage in DesignTabControl.TabPages)
            {
                if (tabPage.Text.TrimStart('*') == name)
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
                newTabPage.Controls.Add(design);
                design.Dock = DockStyle.Fill;
                DesignTabControl.TabPages.Add(newTabPage);
                DesignTabControl.SelectedTab = newTabPage;
            }
            else
            {
                existingTabPage.Controls.Clear();
                existingTabPage.Controls.Add(design);
                design.Dock = DockStyle.Fill;
            }
        }
    }
}