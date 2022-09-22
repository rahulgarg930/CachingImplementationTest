using System;

namespace CachingImplementationTest.Models
{
    [Serializable]
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public int RollNumber { get; set; }
    }
}
