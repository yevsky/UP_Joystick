using System;
using System.Linq;
using System.Windows.Forms;
using SharpDX.DirectInput;

namespace Joystick
{
    public partial class Form1 : Form
    {
        private DirectInput directInput;
        private ListBox deviceList;
        private Button btnSelect;

        public Form1()
        {
            InitializeComponent();
            directInput = new DirectInput();
            SetupUI();
            EnumerateDevices();
        }

        private void SetupUI()
        {
            this.Text = "Wybierz urządzenie";
            this.Size = new System.Drawing.Size(300, 250);

            deviceList = new ListBox() { Left = 10, Top = 10, Width = 260, Height = 150 };
            this.Controls.Add(deviceList);

            btnSelect = new Button() { Left = 10, Top = 170, Width = 260, Height = 30, Text = "Wybierz" };
            btnSelect.Click += BtnSelect_Click;
            this.Controls.Add(btnSelect);
        }

        private void EnumerateDevices()
        {
            deviceList.Items.Clear();
            var joysticks = directInput
                .GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
                .ToList();

            if (joysticks.Count == 0)
            {
                deviceList.Items.Add("Mouse");
                deviceList.Tag = null; 
            }
            else
            {
                foreach (var dev in joysticks)
                {
                    deviceList.Items.Add(dev.InstanceName);
                }
                deviceList.Tag = joysticks;
            }
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if (deviceList.SelectedIndex < 0)
            {
                MessageBox.Show("Wybierz urządzenie");
                return;
            }

            var devices = deviceList.Tag as System.Collections.Generic.List<DeviceInstance>;

            if (devices == null)
            {
                var joystickForm = new Form2(null); 
                joystickForm.Show();
                return;
            }

            var selectedDevice = devices[deviceList.SelectedIndex];

            var joystickForm2 = new Form2(selectedDevice);
            joystickForm2.Show();
        }
    }
}