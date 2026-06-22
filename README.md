# Auto Clicker Studio

A small Windows automation panel with adjustable click speed, global hotkeys,
keyboard macros, cursor movement, and vertical or horizontal scrolling.

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
- Pick an action mode: click only, keyboard macro, pointer move, scroll wheel,
  click + macro, or full sequence.
- Choose left or right click.
- Set relative cursor movement with X/Y pixel values or a direction preset.
- Set vertical scroll and side-scroll wheel notches.
- Use plain text macro mode for normal typing.
- Use SendKeys syntax for key combos and special keys, such as `{ENTER}`, `{TAB}`,
  `^c` for Ctrl+C, or `%{TAB}` for Alt+Tab.
- Set a start delay so you have time to move the cursor.
- Press `F6` to start or stop clicking from anywhere.
- Press `F7` to stop immediately from anywhere.

Mouse actions apply wherever your cursor is currently located. Keyboard macros
are sent to whichever window is focused when the action runs.
