using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db.Tests.TestData
{
    public class School
    {
        public string Name;
        public List<Class> Classes;
    }

    public class Class
    {
        public string Name;
        public List<Student> Students;
    }

    public class Student
    {
        public string FirstName;
        public string LastName;
    }

}
