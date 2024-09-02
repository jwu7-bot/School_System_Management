/*
 * Name: JiaHui Wu
 * Program: Business Information Technology
 * Course: ADEV-3008 Programming 3
 * Created: 3/20/2024
 * Updated: 3/25/2024
 */

using BITCollege_JW.Data;
using BITCollegeWindows.CollegeRegistration;
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
    public partial class Grading : Form
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
        public Grading(ConstructorData constructor)
        {
            InitializeComponent();

            // set the constructorData that was passed to the constructorData
            this.constructorData = constructor;

            // populate the upper controls with corresponding data received in the constructor
            studentNumberMaskedLabel.Text = Convert.ToString(this.constructorData.Student.StudentNumber);
            fullNameLabel1.Text = this.constructorData.Student.FullName;
            descriptionLabel1.Text = this.constructorData.Student.AcademicProgram.Description;

            // populate the lower controls with corresponding data received in the constructor
            courseNumberMaskedLabel.Text = Convert.ToString(this.constructorData.Registration.Course.CourseNumber);
            titleLabel1.Text = this.constructorData.Registration.Course.Title;
            courseTypeLabel1.Text = this.constructorData.Registration.Course.CourseType;
            gradeTextBox.Text = Convert.ToString(this.constructorData.Registration.Grade);
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
        /// given:  Always open in this form in the top right corner of the frame.
        /// further code required:
        /// </summary>
        private void Grading_Load(object sender, EventArgs e)
        {
            this.Location = new Point(0, 0);

            // use the CourseFormat method within Utility project to set the mask to MaskedLabel
            string courseType = constructorData.Registration.Course.CourseType;
            courseNumberMaskedLabel.Mask = Utility.BusinessRules.CourseFormat(courseType);

            double? grade = constructorData.Registration.Grade;
            if (grade != null)
            {
                // grade is previously entered
                // disable grade textbox and update link 
                // make visible the label that grading is not possible
                gradeTextBox.Enabled = lnkUpdate.Enabled = false;
                lblExisting.Visible = true;
            } 
            else
            {
                // no grade has been previously entered
                // enable grade textbox and update link 
                // make invisible the label that grading is not possible
                gradeTextBox.Enabled = lnkUpdate.Enabled = true;
                lblExisting.Visible = false;
            }
        }

        /// <summary>
        /// Handles the logic for updating a student grade
        /// </summary>
        private void lnkUpdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string grade = gradeTextBox.Text;

            if (!Utility.Numeric.IsNumeric(grade, System.Globalization.NumberStyles.Any))
            {
                // non-numeric

                // display appropriate message box
                MessageBox.Show("Grade must be a numeric value.", 
                                "Error", 
                                MessageBoxButtons.OK);
                return;
            }
            else
            {
                // numeric 

                // convert text to double
                double numericGrade = Convert.ToDouble(grade);

                if (numericGrade < 0 || numericGrade > 1)
                {
                    // out of range

                    // display appropriate message box
                    MessageBox.Show("The grade provided is not within the 0 - 1 range.",
                                    "Error",
                                    MessageBoxButtons.OK);
                }
                else
                {
                    try
                    {
                        // data within proper range

                        // instantiate the Client Endpoint of the WCF Web Service
                        //CollegeRegistrationClient college = new CollegeRegistrationClient();

                        CollegeRegistrations college = new CollegeRegistrations();
                    
                        // update the grade
                        double? gpa = college.UpdateGrade(numericGrade,
                                                              Convert.ToInt32(constructorData.Registration.RegistrationId),
                                                              constructorData.Registration.Notes);

                        // update the gpa
                        constructorData.Student.GradePointAverage = gpa;
                        
                        // update the grade point state
                        constructorData.Student.ChangeState();

                        // persist the change
                        db.SaveChanges();

                        // disable the grade textbox
                        gradeTextBox.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        // // display appropriate message box
                        MessageBox.Show($"Error while updating grade: {ex.Message}",
                                        "Error",
                                        MessageBoxButtons.OK);
                    }
                }
            }            
        }
    }
}
