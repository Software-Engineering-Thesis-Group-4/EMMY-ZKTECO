using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Fingerprint_Attendance.Models;
using Newtonsoft.Json;
using zkemkeeper;

namespace Fingerprint_Attendance
{
    public partial class MainWindow : Window
    {
        // Scanner config
        private static CZKEM zk;
        public string ip_address;
        public int port = 4370;


        public MainWindow()
        {
            InitializeComponent();

            // INITIALZE FINGERPRINT SCANNER & CONFIG
            zk = new CZKEM();
            ip_address = "192.168.1.201";
            port = 4370;

            // INITIALIZE CONNECT ON CREATE
            if (zk.Connect_Net(ip_address, port))
            {
                Logs.Items.Add("Connected to Device!");
                ConnectionStatus.Text = "Connected";
                ConnectionStatus.Foreground = Brushes.LawnGreen;

                // REGISTER FINGERPRINT SCANNER EVENTS
                if (zk.RegEvent(zk.MachineNumber, 1))
                {
                    zk.OnAttTransactionEx += AttendanceTransactionHandler;
                }
            }
        }

        private async void AttendanceTransactionHandler(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
        {
            StringBuilder sb = new StringBuilder();

            // USER IS VERIFIED
            if (IsInValid == -1)
            {
                sb.AppendLine(" ------ [EMPLOYEE LOG] ------");
                sb.AppendFormat("User ID: {0}", EnrollNumber);
                sb.AppendFormat("\nLog Time: {0}/{1}/{2} - {3}:{4}:{5}", Month, Day, Year, Hour, Minute, Second);
            }

            // PRINT NEW EMPLOYEE ATTENDANCE TO LOGS
            Logs.Items.Add(sb.ToString());

            // SEND DATA TO SERVER ------------------------------------------------------------------------------------------------------------------

            // CREATE NEW EMPLOYEE OBJECT
            Employee employee = new Employee();

            // SET EMPLOYEE FIELDS
            employee.enrollNumber = EnrollNumber;
            employee.timestamp.day = Day;
            employee.timestamp.month = Month;
            employee.timestamp.year = Year;
            employee.timestamp.hours = Hour;
            employee.timestamp.minutes = Minute;
            employee.timestamp.seconds = Second;

            // SERIALIZE EMPLOYEE OBJECT TO JSON DATA FORMAT
            var json = JsonConvert.SerializeObject(employee);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // SEND POST REQUEST TO SERVER
            var client = new HttpClient();
            HttpResponseMessage response = null;

            try
            {
                  response = await client.PostAsync("http://localhost:3000/api/employeelogs", data);
                  // EXPECTED RETURN VALUE IS STATUS CODE
                  string result = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Logs.Items.Add("Server failed to process employee attendance log");
            }            
        }


        private void Connect(object sender, RoutedEventArgs e)
        {
            // IF DISCONNECTED, INITIATE CONNECTION TO FINGERPRINT SCANNER
            if (ConnectionStatus.Text == "Disconnected")
            {
                if (zk != null)
                {
                    if (zk.Connect_Net(ip_address, port))
                    {
                        Logs.Items.Add("Connected to Device!");
                        ConnectionStatus.Text = "Connected";
                        ConnectionStatus.Foreground = Brushes.LawnGreen;
                    }
                }
            }
        }


        private void Disconnect(object sender, RoutedEventArgs e)
        {
            // IF THERE IS AN EXISTING CONNECTION, DISCONNECT FROM FIGNERPRINT SCANNER
            if (ConnectionStatus.Text == "Connected")
            {
                zk.Disconnect();
                Logs.Items.Add("Device connection terminated.");
                ConnectionStatus.Text = "Disconnected";
                ConnectionStatus.Foreground = Brushes.Red;
            }
        }

        private void ClearLogs(object sender, RoutedEventArgs e)
        {
            // CLEAR LIST AND FINGERPRINT SCANNER LOGS
            Logs.Items.Clear();
            zk.ClearGLog(zk.MachineNumber);
        }
    }
}
