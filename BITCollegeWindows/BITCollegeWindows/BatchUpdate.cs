/*
 * Name: JiaHui Wu
 * Program: Business Information Technology
 * Course: ADEV-3008 Programming 3
 * Created: 4/14/2024
 * Updated: 4/16/2024
 */

using BITCollege_JW.Data;
using BITCollege_JW.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BITCollegeWindows
{
    public partial class BatchUpdate : Form
    {
        // private instance of the BIT_College Context, and Batch classes. 
        private BITCollege_JWContext db = new BITCollege_JWContext();
        private Batch batch = new Batch();

        public BatchUpdate()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Batch processing
        /// Further code to be added.
        /// </summary>
        private void lnkProcess_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (radSelect.Checked)
            {
                // single transmission selection has been made

                if (descriptionComboBox.SelectedItem != null)
                {
                    // item selected 

                    // call the ProcessTransmission method of the Batch class passing appropriate arguments.
                    batch.ProcessTransmission(descriptionComboBox.SelectedValue.ToString());

                    // call the WriteLogData method of the Batch class to write all logging
                    // information associated with this transmission file.
                    // capture the return value 
                    string returnedValue = batch.WriteLogData();

                    // and append returned value to the richText 
                    rtxtLog.Text += returnedValue;
                }
            }
            else if (radAll.Checked)
            {
                // all transmissions have been selected

                foreach (AcademicProgram programs in descriptionComboBox.Items)
                {
                    // iterate through each item in the ComboBox collection

                    // call the ProcessTransmission method of the Batch class passing appropriate arguments.
                    batch.ProcessTransmission(programs.ProgramAcronym);

                    // call the WriteLogData method of the Batch class to write all 
                    // logging information associated with this transmission file.
                    // capture the return value 
                    string returnedValue = batch.WriteLogData();

                    // and append returned value to the richText 
                    rtxtLog.Text += returnedValue;
                }
            }
        }

        /// <summary>
        /// given:  Always open this form in top right of frame.
        /// Further code to be added.
        /// </summary>
        private void BatchUpdate_Load(object sender, EventArgs e)
        {
            this.Location = new Point(0, 0);

            // query retrieving all records from the AcademicPrograms table
            IQueryable<AcademicProgram> allPrograms = db.AcademicPrograms;

            // populate the BindingSource object associated with the AcademicProgram ComboBox with
            // all academic programs
            descriptionComboBox.DataSource = allPrograms.ToList();

            descriptionComboBox.DisplayMember = "Description";
            descriptionComboBox.ValueMember = "ProgramAcronym";
        }

        /// <summary>
        /// Handles the radAll radio button cheched changed event.
        /// </summary>
        private void radAll_CheckedChanged(object sender, EventArgs e)
        {
            // enable the ComboBox when select radio button is selected
            descriptionComboBox.Enabled = radSelect.Checked;
        }
    }
}
