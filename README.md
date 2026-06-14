# Auto Clicker Studio

A small Windows auto clicker with adjustable click speed, global hotkeys, and
keyboard macros.

## Run on Windows

Double-click `AutoClicker.exe`.

If you want to run the PowerShell version instead:

```powershell
powershell -STA -ExecutionPolicy Bypass -File .\AutoClicker.ps1
```

No extra packages are required for either version.

The source for the `.exe` is in `AutoClicker.cs`. There is also a Python version in
`auto_clicker.py` if you prefer Python later.

## Controls

- Use the speed slider to set clicks per second.
- Edit the interval box if you prefer milliseconds per click.
- Choose left or right click.
- Pick an action mode: mouse click only, keyboard macro only, or click then macro.
- Use plain text macro mode for normal typing.
- Use SendKeys syntax for key combos and special keys, such as `{ENTER}`, `{TAB}`,
  `^c` for Ctrl+C, or `%{TAB}` for Alt+Tab.
- Set a start delay so you have time to move the cursor.
- Press `F6` to start or stop clicking from anywhere.
- Press `F7` to stop immediately from anywhere.

The clicker clicks wherever your mouse cursor is currently located. Keyboard
macros are sent to whichever window is focused when the action runs.
