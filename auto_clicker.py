import ctypes
import queue
import threading
import time
import tkinter as tk
from tkinter import ttk


WM_HOTKEY = 0x0312
WM_QUIT = 0x0012
MOD_NOREPEAT = 0x4000
VK_F6 = 0x75
VK_F7 = 0x76

HOTKEY_TOGGLE_ID = 1
HOTKEY_STOP_ID = 2

INPUT_MOUSE = 0
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004
MOUSEEVENTF_RIGHTDOWN = 0x0008
MOUSEEVENTF_RIGHTUP = 0x0010

user32 = ctypes.windll.user32


class MOUSEINPUT(ctypes.Structure):
    _fields_ = [
        ("dx", ctypes.c_long),
        ("dy", ctypes.c_long),
        ("mouseData", ctypes.c_ulong),
        ("dwFlags", ctypes.c_ulong),
        ("time", ctypes.c_ulong),
        ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong)),
    ]


class INPUT_UNION(ctypes.Union):
    _fields_ = [("mi", MOUSEINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_ulong), ("union", INPUT_UNION)]


class POINT(ctypes.Structure):
    _fields_ = [("x", ctypes.c_long), ("y", ctypes.c_long)]


class MSG(ctypes.Structure):
    _fields_ = [
        ("hwnd", ctypes.c_void_p),
        ("message", ctypes.c_uint),
        ("wParam", ctypes.c_size_t),
        ("lParam", ctypes.c_size_t),
        ("time", ctypes.c_ulong),
        ("pt", POINT),
    ]


def send_mouse_click(button):
    if button == "Right":
        down_flag = MOUSEEVENTF_RIGHTDOWN
        up_flag = MOUSEEVENTF_RIGHTUP
    else:
        down_flag = MOUSEEVENTF_LEFTDOWN
        up_flag = MOUSEEVENTF_LEFTUP

    inputs = (INPUT * 2)()
    inputs[0].type = INPUT_MOUSE
    inputs[0].union.mi = MOUSEINPUT(0, 0, 0, down_flag, 0, None)
    inputs[1].type = INPUT_MOUSE
    inputs[1].union.mi = MOUSEINPUT(0, 0, 0, up_flag, 0, None)
    user32.SendInput(2, ctypes.byref(inputs), ctypes.sizeof(INPUT))


class GlobalHotkeys:
    def __init__(self, events):
        self.events = events
        self.thread = None
        self.thread_id = None
        self.ready = threading.Event()

    def start(self):
        self.thread = threading.Thread(target=self._run, daemon=True)
        self.thread.start()
        self.ready.wait(timeout=1)

    def stop(self):
        if self.thread_id:
            user32.PostThreadMessageW(self.thread_id, WM_QUIT, 0, 0)
        if self.thread:
            self.thread.join(timeout=1)

    def _run(self):
        self.thread_id = ctypes.windll.kernel32.GetCurrentThreadId()
        toggle_registered = user32.RegisterHotKey(
            None, HOTKEY_TOGGLE_ID, MOD_NOREPEAT, VK_F6
        )
        stop_registered = user32.RegisterHotKey(None, HOTKEY_STOP_ID, MOD_NOREPEAT, VK_F7)
        self.ready.set()

        msg = MSG()
        try:
            while user32.GetMessageW(ctypes.byref(msg), None, 0, 0) != 0:
                if msg.message == WM_HOTKEY:
                    if msg.wParam == HOTKEY_TOGGLE_ID:
                        self.events.put("toggle")
                    elif msg.wParam == HOTKEY_STOP_ID:
                        self.events.put("stop")
        finally:
            if toggle_registered:
                user32.UnregisterHotKey(None, HOTKEY_TOGGLE_ID)
            if stop_registered:
                user32.UnregisterHotKey(None, HOTKEY_STOP_ID)


class AutoClickerApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Auto Clicker")
        self.resizable(False, False)

        try:
            ctypes.windll.shcore.SetProcessDpiAwareness(1)
        except Exception:
            pass

        self.events = queue.Queue()
        self.hotkeys = GlobalHotkeys(self.events)

        self.running = threading.Event()
        self.cancel_countdown = threading.Event()
        self.click_thread = None
        self.countdown_thread = None

        self.cps_var = tk.DoubleVar(value=10.0)
        self.interval_var = tk.StringVar(value="100")
        self.button_var = tk.StringVar(value="Left")
        self.delay_var = tk.DoubleVar(value=2.0)
        self.status_var = tk.StringVar(value="Ready")

        self._build_ui()
        self._wire_events()
        self.hotkeys.start()
        self.after(50, self._process_events)

    def _build_ui(self):
        self.columnconfigure(0, weight=1)
        frame = ttk.Frame(self, padding=16)
        frame.grid(row=0, column=0, sticky="nsew")
        frame.columnconfigure(1, weight=1)

        title = ttk.Label(frame, text="Auto Clicker", font=("Segoe UI", 16, "bold"))
        title.grid(row=0, column=0, columnspan=3, sticky="w")

        hotkeys = ttk.Label(
            frame,
            text="F6 toggles clicking. F7 stops immediately.",
            foreground="#555555",
        )
        hotkeys.grid(row=1, column=0, columnspan=3, sticky="w", pady=(2, 14))

        ttk.Label(frame, text="Speed").grid(row=2, column=0, sticky="w", pady=6)
        speed = ttk.Scale(
            frame,
            from_=0.5,
            to=50,
            variable=self.cps_var,
            orient="horizontal",
            command=self._sync_interval_from_cps,
        )
        speed.grid(row=2, column=1, sticky="ew", padx=(12, 8))
        self.cps_label = ttk.Label(frame, text="10.0 CPS", width=10)
        self.cps_label.grid(row=2, column=2, sticky="e")

        ttk.Label(frame, text="Interval").grid(row=3, column=0, sticky="w", pady=6)
        interval = ttk.Entry(frame, textvariable=self.interval_var, width=10)
        interval.grid(row=3, column=1, sticky="w", padx=(12, 8))
        ttk.Label(frame, text="milliseconds per click").grid(row=3, column=2, sticky="w")

        ttk.Label(frame, text="Button").grid(row=4, column=0, sticky="w", pady=6)
        buttons = ttk.Frame(frame)
        buttons.grid(row=4, column=1, columnspan=2, sticky="w", padx=(12, 0))
        ttk.Radiobutton(
            buttons, text="Left", value="Left", variable=self.button_var
        ).grid(row=0, column=0, sticky="w")
        ttk.Radiobutton(
            buttons, text="Right", value="Right", variable=self.button_var
        ).grid(row=0, column=1, sticky="w", padx=(14, 0))

        ttk.Label(frame, text="Start delay").grid(row=5, column=0, sticky="w", pady=6)
        delay = ttk.Spinbox(
            frame,
            from_=0,
            to=10,
            increment=0.5,
            textvariable=self.delay_var,
            width=8,
            format="%.1f",
        )
        delay.grid(row=5, column=1, sticky="w", padx=(12, 8))
        ttk.Label(frame, text="seconds").grid(row=5, column=2, sticky="w")

        actions = ttk.Frame(frame)
        actions.grid(row=6, column=0, columnspan=3, sticky="ew", pady=(16, 8))
        actions.columnconfigure((0, 1), weight=1)

        self.start_button = ttk.Button(actions, text="Start", command=self.start_clicking)
        self.start_button.grid(row=0, column=0, sticky="ew", padx=(0, 6))
        self.stop_button = ttk.Button(actions, text="Stop", command=self.stop_clicking)
        self.stop_button.grid(row=0, column=1, sticky="ew", padx=(6, 0))

        status = ttk.Label(frame, textvariable=self.status_var, font=("Segoe UI", 10, "bold"))
        status.grid(row=7, column=0, columnspan=3, sticky="w", pady=(6, 0))

    def _wire_events(self):
        self.interval_var.trace_add("write", self._sync_cps_from_interval)
        self.protocol("WM_DELETE_WINDOW", self._on_close)

    def _sync_interval_from_cps(self, _value=None):
        cps = max(0.1, float(self.cps_var.get()))
        interval = round(1000 / cps)
        self.interval_var.set(str(interval))
        self.cps_label.config(text=f"{cps:.1f} CPS")

    def _sync_cps_from_interval(self, *_args):
        try:
            interval = float(self.interval_var.get())
        except ValueError:
            return

        if interval <= 0:
            return

        cps = min(50, max(0.5, 1000 / interval))
        if abs(self.cps_var.get() - cps) > 0.05:
            self.cps_var.set(cps)
        self.cps_label.config(text=f"{cps:.1f} CPS")

    def start_clicking(self):
        if self.running.is_set() or (self.countdown_thread and self.countdown_thread.is_alive()):
            return

        self.cancel_countdown.clear()
        self.countdown_thread = threading.Thread(target=self._countdown_then_start, daemon=True)
        self.countdown_thread.start()

    def stop_clicking(self):
        self.cancel_countdown.set()
        self.running.clear()
        self.after(0, self._set_idle_status)

    def toggle_clicking(self):
        if self.running.is_set() or (self.countdown_thread and self.countdown_thread.is_alive()):
            self.stop_clicking()
        else:
            self.start_clicking()

    def _countdown_then_start(self):
        delay = max(0, float(self.delay_var.get()))
        end_time = time.monotonic() + delay

        while True:
            remaining = end_time - time.monotonic()
            if self.cancel_countdown.is_set():
                return
            if remaining <= 0:
                break
            self.after(
                0,
                lambda value=remaining: self.status_var.set(
                    f"Starting in {value:.1f}s. Move your cursor to the target."
                ),
            )
            time.sleep(0.05)

        self.running.set()
        self.click_thread = threading.Thread(target=self._click_loop, daemon=True)
        self.click_thread.start()
        self.after(0, lambda: self.status_var.set("Clicking. Press F7 to stop."))

    def _click_loop(self):
        next_click = time.perf_counter()
        while self.running.is_set():
            try:
                interval_ms = float(self.interval_var.get())
            except ValueError:
                interval_ms = 100

            interval = max(0.02, interval_ms / 1000)
            send_mouse_click(self.button_var.get())

            next_click += interval
            sleep_for = next_click - time.perf_counter()
            if sleep_for < 0:
                next_click = time.perf_counter()
                sleep_for = interval
            self.running.wait(timeout=sleep_for)

    def _set_idle_status(self):
        self.status_var.set("Ready")

    def _process_events(self):
        while True:
            try:
                event = self.events.get_nowait()
            except queue.Empty:
                break

            if event == "toggle":
                self.toggle_clicking()
            elif event == "stop":
                self.stop_clicking()

        self.after(50, self._process_events)

    def _on_close(self):
        self.stop_clicking()
        self.hotkeys.stop()
        self.destroy()


if __name__ == "__main__":
    app = AutoClickerApp()
    app.mainloop()
