Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne "STA") {
    $arguments = @(
        "-NoProfile",
        "-STA",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        "`"$PSCommandPath`""
    )
    Start-Process -FilePath "powershell.exe" -ArgumentList $arguments
    exit
}

Add-Type -ReferencedAssemblies "System.Windows.Forms", "System.Drawing" -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClicker {
    public class HotkeyForm : Form {
        public event Action<int> HotkeyPressed;
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0312 && HotkeyPressed != null) {
                HotkeyPressed((int)m.WParam);
            }
            base.WndProc(ref m);
        }
    }

    public static class Native {
        public const int MOD_NOREPEAT = 0x4000;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT {
            public int type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT {
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

        public static void Click(bool rightClick) {
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
}
"@

[System.Windows.Forms.Application]::EnableVisualStyles()

$form = New-Object AutoClicker.HotkeyForm
$form.Text = "Auto Clicker"
$form.ClientSize = New-Object System.Drawing.Size(430, 285)
$form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
$form.MaximizeBox = $false
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen

$fontTitle = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$fontRegular = New-Object System.Drawing.Font("Segoe UI", 9)
$fontStatus = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)

$title = New-Object System.Windows.Forms.Label
$title.Text = "Auto Clicker"
$title.Font = $fontTitle
$title.AutoSize = $true
$title.Location = New-Object System.Drawing.Point(16, 14)
$form.Controls.Add($title)

$hotkeyLabel = New-Object System.Windows.Forms.Label
$hotkeyLabel.Text = "F6 toggles clicking. F7 stops immediately."
$hotkeyLabel.Font = $fontRegular
$hotkeyLabel.ForeColor = [System.Drawing.Color]::FromArgb(85, 85, 85)
$hotkeyLabel.AutoSize = $true
$hotkeyLabel.Location = New-Object System.Drawing.Point(18, 48)
$form.Controls.Add($hotkeyLabel)

$speedLabel = New-Object System.Windows.Forms.Label
$speedLabel.Text = "Speed"
$speedLabel.Font = $fontRegular
$speedLabel.AutoSize = $true
$speedLabel.Location = New-Object System.Drawing.Point(18, 86)
$form.Controls.Add($speedLabel)

$speedTrack = New-Object System.Windows.Forms.TrackBar
$speedTrack.Minimum = 5
$speedTrack.Maximum = 500
$speedTrack.TickFrequency = 50
$speedTrack.Value = 100
$speedTrack.Location = New-Object System.Drawing.Point(94, 78)
$speedTrack.Size = New-Object System.Drawing.Size(220, 45)
$form.Controls.Add($speedTrack)

$speedValue = New-Object System.Windows.Forms.Label
$speedValue.Text = "10.0 CPS"
$speedValue.Font = $fontRegular
$speedValue.AutoSize = $false
$speedValue.TextAlign = [System.Drawing.ContentAlignment]::MiddleRight
$speedValue.Location = New-Object System.Drawing.Point(322, 86)
$speedValue.Size = New-Object System.Drawing.Size(84, 20)
$form.Controls.Add($speedValue)

$intervalLabel = New-Object System.Windows.Forms.Label
$intervalLabel.Text = "Interval"
$intervalLabel.Font = $fontRegular
$intervalLabel.AutoSize = $true
$intervalLabel.Location = New-Object System.Drawing.Point(18, 132)
$form.Controls.Add($intervalLabel)

$intervalBox = New-Object System.Windows.Forms.NumericUpDown
$intervalBox.Minimum = 20
$intervalBox.Maximum = 2000
$intervalBox.Value = 100
$intervalBox.Increment = 10
$intervalBox.Location = New-Object System.Drawing.Point(106, 128)
$intervalBox.Size = New-Object System.Drawing.Size(82, 23)
$form.Controls.Add($intervalBox)

$intervalUnit = New-Object System.Windows.Forms.Label
$intervalUnit.Text = "milliseconds per click"
$intervalUnit.Font = $fontRegular
$intervalUnit.AutoSize = $true
$intervalUnit.Location = New-Object System.Drawing.Point(198, 132)
$form.Controls.Add($intervalUnit)

$buttonLabel = New-Object System.Windows.Forms.Label
$buttonLabel.Text = "Button"
$buttonLabel.Font = $fontRegular
$buttonLabel.AutoSize = $true
$buttonLabel.Location = New-Object System.Drawing.Point(18, 170)
$form.Controls.Add($buttonLabel)

$leftButton = New-Object System.Windows.Forms.RadioButton
$leftButton.Text = "Left"
$leftButton.Font = $fontRegular
$leftButton.Checked = $true
$leftButton.AutoSize = $true
$leftButton.Location = New-Object System.Drawing.Point(106, 168)
$form.Controls.Add($leftButton)

$rightButton = New-Object System.Windows.Forms.RadioButton
$rightButton.Text = "Right"
$rightButton.Font = $fontRegular
$rightButton.AutoSize = $true
$rightButton.Location = New-Object System.Drawing.Point(168, 168)
$form.Controls.Add($rightButton)

$delayLabel = New-Object System.Windows.Forms.Label
$delayLabel.Text = "Start delay"
$delayLabel.Font = $fontRegular
$delayLabel.AutoSize = $true
$delayLabel.Location = New-Object System.Drawing.Point(18, 206)
$form.Controls.Add($delayLabel)

$delayBox = New-Object System.Windows.Forms.NumericUpDown
$delayBox.Minimum = 0
$delayBox.Maximum = 10
$delayBox.DecimalPlaces = 1
$delayBox.Increment = 0.5
$delayBox.Value = 2
$delayBox.Location = New-Object System.Drawing.Point(106, 202)
$delayBox.Size = New-Object System.Drawing.Size(82, 23)
$form.Controls.Add($delayBox)

$delayUnit = New-Object System.Windows.Forms.Label
$delayUnit.Text = "seconds"
$delayUnit.Font = $fontRegular
$delayUnit.AutoSize = $true
$delayUnit.Location = New-Object System.Drawing.Point(198, 206)
$form.Controls.Add($delayUnit)

$startButton = New-Object System.Windows.Forms.Button
$startButton.Text = "Start"
$startButton.Location = New-Object System.Drawing.Point(20, 240)
$startButton.Size = New-Object System.Drawing.Size(120, 30)
$form.Controls.Add($startButton)

$stopButton = New-Object System.Windows.Forms.Button
$stopButton.Text = "Stop"
$stopButton.Location = New-Object System.Drawing.Point(150, 240)
$stopButton.Size = New-Object System.Drawing.Size(120, 30)
$form.Controls.Add($stopButton)

$statusLabel = New-Object System.Windows.Forms.Label
$statusLabel.Text = "Ready"
$statusLabel.Font = $fontStatus
$statusLabel.AutoSize = $false
$statusLabel.Location = New-Object System.Drawing.Point(286, 246)
$statusLabel.Size = New-Object System.Drawing.Size(124, 20)
$statusLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleLeft
$form.Controls.Add($statusLabel)

$clickTimer = New-Object System.Windows.Forms.Timer
$clickTimer.Interval = [int]$intervalBox.Value
$countdownTimer = New-Object System.Windows.Forms.Timer
$countdownTimer.Interval = 50

$script:running = $false
$script:countingDown = $false
$script:startAt = [DateTime]::MinValue
$script:syncing = $false

function Sync-FromSpeed {
    if ($script:syncing) { return }
    $script:syncing = $true
    $cps = $speedTrack.Value / 10.0
    $interval = [Math]::Max(20, [Math]::Round(1000 / $cps))
    $intervalBox.Value = [decimal]$interval
    $speedValue.Text = "{0:N1} CPS" -f $cps
    if (-not $script:running) {
        $clickTimer.Interval = [int]$interval
    }
    $script:syncing = $false
}

function Sync-FromInterval {
    if ($script:syncing) { return }
    $script:syncing = $true
    $interval = [double]$intervalBox.Value
    $cps = 1000 / $interval
    $trackValue = [Math]::Min(500, [Math]::Max(5, [Math]::Round($cps * 10)))
    $speedTrack.Value = [int]$trackValue
    $speedValue.Text = "{0:N1} CPS" -f ($trackValue / 10.0)
    if (-not $script:running) {
        $clickTimer.Interval = [int]$interval
    }
    $script:syncing = $false
}

function Stop-Clicking {
    $script:countingDown = $false
    $script:running = $false
    $countdownTimer.Stop()
    $clickTimer.Stop()
    $statusLabel.Text = "Ready"
}

function Start-Clicking {
    if ($script:running -or $script:countingDown) { return }

    $script:countingDown = $true
    $script:startAt = [DateTime]::Now.AddMilliseconds([double]$delayBox.Value * 1000)
    $countdownTimer.Start()
}

function Toggle-Clicking {
    if ($script:running -or $script:countingDown) {
        Stop-Clicking
    } else {
        Start-Clicking
    }
}

$speedTrack.Add_ValueChanged({ Sync-FromSpeed })
$intervalBox.Add_ValueChanged({ Sync-FromInterval })
$startButton.Add_Click({ Start-Clicking })
$stopButton.Add_Click({ Stop-Clicking })

$countdownTimer.Add_Tick({
    $remaining = ($script:startAt - [DateTime]::Now).TotalSeconds
    if ($remaining -gt 0) {
        $statusLabel.Text = "Starting in {0:N1}s" -f $remaining
        return
    }

    $countdownTimer.Stop()
    $script:countingDown = $false
    $script:running = $true
    $clickTimer.Interval = [int]$intervalBox.Value
    $statusLabel.Text = "Clicking"
    $clickTimer.Start()
})

$clickTimer.Add_Tick({
    [AutoClicker.Native]::Click($rightButton.Checked)
})

$form.Add_Shown({
    [AutoClicker.Native]::RegisterHotKey($form.Handle, 1, [AutoClicker.Native]::MOD_NOREPEAT, [AutoClicker.Native]::VK_F6) | Out-Null
    [AutoClicker.Native]::RegisterHotKey($form.Handle, 2, [AutoClicker.Native]::MOD_NOREPEAT, [AutoClicker.Native]::VK_F7) | Out-Null
})

$form.Add_HotkeyPressed({
    param($id)
    if ($id -eq 1) {
        Toggle-Clicking
    } elseif ($id -eq 2) {
        Stop-Clicking
    }
})

$form.Add_FormClosing({
    Stop-Clicking
    [AutoClicker.Native]::UnregisterHotKey($form.Handle, 1) | Out-Null
    [AutoClicker.Native]::UnregisterHotKey($form.Handle, 2) | Out-Null
})

Sync-FromSpeed
[System.Windows.Forms.Application]::Run($form)
