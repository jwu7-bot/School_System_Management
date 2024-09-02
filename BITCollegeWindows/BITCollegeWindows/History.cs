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

namespace BITCollegeWindows
{
    public partial class History : Form
    {
        BITCollege_JWContext db = new BITCollege_JWContext();

        ///given:  student and registration data will passed throughout 
        ///application. This object will be used to store the current
        ///student and selected registration
        ConstructorData constructorData;

        /// <summary>
        /// given:  This constructor will be used when called from the
        /// Student form.  This constructor will receive 
        /// specific information about the student and registration
        /// further code required:  
        /// </summary>
        /// <param name="constructorData">constructorData object containing
        /// specific student and registration data.</param>
        public History(ConstructorData constructorData)
        {
            InitializeComponent();

            // set the constructorData that was passed to the constructorData
            this.constructorData = constructorData;

            // populate the upper controls with corresponding data received in the constructor
            studentNumberMaskedLabel.Text = Convert.ToString(this.constructorData.Student.StudentNumber);
            fullNameLabel1.Text = this.constructorData.Student.FullName;
            descriptionLabel1.Text = this.constructorData.Student.AcademicProgram.Description;
        }

        /// <summary>
        /// given: This code will navigate back to the Student form with
        /// the specific student and registration data that launched
        /// this form.
        /// </summary>
        private void lnkReturn_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //return to student with the data selected for this form
            StudentData student = new StudentData(constructorData);
            student.MdiParent = this.MdiParent;
            student.Show();
            this.Close();
        }

        /// <summary>
        /// given:  Open this form in top right corner of the frame.
        /// further code required:
        /// </summary>
        private void History_Load(object sender, EventArgs e)
        {
            this.Location = new Point(0, 0);

            // proper exception handdling 
            try
            {
                // query selecting data from the registrations and courses table who's
                // student id corresponds to student passed to this form
                var query = from registration in db.Registrations
                            join course in db.Courses on registration.CourseId equals course.CourseId
                            where registration.StudentId == constructorData.Student.StudentId
                            select new
                            {
                                RegistrationNumber = registration.RegistrationNumber,
                                RegistrationDate = registration.RegistrationDate,
                                Course = course.Title,
                                Grade = registration.Grade,
                                Notes = registration.Notes
                            };

                // set datasource property of datagridview control to this query
                registrationDataGridView.DataSource = query.ToList();
            }
            catch (Exception ex)
            {
                // messagebox providing details of the exception
                MessageBox.Show($"An error occurred while retrieving data: {ex.Message}", 
                                "Error", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
            }

        }
    }
}
