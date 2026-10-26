namespace BanterBrain_Buddy
{
    partial class TwitchNotableViewers
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
            TwitchFlavourTextBox = new System.Windows.Forms.TextBox();
            ViewerAddButton = new System.Windows.Forms.Button();
            ViewerDeleteButton = new System.Windows.Forms.Button();
            TwitchNotableViewersComboBox = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // TwitchFlavourTextBox
            // 
            TwitchFlavourTextBox.Location = new System.Drawing.Point(95, 59);
            TwitchFlavourTextBox.Multiline = true;
            TwitchFlavourTextBox.Name = "TwitchFlavourTextBox";
            TwitchFlavourTextBox.Size = new System.Drawing.Size(451, 108);
            TwitchFlavourTextBox.TabIndex = 1;
            TwitchFlavourTextBox.TextChanged += TwitchFlavourTextBox_TextChanged;
            TwitchFlavourTextBox.Leave += TwitchFlavourTextBox_Leave;
            TwitchFlavourTextBox.Validating += TwitchNotableViewersComboBox_Validating;
            // 
            // ViewerAddButton
            // 
            ViewerAddButton.Location = new System.Drawing.Point(309, 26);
            ViewerAddButton.Name = "ViewerAddButton";
            ViewerAddButton.Size = new System.Drawing.Size(75, 23);
            ViewerAddButton.TabIndex = 3;
            ViewerAddButton.Text = "New";
            ViewerAddButton.UseVisualStyleBackColor = true;
            ViewerAddButton.Click += ViewerAddButton_Click;
            // 
            // ViewerDeleteButton
            // 
            ViewerDeleteButton.Location = new System.Drawing.Point(390, 26);
            ViewerDeleteButton.Name = "ViewerDeleteButton";
            ViewerDeleteButton.Size = new System.Drawing.Size(75, 23);
            ViewerDeleteButton.TabIndex = 4;
            ViewerDeleteButton.Text = "Delete";
            ViewerDeleteButton.UseVisualStyleBackColor = true;
            ViewerDeleteButton.Click += ViewerDeleteButton_Click;
            // 
            // TwitchNotableViewersComboBox
            // 
            TwitchNotableViewersComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            TwitchNotableViewersComboBox.FormattingEnabled = true;
            TwitchNotableViewersComboBox.Location = new System.Drawing.Point(95, 26);
            TwitchNotableViewersComboBox.Name = "TwitchNotableViewersComboBox";
            TwitchNotableViewersComboBox.Size = new System.Drawing.Size(208, 23);
            TwitchNotableViewersComboBox.TabIndex = 0;
            TwitchNotableViewersComboBox.SelectedIndexChanged += TwitchNotableViewersComboBox_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 30);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(77, 15);
            label1.TabIndex = 7;
            label1.Text = "Viewer Name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(10, 62);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(86, 15);
            label2.TabIndex = 8;
            label2.Text = "Personal touch";
            // 
            // MyTwitchNotableViewers
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(558, 189);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(TwitchNotableViewersComboBox);
            Controls.Add(ViewerDeleteButton);
            Controls.Add(ViewerAddButton);
            Controls.Add(TwitchFlavourTextBox);
            Name = "TwitchNotableViewers";
            Text = "Notable Viewers";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.TextBox TwitchFlavourTextBox;
        private System.Windows.Forms.Button ViewerAddButton;
        private System.Windows.Forms.Button ViewerDeleteButton;
        private System.Windows.Forms.ComboBox TwitchNotableViewersComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}