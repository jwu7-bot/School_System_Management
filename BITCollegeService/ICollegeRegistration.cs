using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BITCollegeService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICollegeRegistration" in both code and config file together.
    [ServiceContract]
    public interface ICollegeRegistration
    {
        [OperationContract]
        void DoWork();

        /// <summary>
        /// Implementation will drop a course and return 
        /// the result as true or false.
        /// </summary>
        /// <param name="registrationId">Represents the registration id.</param>
        /// <returns></returns>
        [OperationContract]
        Boolean DropCourse(int registrationId);

        /// <summary>
        /// Implementation will register a course and
        /// return a code that will indicate reason.
        /// </summary>
        /// <param name="studentId">Represents the student id.</param>
        /// <param name="courseId">Represents the course id.</param>
        /// <param name="notes">Represents the notes.</param>
        /// <returns></returns>
        [OperationContract]
        int RegisterCourse(int studentId, int courseId, String notes);

        /// <summary>
        /// Implementation will update a grade and return the grade 
        /// point average.
        /// </summary>
        /// <param name="grade">Represents grade.</param>
        /// <param name="registrationId">Represents the registration id.</param>
        /// <param name="notes">Represents the notes.</param>
        /// <returns></returns>
        [OperationContract]
        double? UpdateGrade(double grade, int registrationId, String notes);
    }
}
