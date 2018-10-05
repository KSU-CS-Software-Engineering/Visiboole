﻿using System;
using System.Windows.Forms;
using VisiBoole.Controllers;

namespace VisiBoole.Views
{
	/// <summary>
	/// The MainWindow of this application
	/// </summary>
	public partial class MainWindow : Form, IMainWindow
	{
		/// <summary>
		/// Handle to the controller for this view
		/// </summary>
		private IMainWindowController controller;

		#region "Class Initialization"

		/// <summary>
		/// Constructs an instance of MainWindow
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Saves the handle to the controller for this view
		/// </summary>
		/// <param name="controller">The handle to the controller for this view</param>
		public void AttachController(IMainWindowController controller)
		{
			this.controller = controller;
		}

        #endregion

        #region "Utility Methods"

        /// <summary>
        /// Change buttons and icons based on the new display
        /// </summary>
        /// <param name="current"></param>
        private void ChangeControls(IDisplay display)
        {
            openIcon.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT);
            openToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT);
            newIcon.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT);
            newToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT);
            saveIcon.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
            saveAllIcon.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
            saveToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
            saveAsToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
            printToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
            printPreviewToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT) && NavTree.Nodes[0].Nodes.Count > 0;
            runModeToggle.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
            editModeToggle.Enabled = (display.TypeOfDisplay == Globals.DisplayType.RUN);
            closeDesignToolStripMenuItem.Enabled = (display.TypeOfDisplay == Globals.DisplayType.EDIT && NavTree.Nodes[0].Nodes.Count > 0);
        }

		/// <summary>
		/// Adds a new node in the TreeView
		/// </summary>
		/// <param name="path">The filepath string that will be parsed to obtain the name of this treenode</param>
		public void AddNavTreeNode(string path)
		{
			string filename = path.Substring(path.LastIndexOf("\\") + 1);
			TreeNode node = new TreeNode(filename);

			node.Name = filename;

            if (NavTree.Nodes.ContainsKey(filename))
            {
                Globals.DisplayException(new Exception(string.Concat("Node ", filename, " already exists in 'My SubDesings'.")));
            }

			NavTree.Nodes[0].Nodes.Add(node);
			NavTree.ExpandAll();
		}

        /// <summary>
		/// Removes a node in the TreeView
		/// </summary>
		/// <param name="name">The name of the node to be removed</param>
		public void RemoveNavTreeNode(string name)
        {
            NavTree.Nodes[0].Nodes.RemoveByKey(name);
        }

        /// <summary>
        /// Confirms exit with the user if the application is dirty
        /// </summary>
        /// <param name="isDirty">True if any open SubDesigns have been modified since last save</param>
        public void ConfirmExit(bool isDirty)
		{
			if (isDirty == true)
			{
				System.Media.SystemSounds.Asterisk.Play();
				DialogResult response = MessageBox.Show("You have made changes that have not been saved - do you wish to continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

				if (response == DialogResult.Yes)
				{
					Application.Exit();
				}
			}
			else
			{
				Application.Exit();
			}
		}

        /// <summary>
        /// Confrims whether the user wants to close the selected SubDesign
        /// </summary>
        /// <param name="isDirty">True if the SubDesign being closed has been modified since last save</param>
        /// <returns>Whether the selected SubDesign will be closed</returns>
		public bool ConfirmClose(bool isDirty)
        {
            if (isDirty == true)
            {
                System.Media.SystemSounds.Asterisk.Play();
                DialogResult response = MessageBox.Show("You have made changes to the file you are trying to close - do you wish to continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (response == DialogResult.Yes) return true;
                else return false;
            }
            else return true;
        }

        /// <summary>
        /// Loads the given IDisplay
        /// </summary>
        /// <param name="previous">The display to replace</param>
        /// <param name="current">The display to be loaded</param>
        public void LoadDisplay(IDisplay previous, IDisplay current)
		{
            if (current == null)
            {
                Globals.DisplayException(new ArgumentNullException("Unable to load given display - the given display is null."));
            }

            if (!this.MainLayoutPanel.Controls.Contains((Control)previous))
            {
                // No files have been opened
                this.MainLayoutPanel.Controls.Remove(OpenFileLinkLabel);
            }
            else
            {
                this.MainLayoutPanel.Controls.Remove((Control)previous);

                if (NavTree.Nodes[0].Nodes.Count == 0)
                { 
                    this.MainLayoutPanel.Controls.Add(OpenFileLinkLabel, 1, 0);
                }
                    
            }

            ChangeControls(current); // Change controls to match the new display

            if (!this.MainLayoutPanel.Controls.Contains(OpenFileLinkLabel))
            {
                Control c = (Control)current;
                c.Dock = DockStyle.Fill;
                this.MainLayoutPanel.Controls.Add(c);
            }
		}

		/// <summary>
		/// Displays file-save success message to the user
		/// </summary>
		/// <param name="fileSaved">True if the file was saved successfully</param>
		public void SaveFileSuccess(bool fileSaved)
		{
			if (fileSaved == true)
			{
				MessageBox.Show("File save successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				MessageBox.Show("File save failed.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

        #endregion

        #region "Click Events"

        /// <summary>
        /// Handles the event that occurs when a node on the treeview was double-clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			controller.SelectTabPage(e.Node.Name);
            controller.checkSingleViewChange();
		}

		/// <summary>
		/// Handles the event that occurs when New button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult response = saveFileDialog1.ShowDialog();

            if (response != DialogResult.OK)
            {
                return;
            }

			controller.ProcessNewFile(saveFileDialog1.FileName, true);
			saveFileDialog1.FileName = "newFile1.vbi";
		}

		/// <summary>
		/// Handles the event that occurs when Open button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult response = openFileDialog1.ShowDialog();

			if (response == DialogResult.OK)
			{
				controller.ProcessNewFile(openFileDialog1.FileName);
				openFileDialog1.FileName = string.Empty;
			}
		}

		/// <summary>
		/// Handles the event that occurs when Save button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			controller.SaveFile();
		}

		/// <summary>
		/// Handles the event that occurs when SaveAs button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DialogResult response = saveFileDialog1.ShowDialog();

			if (response == DialogResult.OK)
			{
				controller.SaveFileAs(saveFileDialog1.FileName);
				saveFileDialog1.FileName = "newFile1.vbi";
			}
		}

		/// <summary>
		/// Handles the event that occurs when Print button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void printToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// Handles the event that occurs when Print-Preview button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// Handles the event that occurs when Exit button (on menustrip) was clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			controller.ExitApplication();
		}

		/// <summary>
		/// Handles the event that occurs when the link-label is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OpenFileLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			DialogResult response = openFileDialog1.ShowDialog();

            if (response != DialogResult.OK)
            {
                return;
            }

			controller.ProcessNewFile(openFileDialog1.FileName);
			openFileDialog1.FileName = string.Empty;
		}

		#endregion

        private void increaseFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach( var sub in Globals.SubDesigns)
            {
                sub.Value.IncreaseFont();
                // Change browser font
            }
        }

        private void decreaseFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Globals.FontSize > 5)
            {
                foreach (var sub in Globals.SubDesigns)
                {
                    sub.Value.DecreaseFont();
                    // Change browser font
                }
            }
        }

        private void lightThemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeTheme("light");
        }

        private void darkThemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeTheme("dark");
        }

        private void runModeToggle_Click(object sender, EventArgs e)
        {
            controller.Run();
        }

        private void editModeToggle_Click(object sender, EventArgs e)
        {
            controller.checkSingleViewChange();
        }

        private void closeDesignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = controller.CloseFile();
            if (name != null)
            {
                RemoveNavTreeNode(name);
                if (NavTree.Nodes[0].Nodes.Count == 0) controller.LoadDisplay(Globals.DisplayType.EDIT); // Switches to default view
            }
        }
    }
}
