/*
 * Name: JiaHui Wu
 * Program: Business Information Technology
 * Course: ADEV-3008 Programming 3
 * Created: 1/7/2024
 * Updated: 2/20/2024
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using Utility;
using BITCollege_JW.Data;
using System.Data.SqlClient;
using System.Data;

namespace BITCollege_JW.Models
{
    /// <summary>
    /// Student model. Represents the Students table in the database.
    /// </summary>
    public class Student
    {
        private BITCollege_JWContext db = new BITCollege_JWContext();

        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int StudentId { get; set; }

        [Required]
        [ForeignKey("GradePointState")]
        public int GradePointStateId { get; set; }

        [ForeignKey("AcademicProgram")]
        public int? AcademicProgramId { get; set; }

        [Display(Name = "Student\nNumber")]
        public long StudentNumber { get; set; }

        [Required]
        [Display(Name = "First\nName")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last\nName")]
        public string LastName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        [RegularExpression("^(N[BLSTU]|[AMN]B|[BQ]C|ON|PE|SK|YT)", 
            ErrorMessage = "Province must be a valid canadian province code")]
        public string Province { get; set; }

        [Required]
        [Display(Name = "Date\nCreated")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime DateCreated { get; set; }

        [Display(Name = "Grade Point\nAverage")]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Range(0, 4.5)]
        public double? GradePointAverage { get; set; }

        [Required]
        [Display(Name = "Fees")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        public double OutstandingFees { get; set; }
        
        public string Notes { get; set; }

        [Display(Name = "Name")]
        public string FullName 
        {
            get
            {
                return String.Format("{0} {1}", FirstName, LastName);
            }
        }

        [Display(Name = "Address")]
        public string FullAddress 
        {
            get
            {
                return String.Format("{0} {1} {2}",
                    Address, City, Province);
            } 
        }

        // Navigation property
        public virtual AcademicProgram AcademicProgram { get; set; }
        public virtual GradePointState GradePointState { get; set; }
        public virtual ICollection<Registration> Registration { get; set; }

        /// <summary>
        /// Changes the state of the current GradePoint instance by invoking StateChangeCheck method
        /// until the state remains unchanged.
        /// </summary>
        public void ChangeState()
        {
            GradePointState current = db.GradePointStates.Find(GradePointStateId);

            int next = 0;

            while (current.GradePointStateId != next)
            {
                current.StateChangeCheck(this);
                next = current.GradePointStateId;
                current = db.GradePointStates.Find(GradePointStateId);
            }
        }

        /// <summary>
        /// Set the StudentNumber to the value returned by 
        /// the NextNumber static method.
        /// </summary>
        public void SetNextStudentNumber()
        {
            StudentNumber = (long)StoredProcedure.NextNumber("NextStudent");
        }
    }

    /// <summary>
    /// AcademicProgram model. Represents the AcademicPrograms table in the database.
    /// </summary>
    public class AcademicProgram
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int AcademicProgramId { get; set; }

        [Required]
        [Display(Name = "Program")]
        public string ProgramAcronym { get; set; }

        [Required]
        [Display(Name = "Program\nName")]
        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<Student> Student { get; set; }
        public virtual ICollection<Course> Course { get; set; }
    }

    /// <summary>
    /// GradePointState model. Represents the GradePointStates table in the database.
    /// </summary>
    public abstract class GradePointState
    {
        protected static BITCollege_JWContext db = new BITCollege_JWContext();

        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Key]
        public int GradePointStateId { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Display(Name = "Lower\nLimit")]
        public double LowerLimit { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Display(Name = "Upper\nLimit")]
        public double UpperLimit { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:F3}")]
        [Display(Name = "Tuition Rate\nFactor")]
        public double TuitionRateFactor { get; set; }

        [Display(Name = "State")]
        public string Description 
        {
            get
            {
                return BusinessRules.ParseString(GetType().Name, "State");
            }
        }

        // Navigation property
        public virtual ICollection<Student> Student { get; set; }

        /// <summary>
        /// Abstract method that will be implemented in the derived classes that 
        /// calculates the tuition rate adjustment for a given student.
        /// </summary>
        /// <param name="student">The student that tuition rate adjustment will be applied.</param>
        /// <returns>A double value representing the calculated tuition rate adjustment.</returns>
        public abstract double TuitionRateAdjustment(Student student);

        /// <summary>
        /// Abstract method that will be implemented in the derived classes that
        /// checks for state changes in the specified Student object and performs necessary actions.
        /// </summary>
        /// <param name="student">The Student object whose will check for state changes.</param>
        public abstract void StateChangeCheck(Student student);
    }

    /// <summary>
    /// SuspendedState model. Represents the SuspendedStates table in the database.
    /// </summary>
    public class SuspendedState : GradePointState
    {
        private static SuspendedState suspendedState;

        /// <summary>
        /// Initializes a new intance of the SuspendedState class
        /// </summary>
        private SuspendedState()
        {
            LowerLimit = 0;
            UpperLimit = 1;
            TuitionRateFactor = 1.1;
        }

        /// <summary>
        /// Retrieves an instance of the SuspendedState class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning
        /// </summary>
        /// <returns>An instance of the SuspendedState class.</returns>
        public static SuspendedState GetInstance()
        {
            if (suspendedState is null)
            {
                suspendedState = db.SuspendedStates.SingleOrDefault();

                if (suspendedState is null)
                {
                    suspendedState = new SuspendedState();
                    db.GradePointStates.Add(suspendedState);
                    db.SaveChanges();
                }
            }
            return suspendedState;
        }

        /// <summary>
        /// Checks if the student's grade point average is below 0.5 or 0.75.
        /// If below 0.50, student's tuition rate factor will have a 5% increase.
        /// If below 0.75, student's tuition rate factor will have a 2% increase.
        /// </summary>
        /// <param name="student">The student whose grade point average is being checked.</param>
        /// <returns>The adjusted tuition rate.</returns>
        public override double TuitionRateAdjustment(Student student)
        {
            double adjustedTuitionRate = TuitionRateFactor;

            if (student.GradePointAverage < 0.50)
            {
                adjustedTuitionRate += 0.05;
            }
            else if (student.GradePointAverage < 0.75)
            {
                adjustedTuitionRate += 0.02;
            }

            return adjustedTuitionRate;
        }

        /// <summary>
        /// Checks if the student's grade point average is higher than 1.
        /// If higher than 1, the student's grade point state is change to Probation state.
        /// </summary>
        /// <param name="student">The student whose state is being checked.</param>
        public override void StateChangeCheck(Student student)
        {
            if (student.GradePointAverage > UpperLimit)
            {
                ProbationState probationState = ProbationState.GetInstance();
                student.GradePointStateId = probationState.GradePointStateId;
            }

            db.SaveChanges();
        }
    }

    /// <summary>
    /// ProbationState model. Represents the ProbationStates table in the database.
    /// </summary>
    public class ProbationState : GradePointState
    {
        private static ProbationState probationState;

        /// <summary>
        /// Initializes a new intance of the ProbationState class
        /// </summary>
        private ProbationState()
        {
            LowerLimit = 1;
            UpperLimit = 2;
            TuitionRateFactor = 1.075;
        }

        /// <summary>
        /// Retrieves an instance of the ProbationState class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning
        /// </summary>
        /// <returns>An instance of the ProbationStates class.</returns>
        public static ProbationState GetInstance()
        {
            if (probationState is null)
            {
                probationState = db.ProbationStates.SingleOrDefault();

                if (probationState is null)
                {
                    probationState = new ProbationState();
                    db.GradePointStates.Add(probationState);
                    db.SaveChanges();
                }
            }
            return probationState;
        }

        /// <summary>
        /// Checks if the student has completed 5 or more courses.
        /// If yes, the student's tuition rate factor will only increase by only 3.5%.
        /// If not, the student's tuition rate factor will remain additional 7.5%.
        /// </summary>
        /// <param name="student">The student whose number of courses is being checked.</param>
        /// <returns>The adjusted tuition rate.</returns>
        public override double TuitionRateAdjustment(Student student)
        {
            IQueryable<Registration> studentCourses = db.Registrations.Where(x => x.StudentId == student.StudentId
                                                      && x.Grade != null);

            int courseCount = studentCourses.Count();

            double adjustedTuitionRate = TuitionRateFactor;

            if (courseCount >= 5)
            {
                adjustedTuitionRate = 1.035;
            }

            return adjustedTuitionRate;
        }

        /// <summary>
        /// Checks if the student's grade point avergae is lower than 1 or higher than 2.
        /// If lower than 1, the student's grade point state is changed to Suspended state.
        /// If higher than 2, the student's grade point state is changed to Regular state.
        /// </summary>
        /// <param name="student">The student whose state is being checked.</param>
        public override void StateChangeCheck(Student student)
        {
            if (student.GradePointAverage < LowerLimit)
            {
                SuspendedState suspendedState = SuspendedState.GetInstance();
                student.GradePointStateId = suspendedState.GradePointStateId;
            }

            else if (student.GradePointAverage > UpperLimit)
            {
                RegularState regularState = RegularState.GetInstance();
                student.GradePointStateId = regularState.GradePointStateId;
            }

            db.SaveChanges();
        }
    }

    /// <summary>
    /// RegularState model. Represents the RegularStates table in the database.
    /// </summary>
    public class RegularState : GradePointState
    {
        private static RegularState regularState;

        /// <summary>
        /// Initializes a new intance of the RegularState class 
        /// </summary>
        private RegularState()
        {
            LowerLimit = 2;
            UpperLimit = 3.7;
            TuitionRateFactor = 1;
        }

        /// <summary>
        /// Retrieves an instance of the RegularState class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning 
        /// </summary>
        /// <returns>An instance of the RegularState class.</returns>
        public static RegularState GetInstance()
        {
            if (regularState is null)
            {
                regularState = db.RegularStates.SingleOrDefault();

                if (regularState is null)
                {
                    regularState = new RegularState();
                    db.GradePointStates.Add(regularState);
                    db.SaveChanges();
                }
            }
            return regularState;
        }

        /// <summary>
        /// There is no tuition rate adjustment for the RegularState student.
        /// </summary>
        /// <param name="student">The student whose being checked</param>
        /// <returns>The tuition rate factor without adjustment.</returns>
        public override double TuitionRateAdjustment(Student student)
        {
            return TuitionRateFactor;
        }

        /// <summary>
        /// Checks if the student's grade point average is lower than 2 or higher than 3.7.
        /// If lower than 2, the student's grade point state is changed to Probation state.
        /// If higher than 3.7, the student's grade point state is changed to Honours state.
        /// </summary>
        /// <param name="student">The student whose state is being checked.</param>
        public override void StateChangeCheck(Student student)
        {
            if (student.GradePointAverage < LowerLimit)
            {
                ProbationState probationState = ProbationState.GetInstance();
                student.GradePointStateId = probationState.GradePointStateId;
            } 
            else if (student.GradePointAverage > UpperLimit)
            {
                HonoursState honoursState = HonoursState.GetInstance();
                student.GradePointStateId = honoursState.GradePointStateId;
            }

            db.SaveChanges();
        }
    }

    /// <summary>
    /// HonoursState model. Represents the HonoursStates table in the database.
    /// </summary>
    public class HonoursState : GradePointState
    {
        private static HonoursState honoursState;

        /// <summary>
        /// Initializes a new intance of the HonoursState class
        /// </summary>
        private HonoursState()
        {
            LowerLimit = 3.7;
            UpperLimit = 4.5;
            TuitionRateFactor = 0.9;
        }

        /// <summary>
        /// Retrieves an instance of the HonoursState class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning
        /// </summary>
        /// <returns>An instace of HonoursState class.</returns>
        public static HonoursState GetInstance()
        {
            if (honoursState is null)
            {
                honoursState = db.HonoursStates.SingleOrDefault();

                if (honoursState is null)
                {
                    honoursState = new HonoursState();
                    db.GradePointStates.Add(honoursState);
                    db.SaveChanges();
                }
            }
            return honoursState;
        }

        /// <summary>
        /// Checks if the student's grade point average is higher than 4.25 and has completed 5 or more courses.
        /// If the student's grade point average is higher than 4.25, then student will have an additional 2% discount.
        /// If the student has completed 5 or more courses, then the student will have an additional 5% discount.
        /// Student can be eligible for both discounts.
        /// </summary>
        /// <param name="student">The student whose grade point average and number of courses is being checked.</param>
        /// <returns>The adjusted tuition rate.</returns>
        public override double TuitionRateAdjustment(Student student)
        {
            IQueryable<Registration> studentCourses = db.Registrations.Where(x => x.StudentId == student.StudentId
                                          && x.Grade != null);

            int courseCount = studentCourses.Count();

            double adjustedTuitionRate = TuitionRateFactor;

            if (courseCount >= 5)
            {
                adjustedTuitionRate -= 0.05;
            }

            if (student.GradePointAverage > 4.25)
            {
                adjustedTuitionRate -= 0.02;
            }

            return adjustedTuitionRate;
        }

        /// <summary>
        /// Checks if the student's grade point average is lower than 3.7.
        /// If lower than 3.7, the student's grade point state is changed to Regular state.
        /// </summary>
        /// <param name="student">The student whose state is being checked.</param>
        public override void StateChangeCheck(Student student)
        {
            if (student.GradePointAverage < LowerLimit)
            {
                RegularState regularState = RegularState.GetInstance();
                student.GradePointStateId = regularState.GradePointStateId;
            }

            db.SaveChanges();
        }
    }

    /// <summary>
    /// Course model. Represents the Courses table in the database.
    /// </summary>
    public abstract class Course
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Key]
        public int CourseId { get; set; }

        [ForeignKey("AcademicProgram")]
        public int? AcademicProgramId { get; set; }

        [Display(Name = "Course\nNumber")]
        public string CourseNumber { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Display(Name = "Credit\nHours")]
        public double CreditHours { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Display(Name = "Tuition")]
        public double TuitionAmount { get; set; }

        [Display(Name = "Course\nType")]
        public string CourseType 
        {
            get
            {
                return BusinessRules.ParseString(GetType().Name, "Course");
            }
        }

        public string Notes { get; set; }

        // Navigation property
        public virtual AcademicProgram AcademicProgram { get; set; }
        public virtual ICollection<Registration> Registration { get; set; }


        /// <summary>
        /// Abstract method that will be implemented in the derived classes 
        /// that will set the next course number.
        /// </summary>
        public abstract void SetNextCourseNumber();
    }

    /// <summary>
    /// GradedCourse model. Represents the GradedCourses table in the database.
    /// </summary>
    public class GradedCourse : Course
    {
        [Required]
        [Display(Name = "Assignments")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public double AssignmentWeight { get; set; }

        [Required]
        [Display(Name = "Exams")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public double ExamWeight { get; set; }

        /// <summary>
        /// Set the CourseNumber to the value of "G-" followed by
        /// the value returned by the NextNumber static method.
        /// </summary>
        public override void SetNextCourseNumber()
        {
            CourseNumber = "G-" + StoredProcedure.NextNumber("NextGradedCourse");
        }
    }

    /// <summary>
    /// MasteryCourse model. Represents the MasteryCourses table in the database.
    /// </summary>
    public class MasteryCourse : Course
    {
        [Required]
        [Display(Name = "Maximum\nAttempts")]
        public int MaximumAttempts { get; set; }

        /// <summary>
        /// Set the CourseNumber to the value of "M-" followed by
        /// the value returned by the NextNumber static method.
        /// </summary>
        public override void SetNextCourseNumber()
        {
            CourseNumber = "M-" + StoredProcedure.NextNumber("NextMasteryCourse");
        }
    }

    /// <summary>
    /// AuditCourse model. Represents the AuditCourses table in the database.
    /// </summary>
    public class AuditCourse : Course
    {
        /// <summary>
        /// Set the CourseNumber to the value of "A-" followed by
        /// the value returned by the NextNumber static method.
        /// </summary>
        public override void SetNextCourseNumber()
        {
            CourseNumber = "A-" + StoredProcedure.NextNumber("NextAuditCourse");
        }
    }

    /// <summary>
    /// Registration model. Represents the Registrations table in the database.
    /// </summary>
    public class Registration
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int RegistrationId { get; set; }

        [Required]
        [ForeignKey("Student")]
        public int StudentId { get; set; }

        [Required]
        [ForeignKey("Course")]
        public int CourseId { get; set; }

        [Display(Name = "Registration\nNumber")]
        public long RegistrationNumber { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime RegistrationDate { get; set; }

        [DisplayFormat(NullDisplayText = "Ungraded")]
        [Range(0, 1)]
        public double? Grade { get; set; }

        public string Notes { get; set; }

        // Navigation property
        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }

        /// <summary>
        /// Set the RegistrationNumber to the value returned by 
        /// the NextNumber static method.
        /// </summary>
        public void SetNextRegistrationNumber()
        {
            RegistrationNumber = (long)StoredProcedure.NextNumber("NextRegistration");
        }
    }

    /// <summary>
    /// NextUniqueNumber model. Represents the NextUniqueNumbers table in the database.
    /// </summary>
    public abstract class NextUniqueNumber
    {
        protected static BITCollege_JWContext db = new BITCollege_JWContext();

        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Key]
        public int NextUniqueNumberId { get; set; }

        [Required]
        public long NextAvailableNumber { get; set; }
    }

    /// <summary>
    /// NextStudent model. Represents the NextStudents table in the database.
    /// </summary>
    public class NextStudent : NextUniqueNumber
    {
        private static NextStudent nextStudent;

        /// <summary>
        /// Initializes a new intance of the NextStudent class
        /// </summary>
        private NextStudent()
        {
            NextAvailableNumber = 20000000;
        }

        /// <summary>
        /// Retrieves an instance of the NextStudent class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning 
        /// </summary>
        /// <returns>An instance of the NextStudent class.</returns>
        public static NextStudent GetInstance()
        {
            if (nextStudent is null)
            {
                nextStudent = db.NextStudents.SingleOrDefault();

                if (nextStudent is null)
                {
                    nextStudent = new NextStudent();
                    db.NextUniqueNumbers.Add(nextStudent);
                    db.SaveChanges();
                }
            }
            return nextStudent;
        }
    }

    /// <summary>
    /// NextRegistration model. Represents the NextRegistrations table in the database.
    /// </summary>
    public class NextRegistration : NextUniqueNumber
    {
        private static NextRegistration nextRegistration;

        /// <summary>
        /// Initializes a new intance of the NextRegistration class
        /// </summary>
        private NextRegistration()
        {
            NextAvailableNumber = 700;
        }

        /// <summary>
        /// Retrieves an instance of the NextRegistration class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning 
        /// </summary>
        /// <returns>An instance of the NextRegistration class.</returns>
        public static NextRegistration GetInstace()
        {
            if (nextRegistration is null)
            {
                nextRegistration = db.NextRegistrations.SingleOrDefault();

                if (nextRegistration is null)
                {
                    nextRegistration = new NextRegistration();
                    db.NextUniqueNumbers.Add(nextRegistration);
                    db.SaveChanges();
                }
            }
            return nextRegistration;
        }
    }

    /// <summary>
    /// NextGradedCourse model. Represents the NextGradedCourses table in the database.
    /// </summary>
    public class NextGradedCourse : NextUniqueNumber
    {
        private static NextGradedCourse nextGradedCourse;

        /// <summary>
        /// Initializes a new intance of the NextGradedCourse class
        /// </summary>
        private NextGradedCourse()
        {
            NextAvailableNumber = 200000;
        }

        /// <summary>
        /// Retrieves an instance of the NextGradedCourse class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning 
        /// </summary>
        /// <returns>An instance of the NextGradedCourse class.</returns>
        public static NextGradedCourse GetInstace()
        {
            if (nextGradedCourse is null)
            {
                nextGradedCourse = db.NextGradedCourses.SingleOrDefault();

                if (nextGradedCourse is null)
                {
                    nextGradedCourse = new NextGradedCourse();
                    db.NextUniqueNumbers.Add(nextGradedCourse);
                    db.SaveChanges();
                }
            }
            return nextGradedCourse;
        }
    }

    /// <summary>
    /// NextAuditCourse model. Represents the NextAuditCourses table in the database.
    /// </summary>
    public class NextAuditCourse : NextUniqueNumber
    {
        private static NextAuditCourse nextAuditCourse;

        /// <summary>
        /// Initializes a new intance of the NextAuditCourse class
        /// </summary>
        private NextAuditCourse()
        {
            NextAvailableNumber = 2000;
        }

        /// <summary>
        /// Retrieves an instance of the NextAuditCourse class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning 
        /// </summary>
        /// <returns>An instance of the NextAuditCourse class.</returns>
        public static NextAuditCourse GetInstace()
        {
            if (nextAuditCourse is null)
            {
                nextAuditCourse = db.NextAuditCourses.SingleOrDefault();

                if (nextAuditCourse is null)
                {
                    nextAuditCourse = new NextAuditCourse();
                    db.NextUniqueNumbers.Add(nextAuditCourse);
                    db.SaveChanges();
                }
            }
            return nextAuditCourse;
        }
    }

    /// <summary>
    /// NextMasteryCourse model. Represents the NextMasteryCourses table in the database.
    /// </summary>
    public class NextMasteryCourse : NextUniqueNumber
    {
        private static NextMasteryCourse nextMasteryCourse;

        /// <summary>
        /// Initializes a new intance of the NextMasteryCourse class
        /// </summary>
        private NextMasteryCourse()
        {
            NextAvailableNumber = 20000;
        }

        /// <summary>
        /// Retrieves an instance of the NextMasteryCourse class, applying the singleton pattern. 
        /// If no instance exists in the database, a new instance is created and added to the database before returning 
        /// </summary>
        /// <returns>An instance of the NextMasteryCourse class.</returns>
        public static NextMasteryCourse GetInstace()
        {
            if (nextMasteryCourse is null)
            {
                nextMasteryCourse = db.NextMasteryCourses.SingleOrDefault();

                if (nextMasteryCourse is null)
                {
                    nextMasteryCourse = new NextMasteryCourse();
                    db.NextUniqueNumbers.Add(nextMasteryCourse);
                    db.SaveChanges();
                }
            }
            return nextMasteryCourse;
        }
    }

    /// <summary>
    /// Static class that represents our Stored Procedure.
    /// </summary>
    public static class StoredProcedure
    {
        /// <summary>
        /// Retrieves the next available number.
        /// </summary>
        /// <param name="discriminator">The table to increment the ID.</param>
        /// <returns>The next available number.</returns>
        public static long? NextNumber(String discriminator)
        {
            try
            {
                long? returnValue = 0;

                SqlConnection connection = new SqlConnection("Data Source=JIAHUIWU\\EARTH; " +
                "Initial Catalog=BITCollege_JWContext;Integrated Security=True");
                SqlCommand storedProcedure = new SqlCommand("next_number", connection);
                storedProcedure.CommandType = CommandType.StoredProcedure;
                storedProcedure.Parameters.AddWithValue("@Discriminator", discriminator);
                SqlParameter outputParameter = new SqlParameter("@NewVal", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                storedProcedure.Parameters.Add(outputParameter);
                connection.Open();
                storedProcedure.ExecuteNonQuery();
                connection.Close();
                returnValue = (long?)outputParameter.Value;
                return returnValue;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}