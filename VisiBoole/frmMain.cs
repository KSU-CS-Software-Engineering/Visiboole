﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace VisiBoole
{
    /// <summary>
    /// The Main Menu of the application
    /// </summary>
    public partial class frmMain : Form
    {
        /// <summary>
        /// The currently displayed UserControl
        /// </summary>
        public UserControl CurrentDisplay { get; set; }

        public Dictionary<string, SubDesign> subDesigns { get; set; }

        DisplayBase display = new DisplayBase();

        /// <summary>
        /// Binding Source for the UsersubDesigns ListBox
        /// </summary>
        private BindingSource usersubDesignsBindingSource;

        /// <summary>
        /// Constructs an instance of frmMain
        /// </summary>
        public frmMain()
        {
            InitializeComponent();

            InitializeUsersubDesigns();

            // Load the default UserControl to display on application Startup
            LoadDisplay(new DisplaySingleEditor());
        }

        private void InitializeUsersubDesigns()
        {
            // Initialize our dictionary of VisiBoole Functions
            subDesigns = new Dictionary<string, SubDesign>();

            if (!Directory.Exists(Path.Combine(Application.StartupPath, "UsersubDesigns")))
            {
                Directory.CreateDirectory(Path.Combine(Application.StartupPath, "UsersubDesigns"));
            }

            GetFiles(Path.Combine(Application.StartupPath, "UsersubDesigns"));
        }

        private void GetFiles(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles("*.vbi");

            foreach (FileInfo file in files)
            {
                if (!subDesigns.ContainsKey(file.Name))
                {
                    SubDesign value = new SubDesign(file.Name);
                    value.File = file;
                    subDesigns.Add(value.Name, value);
                    if(path != Path.Combine(Path.Combine(Application.StartupPath, "UsersubDesigns")))
                    {
                        file.CopyTo(Path.Combine(Path.Combine(Application.StartupPath, "UsersubDesigns"), file.Name));
                    }
                }
                //TestOutputParser(file);
            }
            usersubDesignsBindingSource = new BindingSource(subDesigns, null);
            lboSource.DisplayMember = "Key";
            lboSource.ValueMember = "Value";
            lboSource.DataSource = usersubDesignsBindingSource;
        }

        /// <summary>
        /// Replace the currently displayed UserControl with the given UserControl
        /// </summary>
        /// <param name="pNewDisplay">The UserControl to replace the currently displayed one with</param>
        private void LoadDisplay(UserControl pNewDisplay)
        {
            if (pNewDisplay == null)
                return;

            pnlDisplay.Controls.Remove(CurrentDisplay);

            CurrentDisplay = pNewDisplay;

            pnlDisplay.Controls.Add(CurrentDisplay);

            CurrentDisplay.Dock = DockStyle.Fill;
            CurrentDisplay.Show();
        }

        /// <summary>
        /// Display the default Single View
        /// </summary>
        private void standardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDisplay(new DisplaySingleEditor());
        }

        /// <summary>
        /// Display the Horizontal View
        /// </summary>
        private void horizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDisplay(new DisplayHorizontal());
        }

        /// <summary>
        /// Display the Vertical View
        /// </summary>
        private void verticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDisplay(new DisplayVertical());
        }

        /// <summary>
        /// Displays the OpenFileDialog for user to select a *.vbi file to open
        /// </summary>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = openFileDialog1.ShowDialog();

            if (response == DialogResult.OK)
            {
                string fileName = openFileDialog1.FileName;
                
                // Check that the selected file is a valid VisiBoole file before attempting to open
                if (string.IsNullOrEmpty(fileName) || Path.GetExtension(fileName) != ".vbi")
                {
                    MessageBox.Show(Path.Combine(Application.StartupPath, "UsersubDesigns"));
                    MessageBox.Show("Invalid filename. Select a valid VisiBoole file (*.vbi) extension.", "Invalid Filename", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    return;
                }
                
                //Copy the file to our UsersubDesigns project directory
                File.Copy(fileName, Path.Combine(Application.StartupPath, "UsersubDesigns"));
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = folderBrowserDialog1.ShowDialog();
            if (response == DialogResult.OK)
            {
                GetFiles(folderBrowserDialog1.SelectedPath);
            }
        }

        private void lboSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            //How to get tab and file we need?
            if(CurrentDisplay != null)
            {
                if(CurrentDisplay is DisplaySingleEditor)
                {
                    TabControl tabs = ((VisiBoole.DisplaySingleEditor)CurrentDisplay).tabEditor;
                    display.GenerateNewTab(tabs, subDesigns[((lboSource.SelectedItem.ToString().Substring(1)).Split(','))[0]]);
                }
                else if (CurrentDisplay is DisplayVertical)
                {
                    TabControl tabs = ((VisiBoole.DisplayVertical)CurrentDisplay).tabEditor;
                    display.GenerateNewTab(tabs, subDesigns[((lboSource.SelectedItem.ToString().Substring(1)).Split(','))[0]]);
                }
                else if(CurrentDisplay is DisplayHorizontal)
                {
                    TabControl tabs = ((VisiBoole.DisplayHorizontal)CurrentDisplay).tabEditor;
                    display.GenerateNewTab(tabs, subDesigns[((lboSource.SelectedItem.ToString().Substring(1)).Split(','))[0]]);
                }
            }
        }

        private void TestOutputParser(FileInfo f)
        {
            var Parser = new OutputParser(f);

            List<string> OutputList = Parser.GenerateOutput();
            string OutputText = string.Empty;

            for (int i = 0; i < OutputList.Count; i++)
            {
                OutputText += OutputList[i] + Environment.NewLine;
            }

            MessageBox.Show(OutputText);
        }
    }
}
