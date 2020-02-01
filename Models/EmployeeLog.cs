using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fingerprint_Attendance.Models
{
    class EmployeeLog
    {
        public string enrollNumber { get; set; }
        public DateTime timestamp { get; set; }
        public int attendanceState { get; set; }
    }
}
