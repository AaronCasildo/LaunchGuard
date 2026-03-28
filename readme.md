<h1>
  <img src="media/lock.png" width="21" style="vertical-align:up"/> LaunchGuard - App Locker for Windows
</h1> 

A free, open-source Windows utility that blocks any application behind a custom keyword prompt — showing a lock screen the moment the app is launched.

---

## Motivation
Every existing app locker for Windows that I could find is either paywalled ($20–$30+), poorly maintained, or both. They're simple background utilities — there's no justification for that price. LaunchGuard exists to fill that gap: a clean, free, and open alternative that anyone can use, inspect, and contribute.

---

## How It Works

LaunchGuard runs as a background Windows Service and monitors for the launch of any app you've marked as protected. The moment a target process is detected, it is immediately suspended and a keyword prompt overlays the screen. The app only continues if the correct keyword is entered.        
No modifications to the original executables.

---

## Tech Stack

- **Language:** C#
- **UI:** WPF (.NET)
- **Process monitoring:** WMI event subscriptions
- **Config:** Local JSON
- **Distribution:** Windows Service + installer

---

## License

To be determined — likely MIT for the core, with a potential Pro tier for advanced features down the line.

---

*Built because the alternatives are overpriced and mediocre.*