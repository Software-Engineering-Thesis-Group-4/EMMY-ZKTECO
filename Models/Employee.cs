using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingerprint_Attendance.Models
{
    class Employee
    {
        public string enrollNumber { get; set; }
        public Timestamp timestamp;

        public Employee()
        {
            timestamp = new Timestamp();
        }
    }

    class Timestamp
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }

        public int hours { get; set; }
        public int minutes { get; set; }
        public int seconds { get; set; }
    }
}
