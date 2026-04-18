# 🖥️ WorkPulse 
> **Feel the rhythm of your work.**

WorkPulse is an employee self-service web application that lets you log daily work status, manage your profile, track attendance, view holidays, submit reimbursements, and stay organized — all in one place.

---

## ✨ Features

- 📋 **Daily Status Logging** — Log what you worked on each day in seconds
- ✏️ **Edit & Manage Entries** — Update or delete past status entries anytime
- 👤 **Employee Profile Management** — View and edit your profile details (name, department, designation, employee code)
- 🔐 **Secure Session-Based Access** — Auth protected with username/password login
- 🔑 **Password Reset Flow** — Step-by-step guided reset (verify username → new password → done)
- 🗓️ **Holiday Calendar** — View all national & festival holidays for the year
- 💰 **Salary & Payslips** — View monthly payslip breakdown
- 🕐 **Attendance Tracker** — Clock in/out and view your attendance history
- 🧾 **Reimbursements** — Submit and track expense claims
- 📢 **Announcements** — Stay updated with company-wide notices and updates
- 🚀 **Coming Soon** — Leave application & tracking

---

## 📸 Screenshots

### Login Page
![Login Page](./screenshots/Login-Page.png)

### Register Page
![Register Page](./screenshots/Register-Page.png)

### Forgot Password
![Forgot Password](./screenshots/Forgot-Password.png)

### Employee Dashboard
![Employee Dashboard](./screenshots/Employee-Dasboard.png)

### Daily Status
![Daily Status](./screenshots/Status.png)

### Employee Profile
![Employee Profile](./screenshots/Employee-Profile.png)

---

## 🛠️ Tech Stack

- **Framework:** ASP.NET Core MVC (C#)
- **Frontend:** Razor Views, HTML/CSS
- **Port:** `localhost:7248`

---

## 🚀 Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (version 6.0 or later)
- Visual Studio or VS Code

### Run Locally

```bash
# Clone the repository
git clone https://github.com/Amoghdev57/WorkPulse.git

# Navigate into the project
cd WorkPulse/WorkDailystatus

# Run the application
dotnet run
```

Open your browser and go to `https://localhost:7248`

---

## 📂 Project Structure

WorkPulse/
├── WorkDailystatus/        # Main ASP.NET MVC project
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   │   └── Home/
│   │       ├── Index.cshtml
│   │       ├── Status.cshtml
│   │       ├── EmployeeProfile.cshtml
│   │       ├── Holidays.cshtml
│   │       ├── Salary.cshtml
│   │       ├── Attendance.cshtml
│   │       ├── Reimbursements.cshtml
│   │       └── Announcements.cshtml
│   └── ...
├── screenshots/            # App screenshots
├── .gitignore
└── README.md


---

## 📋 Pages Overview

| Page | URL | Description |
|------|-----|-------------|
| Login | `/` | Sign in with username & password |
| Register | `/Home/Register` | Create a new employee account |
| Dashboard | `/Home/Index` | Quick access to all features |
| Daily Status | `/Home/Status` | Log and manage work entries |
| Employee Profile | `/Home/EmployeeProfile` | View and edit profile details |
| Forgot Password | `/` (modal) | 3-step password reset flow |
| Holidays | `/Home/Holidays` | National & festival holiday calendar |
| Salary | `/Home/Salary` | Monthly payslip & breakdown |
| Attendance | `/Home/Attendance` | Clock in/out & attendance history |
| Reimbursements | `/Home/Reimbursements` | Submit & track expense claims |
| Announcements | `/Home/Announcements` | Company updates & notices |

---

## 🔮 Roadmap

- [x] Holidays calendar
- [x] Salary & payslip breakdown
- [x] Attendance tracker
- [x] Reimbursements & expense claims
- [x] Company announcements


---

## 📄 License

This project is private. © WorkPulse 2026
