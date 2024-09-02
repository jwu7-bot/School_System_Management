/*
 * Name: JiaHui Wu
 * Program: Business Information Technology
 * Course: ADEV-3008 Programming 3
 * Created: 3/20/2024
 * Updated: 3/23/2024
 */

using BITCollege_JW.Data;
using BITCollege_JW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Utility;

namespace BITCollegeService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CollegeRegistration" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select CollegeRegistration.svc or CollegeRegistration.svc.cs at the Solution Explorer and start debugging.
    public class CollegeRegistration : ICollegeRegistration
    {
        BITCollege_JWContext db = new BITCollege_JWContext();

        public void DoWork()
        {
        }

        /// <summary>
        /// Implementation will drop a course and return 
        /// the result as true or false.
        /// </summary>
        /// <param name="registrationId">Represents the registration id.</param>
        /// <returns></returns>
        public bool DropCourse(int registrationId)
        {
            // flag variable to return
            bool found = false;

            // find the registration using the Find function
            Registration registration = db.Registrations.Find(registrationId);

            if (registration != null)
            {
                found = true;

                // remove from the database
                db.Registrations.Remove(registration);

                // persist change
                db.SaveChanges();
            }

            return found;
        }

        /// <summary>
        /// Implementation will register a course and
        /// return a code that will indicate reason.
        /// </summary>
        /// <param name="studentId">Represents the student id.</param>
        /// <param name="courseId">Represents the course id.</param>
        /// <param name="notes">Represents the notes.</param>
        /// <returns></returns>
        public int RegisterCourse(int studentId, int courseId, string notes)
        {
            // return code variable
            int returnCode = 0;

            // retrieve all records from the Registrations table with the corresponding method param value
            IQueryable<Registration> allRegistrations = db.Registrations.Where(x => x.StudentId == studentId 
                                                                                &&  x.CourseId == courseId);

            // retrieve course record represented by the corresponding method param value
            Course course = db.Courses.Where(x => x.CourseId == courseId).SingleOrDefault();

            // retrieve student record represented by the corresponding method param value
            Student student = db.Students.Where(x => x.StudentId == studentId).SingleOrDefault();

            // retrieve if any of the registrations has grade of null
            IEnumerable<Registration> incompletedRegistrations = allRegistrations.Where(x => x.Grade == null);

            // check if number of incompleted registrations is greater than 0
            if (incompletedRegistrations.Count() > 0)
            {
                returnCode = -100;
            }

            // get the course name
            string courseName = course.CourseType;

            // get course enum
            CourseType courseEnum = BusinessRules.CourseTypeLookup(courseName);

            // check if the course is of type MASTERY
            if (courseEnum == CourseType.MASTERY)
            {
                // get maximum attempts based on the course id
                int maximumAttempts = db.MasteryCourses.Where(x => x.CourseId == courseId)
                                                      .Select(y => y.MaximumAttempts).SingleOrDefault();

                // retrive completed registrations that have already taken place between the student and course in qustion
                IEnumerable<Registration> completedRegistrations = allRegistrations.Where(x => x.Grade != null);

                // check if completed registrations is greater than the maximum attemps allowed
                if (completedRegistrations.Count() >= maximumAttempts)
                {
                    returnCode = -200;
                }
            }

            // registration can proceed
            if (returnCode == 0)
            {
                try
                {
                    // new registration object 
                    Registration newRegistration = new Registration();
                    newRegistration.StudentId = studentId;
                    newRegistration.CourseId = courseId;
                    newRegistration.Notes = notes;
                    newRegistration.RegistrationDate = DateTime.Now;
                    newRegistration.RegistrationNumber = (long)StoredProcedure.NextNumber("NextRegistration");

                    // add the registration object to Registrations table
                    db.Registrations.Add(newRegistration);

                    // persist this new record to the database
                    db.SaveChanges();
                
                    // tuition amount of the course
                    double tuitionAmount = course.TuitionAmount;

                    // students grade point state
                    GradePointState state = db.GradePointStates.Find(student.GradePointStateId);

                    // charge appropriate fees based on the rate adjustment method
                    student.OutstandingFees += tuitionAmount * state.TuitionRateAdjustment(student);

                    // persist change to the database
                    db.SaveChanges();
                }

                // if any exception occurs return code should be -300
                catch (Exception)
                {
                    returnCode = -300;
                }
            }
            return returnCode;
        }


        /// <summary>
        /// Implementation will update a grade and return the grade 
        /// point average.
        /// </summary>
        /// <param name="grade">Represents grade.</param>
        /// <param name="registrationId">Represents the registration id.</param>
        /// <param name="notes">Represents the notes.</param>
        /// <returns>The newly calculated grade point average.</returns>
        public double? UpdateGrade(double grade, int registrationId, string notes)
        {
            // retrieve the Registration record from the database which corresponds to the method arg
            Registration registration = db.Registrations.Find(registrationId);

            // set the grade property of the Registration to the value of grade arg
            registration.Grade = grade;

            // modify the notes property with the value of the method argument
            registration.Notes = notes;

            // persist the change
            db.SaveChanges();

            // call CalculateGradePointAverage method and capture result into local varible
            double? calculatedGPA = CalculateGradePointAverage(registration.StudentId);

            // return the calculated GPA
            return calculatedGPA;
        }

        /// <summary>
        /// Implementatiton will calculate the grade point average
        /// of the given student and return the result.
        /// </summary>
        /// <param name="studentId">Represents the student id.</param>
        /// <returns>The newly grade point average.</returns>
        private double? CalculateGradePointAverage(int studentId)
        {
            // variables to hold grade, the course type, the grade point value,
            // total credit hours, total grade point value, and the newly calculated GPA  
            double grade = 0;
            CourseType courseType;
            double gradePointValue = 0;
            double totalCreditHours = 0;
            double totalGradePointValue = 0;
            Double? calculatedGradePointAverage = 0;

            // retrieve all registration records with a grade value not equal to null that belong to a student
            IQueryable<Registration> registrations = db.Registrations.Where(x => x.StudentId == studentId 
                                                                             && x.Grade != null);

            // iterate each registration
            foreach (Registration registration in registrations.ToList())
            {
                // grade for the registration
                grade = (double)registration.Grade;

                // course type
                courseType = BusinessRules.CourseTypeLookup(registration.Course.CourseType);
                
                // exclude any audit courses from the GPA calc
                if (courseType != CourseType.AUDIT)
                {
                    // grade point value for the grade
                    gradePointValue = BusinessRules.GradeLookup(grade, courseType);

                    // total grade point value for all registrations 
                    totalGradePointValue += gradePointValue * registration.Course.CreditHours;

                    // total credit hours accumulated
                    totalCreditHours += registration.Course.CreditHours;
                }
            }

            // if the total credit hours is not equal to 0
            if (totalCreditHours == 0)
            {
                calculatedGradePointAverage = null;
            }
            else
            {
                // GPA
                calculatedGradePointAverage = totalGradePointValue / totalCreditHours;
            }

            // retrieve student record to which newly calculated GPA applies
            Student student = db.Students.Find(studentId);

            // set the student's GPA property to the newly calculated GPA
            student.GradePointAverage = calculatedGradePointAverage;

            // persist the change
            db.SaveChanges();

            // ensure that any change to student's GPA cause student to be placed in appropriate GPA State
            student.ChangeState();

            // return the newly calculated GPA
            return calculatedGradePointAverage;
        }
    }
}
