using System;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private const int IDLE_THRESHOLD = 2 * 60 * 1000; // 2 minutes in milliseconds
        private System.Timers.Timer? _idleTimer;
        private DateTime _lastActivityTime;
        private Employee? _employee;
        private bool _isPunchIn = false;

        public MainWindow()
        {
            InitializeComponent();
            FetchEmployee();
            DisableCloseAndMinimize();
        }

        private string GetCurrentUsername()
        {
            return WindowsIdentity.GetCurrent().Name;
        }

        private async void FetchEmployee()
        {
            try
            {
                string username = GetCurrentUsername();
                MessageBox.Show($"Employee details JSON response: {username}");
                var employee = await GetEmployeeAsync("6684fffa89229a35226d460a");
                if (employee != null)
                {
                    _employee = employee;
                    LogEmployeeDetails(_employee);
                    DisplayButtons(_employee.Shift);
                    SetupIdleTimer();
                }
                else
                {
                    MessageBox.Show("Failed to fetch employee details");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private static async Task<Employee?> GetEmployeeAsync(string username)
        {
            using var client = new HttpClient();
            var response = await client.PostAsync("http://localhost:8000/demo/findOneUserDetails", new StringContent(JsonConvert.SerializeObject(new { username }), Encoding.UTF8, "application/json"));

            var jsonResponse = await response.Content.ReadAsStringAsync();
            MessageBox.Show($"Employee details JSON response: {jsonResponse}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<EmployeeResponse>(jsonResponse);
                    if (data?.StatusCode == 200)
                    {
                        return data.data;
                    }
                    else
                    {
                        MessageBox.Show($"Error: Status code is {data?.StatusCode}");
                    }
                }
                catch (JsonException jsonEx)
                {
                    MessageBox.Show($"Error deserializing JSON: {jsonEx.Message}");
                }
            }
            else
            {
                MessageBox.Show($"Error: {response.ReasonPhrase}");
            }
            return null;
        }

        private void LogEmployeeDetails(Employee employee)
        {
            // Log to console
            Console.WriteLine($"Employee ID: {employee.Id}");
            Console.WriteLine($"Employee Shift: {employee.Shift}");

            // Display in a message box
            MessageBox.Show($"Employee ID: {employee.Id}\nEmployee Shift: {employee.Shift}", "Employee Details");
        }

        private void DisplayButtons(string shift)
        {
            ButtonPanel.Children.Clear();
            string[] buttons = shift == "day"
                ? new[] { "punchin", "coffee-break", "tea-break", "meeting", "lunch", "punchout" }
                : new[] { "punchin", "dinner", "coffee-break", "punchout" };

            foreach (var button in buttons)
            {
                var btn = new Button { Content = button, Margin = new Thickness(5) };
                btn.Click += (s, e) => HandleEvent(button);
                ButtonPanel.Children.Add(btn);
            }
        }

        private void SetupIdleTimer()
        {
            _lastActivityTime = DateTime.Now;
            _idleTimer = new System.Timers.Timer(60000);
            _idleTimer.Elapsed += CheckIdleTime;
            _idleTimer.Start();

            this.MouseMove += (s, e) => ResetIdleTimer();
            this.KeyDown += (s, e) => ResetIdleTimer();
        }

        private void ResetIdleTimer()
        {
            _lastActivityTime = DateTime.Now;
        }

        private void CheckIdleTime(object? sender, ElapsedEventArgs e)
        {
            var idleTime = DateTime.Now - _lastActivityTime;
            if (idleTime.TotalMilliseconds > IDLE_THRESHOLD && _isPunchIn)
            {
                HandleEvent("idle");
                _lastActivityTime = DateTime.Now;
            }
        }

        private async void HandleEvent(string eventType)
        {
            if (eventType == "punchin")
            {
                _isPunchIn = true;
            }
            else if (eventType == "punchout")
            {
                _isPunchIn = false;
            }

            await PostEventAsync(eventType);
        }

        private async Task PostEventAsync(string eventType)
        {
            if (_employee == null)
            {
                MessageBox.Show("Employee details are not available.");
                return;
            }

            using var client = new HttpClient();
            var response = await client.PostAsync("http://localhost:8000/demo/empoloyeeSheet", new StringContent(JsonConvert.SerializeObject(new
            {
                username = GetCurrentUsername(), // Use the current username here
                shift = _employee.Shift,
                employeeId = _employee.Id,
                eventType
            }), Encoding.UTF8, "application/json"));

            var jsonResponse = await response.Content.ReadAsStringAsync();
            MessageBox.Show($"Employee details JSON response: {jsonResponse}");

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Failed to log event.");
            }
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                Background = System.Windows.Media.Brushes.White;
            }
            else
            {
                WindowState = WindowState.Maximized;
                Background = System.Windows.Media.Brushes.Black;
            }
        }

        private void DisableCloseAndMinimize()
        {
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
        }
    }

    public class EmployeeResponse
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("data")]
        public Employee? data { get; set; }
    }

    public class Employee
    {
        [JsonProperty("_id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("shift")]
        public string Shift { get; set; } = string.Empty;
    }
}
