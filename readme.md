<h1>
  <img src="media/lock.png" width="21" style="vertical-align:up"/> LaunchGuard - App Locker for Windows
</h1> 

A free, open-source Windows utility that blocks selected applications behind custom authentication.

![LaunchGuard main UI screenshot](media/main.png)

---

## Motivation
Every existing app locker for Windows that I could find is either paywalled ($20–$30+), poorly maintained, or both. They're simple background utilities — there's no justification for that price. LaunchGuard exists to fill that gap: a clean, free, and open alternative that anyone can use, inspect, and contribute.

---

## Development Direction

LaunchGuard is moving to a **split architecture**:

- **Windows Service (core enforcement):** always-on process monitoring, policy enforcement, and tamper resistance.
- **WinForms UI (control panel):** user-facing settings, protected app management, and status/diagnostics.

This direction is intended to make LaunchGuard harder to disable while keeping configuration simple for everyday use.

## How It Works (Target Architecture)

1. The service monitors process creation events for protected apps.
2. When a protected app is launched, the service enforces the configured policy.
3. The WinForms UI communicates with the service to update rules and display current protection state.
4. Authentication is required for sensitive actions such as disabling protection or modifying protected app rules.

No modifications are made to the original executables.

---

## Tech Stack

- **Language:** C#
- **UI:** WinForms (.NET)
- **Core runtime:** Windows Service
- **Process monitoring:** WMI event subscriptions (current), with room for service-level hardening
- **Config:** Local JSON (evolving toward service-managed configuration)
- **Distribution:** Service + desktop UI + installer

---

## Current Status

This repository is actively transitioning toward the service + WinForms model. During this phase, some implementation details may differ from the final architecture while the service boundary and tamper-resistance model are being completed.

---

*Built to offer a simpler, more accessible alternative.*