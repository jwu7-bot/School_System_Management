/*
 * Name: JiaHui Wu
 * Program: Business Information Technology
 * Course: ADEV-3008 Programming 3
 * Created: 3/20/2024
 * Updated: 3/25/2024
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
using Utility;

namespace BITCollegeWindows
{
    public partial class StudentData : Form
    {
        private BITCollege_JWContext db = new BITCollege_JWContext();

        private Student currentStudent;

        ///Given: Student and Registration data will be retrieved
        ///in this form and passed throughout application
        ///These variables will be used to store the current
        ///Student and selected Registration
        ConstructorData constructorData = new ConstructorData();

        /// <summary>
        /// This constructor will be used when this form is opened from
        /// the MDI Frame.
        /// </summary>
        public StudentData()
        {
            InitializeComponent();
        }

        /// <summary>
        /// given:  This constructor will be used when returning to StudentData
        /// from another form.  This constructor will pass back
        /// specific information about the student and registration
        /// based on activites taking place in another form.
        /// </summary>
        /// <param name="constructorData">constructorData object containing
        /// specific student and registration data.</param>
        public StudentData(ConstructorData constructor)
        {
            InitializeComponent();

            // set constructor data instance to the argument
            constructorData.Student = constructor.Student;
            constructorData.Registration = constructor.Registration;

            // set student number masked textbox to the student property of the constructor
            studentNumberMaskedTextBox.Text = Convert.ToString(constructor.Student.StudentNumber);

            // call masked textbox leave event passing null
            studentNumberMaskedTextBox_Leave(null, null);
        }

        /// <summary>
        /// given: Open grading form passing constructor data.
        /// </summary>
        private void lnkUpdateGrade_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PopulateConstructorData();
            Grading grading = new Grading(constructorData);
            grading.MdiParent = this.MdiParent;
            grading.Show();
            this.Close();
        }


        /// <summary>
        /// given: Open history form passing constructor data.
        /// </summary>
        private void lnkViewDetails_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PopulateConstructorData();
            History history = new History(constructorData);
            history.MdiParent = this.MdiParent;
            history.Show();
            this.Close();
        }

        /// <summary>
        /// given:  Opens the form in top right corner of the frame.
        /// </summary>
        private void StudentData_Load(object sender, EventArgs e)
        {
            //keeps location of form static when opened and closed
            this.Location = new Point(0, 0);
        }

        /// <summary>
        /// Handles the leave event of the student number masked textbox.
        /// </summary>
        private void studentNumberMaskedTextBox_Leave(object sender, EventArgs e)
        {
            if (studentNumberMaskedTextBox.MaskCompleted)
            {
                // student number masked textbox is not empty

                // parse the student number to type long
                long studentNumber = long.Parse(studentNumberMaskedTextBox.Text);

                // query selecting data form the students table that matches the student number
                Student student = db.Students.Where(x => x.StudentNumber == studentNumber).SingleOrDefault();

                if (student != null)
                {
                    // student record retrieved

                    // sets datasource property of student bindingsource to the student query
                    studentBindingSource.DataSource = student;

                    currentStudent = student;
                    
                    // query selecting all registrations in which the student id corresponds to student query
                    IQueryable<Registration> allRegistrations = db.Registrations.Where(
                                                                x => x.StudentId == student.StudentId);

                    if (allRegistrations != null)
                    {
                        // registration record(s) were retrieved

                        // set datasource property of registrations control to the registrations query
                        registrationBindingSource1.DataSource = allRegistrations.ToList();

                        // enable the link lables 
                        lnkUpdateGrade.Enabled = lnkViewDetails.Enabled = true;

                        if (constructorData.Registration != null)
                        {
                            // registration object of the constructordata is not null

                            // set the registrationnumber combobox text property to the value of 
                            // registration number returned to this form
                            registrationNumberComboBox.Text = Convert.ToString(constructorData.Registration.RegistrationNumber);
                        }
                    }
                    else
                    {
                        // no registration record(s) were retrieved

                        // disable the link lables
                        lnkUpdateGrade.Enabled = lnkViewDetails.Enabled = false;

                        // clear registration binding source object
                        registrationBindingSource1.DataSource = typeof(Registration);
                    }
                }
                else
                {
                    // no record retrieved

                    // disable link labels
                    lnkUpdateGrade.Enabled = lnkViewDetails.Enabled = false;

                    // set focus back to masked textbox control
                    studentNumberMaskedTextBox.Focus();

                    // clear student binding source object
                    studentBindingSource.DataSource = typeof(Student);

                    // clear registration binding source object
                    registrationBindingSource1.DataSource = typeof(Registration);

                    // display messagebox showing student number does not exist
                    MessageBox.Show(
                        String.Format("Student {0} does not exist.", studentNumber),
                        "Invalid Student Number",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.None);

                    currentStudent = null;
                }
            } 
        }

        /// <summary>
        /// Helper method to populate the constructor data with current student and registration
        /// </summary>
        private void PopulateConstructorData()
        {
            // populates the constructor data student and registration attributes
            constructorData.Student = currentStudent;
            constructorData.Registration = (Registration)registrationNumberComboBox.SelectedItem;
        }
    }
}
