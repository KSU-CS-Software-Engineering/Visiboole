﻿namespace VisiBoole.Controllers
{
	/// <summary>
	/// Exposes methods on the controller for the MainWindow
	/// </summary>
	public interface IMainWindowController
	{
		/// <summary>
		/// Loads into the MainWindow the display of the given type
		/// </summary>
		/// <param name="dType">The type of display that should be loaded</param>
		void LoadDisplay(Globals.DisplayType dType);

		/// <summary>
		/// Processes a new file that is created or opened by the user
		/// </summary>
		/// <param name="path">The path of the file that was created or opened by the user</param>
		/// <param name="overwriteExisting">True if the file at the given path should be overwritten</param>
		void ProcessNewFile(string path, bool overwriteExisting = false);

		/// <summary>
		/// Saves the file that is currently active in the selected tabpage
		/// </summary>
		void SaveFile();

		/// <summary>
		/// Saves the file that is currently active in the selected tabpage with the filename chosen by the user
		/// </summary>
		/// <param name="path">The new file path to save the active file to</param>
		void SaveFileAs(string filePath);

        /// <summary>
		/// Saves all files opened
		/// </summary>
		void SaveAll();

        /// <summary>
        /// Performs a dirty check and confirms application exit with the user
        /// </summary>
        void ExitApplication();

        /// <summary>
        /// Closes the selected open file
        /// </summary>
        /// <returns>The name of the file closed</returns>
        string CloseFile();

		/// <summary>
		/// Selects the tabpage in the tabcontrol with name matching the given string
		/// </summary>
		/// <param name="fileName">The name of the tabpage to select</param>
		void SelectTabPage(string fileName);

        /// <summary>
        /// Used to check if the display is the output, if it is, change it to editor.
        /// </summary>
        void checkSingleViewChange();

        /// <summary>
        /// Run call to other controller
        /// </summary>
        void Run();

    }
}
