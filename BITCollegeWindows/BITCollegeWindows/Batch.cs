/*
 * Name: JiaHui Wu
 * Program: Business Information Technology
 * Course: ADEV-3008 Programming 3
 * Created: 4/13/2024
 * Updated: 4/16/2024
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml;
using System.ServiceModel.Configuration;
using BITCollege_JW.Data;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BITCollegeWindows.CollegeRegistration;
using BITCollege_JW.Models;

namespace BITCollegeWindows
{
    /// <summary>
    /// Batch:  This class provides functionality that will validate
    /// and process incoming xml files.
    /// </summary>
    public class Batch
    {
        // database
        private BITCollege_JWContext db = new BITCollege_JWContext();

        // represent the name of the file being processed
        private String inputFileName;

        // represent the name of the log file that corresponds with the file being processed
        private String logFileName;

        // represent all data to be written to the log file that corresponds with the file being processed
        private String logData;


        /// <summary>
        /// Method will process all detail errors found within the current file being processed.
        /// Method will be called after each round of detail record validation in ProcessDetails.
        /// </summary>
        /// <param name="beforeQuery">Represents the records that existed before the round of validation.</param>
        /// <param name="afterQuery">Represents the records that remained following the round of validation.</param>
        /// <param name="message">: Represents the error message that is to be written to the log file based on the 
        ///                         record failing the round of validation.</param>
        private void ProcessErrors(IEnumerable<XElement> beforeQuery, IEnumerable<XElement> afterQuery, String message)
        {
            //  compare the records from the beforeQuery with those from the afterQuery using Except()
            IEnumerable<XElement> failedRecords = beforeQuery.Except(afterQuery);

            foreach (XElement xele in failedRecords)
            {
                // Process each of the records that failed validation by appending relevant information to the 
                // logData instance variable.
                logData += "\r\n--------ERROR--------";
                logData += $"\r\nFile: {inputFileName}";
                logData += $"\r\nProgram: {xele.Element("program")}";
                logData += $"\r\nStudent Number: {xele.Element("student_no")}";
                logData += $"\r\nCourse Number: {xele.Element("course_no")}";
                logData += $"\r\nRegistration Number: {xele.Element("registration_no")}";
                logData += $"\r\nType: {xele.Element("type")}";
                logData += $"\r\nGrade: {xele.Element("grade")}";
                logData += $"\r\nNotes: {xele.Element("notes")}";
                logData += $"\r\nNodes: {xele.Nodes().Count()}";
                logData += $"\r\nMessage: {message}";
                logData += "\r\n---------------------\r\n";
            }
        }


        /// <summary>
        /// Method used to verify the attributes of the xml file’s root element. 
        /// If any of the attributes produce an error, the file is NOT to be processed.
        /// </summary>
        private void ProcessHeader()
        {
            // define an XDocument object and populate with the contents of the current input file
            XDocument xDocument = XDocument.Load(inputFileName);

            // define an XElement object and populate XElement object with
            // the data found within the root element of the xml file
            XElement root = xDocument.Element("student_update");

            if (root.Attributes().Count() != 3)
            {
                // XElement object does not have 3 attributes

                throw new Exception("The XElement object does not have 3 attributes.");
            }

            XAttribute dateAttribute = root.Attribute("date");
            if (!DateTime.TryParse(dateAttribute.Value, out DateTime dateValue) || 
                dateValue != DateTime.Today)
            {
                // date attribute of the XElement object is not equal to today’s date.

                throw new Exception("The 'date' attribute of the XElement object is not equal to today’s date.");
            }

            XAttribute programAttribute = root.Attribute("program");
            bool programExists = db.AcademicPrograms.Any(x => x.ProgramAcronym == programAttribute.Value);
            if (!programExists)
            {
                // program acronym not listed in the database

                throw new Exception("The 'program' attribute does not match an academic " +
                    "program acronym in the database.");
            }

            // sum of all student no
            int sumOfStudentNumbers = root
                        .Descendants("student_no")
                        .Where(e => Int32.TryParse(e.Value, out _)) 
                        .Select(e => Int32.Parse(e.Value)) 
                        .Sum();

            XAttribute checksumAttribute = root.Attribute("checksum");
            if (Int32.Parse(checksumAttribute.Value) != sumOfStudentNumbers)
            {
                // checksum not match the sum of all student_no

                throw new Exception("The 'checksum' attribute does not match the sum " +
                    "of all student_no elements in the file");
            }
        }


        /// <summary>
        /// Method used to verify the contents of the detail records in the input file. If any of the records
        /// produce an error, that record will be skipped, but the file processing will continue.
        /// </summary>
        private void ProcessDetails()
        {
            // define an XDocument object and populate with the contents of the current input file
            XDocument xDocument = XDocument.Load(inputFileName);

            // IEnumerable<XElement> LINQ-to-XML query against the XDocument object
            IEnumerable<XElement> transactions = xDocument.Descendants("transaction");

            // ROUND 1: query to select transactions with 7 child elements
            IEnumerable<XElement> validTransactionsRound1 = transactions
                .Where(t => t.Nodes().OfType<XElement>().Count() == 7);
            // ROUND 1: call ProcessErrors
            ProcessErrors(transactions,
                          validTransactionsRound1, 
                          "Incorrect number of child elements");

            // ROUND 2: query to select transactions that program element match the
            //          program attribute of the root element
            XElement root = xDocument.Element("student_update");
            string rootProgram = root.Attribute("program").Value;
            IEnumerable<XElement> validTransactionsRound2 = validTransactionsRound1
                .Where(t => t.Element("program").Value == rootProgram);
            // ROUND 2: call ProcessErrors
            ProcessErrors(validTransactionsRound1, 
                          validTransactionsRound2, 
                          "Program element not match the program attribute of the root element");

            // ROUND 3: query to select transactions that type element is numeric
            IEnumerable<XElement> validTransactionsRound3 = validTransactionsRound2
                .Where(t => Utility.Numeric.IsNumeric(t.Element("type").Value, 
                                                      System.Globalization.NumberStyles.Number));
            // ROUND 3: call ProcessErrors
            ProcessErrors(validTransactionsRound2,
                          validTransactionsRound3,
                          "Type element is not numeric");

            // ROUND 4: query to select transactions that grade element are either numeric or '*'
            IEnumerable<XElement> validTransactionsRound4 = validTransactionsRound3
                .Where(t => Utility.Numeric.IsNumeric(t.Element("grade").Value, 
                                                      System.Globalization.NumberStyles.Number)
                      || t.Element("grade").Value == "*");
            // ROUND 4: call ProcessErrors
            ProcessErrors(validTransactionsRound3,
                          validTransactionsRound4,
                          "Grade element is not numeric or '*'");

            // ROUND 5: query to select transactions that type element are either 1 or 2
            IEnumerable<XElement> validTransactionsRound5 = validTransactionsRound4
                .Where(t => t.Element("type").Value == "1" || t.Element("type").Value == "2");
            // ROUND 5: call ProcessErrors
            ProcessErrors(validTransactionsRound4,
                          validTransactionsRound5,
                          "Type elements is not 1 or 2");

            // ROUND 6: query to select transactions that grade element of type 1 must have '*'
            //          and type 2 must have value between 0 and 100 inclusive
            IEnumerable<XElement> validTransactionsRound6 = validTransactionsRound5
                .Where(t => (t.Element("type").Value == "1" && t.Element("grade").Value == "*")
                         || (t.Element("type").Value == "2" && (Double.Parse(t.Element("grade").Value) >= 0 &&
                                                               Double.Parse(t.Element("grade").Value) <= 100)));
            // ROUND 6: call ProcessErrors
            ProcessErrors(validTransactionsRound5,
                          validTransactionsRound6,
                          "Grade is not '*' or grade out of range");

            // ROUND 7: query to select transactions that student_no element in the database 
            IEnumerable<long> allStudentNumbers = db.Students.Select(s => s.StudentNumber).ToList();
            IEnumerable<XElement> validTransactionsRound7 = validTransactionsRound6
                .Where(t => allStudentNumbers.Contains(long.Parse(t.Element("student_no").Value)));
            // ROUND 7: call ProcessErrors
            ProcessErrors(validTransactionsRound6,
                          validTransactionsRound7,
                          "Student number not in the database");

            // ROUND 8: query to select transactions that course_no element for type 2 must be '*'
            //          and type 1 must exist in the database
            IEnumerable<string> allCourseNumbers = db.Courses.Select(c => c.CourseNumber).ToList();
            IEnumerable<XElement> validTransactionsRound8 = validTransactionsRound7
                .Where(t => (t.Element("type").Value == "2" && t.Element("course_no").Value == "*")
                         || (t.Element("type").Value == "1" && allCourseNumbers.Contains(
                                                            t.Element("course_no").Value)));
            // ROUND 8: call ProcessErrors
            ProcessErrors(validTransactionsRound7,
                          validTransactionsRound8,
                          "Course number is not '*' or not in database");

            // ROUND 9: query to select transactions that registration_no element for type 1 must be '*'
            //          or type 2 must exist in the database 
            IEnumerable<long> allRegistrationNumbers = db.Registrations.Select(r => r.RegistrationNumber).ToList();
            IEnumerable<XElement> validTransactionsRound9 = validTransactionsRound8
                .Where(t => (t.Element("type").Value == "1" && t.Element("registration_no").Value == "*")
                         || (t.Element("type").Value == "2" && allRegistrationNumbers.Contains(
                                                            long.Parse(t.Element("registration_no").Value))));
            // ROUND 9: call ProcessErrors
            ProcessErrors(validTransactionsRound8,
                          validTransactionsRound9,
                          "Registration number is not '*' or not in database");

            // call the ProcessTransactions method passing the error free result set
            ProcessTransactions(validTransactionsRound9);
        }


        /// <summary>
        /// Method used to process all valid transaction records.
        /// </summary>
        /// <param name="transactionRecords"></param>
        private void ProcessTransactions(IEnumerable<XElement> transactionRecords)
        {
            // WCF Service
            CollegeRegistrations college = new CollegeRegistrations();

            foreach (XElement xele in transactionRecords)
            {
                // Extract values from the XElement objects in the collection as necessary in order to proceed.

                string studentNumber = xele.Element("student_no").Value;
                string courseNumber = xele.Element("course_no").Value;
                string registrationNumber = xele.Element("registration_no").Value;
                string type = xele.Element("type").Value;
                string grade = xele.Element("grade").Value;
                string notes = xele.Element("notes").Value;

                if (type == "1")
                {
                    // type 1 (indicating registration)

                    int number = int.Parse(studentNumber);

                    int courseId = db.Courses.Where(x => x.CourseNumber == courseNumber)
                                        .Select(x => x.CourseId).SingleOrDefault();

                    int studentId = db.Students.Where(x => x.StudentNumber == number)
                                        .Select(x => x.StudentId).SingleOrDefault();

                    // use WCF Service to register student and store return code
                    int returnCode = college.RegisterCourse(studentId, 
                                                            courseId, 
                                                            notes);

                    if (returnCode == 0)
                    {
                        // transaction was successful

                        // append a relevant message to logData indicating that the transaction was successful
                        logData += $"\r\nStudent: {studentNumber} has successfully registered for " +
                            $"course: {courseNumber}.";
                    }
                    else
                    {
                        // transaction was unsuccessful

                        // append a relevant message to logData indicating that the transaction was unsuccessful
                        // use the RegisterError method from Utility project    
                        logData += $"\r\nREGISTRATION ERROR: {Utility.BusinessRules.RegisterError(returnCode)}";
                    }
                }

                else if(type == "2")
                {
                    // type 2 (indicating grading)

                    // grade formatted between 0 and 1
                    double formattedGrade = double.Parse(grade) / 100.0;

                    long number = long.Parse(registrationNumber);

                    int registration = db.Registrations.
                        Where(x => x.RegistrationNumber == number).
                        Select(x => x.RegistrationId).SingleOrDefault();

                    // use the WCF Service to update the student’s grade.
                    double? updateGrade = college.UpdateGrade(formattedGrade, 
                                                              registration, 
                                                              notes);

                    if (updateGrade != null)
                    {
                        // transaction was successful

                        // append a relevant message to logData indicating that the transaction was successful
                        logData += $"\r\nA grade of: {grade} has been successfully applied " +
                            $"to registration: {registrationNumber}.";
                    }

                    else
                    {
                        // transaction was unsuccessful

                        // Append a message to log data providing enough details exception to help troubleshoot
                        logData += $"\r\nUPDATE GRADE ERROR: Failed to update grade for " +
                            $"Student {studentNumber} with Registration {courseNumber}.";
                    }
                }
            }
        }


        /// <summary>
        /// Method will be called upon completion of a file being processed.
        /// </summary>
        /// <returns></returns>
        public String WriteLogData()
        {
            try
            {
                // instantiate a StreamWriter associated with the value of the logFileName instance variable
                StreamWriter writer = new StreamWriter(logFileName);

                // write the accumulated logging data (logData) to the log file.
                writer.Write(logData);

                // close the StreamWriter.
                writer.Close();
            }
            catch (Exception ex)
            {
                // error

                logData += $"\r\nError writing to log file: {ex.Message}";
            }

            // local variable store content of logging instance variable (logData) to return
            string capturedLoggingData = logData;

            // set logData and logFileName to an empty String for next use
            logData = String.Empty;
            logFileName = String.Empty;

            // return the local variable containing the captured logging data to the calling routine.
            return capturedLoggingData;
        }


        /// <summary>
        /// Initiate the batch process by determining the appropriate filename and 
        /// then proceeding with the header and detail processing.
        /// </summary>
        /// <param name="programAcronym">The program accronym to be processed.</param>
        public void ProcessTransmission(String programAcronym)
        {
            // get current date
            DateTime currentDate = DateTime.Now;

            // current year with 4 digits and days of the year with 3 digits
            string year = currentDate.Year.ToString("D4");
            string dayOfYear = currentDate.DayOfYear.ToString("D3");

            // formulate the input file Name
            inputFileName = $"{year}-{dayOfYear}-{programAcronym}.xml";

            // formulate the log file name
            logFileName = $"LOG {inputFileName.Replace(".xml", ".txt")}";

            try
            {
                if(!File.Exists(inputFileName))
                {
                    // file does not exist

                    // append a relevant message to logData indicating that the file does not exist.
                    logData += $"\r\nFile does not exist: {inputFileName}";
                }
                else
                {
                    // file exists

                    try
                    {
                        // call ProcessHeader()
                        ProcessHeader();

                        // call ProcessDetails()
                        ProcessDetails();
                    }
                    catch (Exception ex)
                    {
                        // an exceptions occurs

                        // append a relevant message to logData indicating the reason for the exception
                        logData += $"\r\nException occuried: {ex.Message}";
                    }
                }
            } 
            catch (Exception ex)
            {
                // an exceptions occurs

                // append a relevant message to logData indicating the reason for the exception
                logData += $"\r\nException occuried: {ex.Message}";
            }
        }
    }
}
