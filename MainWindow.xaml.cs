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
using System.Configuration;

namespace Fingerprint_Attendance
{
    public partial class MainWindow : Window
    {
        // Scanner config
        private static CZKEM zk;
        public string ZK_IP;
        public int ZK_PORT;
        public string API_URL;

        public MainWindow()
        {
            InitializeComponent();

            // INITIALZE FINGERPRINT SCANNER & CONFIG
            zk = new CZKEM();
            ZK_IP = ConfigurationManager.AppSettings["ZK_IP"];
            ZK_PORT = Convert.ToInt32(ConfigurationManager.AppSettings["ZK_PORT"]);
            API_URL = ConfigurationManager.AppSettings["API_URL"];

            // INITIALIZE CONNECT ON CREATE
            if (zk.Connect_Net(ZK_IP, ZK_PORT))
            {
                Logs.Items.Add("Connected to Device!");
                ConnectionStatus.Text = "Connected";
                ConnectionStatus.Foreground = Brushes.LawnGreen;

                // REGISTER FINGERPRINT SCANNER EVENTS
                if (zk.RegEvent(zk.MachineNumber, 65355))
                {
                    zk.OnAttTransactionEx += AttendanceTransactionHandler;
                }
            }
        }

        private async void AttendanceTransactionHandler(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
        {
            StringBuilder sb = new StringBuilder();

            // GENERATE LOG TIME --------------------------------------------------------------------------------------------------------------------
            DateTime dt = DateTime.Now;

            /*
                sb.AppendLine("Enroll Number: " + EnrollNumber);
                sb.AppendLine("Invalid?: " + IsInValid);
                sb.AppendLine("Attendance State: " + AttState);
                sb.AppendLine("Verify Method: " + VerifyMethod);
                sb.AppendLine("Year: " + Year);
                sb.AppendLine("Month: " + Month);
                sb.AppendLine("Day: " + Day);
                sb.AppendLine("Hour: " + Hour);
                sb.AppendLine("Minute: " + Minute);
                sb.AppendLine("Second: " + Second);
                sb.AppendLine("Work Code: " + WorkCode);
            */

            // USER IS VERIFIED
            if (IsInValid <= 0)
            {
                sb.AppendLine(" ------ [EMPLOYEE LOG] ------");
                sb.AppendFormat("User ID: {0}", EnrollNumber);
                sb.AppendFormat("\nLog Time: {0}", dt.ToLongDateString());
            }
            Logs.Items.Add(sb.ToString());


            // SEND DATA TO SERVER ------------------------------------------------------------------------------------------------------------------
            EmployeeLog el = new EmployeeLog();
            el.enrollNumber = EnrollNumber;
            el.timestamp = dt;
            el.attendanceState = AttState;

            // SERIALIZE EMPLOYEE OBJECT TO JSON DATA FORMAT
            var json = JsonConvert.SerializeObject(el);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            // SEND POST REQUEST TO SERVER
            var client = new HttpClient();
            HttpResponseMessage response;

            try
            {
                  response = await client.PostAsync(API_URL, data);
                  string result = response.Content.ReadAsStringAsync().Result;
                  Logs.Items.Add(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Logs.Items.Add("ERROR: Server failed to process employeeLog attendance log");
            }            
        }


        private void Connect(object sender, RoutedEventArgs e)
        {
            // IF DISCONNECTED, INITIATE CONNECTION TO FINGERPRINT SCANNER
            if (ConnectionStatus.Text == "Disconnected")
            {
                if (zk != null)
                {
                    if (zk.Connect_Net(ZK_IP, ZK_PORT))
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

            zk.Disconnect();
            Logs.Items.Add("Device connection terminated.");
            ConnectionStatus.Text = "Disconnected";
            ConnectionStatus.Foreground = Brushes.Red;
        }

        private void ClearLogs(object sender, RoutedEventArgs e)
        {
            // CLEAR LIST AND FINGERPRINT SCANNER LOGS
            Logs.Items.Clear();
            zk.ClearGLog(zk.MachineNumber);
        }
    }
}
