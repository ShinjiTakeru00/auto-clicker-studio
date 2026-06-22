using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoClicker
{
    internal static class Theme
    {
        public static readonly Color Window = Color.FromArgb(8, 13, 24);
        public static readonly Color HeaderA = Color.FromArgb(8, 14, 28);
        public static readonly Color HeaderB = Color.FromArgb(13, 71, 76);
        public static readonly Color Panel = Color.FromArgb(15, 23, 42);
        public static readonly Color PanelSoft = Color.FromArgb(20, 31, 52);
        public static readonly Color Border = Color.FromArgb(45, 212, 191);
        public static readonly Color BorderSoft = Color.FromArgb(45, 61, 89);
        public static readonly Color Text = Color.FromArgb(232, 246, 255);
        public static readonly Color Muted = Color.FromArgb(145, 168, 190);
        public static readonly Color Accent = Color.FromArgb(45, 212, 191);
        public static readonly Color AccentDeep = Color.FromArgb(14, 165, 233);
        public static readonly Color Danger = Color.FromArgb(244, 63, 94);
        public static readonly Color Warning = Color.FromArgb(250, 204, 21);
        public static readonly Color Input = Color.FromArgb(10, 18, 32);
    }

    internal static class Native
    {
        public const int MOD_NOREPEAT = 0x4000;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;

        private const int INPUT_MOUSE = 0;
        private const int WHEEL_DELTA = 120;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x1000;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void Click(bool rightClick)
        {
            uint down = rightClick ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN;
            uint up = rightClick ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP;
            SendMouseInput(new MouseAction(0, 0, 0, down), new MouseAction(0, 0, 0, up));
        }

        public static void MoveRelative(int dx, int dy)
        {
            if (dx == 0 && dy == 0)
            {
                return;
            }

            SendMouseInput(new MouseAction(dx, dy, 0, MOUSEEVENTF_MOVE));
        }

        public static void ScrollVertical(int notches)
        {
            if (notches == 0)
            {
                return;
            }

            SendMouseInput(new MouseAction(0, 0, notches * WHEEL_DELTA, MOUSEEVENTF_WHEEL));
        }

        public static void ScrollHorizontal(int notches)
        {
            if (notches == 0)
            {
                return;
            }

            SendMouseInput(new MouseAction(0, 0, notches * WHEEL_DELTA, MOUSEEVENTF_HWHEEL));
        }

        private static void SendMouseInput(params MouseAction[] actions)
        {
            INPUT[] inputs = new INPUT[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                inputs[i].type = INPUT_MOUSE;
                inputs[i].mi.dx = actions[i].Dx;
                inputs[i].mi.dy = actions[i].Dy;
                inputs[i].mi.mouseData = unchecked((uint)actions[i].Data);
                inputs[i].mi.dwFlags = actions[i].Flags;
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private struct MouseAction
        {
            public readonly int Dx;
            public readonly int Dy;
            public readonly int Data;
            public readonly uint Flags;

            public MouseAction(int dx, int dy, int data, uint flags)
            {
                Dx = dx;
                Dy = dy;
                Data = data;
                Flags = flags;
            }
        }
    }

    internal sealed class HeaderPanel : Panel
    {
        public HeaderPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, Theme.HeaderA, Theme.HeaderB, LinearGradientMode.Horizontal))
            using (Pen gridPen = new Pen(Color.FromArgb(18, Theme.Accent), 1F))
            using (Pen glowPen = new Pen(Color.FromArgb(110, Theme.Accent), 2F))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
                for (int x = 0; x < Width; x += 42)
                {
                    e.Graphics.DrawLine(gridPen, x, 0, x + 34, Height);
                }
                e.Graphics.DrawLine(glowPen, 24, Height - 2, Width - 24, Height - 2);
            }

            base.OnPaint(e);
        }
    }

    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            BackColor = Theme.Panel;
            Padding = new Padding(18);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            using (SolidBrush fill = new SolidBrush(BackColor))
            using (Pen border = new Pen(Theme.BorderSoft))
            using (Pen accent = new Pen(Color.FromArgb(185, Theme.Border), 1.6F))
            using (SolidBrush accentDot = new SolidBrush(Color.FromArgb(190, Theme.Accent)))
            {
                Rectangle shadowRect = new Rectangle(4, 5, Width - 9, Height - 9);
                Rectangle rect = new Rectangle(0, 0, Width - 8, Height - 8);
                using (GraphicsPath shadowPath = RoundedRect(shadowRect, 14))
                using (GraphicsPath path = RoundedRect(rect, 14))
                {
                    e.Graphics.FillPath(shadow, shadowPath);
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }

                e.Graphics.DrawLine(accent, 20, 1, Width - 30, 1);
                e.Graphics.FillEllipse(accentDot, 18, 18, 5, 5);
            }

            base.OnPaint(e);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class AccentButton : Button
    {
        private readonly Color normalColor;
        private readonly Color hoverColor;

        public AccentButton(Color normal, Color hover)
        {
            normalColor = normal;
            hoverColor = hover;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = normalColor;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            BackColor = hoverColor;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            BackColor = normalColor;
            base.OnMouseLeave(e);
        }
    }

    internal sealed class MainForm : Form
    {
        private const int WM_HOTKEY = 0x0312;

        private readonly TrackBar speedTrack = new TrackBar();
        private readonly Label speedValue = new Label();
        private readonly NumericUpDown intervalBox = new NumericUpDown();
        private readonly NumericUpDown delayBox = new NumericUpDown();
        private readonly ComboBox actionModeBox = new ComboBox();
        private readonly RadioButton leftButton = new RadioButton();
        private readonly RadioButton rightButton = new RadioButton();
        private readonly ComboBox movePresetBox = new ComboBox();
        private readonly NumericUpDown moveXBox = new NumericUpDown();
        private readonly NumericUpDown moveYBox = new NumericUpDown();
        private readonly NumericUpDown verticalScrollBox = new NumericUpDown();
        private readonly NumericUpDown horizontalScrollBox = new NumericUpDown();
        private readonly ComboBox macroFormatBox = new ComboBox();
        private readonly TextBox macroBox = new TextBox();
        private readonly NumericUpDown keyPauseBox = new NumericUpDown();
        private readonly AccentButton startButton = new AccentButton(Theme.AccentDeep, Color.FromArgb(2, 132, 199));
        private readonly AccentButton stopButton = new AccentButton(Theme.Danger, Color.FromArgb(225, 29, 72));
        private readonly Label statusLabel = new Label();
        private readonly Panel statusDot = new Panel();
        private readonly System.Windows.Forms.Timer actionTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer countdownTimer = new System.Windows.Forms.Timer();

        private bool running;
        private bool countingDown;
        private bool syncingSpeed;
        private bool syncingPointer;
        private bool actionBusy;
        private DateTime startAt = DateTime.MinValue;

        public MainForm()
        {
            Text = "Auto Clicker Studio";
            ClientSize = new Size(900, 640);
            MinimumSize = new Size(900, 640);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Window;
            Font = new Font("Segoe UI", 9F);

            BuildUi();
            StyleInteractiveControls(this);
            WireEvents();
            SyncFromSpeed();
            SetStatus("READY", Theme.Accent);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Native.RegisterHotKey(Handle, 1, Native.MOD_NOREPEAT, Native.VK_F6);
            Native.RegisterHotKey(Handle, 2, Native.MOD_NOREPEAT, Native.VK_F7);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopAction();
            Native.UnregisterHotKey(Handle, 1);
            Native.UnregisterHotKey(Handle, 2);
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == 1)
                {
                    ToggleAction();
                }
                else if (id == 2)
                {
                    StopAction();
                }
            }

            base.WndProc(ref m);
        }

        private void BuildUi()
        {
            HeaderPanel header = new HeaderPanel
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 112)
            };
            Controls.Add(header);

            Label title = new Label
            {
                Text = "Auto Clicker Studio",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Theme.Text,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(24, 18)
            };
            header.Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "Repeat clicks, keyboard macros, cursor movement, and wheel input with one timing engine",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(200, 240, 238),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(28, 62)
            };
            header.Controls.Add(subtitle);

            Label hotkeys = new Label
            {
                Text = "F6 toggle  |  F7 stop",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(724, 44)
            };
            header.Controls.Add(hotkeys);

            CardPanel automationCard = new CardPanel
            {
                Location = new Point(20, 132),
                Size = new Size(280, 372)
            };
            Controls.Add(automationCard);

            AddSectionTitle(automationCard, "Automation", 18, 16);
            AddMutedLabel(automationCard, "Repeat rate", 20, 58);

            speedTrack.Minimum = 5;
            speedTrack.Maximum = 500;
            speedTrack.TickFrequency = 50;
            speedTrack.Value = 100;
            speedTrack.Location = new Point(18, 78);
            speedTrack.Size = new Size(158, 45);
            automationCard.Controls.Add(speedTrack);

            speedValue.Text = "10.0 CPS";
            speedValue.BackColor = Color.FromArgb(16, 58, 66);
            speedValue.ForeColor = Theme.Accent;
            speedValue.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            speedValue.TextAlign = ContentAlignment.MiddleCenter;
            speedValue.Location = new Point(184, 82);
            speedValue.Size = new Size(72, 28);
            automationCard.Controls.Add(speedValue);

            AddMutedLabel(automationCard, "Interval", 20, 130);
            intervalBox.Minimum = 20;
            intervalBox.Maximum = 5000;
            intervalBox.Value = 100;
            intervalBox.Increment = 10;
            intervalBox.Location = new Point(20, 152);
            intervalBox.Size = new Size(108, 23);
            automationCard.Controls.Add(intervalBox);
            AddMutedLabel(automationCard, "ms per cycle", 20, 181);

            AddMutedLabel(automationCard, "Start delay", 152, 130);
            delayBox.Minimum = 0;
            delayBox.Maximum = 30;
            delayBox.DecimalPlaces = 1;
            delayBox.Increment = 0.5M;
            delayBox.Value = 2;
            delayBox.Location = new Point(154, 152);
            delayBox.Size = new Size(100, 23);
            automationCard.Controls.Add(delayBox);
            AddMutedLabel(automationCard, "seconds", 154, 181);

            AddMutedLabel(automationCard, "Action mode", 20, 218);
            actionModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionModeBox.Items.AddRange(new object[]
            {
                "Click only",
                "Keyboard macro",
                "Pointer move",
                "Scroll wheel",
                "Click + macro",
                "Full sequence"
            });
            actionModeBox.SelectedIndex = 0;
            actionModeBox.Location = new Point(20, 240);
            actionModeBox.Size = new Size(234, 23);
            automationCard.Controls.Add(actionModeBox);

            AddMutedLabel(automationCard, "Mouse button", 20, 292);
            leftButton.Text = "Left";
            leftButton.Checked = true;
            leftButton.AutoSize = true;
            leftButton.Location = new Point(22, 316);
            automationCard.Controls.Add(leftButton);

            rightButton.Text = "Right";
            rightButton.AutoSize = true;
            rightButton.Location = new Point(92, 316);
            automationCard.Controls.Add(rightButton);

            CardPanel pointerCard = new CardPanel
            {
                Location = new Point(310, 132),
                Size = new Size(280, 372)
            };
            Controls.Add(pointerCard);

            AddSectionTitle(pointerCard, "Pointer Control", 18, 16);
            AddMutedLabel(pointerCard, "Movement preset", 20, 58);
            movePresetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            movePresetBox.Items.AddRange(new object[]
            {
                "Custom X/Y",
                "Move up",
                "Move down",
                "Move left",
                "Move right",
                "Move up-left",
                "Move up-right",
                "Move down-left",
                "Move down-right"
            });
            movePresetBox.SelectedIndex = 4;
            movePresetBox.Location = new Point(20, 80);
            movePresetBox.Size = new Size(234, 23);
            pointerCard.Controls.Add(movePresetBox);

            AddMutedLabel(pointerCard, "Horizontal move", 20, 126);
            moveXBox.Minimum = -1000;
            moveXBox.Maximum = 1000;
            moveXBox.Value = 20;
            moveXBox.Increment = 5;
            moveXBox.Location = new Point(20, 148);
            moveXBox.Size = new Size(108, 23);
            pointerCard.Controls.Add(moveXBox);
            AddMutedLabel(pointerCard, "pixels", 20, 177);

            AddMutedLabel(pointerCard, "Vertical move", 150, 126);
            moveYBox.Minimum = -1000;
            moveYBox.Maximum = 1000;
            moveYBox.Value = 0;
            moveYBox.Increment = 5;
            moveYBox.Location = new Point(152, 148);
            moveYBox.Size = new Size(102, 23);
            pointerCard.Controls.Add(moveYBox);
            AddMutedLabel(pointerCard, "pixels", 152, 177);

            AddMutedLabel(pointerCard, "Vertical scroll", 20, 220);
            verticalScrollBox.Minimum = -20;
            verticalScrollBox.Maximum = 20;
            verticalScrollBox.Value = -3;
            verticalScrollBox.Location = new Point(20, 242);
            verticalScrollBox.Size = new Size(108, 23);
            pointerCard.Controls.Add(verticalScrollBox);
            AddMutedLabel(pointerCard, "wheel notches", 20, 271);

            AddMutedLabel(pointerCard, "Side scroll", 150, 220);
            horizontalScrollBox.Minimum = -20;
            horizontalScrollBox.Maximum = 20;
            horizontalScrollBox.Value = 0;
            horizontalScrollBox.Location = new Point(152, 242);
            horizontalScrollBox.Size = new Size(102, 23);
            pointerCard.Controls.Add(horizontalScrollBox);
            AddMutedLabel(pointerCard, "left / right", 152, 271);

            Label pointerHint = new Label
            {
                Text = "Positive X moves right. Positive Y moves down.",
                ForeColor = Theme.Muted,
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Location = new Point(20, 314),
                Size = new Size(232, 34)
            };
            pointerCard.Controls.Add(pointerHint);

            CardPanel macroCard = new CardPanel
            {
                Location = new Point(600, 132),
                Size = new Size(280, 372)
            };
            Controls.Add(macroCard);

            AddSectionTitle(macroCard, "Keyboard Macro", 18, 16);
            AddMutedLabel(macroCard, "Macro format", 20, 58);
            macroFormatBox.DropDownStyle = ComboBoxStyle.DropDownList;
            macroFormatBox.Items.AddRange(new object[] { "Plain text", "SendKeys syntax" });
            macroFormatBox.SelectedIndex = 0;
            macroFormatBox.Location = new Point(20, 80);
            macroFormatBox.Size = new Size(234, 23);
            macroCard.Controls.Add(macroFormatBox);

            AddMutedLabel(macroCard, "Keys to send", 20, 126);
            macroBox.Multiline = true;
            macroBox.ScrollBars = ScrollBars.Vertical;
            macroBox.AcceptsReturn = true;
            macroBox.AcceptsTab = true;
            macroBox.Text = "hello";
            macroBox.Location = new Point(20, 148);
            macroBox.Size = new Size(234, 116);
            macroCard.Controls.Add(macroBox);

            AddMutedLabel(macroCard, "Character pause", 20, 292);
            keyPauseBox.Minimum = 0;
            keyPauseBox.Maximum = 1000;
            keyPauseBox.Increment = 5;
            keyPauseBox.Value = 0;
            keyPauseBox.Location = new Point(20, 314);
            keyPauseBox.Size = new Size(92, 23);
            macroCard.Controls.Add(keyPauseBox);
            AddMutedLabel(macroCard, "milliseconds", 124, 317);

            Panel footer = new Panel
            {
                BackColor = Theme.Window,
                Location = new Point(20, 528),
                Size = new Size(860, 74)
            };
            Controls.Add(footer);

            startButton.Text = "Start";
            startButton.Location = new Point(0, 14);
            startButton.Size = new Size(156, 42);
            footer.Controls.Add(startButton);

            stopButton.Text = "Stop";
            stopButton.Location = new Point(172, 14);
            stopButton.Size = new Size(156, 42);
            footer.Controls.Add(stopButton);

            statusDot.BackColor = Theme.Accent;
            statusDot.Location = new Point(630, 30);
            statusDot.Size = new Size(10, 10);
            footer.Controls.Add(statusDot);

            statusLabel.Text = "Ready";
            statusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            statusLabel.ForeColor = Theme.Text;
            statusLabel.AutoSize = false;
            statusLabel.Location = new Point(648, 22);
            statusLabel.Size = new Size(198, 26);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            footer.Controls.Add(statusLabel);
        }

        private static void AddSectionTitle(Control parent, string text, int x, int y)
        {
            Label label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.Text,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            };
            parent.Controls.Add(label);
        }

        private static void AddMutedLabel(Control parent, string text, int x, int y)
        {
            Label label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Theme.Muted,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            };
            parent.Controls.Add(label);
        }

        private static void StyleInteractiveControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is ComboBox)
                {
                    ComboBox box = (ComboBox)control;
                    box.BackColor = Theme.Input;
                    box.ForeColor = Theme.Text;
                    box.FlatStyle = FlatStyle.Flat;
                }
                else if (control is NumericUpDown)
                {
                    NumericUpDown number = (NumericUpDown)control;
                    number.BackColor = Theme.Input;
                    number.ForeColor = Theme.Text;
                }
                else if (control is TextBox)
                {
                    TextBox text = (TextBox)control;
                    text.BackColor = Theme.Input;
                    text.ForeColor = Theme.Text;
                    text.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is RadioButton)
                {
                    RadioButton radio = (RadioButton)control;
                    radio.ForeColor = Theme.Text;
                    radio.BackColor = Color.Transparent;
                    radio.FlatStyle = FlatStyle.Flat;
                }
                else if (control is TrackBar)
                {
                    control.BackColor = Theme.Panel;
                    control.ForeColor = Theme.Accent;
                }

                if (control.HasChildren)
                {
                    StyleInteractiveControls(control);
                }
            }
        }

        private void WireEvents()
        {
            speedTrack.ValueChanged += delegate { SyncFromSpeed(); };
            intervalBox.ValueChanged += delegate { SyncFromInterval(); };
            movePresetBox.SelectedIndexChanged += delegate { ApplyMovePreset(); };
            moveXBox.ValueChanged += delegate { MarkCustomMove(); };
            moveYBox.ValueChanged += delegate { MarkCustomMove(); };
            startButton.Click += delegate { StartAction(); };
            stopButton.Click += delegate { StopAction(); };

            countdownTimer.Interval = 50;
            countdownTimer.Tick += CountdownTimerTick;
            actionTimer.Tick += delegate { RunActionCycle(); };
        }

        private void SyncFromSpeed()
        {
            if (syncingSpeed)
            {
                return;
            }

            syncingSpeed = true;
            double cps = speedTrack.Value / 10.0;
            decimal interval = Math.Max(20, (decimal)Math.Round(1000 / cps));
            intervalBox.Value = interval;
            speedValue.Text = string.Format("{0:N1} CPS", cps);
            if (!running)
            {
                actionTimer.Interval = (int)interval;
            }
            syncingSpeed = false;
        }

        private void SyncFromInterval()
        {
            if (syncingSpeed)
            {
                return;
            }

            syncingSpeed = true;
            double interval = (double)intervalBox.Value;
            double cps = 1000 / interval;
            int trackValue = Math.Min(500, Math.Max(5, (int)Math.Round(cps * 10)));
            speedTrack.Value = trackValue;
            speedValue.Text = string.Format("{0:N1} CPS", trackValue / 10.0);
            if (!running)
            {
                actionTimer.Interval = (int)interval;
            }
            syncingSpeed = false;
        }

        private void ApplyMovePreset()
        {
            if (syncingPointer || movePresetBox.SelectedIndex <= 0)
            {
                return;
            }

            syncingPointer = true;
            int step = 20;
            int x = 0;
            int y = 0;

            switch (movePresetBox.SelectedIndex)
            {
                case 1:
                    y = -step;
                    break;
                case 2:
                    y = step;
                    break;
                case 3:
                    x = -step;
                    break;
                case 4:
                    x = step;
                    break;
                case 5:
                    x = -step;
                    y = -step;
                    break;
                case 6:
                    x = step;
                    y = -step;
                    break;
                case 7:
                    x = -step;
                    y = step;
                    break;
                case 8:
                    x = step;
                    y = step;
                    break;
            }

            moveXBox.Value = x;
            moveYBox.Value = y;
            syncingPointer = false;
        }

        private void MarkCustomMove()
        {
            if (syncingPointer || movePresetBox.SelectedIndex == 0)
            {
                return;
            }

            syncingPointer = true;
            movePresetBox.SelectedIndex = 0;
            syncingPointer = false;
        }

        private void StartAction()
        {
            if (running || countingDown)
            {
                return;
            }

            if (!ValidateAction())
            {
                return;
            }

            countingDown = true;
            startAt = DateTime.Now.AddMilliseconds((double)delayBox.Value * 1000);
            SetStatus("ARMING", Theme.Warning);
            countdownTimer.Start();
        }

        private bool ValidateAction()
        {
            if (MacroRequired() && macroBox.Text.Length == 0)
            {
                SetStatus("ADD MACRO KEYS", Theme.Danger);
                return false;
            }

            if (MoveRequired() && moveXBox.Value == 0 && moveYBox.Value == 0)
            {
                SetStatus("SET POINTER MOVE", Theme.Danger);
                return false;
            }

            if (ScrollRequired() && verticalScrollBox.Value == 0 && horizontalScrollBox.Value == 0)
            {
                SetStatus("SET SCROLL AMOUNT", Theme.Danger);
                return false;
            }

            return true;
        }

        private void StopAction()
        {
            countingDown = false;
            running = false;
            actionBusy = false;
            countdownTimer.Stop();
            actionTimer.Stop();
            SetStatus("READY", Theme.Accent);
        }

        private void ToggleAction()
        {
            if (running || countingDown)
            {
                StopAction();
            }
            else
            {
                StartAction();
            }
        }

        private void CountdownTimerTick(object sender, EventArgs e)
        {
            double remaining = (startAt - DateTime.Now).TotalSeconds;
            if (remaining > 0)
            {
                SetStatus(string.Format("ARMING {0:N1}s", remaining), Theme.Warning);
                return;
            }

            countdownTimer.Stop();
            countingDown = false;
            running = true;
            actionTimer.Interval = (int)intervalBox.Value;
            SetStatus("RUNNING", Theme.Accent);
            RunActionCycle();
            actionTimer.Start();
        }

        private void RunActionCycle()
        {
            if (!running || actionBusy)
            {
                return;
            }

            actionBusy = true;
            try
            {
                int mode = actionModeBox.SelectedIndex;
                if (mode == 0 || mode == 4 || mode == 5)
                {
                    Native.Click(rightButton.Checked);
                }

                if (mode == 2 || mode == 5)
                {
                    Native.MoveRelative((int)moveXBox.Value, (int)moveYBox.Value);
                }

                if (mode == 3 || mode == 5)
                {
                    Native.ScrollVertical((int)verticalScrollBox.Value);
                    Native.ScrollHorizontal((int)horizontalScrollBox.Value);
                }

                if (mode == 1 || mode == 4 || mode == 5)
                {
                    SendMacro();
                }
            }
            catch
            {
                StopAction();
                SetStatus("ACTION FAILED", Theme.Danger);
            }
            finally
            {
                actionBusy = false;
            }
        }

        private bool MacroRequired()
        {
            int mode = actionModeBox.SelectedIndex;
            return mode == 1 || mode == 4 || mode == 5;
        }

        private bool MoveRequired()
        {
            int mode = actionModeBox.SelectedIndex;
            return mode == 2;
        }

        private bool ScrollRequired()
        {
            int mode = actionModeBox.SelectedIndex;
            return mode == 3;
        }

        private void SendMacro()
        {
            if (macroFormatBox.SelectedIndex == 1)
            {
                SendKeys.SendWait(macroBox.Text);
                return;
            }

            int pause = (int)keyPauseBox.Value;
            string macro = macroBox.Text;
            for (int i = 0; i < macro.Length; i++)
            {
                char ch = macro[i];
                if (ch == '\r')
                {
                    continue;
                }

                SendKeys.SendWait(ToSendKeysLiteral(ch));
                if (pause > 0 && i < macro.Length - 1)
                {
                    Thread.Sleep(pause);
                    Application.DoEvents();
                }
            }
        }

        private static string ToSendKeysLiteral(char ch)
        {
            switch (ch)
            {
                case '\n':
                    return "{ENTER}";
                case '\t':
                    return "{TAB}";
                case '+':
                case '^':
                case '%':
                case '~':
                case '(':
                case ')':
                case '[':
                case ']':
                    return "{" + ch + "}";
                case '{':
                    return "{{}";
                case '}':
                    return "{}}";
                default:
                    return ch.ToString();
            }
        }

        private void SetStatus(string text, Color color)
        {
            statusLabel.Text = text;
            statusDot.BackColor = color;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
