using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SharpDX.DirectInput;
using DIJoystick = SharpDX.DirectInput.Joystick;

namespace Joystick
{
    public partial class Form2 : Form
    {
        private DIJoystick joystick;
        private DeviceInstance device;

        private TrackBar throttleBar;
        private TextBox throttlePercent;
        private TextBox throttleRaw;
        private TextBox xBox;
        private TextBox yBox;
        private TextBox zBox;
        private CheckBox[] buttonChecks;

        private Timer updateTimer;

        public Form2(DeviceInstance selectedDevice)
        {
            InitializeComponent();
            this.device = selectedDevice;
            SetupUI();
            InitializeJoystick();
        }

        private void SetupUI()
        {
            this.Text = device.InstanceName;
            this.Size = new Size(400, 400);

            int top = 10;

            // Suwak przepustnicy
            throttleBar = new TrackBar() { Left = 10, Top = top, Width = 200, Minimum = 0, Maximum = 65535 };
            this.Controls.Add(throttleBar);

            throttlePercent = new TextBox() { Left = 220, Top = top, Width = 50 };
            this.Controls.Add(throttlePercent);

            throttleRaw = new TextBox() { Left = 280, Top = top, Width = 60 };
            this.Controls.Add(throttleRaw);

            top += 50;

            // Oś X
            var lblX = new Label() { Left = 10, Top = top, Text = "X:" };
            this.Controls.Add(lblX);
            xBox = new TextBox() { Left = 40, Top = top, Width = 100 };
            this.Controls.Add(xBox);
            top += 30;

            // Oś Y
            var lblY = new Label() { Left = 10, Top = top, Text = "Y:" };
            this.Controls.Add(lblY);
            yBox = new TextBox() { Left = 40, Top = top, Width = 100 };
            this.Controls.Add(yBox);
            top += 30;

            // Przepustnica (Z/Slider)
            var lblZ = new Label() { Left = 10, Top = top, Text = "Throttle:" };
            this.Controls.Add(lblZ);
            zBox = new TextBox() { Left = 80, Top = top, Width = 100 };
            this.Controls.Add(zBox);
            top += 40;

            // Przyciski
            buttonChecks = new CheckBox[16];
            for (int i = 0; i < 16; i++)
            {
                buttonChecks[i] = new CheckBox() { Left = 10 + (i % 4) * 90, Top = top + (i / 4) * 25, Width = 80, Text = $"Button {i}" };
                this.Controls.Add(buttonChecks[i]);
            }

            // Timer
            updateTimer = new Timer();
            updateTimer.Interval = 20;
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void InitializeJoystick()
        {
            var directInput = new DirectInput();
            joystick = new DIJoystick(directInput, device.InstanceGuid);
            joystick.Properties.BufferSize = 128;
            joystick.Acquire();

            updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            var state = joystick.GetCurrentState();
            if (state == null) return;

            // Suwak
            int throttleValue = device.InstanceName == "Logitech Attack 3" ? state.Z : state.Sliders[0];
            throttleBar.Value = Clamp(throttleValue, 0, 65535);
            throttlePercent.Text = ((throttleValue / 65535f) * 100f).ToString("0.0") + " %";
            throttleRaw.Text = throttleValue.ToString();

            // X/Y
            xBox.Text = state.X.ToString();
            yBox.Text = state.Y.ToString();

            // Z/Slider
            zBox.Text = throttleValue.ToString();

            // Przyciski
            for (int i = 0; i < buttonChecks.Length; i++)
            {
                if (i < state.Buttons.Length)
                    buttonChecks[i].Checked = state.Buttons[i];
                else
                    buttonChecks[i].Checked = false;
            }
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
