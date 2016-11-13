﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisiBoole
{
    /// <summary>
    /// The default view for the main menu display
    /// </summary>
    public partial class DisplaySingleEditor : DisplayBase
    {
        /// <summary>
        /// Constructs an instance of ctlDisplaySingleEditor
        /// </summary>
        public DisplaySingleEditor()
        {
            InitializeComponent();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            this.Run(Globals.subDesigns[((tabEditor.SelectedTab.ToString().Substring(0)).Split('{'))[1].TrimEnd('}')]);
        }
    }
}