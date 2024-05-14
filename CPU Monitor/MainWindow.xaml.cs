using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace CPU_Monitor
{
    public class ThreadInfo
    {
        public int Id { get; set; }
        public string State { get; set; }
        public string PriorityLevel { get; set; }
        public int BasePriority { get; set; }
        public int CurrentPriority { get; set; }
        public string StartTime { get; set; }
        public string ThreadState { get; set; }
        public string WaitReason { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// This is where all values fetched and calculated
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private PerformanceCounter cpuCounter;
        public ObservableCollection<ThreadInfo> Threads { get; set; }


        public Visibility threadLabelsVisibility = Visibility.Collapsed;
        public Visibility ThreadLabelsVisibility { get { return threadLabelsVisibility; } set { threadLabelsVisibility = value; } }

        public ThreadInfo selectedThread;

        public MainWindow()
        {
            InitializeComponent();

            SetCoreCount();

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            Threads = new ObservableCollection<ThreadInfo>();
            DataContext = this;
            ThreadsListBox.ItemsSource = Threads;
            LoadThreads();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void SetCoreCount()
        {
            cpuCoreCountValueLabel.Text = Environment.ProcessorCount.ToString();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            setCPUUsage();
            setCPUTemperature();
            LoadThreads();
        }
        private void setCPUUsage() {
            float cpuUsage = cpuCounter.NextValue();
            cpuUsagePercentageProgressBar.Value = cpuUsage;
            cpuUsagePercentageValueLabel.Text = $"%{cpuUsage:N2}";
        }
        private void setCPUTemperature()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");

                ManagementObjectCollection results = searcher.Get();
                ManagementObject queryObj = results.Cast<ManagementObject>().FirstOrDefault();
                double temperature = Convert.ToDouble(queryObj["CurrentTemperature"].ToString());
                cpuTemperatureValueLabel.Text = $"{(temperature / 10.0) - 273.15} °C"; 
                if (temperature >= 50 && temperature <= 70)
                {
                    double fraction = (temperature - 50) / (70 - 50);
                    byte red = (byte)(255 * fraction);
                    byte green = (byte)(255 * (1 - fraction));
                    byte blue = 0;

                    temperatureIndicatorRectangle.Fill = new SolidColorBrush(Color.FromRgb(red, green, blue));
                }
                else if (temperature < 50)
                {
                    temperatureIndicatorRectangle.Fill = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    temperatureIndicatorRectangle.Fill = new SolidColorBrush(Colors.Red);
                }
            }
            catch
            {
                cpuTemperatureValueLabel.Text = "0 °C";
            }
        }

        private void LoadThreads()
        {
            Task.Run(() =>
            {
                ObservableCollection<ThreadInfo> threads = new ObservableCollection<ThreadInfo>();
                Process currentProcess = Process.GetCurrentProcess();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (ProcessThread thread in currentProcess.Threads)
                    {
                        threads.Add(new ThreadInfo
                        {
                            Id = thread.Id,
                            State = thread.ThreadState.ToString(),
                            PriorityLevel = thread.PriorityLevel.ToString(),
                            BasePriority = thread.BasePriority,
                            CurrentPriority = thread.CurrentPriority,
                            StartTime = thread.StartTime.ToString("G"),
                            ThreadState = thread.ThreadState.ToString(),
                            WaitReason = thread.ThreadState == System.Diagnostics.ThreadState.Wait ? thread.WaitReason.ToString() : "N/A",
                        });
                    }
                    UsedThreadCountValueLabel.Text = threads.Count.ToString();
                    if (!IsThreadsEqual(threads, Threads))
                    {
                        if (Threads.Count != 0) Threads.Clear();
                        foreach (var thread in threads.OrderBy(thread => thread.Id))
                        {
                            Threads.Add(thread);
                        }
                    }
                });
            });
        }
        public bool IsThreadsEqual(ObservableCollection<ThreadInfo> oldThreads, ObservableCollection<ThreadInfo> newThreads)
        {
            foreach (ThreadInfo thread in oldThreads)
            {
                if (!newThreads.Contains(thread)) return false;
            }
            return oldThreads.Count == newThreads.Count;
        }
        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedThread = (ThreadInfo)ThreadsListBox.SelectedItem;
            if (selectedThread != null)
            {
                ThreadLabelsVisibility = Visibility.Visible;

                ThreadIdValueLabel.Content = selectedThread.Id;
                ThreadStateValueLabel.Content = selectedThread.State;
                ThreadPriorityLevelValueLabel.Content = selectedThread.PriorityLevel;
                ThreadBasePriorityValueLabel.Content = selectedThread.BasePriority;
                ThreadCurrentPriorityValueLabel.Content = selectedThread.CurrentPriority;
                ThreadStartTimeValueLabel.Content = selectedThread.StartTime;
                ThreadThreadStateValueLabel.Content = selectedThread.ThreadState;
                ThreadWaitReasonValueLabel.Content = selectedThread.WaitReason;
            } 
            else 
            { 
                threadLabelsVisibility = Visibility.Collapsed;
            }
        }
    }
}