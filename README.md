<h1 align="center">Fusilone Gearbase</h1>

<p align="center">
  <b>Primarily for personal use, but also a modern, portable, and intelligent inventory/maintenance management system for technical services departments.
</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Sürüm-v1.0.0-blue?style=flat-square" alt="Version">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/Platform-Windows_WPF-lightgrey?style=flat-square&logo=windows" alt="WPF">
  <img src="https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square&logo=sqlite" alt="SQLite">
</p>

---

## About Fusilone Gearbase

**Fusilone Gearbase** is a local (offline) desktop automation system that enables device registration, maintenance/failure history tracking, and the generation of QR code smart tags.

Unlike complex cloud-based systems, it **requires no installation** (Single-File EXE). It securely stores the entire database on the user's own computer. It is designed for small/medium-sized technical services, school laboratories, and those who want to professionally manage their personal equipment inventory.

> **What's new in v1.0.0:** The application was renamed from "Fusilone" to "Fusilone Gearbase", the entire design language was overhauled (new logo, custom title bars, and a token-based theme system with polished light & dark modes), and an extensive under-the-hood code cleanup resolved performance and security issues.

## Key Features:

*  **Smart Tags and QR Codes:** It produces device-specific, compact barcodes with a resolution of 1050x600 that also display the last operation performed.
*  **Dark/Light Theme:** Powered by the custom Fusilone design system (teal accent) built on Material Design, with dynamic theme transitions that are easy on the eyes.
*  **Comprehensive Device Support:** All-In-One, PC, Laptop, Tablet, Gaming Console, Monitor, Mobile Phone, SmartBoard, Printer, Router, Projection
*  **Portable:** No SQL Server installation is required. The database (in `.db` format) is stored in the `My Documents\Fusilone` folder and can be easily backed up.
*  **Multilanguage:** It has the infrastructure for Turkish and English (localization) support.

## 🛠️ Technologies Used

* **Language & Framework:** C#, .NET 10.0 (LTS)
* **UI:** WPF (Windows Presentation Foundation)
* **Design Language:** Fusilone design tokens on top of MaterialDesignThemes in XAML
* **Database:** SQLite (Microsoft.Data.Sqlite)
* **Libraries:** QRCoder (Labeling), LiveChartsCore (Charts), Serilog (Logging), ClosedXML (Excel Export)
* **Agents:** Claude Code, Gemini CLI, CoPilot Pro

##  Installations and Usage

The application does not require .NET or SQL to be installed.
1. Download the executable (`Fusilone.exe`) from the **Releases** section of this repository.
2. Double-click the downloaded file. (The application will automatically create its own database on the first launch).

---

## Developer and Process

This project is developed by Hexebit.

🌐 **Website:** [www.fusilone.com](https://www.fusilone.com)

The project's core architecture, XAML designs, and database schema were built from scratch with the "AI Pair-Programming" method using **Gemini 3 Pro**, **GitHub Copilot**, and — for the v1.0.0 redesign — **Claude Code**.

**Licence:** MIT License
