using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Management;
using System.Diagnostics;
using MouseKeyboardLibrary;
using System.Runtime.InteropServices;
using System.Threading;

namespace COMPort {
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private SerialPort port;
        private DispatcherTimer time;
        private string data;
        private Point mPos;
        private MouseEventFlags mEvent;

        public MainWindow() {
            InitializeComponent();
            time = new DispatcherTimer();
            time.Interval = TimeSpan.FromMilliseconds(1);
            this.Closing += MainWindow_Closing;
            var usbDevices = GetUSBDevices("Arduino");
            foreach (var i in usbDevices) {
                string portName = i.Name.Substring(i.Name.LastIndexOf('(') + 1).Replace(")", "");
                port = new SerialPort(portName);
                port.BaudRate = 9600;
                port.StopBits = StopBits.One;
                port.Parity = Parity.None;
                port.DataBits = 8;
                port.DtrEnable = true;
                port.DataReceived += port_DataReceived;
                time.Tick += time_Tick;
                port.Open();
                tb.IsChecked = true;
                time.Start();
            }
        }

        void MainWindow_Closing(object sender, CancelEventArgs e) {
            if (port != null) {
                port.Close();
                port.Dispose();
                port = null;
            }
        }

        static List<USBDeviceInfo> GetUSBDevices(string filter = null) {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            try {
                foreach (var device in collection) {
                    string name = device.GetPropertyValue("Name").ToString();
                    if (filter != null && !name.ToLower().Contains(filter.ToLower())) continue;
                    string description = (string)device.GetPropertyValue("Description");
                    string pnpDeviceID = (string)device.GetPropertyValue("PNPDeviceID");
                    string deviceID = (string)device.GetPropertyValue("DeviceID");
                    devices.Add(new USBDeviceInfo(deviceID, pnpDeviceID, description, name));
                }
            } catch { }

            collection.Dispose();
            return devices;
        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            data = port.ReadLine();
        }

        private void Comports_DropDownOpened(object sender, EventArgs e) {
            Comports.Items.Clear();
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
                if (!Comports.Items.Contains(port))
                    Comports.Items.Add(port);
        }

        void time_Tick(object sender, EventArgs e) {
            string data = this.data;
            try {
                if (!Regex.IsMatch(data, @"^[0-1]+\tX=[0-9\-]+\tY=[0-9\-]+\tZ=[0-9\-]+")) return;
                int indx_x = data.IndexOf("X=") + 2;
                int length_x = data.IndexOf("\t", indx_x) - indx_x;
                int indx_y = data.IndexOf("Y=") + 2;
                int length_y = data.IndexOf("\t", indx_y) - indx_y;
                int indx_z = data.IndexOf("Z=") + 2;
                int length_z = data.IndexOf("\r", indx_z) - indx_z;
                int x = Convert.ToInt32(data.Substring(indx_x, length_x));
                int y = -Convert.ToInt32(data.Substring(indx_y, length_y));
                int z = Convert.ToInt32(data.Substring(indx_z, length_z));
                int mouseButton = Convert.ToInt32(data.Substring(0, data.IndexOf("\t")), 2);
                this.data = null;

                //double left = Canvas.GetLeft(cursor) + x;
                //double top = Canvas.GetTop(cursor) + y;
                mPos = GlobalMouse.Position;
                int X = (int)(mPos.X + x);
                int Y = (int)(mPos.Y + y);
                GlobalMouse.Position = new Point(X, Y);
                char[] mask = "000".ToCharArray();
                if (z > 0)
                    GlobalMouse.MouseWheel(z);
                else if (z < 0)
                    GlobalMouse.MouseWheel(z);
                if ((mouseButton & 9) == 9 | (mouseButton & 10) == 10 | (mouseButton & 12) == 12) {
                    if ((mouseButton & 9) == 9 && (mEvent & MouseEventFlags.LeftUp) != MouseEventFlags.LeftUp) {
                        GlobalMouse.MouseAction(MouseEventFlags.LeftDown);
                        Thread.Sleep(20);
                        mEvent |= MouseEventFlags.LeftUp;
                        mask[0] = '1';
                        leftClick.Fill = Brushes.DarkRed;
                    }
                    if ((mouseButton & 10) == 10 && (mEvent & MouseEventFlags.RightUp) != MouseEventFlags.RightUp) {
                        GlobalMouse.MouseAction(MouseEventFlags.RightDown);
                        Thread.Sleep(20);
                        mEvent |= MouseEventFlags.RightUp;
                        mask[1] = '1';
                        rigthClick.Fill = Brushes.DarkRed;
                    }
                    if ((mouseButton & 12) == 12 && (mEvent & MouseEventFlags.MiddleUp) != MouseEventFlags.MiddleUp) {
                        GlobalMouse.MouseAction(MouseEventFlags.MiddleDown);
                        Thread.Sleep(20);
                        mEvent |= MouseEventFlags.MiddleUp;
                        mask[2] = '1';
                        centerClick.Fill = Brushes.DarkRed;
                    }
                } else {
                    GlobalMouse.MouseAction(mEvent);
                    mEvent = MouseEventFlags.MoveNoCoalesce;
                    leftClick.Fill = new SolidColorBrush(Color.FromRgb(0x5C, 0x54, 0x6A));
                    rigthClick.Fill = new SolidColorBrush(Color.FromRgb(0x86, 0x84, 0x91));
                    centerClick.Fill = new SolidColorBrush(Color.FromRgb(0xB4, 0xB6, 0xBC));
                }

                //Title = left + ", " + top;
                //Canvas.SetLeft(cursor, left);
                //Canvas.SetTop(cursor, top);

            } catch { }

            this.data = null;
        }

        private void clicked(object sender, RoutedEventArgs e) {
            ToggleButton tb = (ToggleButton)sender;
            if ((bool)tb.IsChecked) {
                tb.Content = "Stop";
                if (port != null) {
                    port.Close();
                    port.Dispose();
                    port = null;
                }
                port = new SerialPort((string)Comports.SelectedItem);
                port.BaudRate = 9600;
                port.StopBits = StopBits.One;
                port.Parity = Parity.None;
                port.DataBits = 8;
                port.DtrEnable = true;
                port.DataReceived += port_DataReceived;
                time.Tick += time_Tick;
                port.Open();
                time.Start();
            } else {
                tb.Content = "Start";
                time.Stop();
                if (port != null) {
                    port.Close();
                    port.Dispose();
                    port = null;
                }
            }
        }
    }

    public class USBDeviceInfo {
        public USBDeviceInfo(string _deviceID, string _pnpDeviceID, string _description, string _name) {
            this.DeviceID = _deviceID;
            this.PnpDeviceID = _pnpDeviceID;
            this.Description = _description;
            this.Name = _name;
        }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Description { get; private set; }
        public string Name { get; private set; }
    }
}
