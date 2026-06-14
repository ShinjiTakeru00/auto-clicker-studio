using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoClicker
{
    internal static class Native
    {
        public const int MOD_NOREPEAT = 0x4000;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

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

            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dwFlags = down;
            inputs[1].type = INPUT_MOUSE;
            inputs[1].mi.dwFlags = up;

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            BackColor = Color.White;
            Padding = new Padding(18);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(18, 22, 27, 34)))
            using (SolidBrush fill = new SolidBrush(BackColor))
            using (Pen border = new Pen(Color.FromArgb(226, 232, 240)))
            {
                Rectangle shadowRect = new Rectangle(3, 4, Width - 7, Height - 7);
                Rectangle rect = new Rectangle(0, 0, Width - 7, Height - 7);
                using (GraphicsPath shadowPath = RoundedRect(shadowRect, 12))
                using (GraphicsPath path = RoundedRect(rect, 12))
                {
                    e.Graphics.FillPath(shadow, shadowPath);
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
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
        private readonly RadioButton leftButton = new RadioButton();
        private readonly RadioButton rightButton = new RadioButton();
        private readonly ComboBox actionModeBox = new ComboBox();
        private readonly ComboBox macroFormatBox = new ComboBox();
        private readonly TextBox macroBox = new TextBox();
        private readonly NumericUpDown keyPauseBox = new NumericUpDown();
        private readonly AccentButton startButton = new AccentButton(Color.FromArgb(20, 184, 166), Color.FromArgb(13, 148, 136));
        private readonly AccentButton stopButton = new AccentButton(Color.FromArgb(244, 63, 94), Color.FromArgb(225, 29, 72));
        private readonly Label statusLabel = new Label();
        private readonly Panel statusDot = new Panel();
        private readonly System.Windows.Forms.Timer actionTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer countdownTimer = new System.Windows.Forms.Timer();

        private bool running;
        private bool countingDown;
        private bool syncing;
        private bool actionBusy;
        private DateTime startAt = DateTime.MinValue;

        public MainForm()
        {
            Text = "Auto Clicker Studio";
            ClientSize = new Size(760, 520);
            MinimumSize = new Size(760, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(246, 248, 251);
            Font = new Font("Segoe UI", 9F);

            BuildUi();
            WireEvents();
            SyncFromSpeed();
            SetStatus("Ready", Color.FromArgb(20, 184, 166));
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
            Panel header = new Panel
            {
                BackColor = Color.FromArgb(30, 41, 59),
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 96)
            };
            Controls.Add(header);

            Label title = new Label
            {
                Text = "Auto Clicker Studio",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 18)
            };
            header.Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "Mouse clicks and keyboard macros with adjustable repeat speed",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Location = new Point(27, 57)
            };
            header.Controls.Add(subtitle);

            Label hotkeys = new Label
            {
                Text = "F6 toggle  |  F7 stop",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(153, 246, 228),
                AutoSize = true,
                Location = new Point(612, 38)
            };
            header.Controls.Add(hotkeys);

            CardPanel clickCard = new CardPanel
            {
                Location = new Point(20, 116),
                Size = new Size(350, 302)
            };
            Controls.Add(clickCard);

            AddSectionTitle(clickCard, "Click Settings", 18, 16);
            AddMutedLabel(clickCard, "Repeat rate", 20, 58);

            speedTrack.Minimum = 5;
            speedTrack.Maximum = 500;
            speedTrack.TickFrequency = 50;
            speedTrack.Value = 100;
            speedTrack.Location = new Point(18, 78);
            speedTrack.Size = new Size(196, 45);
            clickCard.Controls.Add(speedTrack);

            speedValue.Text = "10.0 CPS";
            speedValue.BackColor = Color.FromArgb(236, 253, 245);
            speedValue.ForeColor = Color.FromArgb(15, 118, 110);
            speedValue.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            speedValue.TextAlign = ContentAlignment.MiddleCenter;
            speedValue.Location = new Point(226, 82);
            speedValue.Size = new Size(98, 28);
            clickCard.Controls.Add(speedValue);

            AddMutedLabel(clickCard, "Interval", 20, 130);
            intervalBox.Minimum = 20;
            intervalBox.Maximum = 5000;
            intervalBox.Value = 100;
            intervalBox.Increment = 10;
            intervalBox.Location = new Point(20, 152);
            intervalBox.Size = new Size(130, 23);
            clickCard.Controls.Add(intervalBox);
            AddMutedLabel(clickCard, "ms per action", 20, 181);

            AddMutedLabel(clickCard, "Start delay", 188, 130);
            delayBox.Minimum = 0;
            delayBox.Maximum = 30;
            delayBox.DecimalPlaces = 1;
            delayBox.Increment = 0.5M;
            delayBox.Value = 2;
            delayBox.Location = new Point(190, 152);
            delayBox.Size = new Size(130, 23);
            clickCard.Controls.Add(delayBox);
            AddMutedLabel(clickCard, "seconds before start", 190, 181);

            AddMutedLabel(clickCard, "Mouse button", 20, 226);
            leftButton.Text = "Left";
            leftButton.Checked = true;
            leftButton.AutoSize = true;
            leftButton.Location = new Point(22, 250);
            clickCard.Controls.Add(leftButton);

            rightButton.Text = "Right";
            rightButton.AutoSize = true;
            rightButton.Location = new Point(90, 250);
            clickCard.Controls.Add(rightButton);

            CardPanel macroCard = new CardPanel
            {
                Location = new Point(390, 116),
                Size = new Size(350, 302)
            };
            Controls.Add(macroCard);

            AddSectionTitle(macroCard, "Keyboard Macro", 18, 16);
            AddMutedLabel(macroCard, "Action mode", 20, 58);

            actionModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionModeBox.Items.AddRange(new object[] { "Mouse click only", "Keyboard macro only", "Click then macro" });
            actionModeBox.SelectedIndex = 0;
            actionModeBox.Location = new Point(20, 80);
            actionModeBox.Size = new Size(150, 23);
            macroCard.Controls.Add(actionModeBox);

            AddMutedLabel(macroCard, "Macro format", 190, 58);
            macroFormatBox.DropDownStyle = ComboBoxStyle.DropDownList;
            macroFormatBox.Items.AddRange(new object[] { "Plain text", "SendKeys syntax" });
            macroFormatBox.SelectedIndex = 0;
            macroFormatBox.Location = new Point(190, 80);
            macroFormatBox.Size = new Size(130, 23);
            macroCard.Controls.Add(macroFormatBox);

            AddMutedLabel(macroCard, "Keys to send", 20, 120);
            macroBox.Multiline = true;
            macroBox.ScrollBars = ScrollBars.Vertical;
            macroBox.AcceptsReturn = true;
            macroBox.AcceptsTab = true;
            macroBox.Text = "hello";
            macroBox.Location = new Point(20, 142);
            macroBox.Size = new Size(300, 72);
            macroCard.Controls.Add(macroBox);

            AddMutedLabel(macroCard, "Character pause", 20, 232);
            keyPauseBox.Minimum = 0;
            keyPauseBox.Maximum = 1000;
            keyPauseBox.Increment = 5;
            keyPauseBox.Value = 0;
            keyPauseBox.Location = new Point(20, 254);
            keyPauseBox.Size = new Size(84, 23);
            macroCard.Controls.Add(keyPauseBox);
            AddMutedLabel(macroCard, "milliseconds", 114, 257);

            Panel footer = new Panel
            {
                BackColor = Color.FromArgb(246, 248, 251),
                Location = new Point(20, 438),
                Size = new Size(720, 58)
            };
            Controls.Add(footer);

            startButton.Text = "Start";
            startButton.Location = new Point(0, 10);
            startButton.Size = new Size(150, 38);
            footer.Controls.Add(startButton);

            stopButton.Text = "Stop";
            stopButton.Location = new Point(164, 10);
            stopButton.Size = new Size(150, 38);
            footer.Controls.Add(stopButton);

            statusDot.BackColor = Color.FromArgb(20, 184, 166);
            statusDot.Location = new Point(500, 24);
            statusDot.Size = new Size(10, 10);
            footer.Controls.Add(statusDot);

            statusLabel.Text = "Ready";
            statusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            statusLabel.ForeColor = Color.FromArgb(51, 65, 85);
            statusLabel.AutoSize = false;
            statusLabel.Location = new Point(518, 17);
            statusLabel.Size = new Size(196, 24);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            footer.Controls.Add(statusLabel);
        }

        private static void AddSectionTitle(Control parent, string text, int x, int y)
        {
            Label label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
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
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            };
            parent.Controls.Add(label);
        }

        private void WireEvents()
        {
            speedTrack.ValueChanged += delegate { SyncFromSpeed(); };
            intervalBox.ValueChanged += delegate { SyncFromInterval(); };
            startButton.Click += delegate { StartAction(); };
            stopButton.Click += delegate { StopAction(); };

            countdownTimer.Interval = 50;
            countdownTimer.Tick += CountdownTimerTick;

            actionTimer.Tick += delegate { RunActionCycle(); };
        }

        private void SyncFromSpeed()
        {
            if (syncing)
            {
                return;
            }

            syncing = true;
            double cps = speedTrack.Value / 10.0;
            decimal interval = Math.Max(20, (decimal)Math.Round(1000 / cps));
            intervalBox.Value = interval;
            speedValue.Text = string.Format("{0:N1} CPS", cps);
            if (!running)
            {
                actionTimer.Interval = (int)interval;
            }
            syncing = false;
        }

        private void SyncFromInterval()
        {
            if (syncing)
            {
                return;
            }

            syncing = true;
            double interval = (double)intervalBox.Value;
            double cps = 1000 / interval;
            int trackValue = Math.Min(500, Math.Max(5, (int)Math.Round(cps * 10)));
            speedTrack.Value = trackValue;
            speedValue.Text = string.Format("{0:N1} CPS", trackValue / 10.0);
            if (!running)
            {
                actionTimer.Interval = (int)interval;
            }
            syncing = false;
        }

        private void StartAction()
        {
            if (running || countingDown)
            {
                return;
            }

            if (MacroRequired() && macroBox.Text.Length == 0)
            {
                SetStatus("Add macro keys first", Color.FromArgb(244, 63, 94));
                return;
            }

            countingDown = true;
            startAt = DateTime.Now.AddMilliseconds((double)delayBox.Value * 1000);
            SetStatus("Starting soon", Color.FromArgb(245, 158, 11));
            countdownTimer.Start();
        }

        private void StopAction()
        {
            countingDown = false;
            running = false;
            actionBusy = false;
            countdownTimer.Stop();
            actionTimer.Stop();
            SetStatus("Ready", Color.FromArgb(20, 184, 166));
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
                SetStatus(string.Format("Starting in {0:N1}s", remaining), Color.FromArgb(245, 158, 11));
                return;
            }

            countdownTimer.Stop();
            countingDown = false;
            running = true;
            actionTimer.Interval = (int)intervalBox.Value;
            SetStatus("Running", Color.FromArgb(20, 184, 166));
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
                if (mode == 0 || mode == 2)
                {
                    Native.Click(rightButton.Checked);
                }

                if (mode == 1 || mode == 2)
                {
                    SendMacro();
                }
            }
            catch
            {
                StopAction();
                SetStatus("Macro failed", Color.FromArgb(244, 63, 94));
            }
            finally
            {
                actionBusy = false;
            }
        }

        private bool MacroRequired()
        {
            return actionModeBox.SelectedIndex == 1 || actionModeBox.SelectedIndex == 2;
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
