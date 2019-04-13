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
using System.Collections.Generic;
using VisiBoole.Models;
using VisiBoole.ParsingEngine.ObjectCode;
using VisiBoole.Views;

namespace VisiBoole.Controllers
{
	/// <summary>
	/// Exposes methods on the controller for the MainWindow
	/// </summary>
	public interface IMainWindowController
	{
        /// <summary>
        /// Gets the display of the main window.
        /// </summary>
        /// <returns>The display</returns>
        IDisplay GetDisplay();

        /// <summary>
        /// Switch display mode
        /// </summary>
        void SwitchDisplay();

        /// <summary>
        /// Focuses the main window.
        /// </summary>
        void RetrieveFocus();

        /// <summary>
        /// Selects the file at the specified index.
        /// </summary>
        /// <param name="index">The index of the file</param>
        void SelectFile(int index);

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
		void SaveFiles();

        /// <summary>
        /// Closes the selected open file
        /// </summary>
        /// <returns>The name of the file closed</returns>
        void CloseActiveFile();

        /// <summary>
        /// Closes all files.
        /// </summary>
        void CloseFiles();

        /// <summary>
        /// Closes all files except for the provided file name.
        /// </summary>
        /// <param name="name">Name of the file to keep open</param>
        void CloseFilesExceptFor(string name);

        /// <summary>
        /// Handles the event that occurs when an edit has been made to a design.
        /// </summary>
        /// <param name="sender">Design being edited</param>
        /// <param name="eventArgs">Arguments of the edit</param>
        void OnDesignEdit(object sender, DesignEditEventArgs eventArgs);

        /// <summary>
        /// Handles the event that occurs when two designs are being swapped on the tab control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void SwapDesigns(object sender, TabSwapEventArgs eventArgs);

        /// <summary>
        /// Set theme of Designs
        /// </summary>
        void SetTheme();

        /// <summary>
        /// Update font sizes
        /// </summary>
        void SetFontSize();

        /// <summary>
        /// Loads into the MainWindow the display of the given type
        /// </summary>
        /// <param name="dType">The type of display that should be loaded</param>
        void LoadDisplay(DisplayType dType);

        /// <summary>
        /// Handles the event that occurs when the user runs the active design.
        /// </summary>
        void Run();

        /// <summary>
        /// Runs a subdesign from the provided instantiation.
        /// </summary>
        /// <param name="instantiation">Instantiation to run</param>
        /// <returns>Output of the parsed instantiation</returns>
        List<IObjectCodeElement> RunSubdesign(string instantiation);

        /// <summary>
        /// Handles the event that occurs when the browser needs to be refreshed.
        /// </summary>
        void RefreshOutput();

        /// <summary>
        /// Handles the event that occurs when the user ticks the active design.
        /// </summary>
        /// <returns>Output list of the ticked design</returns>
        List<IObjectCodeElement> Tick();

        /// <summary>
        /// Handles the event that occurs when the user clicks on an independent variable.
        /// </summary>
        /// <param name="variableName">The name of the variable that was clicked by the user</param>
        /// <returns></returns>
        List<IObjectCodeElement> Variable_Click(string variableName);
    }
}