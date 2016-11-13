﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace VisiBoole
{
    /// <summary>
    /// A User-Created SubDesign
    /// </summary>
    public class SubDesign : RichTextBox
    {
        /// <summary>
        /// The index of the TabControl that this occupies
        /// </summary>
        public int TabPageIndex { get; set; }

        /// <summary>
        /// The file location that this SubDesign is saved in
        /// </summary>
        public FileInfo FileSource { get; set; }

        /// <summary>
        /// The short filename of the FileSource
        /// </summary>
        public string FileSourceName { get; set; }

        /// <summary>
        /// Constructs a new SubDesign object
        /// </summary>
        /// <param name="filename">The path of the file source for this SubDesign</param>
        public SubDesign(string filename)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException("Invalid filename");

            FileSource = new FileInfo(filename);
            this.FileSourceName = FileSource.Name;

            CopyFileTextToTextProperty();
        }

        /// <summary>
        /// Copies the text contents of this subdesign file to its Text property
        /// </summary>
        private void CopyFileTextToTextProperty()
        {
            string text = string.Empty;

            using (StreamReader reader = this.FileSource.OpenText())
            {
                string nextLine = string.Empty;
                while ((nextLine = reader.ReadLine()) != null)
                {
                    text += nextLine;
                }
            }

            this.Text = text;
        }
    }
}