﻿namespace VisiBoole.Views
{
    partial class DialogBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DialogBox));
            this.uxButtonExit = new System.Windows.Forms.Button();
            this.uxPanelTop = new System.Windows.Forms.Panel();
            this.uxLabelTitle = new System.Windows.Forms.Label();
            this.uxButton1 = new System.Windows.Forms.Button();
            this.uxButton2 = new System.Windows.Forms.Button();
            this.uxLabelMessage = new System.Windows.Forms.Label();
            this.uxButton3 = new System.Windows.Forms.Button();
            this.uxPanelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // uxButtonExit
            // 
            this.uxButtonExit.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.uxButtonExit.FlatAppearance.BorderSize = 0;
            this.uxButtonExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.uxButtonExit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.uxButtonExit.ForeColor = System.Drawing.Color.White;
            this.uxButtonExit.Location = new System.Drawing.Point(397, 3);
            this.uxButtonExit.Name = "uxButtonExit";
            this.uxButtonExit.Size = new System.Drawing.Size(25, 23);
            this.uxButtonExit.TabIndex = 2;
            this.uxButtonExit.Text = "X";
            this.uxButtonExit.UseVisualStyleBackColor = true;
            // 
            // uxPanelTop
            // 
            this.uxPanelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this.uxPanelTop.Controls.Add(this.uxLabelTitle);
            this.uxPanelTop.Controls.Add(this.uxButtonExit);
            this.uxPanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.uxPanelTop.Location = new System.Drawing.Point(0, 0);
            this.uxPanelTop.Name = "uxPanelTop";
            this.uxPanelTop.Size = new System.Drawing.Size(425, 30);
            this.uxPanelTop.TabIndex = 1;
            // 
            // uxLabelTitle
            // 
            this.uxLabelTitle.AutoSize = true;
            this.uxLabelTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.uxLabelTitle.ForeColor = System.Drawing.Color.White;
            this.uxLabelTitle.Location = new System.Drawing.Point(3, 7);
            this.uxLabelTitle.Name = "uxLabelTitle";
            this.uxLabelTitle.Size = new System.Drawing.Size(38, 19);
            this.uxLabelTitle.TabIndex = 4;
            this.uxLabelTitle.Text = "Title";
            // 
            // uxButton1
            // 
            this.uxButton1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uxButton1.Location = new System.Drawing.Point(338, 194);
            this.uxButton1.Name = "uxButton1";
            this.uxButton1.Size = new System.Drawing.Size(78, 26);
            this.uxButton1.TabIndex = 0;
            this.uxButton1.Text = "Button 1";
            this.uxButton1.UseVisualStyleBackColor = true;
            // 
            // uxButton2
            // 
            this.uxButton2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uxButton2.Location = new System.Drawing.Point(254, 194);
            this.uxButton2.Name = "uxButton2";
            this.uxButton2.Size = new System.Drawing.Size(78, 26);
            this.uxButton2.TabIndex = 1;
            this.uxButton2.Text = "Button 2";
            this.uxButton2.UseVisualStyleBackColor = true;
            // 
            // uxLabelMessage
            // 
            this.uxLabelMessage.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uxLabelMessage.ForeColor = System.Drawing.Color.Black;
            this.uxLabelMessage.Location = new System.Drawing.Point(7, 33);
            this.uxLabelMessage.Name = "uxLabelMessage";
            this.uxLabelMessage.Size = new System.Drawing.Size(409, 158);
            this.uxLabelMessage.TabIndex = 3;
            this.uxLabelMessage.Text = "Message";
            this.uxLabelMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uxButton3
            // 
            this.uxButton3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.uxButton3.Location = new System.Drawing.Point(170, 194);
            this.uxButton3.Name = "uxButton3";
            this.uxButton3.Size = new System.Drawing.Size(78, 26);
            this.uxButton3.TabIndex = 4;
            this.uxButton3.Text = "Button 3";
            this.uxButton3.UseVisualStyleBackColor = true;
            // 
            // DialogBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(425, 225);
            this.Controls.Add(this.uxButton3);
            this.Controls.Add(this.uxLabelMessage);
            this.Controls.Add(this.uxButton2);
            this.Controls.Add(this.uxButton1);
            this.Controls.Add(this.uxPanelTop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogBox";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DialogBox";
            this.TopMost = true;
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DialogBoxPaint);
            this.uxPanelTop.ResumeLayout(false);
            this.uxPanelTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button uxButtonExit;
        private System.Windows.Forms.Panel uxPanelTop;
        private System.Windows.Forms.Label uxLabelTitle;
        private System.Windows.Forms.Button uxButton1;
        private System.Windows.Forms.Button uxButton2;
        private System.Windows.Forms.Label uxLabelMessage;
        private System.Windows.Forms.Button uxButton3;
    }
}