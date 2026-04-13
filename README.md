<p align="center">
  <img src="Assets/Icons/Philips E-SCenter Logo.png" width="90" alt="E-SCenter Logo"/>
</p>

<h1 align="center">E-SCenter</h1>

<p align="center">
  A dark-themed WPF desktop application for managing electronics service center operations — tickets, inventory, parts, and more.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows-blue?style=flat-square"/>
  <img src="https://img.shields.io/badge/framework-.NET%20WPF-5C2D91?style=flat-square"/>
  <img src="https://img.shields.io/badge/language-C%23-239120?style=flat-square"/>
  <img src="https://img.shields.io/badge/pattern-MVVM-00B4D8?style=flat-square"/>
</p>

---

## Overview

**E-SCenter** is an all-in-one service center management tool built for electronics repair shops. It gives technicians and managers a fast, keyboard-driven interface to track repair tickets from intake to pickup, manage parts inventory with stock-level alerts, and monitor operational health from a live dashboard — all in a polished dark UI that stays out of your way.

---

## Features

### 🗂 Repair Tickets
- Create, edit, and close repair tickets with full customer and device details
- Ticket priority levels — mark items as **Critical** or **Overdue** with instant visual feedback
- Status tracking: Open → In Progress → Ready for Pickup → Closed
- Quick-filter by status and keyword search across all fields

### 📊 Live Dashboard
- Real-time ticket activity chart (LiveCharts) with per-status toggle filters
- Summary counters for Total, Open, Closed, Critical, and Ready-for-Pickup tickets
- Alert indicators for overdue and expiring warranty items
- Animated chart with smooth entry transitions

### 📦 Inventory
- Full parts inventory with Type, Brand, Model, Variant, Compatibility, Specs, Size, Qty, Price, Condition, Quality Grade, Source, Box Location, and Tags
- Color-coded Qty badges: green (in stock) → yellow → orange → red (out of stock)
- Condition and quality pill badges for instant readability
- AI-powered search bar with type filter
- Add / Edit / Remove workflow

### 🔩 Parts Control
- Manage parts linked to service tickets
- Track which parts are consumed per repair

### 🪦 Boneyard
- Catalogue of salvaged or retired parts
- Separate tracking from live inventory

### 🛡 Warranty System
- Track warranties per ticket
- Badge counter on toolbar when warranties are expiring soon

### 🤖 Al-Baraka Integration
- Dedicated view for Al-Baraka branch operations

### ⚙️ System
- Global search (Ctrl+F) across tickets, inventory, and parts
- Full keyboard shortcut navigation (F1–F7 + Ctrl+N)
- Backup Now with last-backup timestamp
- Auto-startup toggle (Windows startup registry)
- App Logger for session diagnostics
- Database connection status indicator in status bar
- User profile with per-session authentication
- Admin access control with auto-grant toggle

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | WPF (.NET) |
| Architecture | MVVM |
| Charts | LiveCharts2 (SkiaSharp WPF) |
| Database | Local (SQLite / SQL Server) |
| Language | C# |
| Styling | Custom dark theme with neon accent palette |

---

## Project Structure

```
E-SCenter/
├── App.xaml / App.xaml.cs       # App entry point, resource dictionaries
├── MainWindow.xaml              # Shell: title bar, nav tabs, content host
├── Assets/                      # Icons and images
├── Converters/                  # Value converters (status → icon, key → brush, etc.)
├── Core/                        # Base classes, commands, helpers
├── Data/                        # Data access layer, database context
├── Models/                      # Domain models (Ticket, InventoryItem, Part, etc.)
├── Services/                    # Business logic and data services
├── Themes/
│   ├── Brushes.xaml             # Named brush resources
│   ├── Colors.xaml              # Color palette
│   ├── Controls.xaml            # All control styles (buttons, grids, inputs, etc.)
│   └── IconsTemplates.xaml      # Icon resource templates
├── ViewModels/                  # MVVM view models for each view
└── Views/                       # XAML views and UserControls
    ├── DashboardView.xaml
    ├── RepairTicketsView.xaml
    ├── InventoryView.xaml
    ├── PartsControlView.xaml
    ├── BoneyardView.xaml
    ├── AlBarakaView.xaml
    └── ...
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `F1` | Dashboard |
| `F2` | Repair Tickets |
| `F3` | Parts Control |
| `F4` | Inventory |
| `F5` | Boneyard |
| `F6` | Al-Baraka |
| `F7` | Warranty System |
| `Ctrl+N` | New Ticket |
| `Ctrl+F` | Global Search |

---

## Getting Started

### Prerequisites
- Windows 10 / 11
- .NET 8 (or the version targeted by the `.csproj`)
- Visual Studio 2022+

### Build & Run

```bash
git clone https://github.com/MhdSukar/E-SCenter.git
cd E-SCenter
git checkout E-SCenter
```

Open `E-SCenter.slnx` in Visual Studio and press **F5** to build and run.

> Make sure NuGet packages are restored automatically on first build (`LiveChartsCore.SkiaSharpView.WPF` and any other dependencies in the `.csproj`).

---

## License

This project is private. All rights reserved © MhdSukar.
