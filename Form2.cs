using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SharpDX.DirectInput;
using DIJoystick = SharpDX.DirectInput.Joystick;
using System.Collections.Generic;

namespace Joystick
{
    public partial class Form2 : Form
    {
        private DIJoystick joystick;
        private DeviceInstance device;
        private TrackBar throttleBar;
        private TextBox throttlePercent;
        private TextBox throttleRaw;
        private CheckBox[] buttonChecks;
        private Timer updateTimer;
        private Panel drawingPanel;
        private Dictionary<Color, List<PointF>> colorTracePoints = new Dictionary<Color, List<PointF>>();
        private JoystickState currentState;
        private Color currentTracePenColor = Color.Blue;
        private TrackBar lineThicknessBar;
        private TextBox lineThicknessTextBox;

        private bool useMouse = false;
        private Point lastMousePos;
        private bool mouseDrawing = false;

        public Form2(DeviceInstance selectedDevice)
        {
            InitializeComponent();

            device = selectedDevice;
            useMouse = (device == null); 

            SetupUI();

            if (!useMouse)
                InitializeJoystick();
            else
                InitializeMouseMode();
        }

        private void SetupUI()
        {
            this.Text = useMouse ? "Mouse (Fallback)" : device.InstanceName;
            this.WindowState = FormWindowState.Maximized;

            int top = 10;

            throttleBar = new TrackBar()
            {
                Left = 10,
                Top = top,
                Width = 200,
                Minimum = 1,
                Maximum = 10
            };
            Controls.Add(throttleBar);

            throttlePercent = new TextBox()
            {
                Left = 220,
                Top = top,
                Width = 50
            };
            Controls.Add(throttlePercent);

            throttleRaw = new TextBox()
            {
                Left = 280,
                Top = top,
                Width = 60
            };
            Controls.Add(throttleRaw);

            Button clearButton = new Button()
            {
                Left = 350,
                Top = top,
                Text = "Wyczyść"
            };
            clearButton.Click += (s, e) =>
            {
                colorTracePoints.Clear();
                drawingPanel.Invalidate();
            };
            Controls.Add(clearButton);

            top += 50;

            buttonChecks = new CheckBox[16];
            for (int i = 0; i < 16; i++)
            {
                buttonChecks[i] = new CheckBox()
                {
                    Left = 10 + (i % 4) * 90,
                    Top = top + (i / 4) * 25,
                    Width = 80,
                    Text = $"Button {i}"
                };
                Controls.Add(buttonChecks[i]);
            }

            drawingPanel = new Panel()
            {
                Left = 10,
                Top = top + 100,
                Width = this.ClientSize.Width - 20,
                Height = this.ClientSize.Height - top - 120
            };
            drawingPanel.Paint += DrawingPanel_Paint;
            Controls.Add(drawingPanel);

            lineThicknessBar = new TrackBar()
            {
                Left = 10,
                Top = top + 200,
                Width = 200,
                Minimum = 1,
                Maximum = 10,
                Value = 2
            };
            Controls.Add(lineThicknessBar);

            lineThicknessTextBox = new TextBox()
            {
                Left = 220,
                Top = top + 200,
                Width = 50,
                Text = "2"
            };
            Controls.Add(lineThicknessTextBox);

            lineThicknessBar.ValueChanged += LineThicknessBar_ValueChanged;

            updateTimer = new Timer();
            updateTimer.Interval = 20;
            updateTimer.Tick += UpdateTimer_Tick;

            this.Resize += Form2_Resize;
        }

        private void LineThicknessBar_ValueChanged(object sender, EventArgs e)
        {
            lineThicknessTextBox.Text = lineThicknessBar.Value.ToString();
            drawingPanel.Invalidate();
        }

        private void Form2_Resize(object sender, EventArgs e)
        {
            drawingPanel.Width = this.ClientSize.Width - 20;
            drawingPanel.Height = this.ClientSize.Height - (throttleBar.Top + throttleBar.Height + 20);
            drawingPanel.Invalidate();
        }


        private void InitializeMouseMode()
        {
            throttleRaw.Text = "Mouse";
            throttlePercent.Text = "Mouse";

            drawingPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    currentTracePenColor = Color.Blue;
                    mouseDrawing = true;
                }
                else if (e.Button == MouseButtons.Right)
                {
                    colorTracePoints.Clear();
                    drawingPanel.Invalidate();
                }
            };

            drawingPanel.MouseUp += (s, e) =>
            {
                mouseDrawing = false;
            };

            drawingPanel.MouseMove += (s, e) =>
            {
                lastMousePos = e.Location;
                if (mouseDrawing)
                    AddMousePoint(e.Location);

                drawingPanel.Invalidate();
            };

            drawingPanel.MouseWheel += (s, e) =>
            {
                throttleBar.Value = Clamp(
                    throttleBar.Value + (e.Delta > 0 ? 1 : -1),
                    throttleBar.Minimum,
                    throttleBar.Maximum
                );

                throttleRaw.Text = throttleBar.Value.ToString();
                throttlePercent.Text = ((throttleBar.Value / 10f) * 100).ToString("0.0") + " %";
            };

            updateTimer.Start();
        }

        private void AddMousePoint(Point p)
        {
            if (!colorTracePoints.ContainsKey(currentTracePenColor))
                colorTracePoints[currentTracePenColor] = new List<PointF>();

            colorTracePoints[currentTracePenColor].Add(p);
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
            if (useMouse)
            {
                UpdateMouseMode();
                return;
            }

            var state = joystick.GetCurrentState();
            if (state == null) return;

            currentState = state;

            int throttleValue = device.InstanceName == "Logitech Attack 3"
                ? state.Z
                : state.Sliders[0];

            int scaled = (int)((float)throttleValue / 65535 * 9) + 1;
            throttleBar.Value = Clamp(scaled, 1, 10);

            throttlePercent.Text = ((throttleValue / 65535f) * 100).ToString("0.0") + " %";
            throttleRaw.Text = throttleValue.ToString();

            for (int i = 0; i < buttonChecks.Length; i++)
                buttonChecks[i].Checked = i < state.Buttons.Length && state.Buttons[i];

            if (state.Buttons.Length > 0)
            {
                if (state.Buttons[0]) AddPointToCurrentColor(state);
                if (state.Buttons[1]) { currentTracePenColor = Color.Green; AddPointToCurrentColor(state); }
                if (state.Buttons[2]) { currentTracePenColor = Color.Red; AddPointToCurrentColor(state); }
                if (state.Buttons[3]) { currentTracePenColor = Color.Blue; AddPointToCurrentColor(state); }
                if (state.Buttons[4]) { colorTracePoints.Clear(); }
            }

            drawingPanel.Invalidate();
        }

        private void UpdateMouseMode()
        {
            currentState = new JoystickState();
            drawingPanel.Invalidate();
        }

        private void AddPointToCurrentColor(JoystickState state)
        {
            if (!colorTracePoints.ContainsKey(currentTracePenColor))
                colorTracePoints[currentTracePenColor] = new List<PointF>();

            float scaleX = (state.X / 65535f) * drawingPanel.Width;
            float scaleY = (state.Y / 65535f) * drawingPanel.Height;

            colorTracePoints[currentTracePenColor].Add(new PointF(scaleX, scaleY));
        }

        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            foreach (var colorPoints in colorTracePoints)
            {
                using (Pen p = new Pen(colorPoints.Key, lineThicknessBar.Value))
                {
                    if (colorPoints.Value.Count > 1)
                        g.DrawLines(p, colorPoints.Value.ToArray());
                }
            }

            if (!useMouse)
            {
                float x = (currentState.X / 65535f) * drawingPanel.Width;
                float y = (currentState.Y / 65535f) * drawingPanel.Height;

                g.FillEllipse(Brushes.Red, x - 5, y - 5, 10, 10);
                g.DrawString($"X: {currentState.X}, Y: {currentState.Y}", this.Font, Brushes.Black, new PointF(10, 10));
            }
            else
            {
                g.FillEllipse(Brushes.Red, lastMousePos.X - 5, lastMousePos.Y - 5, 10, 10);
                g.DrawString($"Mouse X: {lastMousePos.X}, Y: {lastMousePos.Y}", this.Font, Brushes.Black, new PointF(10, 10));
            }
        }

        private int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
