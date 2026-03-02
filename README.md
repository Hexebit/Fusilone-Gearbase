<h1 align="center">Fusilone</h1>

<p align="center">
  <b>Primarily for personal use, but also a modern, portable, and intelligent inventory/maintenance management system for technical services departments.
</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Sürüm-v0.9.3-blue?style=flat-square" alt="Version">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/Platform-Windows_WPF-lightgrey?style=flat-square&logo=windows" alt="WPF">
  <img src="https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square&logo=sqlite" alt="SQLite">
</p>

---

## About Fusilone

**Fusilone**, It is a local (offline) desktop automation system that enables device registration, maintenance/failure history tracking, and the generation of QR code smart tags. 

Unlike complex cloud-based systems, it **requires no installation** (Single-File EXE). It securely stores the entire database on the user's own computer. It is designed for small/medium-sized technical services, school laboratories, and those who want to professionally manage their personal equipment inventory.

## Key Features:

*  **Smart Tags and QR Codes:** It produces device-specific, compact barcodes with a resolution of 1050x600 that also display the last operation performed.
*  **Dark/Light Theme** Adhering to Material Design principles, it offers dynamic theme transitions that are easy on the eyes.
*  **Comprehensive Device Support:** All-In-One, PC, Laptop, Tablet, Gaming Console, Monitor, Mobile Phone, SmartBoard, Printer, Router, Projection
*  **Portable:** No SQL Server installation is required. The database (in `.db` format) is stored in the `My Documents\Fusilone` folder and can be easily backed up.
*  **Multilanguage:** It has the infrastructure for Turkish and English (localization) support.

## 🛠️ Technologies Used

* **Language & Framework:** C#, .NET 10.0 (LTS)
* **UI:** WPF (Windows Presentation Foundation)
* **Design Language:** MaterialDesignThemes In XAML
* **Database:** System.Data.SQLite
* **Libraries:** QRCoder (Etiketleme), LiveCharts.Wpf (Grafikler)
* **Agents:** Gemini CLI, CoPilot Pro

##  Installations and Usage

The application does not require .NET or SQL to be installed.
1. Download the registry file (`Fusilone.exe`) from the **Releases** section in the right-click context menu.
2. Double-click the downloaded file. (The application will automatically create its own database on the first launch).

---

## Developer and Process

This Project developed by Hexebit. 

The project's core architecture, XAML designs, and database schema were built from scratch using **Gemini 3 Pro** and **GitHub Copilot** with the "AI Pair-Programming" method.

**Licence:** MIT License
