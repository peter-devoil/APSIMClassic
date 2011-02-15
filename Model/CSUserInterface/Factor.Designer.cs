﻿namespace CSUserInterface
{
    partial class Factor
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
            this.pnlVariable = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.gridManager = new UIBits.EnhancedGrid();
            this.colActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colManager = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParameters = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.FactorTargets = new CSUserInterface.FactorTargets();
            this.pnlVariable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridManager)).BeginInit();
            this.SuspendLayout();
            // 
            // MyHelpLabel
            // 
            this.MyHelpLabel.Visible = true;
            // 
            // pnlVariable
            // 
            this.pnlVariable.Controls.Add(this.gridManager);
            this.pnlVariable.Controls.Add(this.label1);
            this.pnlVariable.Location = new System.Drawing.Point(-1, 157);
            this.pnlVariable.Name = "pnlVariable";
            this.pnlVariable.Size = new System.Drawing.Size(425, 320);
            this.pnlVariable.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Manager Variables";
            // 
            // gridManager
            // 
            this.gridManager.AllowUserToAddRows = false;
            this.gridManager.AllowUserToDeleteRows = false;
            this.gridManager.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridManager.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridManager.DataSourceTable = null;
            this.gridManager.Location = new System.Drawing.Point(2, 18);
            this.gridManager.Name = "gridManager";
            this.gridManager.RowHeadersVisible = false;
            this.gridManager.Size = new System.Drawing.Size(423, 263);
            this.gridManager.TabIndex = 2;
            // 
            // colActive
            // 
            this.colActive.HeaderText = "";
            this.colActive.Name = "colActive";
            this.colActive.Width = 20;
            // 
            // colManager
            // 
            this.colManager.HeaderText = "Manager Variables";
            this.colManager.Name = "colManager";
            this.colManager.ReadOnly = true;
            this.colManager.Width = 120;
            // 
            // colParameters
            // 
            this.colParameters.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colParameters.HeaderText = "Parameters";
            this.colParameters.MinimumWidth = 20;
            this.colParameters.Name = "colParameters";
            this.colParameters.Width = 85;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Column1";
            this.Column1.Name = "Column1";
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Column2";
            this.Column2.Name = "Column2";
            // 
            // FactorTargets
            // 
            this.FactorTargets.AutoScroll = true;
            this.FactorTargets.BackColor = System.Drawing.SystemColors.Window;
            this.FactorTargets.HelpText = "";
            this.FactorTargets.Location = new System.Drawing.Point(0, 2);
            this.FactorTargets.Margin = new System.Windows.Forms.Padding(0);
            this.FactorTargets.Name = "FactorTargets";
            this.FactorTargets.Size = new System.Drawing.Size(424, 150);
            this.FactorTargets.TabIndex = 2;
            // 
            // Factor
            // 
            this.Controls.Add(this.FactorTargets);
            this.Controls.Add(this.pnlVariable);
            this.Name = "Factor";
            this.Controls.SetChildIndex(this.pnlVariable, 0);
            this.Controls.SetChildIndex(this.FactorTargets, 0);
            this.Controls.SetChildIndex(this.MyHelpLabel, 0);
            this.pnlVariable.ResumeLayout(false);
            this.pnlVariable.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridManager)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private FactorTargets FactorTargets;
        private System.Windows.Forms.Panel pnlVariable;
        private System.Windows.Forms.Label label1;
        private UIBits.EnhancedGrid gridManager;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colActive;
        private System.Windows.Forms.DataGridViewTextBoxColumn colManager;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParameters;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column2;


    }
}
