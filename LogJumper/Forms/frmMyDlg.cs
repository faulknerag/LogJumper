using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Kbg.NppPluginNET
{
    public partial class frmMyDlg : Form
    {
        public frmMyDlg()
        {
            InitializeComponent();
        }

        private void MinutesF1_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.m, 1);
        }

        private void MinutesF2_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.m, 10);
        }

        private void MinutesF3_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.m, 30);
        }

        private void MinutesB1_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.m, -1);
        }

        private void MinutesB2_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.m, -10);
        }

        private void MinutesB3_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.m, -30);
        }

        private void HoursB3_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.h, -12);
        }

        private void HoursB2_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.h, -6);
        }

        private void HoursB1_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.h, -1);
        }

        private void HoursF1_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.h, 1);
        }

        private void HoursF2_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.h, 6);
        }

        private void HoursF3_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.h, 12);
        }

        private void SecondsB3_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.s, -30);
        }

        private void SecondsB2_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.s, -10);
        }

        private void SecondsB1_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.s, -1);
        }

        private void SecondsF1_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.s, 1);
        }

        private void SecondsF2_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.s, 10);
        }

        private void SecondsF3_Click(object sender, EventArgs e)
        {
            Main.JumpTime(Main.JumpUnits.s, 30);
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        private void SettingsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //Toggle settings visibility and update link text
            if (SettingsPanel.Visible == true)
            {
                SettingsPanel.Visible = false;
                SettingsLink.Text = "Settings";
            }
            else
            {
                SettingsPanel.Visible = true;
                SettingsLink.Text = "Hide Settings";
            }
        }

        private void SaveAndVerify_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //Save the new entry 
            try
            {
                Main.regex = new System.Text.RegularExpressions.Regex(textBox1.Text);

                //Make a jump based on the new entry
                Main.JumpTime(Main.JumpUnits.s, 1);
            } 
            catch (Exception ex)
            {
                MessageBox.Show($"Exception while parsing RegEx: {ex.Message}");
            }
        }

        private void textBox1_VisibleChanged(object sender, EventArgs e)
        {
            textBox1.Text = Main.regex.ToString();
        }

        private void DefaultRegexButton_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //Update regex to use default
            Main.regex = new System.Text.RegularExpressions.Regex(Main.defaultRegex);

            //Update text to display default
            textBox1.Text = Main.defaultRegex;
        }

        private void UseCSV_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Main.csvLog = UseCSV_CheckBox.Checked;
        }

        private void CSVDelimTextBox_TextChanged(object sender, EventArgs e)
        {
            if (CSVDelimTextBox.Text.Length > 0)
            {
                //Only grab the first character as the delimiter
                Main.csvDelim = CSVDelimTextBox.Text[0];

                //Set the first character as the text
                CSVDelimTextBox.Text = CSVDelimTextBox.Text[0].ToString();
            }
        }

        private void CSVColumn_NumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Main.csvColumn = (int)CSVColumn_NumericUpDown.Value - 1;
        }

        private void ReverseLogs_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Main.standardLogDirection = !ReverseLogs_CheckBox.Checked;
        }
    }
}
