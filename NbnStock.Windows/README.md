# NBN Stock Portal

A custom-built, offline-first inventory and E-Waste management system designed specifically for NBN field contracting.

## Overview

The NBN Stock Portal is a Windows Desktop application (WPF/C#) built to track the complete physical lifecycle of NBN telecommunications hardware. It replaces manual spreadsheets with a fast, barcode-ready interface for receiving stock, consuming daily materials, and processing legacy equipment into a structured E-Waste pipeline.

All data is stored locally in a secure, offline SQLite database, ensuring lightning-fast performance even in remote areas without internet access.

## Key Features

* **Smart Barcode Scanning:** Rapidly scan serialised units (ODUs, IDUs, NTDs) into inventory, preventing duplicate entries automatically.
* **Bulk Daily Consumption:** A dedicated "End of Day" dashboard to quickly log all cables, conduits, mounts, and hardware used across multiple jobs in a single transaction.
* **E-Waste Recovery Pipeline:** Scan pulled legacy gear directly from the field. Automatically updates known units or assigns "legacy" status to unknown hardware, moving them through a multi-stage disposal pipeline.
* **Dynamic Custom Consumables:** Generate new "Tech-Supplied" bulk items on the fly without needing to touch the database.
* **Automated Monthly Reporting:** Generate clean, formatted CSV reports of all current On-Hand stock (including specific serial numbers) and active E-Waste, ready to be emailed or opened in Excel.
* **Dark Mode / Light Mode Integration:** UI automatically themes itself to match your Windows OS settings for optimal visibility in the truck or the office.

## Technology Stack

* **Framework:** .NET Desktop Runtime (WPF/C#)
* **Database:** SQLite (Local `NbnStock.db` stored in AppData)
* **Architecture:** Repository Pattern, Entity Framework (or direct ADO.NET)
* **UI:** Custom XAML dynamic resource theming

## Installation

1. Download the latest release folder containing `setup.exe`.
2. Double-click `setup.exe` to launch the ClickOnce installer.
3. Follow the standard Windows prompts.
4. The application will install, place a shortcut in your Start Menu under **NBN Stock Portal**, and launch automatically.

## First-Time Setup

On first launch, the application will automatically build the local SQLite database directory and seed the essential tables. No manual database configuration is required.

---

*Developed by Scott (2026).*
