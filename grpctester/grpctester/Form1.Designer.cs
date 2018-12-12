namespace grpctester
{
    partial class Form1
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
            this.requestComboBox = new System.Windows.Forms.ComboBox();
            this.grpcCallButton = new System.Windows.Forms.Button();
            this.replyTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // requestComboBox
            // 
            this.requestComboBox.FormattingEnabled = true;
            this.requestComboBox.Location = new System.Drawing.Point(17, 37);
            this.requestComboBox.Name = "requestComboBox";
            this.requestComboBox.Size = new System.Drawing.Size(175, 21);
            this.requestComboBox.TabIndex = 0;
            this.requestComboBox.SelectedIndexChanged += new System.EventHandler(this.requestComboBox_SelectedIndexChanged);
            // 
            // grpcCallButton
            // 
            this.grpcCallButton.BackColor = System.Drawing.Color.Green;
            this.grpcCallButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.grpcCallButton.Location = new System.Drawing.Point(16, 113);
            this.grpcCallButton.Name = "grpcCallButton";
            this.grpcCallButton.Size = new System.Drawing.Size(174, 39);
            this.grpcCallButton.TabIndex = 1;
            this.grpcCallButton.Text = "grpc call";
            this.grpcCallButton.UseVisualStyleBackColor = false;
            this.grpcCallButton.Click += new System.EventHandler(this.grpcCallButton_Click);
            // 
            // replyTextBox
            // 
            this.replyTextBox.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.replyTextBox.Location = new System.Drawing.Point(17, 87);
            this.replyTextBox.Name = "replyTextBox";
            this.replyTextBox.ReadOnly = true;
            this.replyTextBox.Size = new System.Drawing.Size(173, 20);
            this.replyTextBox.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Request:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Reply:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(205, 164);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.replyTextBox);
            this.Controls.Add(this.grpcCallButton);
            this.Controls.Add(this.requestComboBox);
            this.Name = "Form1";
            this.Text = "grpc Tester";
            this.Load += new System.EventHandler(this.mainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox requestComboBox;
        private System.Windows.Forms.Button grpcCallButton;
        private System.Windows.Forms.TextBox replyTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}

