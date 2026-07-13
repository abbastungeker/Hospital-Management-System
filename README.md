# Hospital Management System

A role-based C# console application for managing doctors, patients and appointment bookings. The application targets .NET Framework 4.7.2 and stores its data in text files beside the compiled executable.

## Features

### Patient Portal
- Log in with an individual patient account
- View personal account details
- View appointment history and status
- Book a future appointment with a doctor
- Prevent patient and doctor double-booking at the same date and time

### Doctor Portal
- Log in with an individual doctor account
- View personal account details
- View patients connected through appointments
- View appointment history and status

### Administrator Portal
- View all doctors, patients and appointments
- Add doctors and patients with unique login credentials
- Remove doctors and patients after confirmation
- Remove related appointments when a doctor or patient is deleted

### Data and Validation
- Patient, doctor and administrator passwords are stored as salted PBKDF2 hashes
- Usernames are unique across all account roles
- IDs are generated from the highest existing ID
- Appointment dates use the exact `yyyy-MM-dd HH:mm` input format
- Invalid, duplicate or orphaned data records are reported during startup
- Patient, doctor and appointment files are saved together with backup-based rollback if a write fails

## Project Layout

The original Visual Studio project layout has been retained.

```text
Hospital Management System.sln
Hospital Management System/
├── Appointment.cs
├── Doctor.cs
├── FileManager.cs
├── Patient.cs
├── Program.cs
├── App.config
├── Hospital Management System.csproj
├── README.md
├── Properties/
├── bin/
└── obj/
```

The included sample data is stored in `Hospital Management System/bin/Debug`. An `Appointments.txt` file is created automatically beside the executable when the application first runs.

## Requirements

- Windows
- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later with the .NET desktop development workload

## Running the Application

1. Open `Hospital Management System.sln` in Visual Studio.
2. Select **Build > Rebuild Solution**.
3. Run the project with `F5` or `Ctrl + F5`.

Rebuilding is important because the executable must be regenerated from the current source files.

## Sample Accounts

| Role | Username | Password |
|---|---|---|
| Patient | `Patient1` | `Password1` |
| Doctor | `Doctor1` | `Password1` |
| Administrator | `Admin1` | `1234` |

Additional sample accounts follow the same numbering pattern. For example, `Patient2` uses `Password2`.

## Stored Data Formats

Patient and doctor records:

```text
Id|Name|Username|PasswordSalt|PasswordHash
```

Administrator records:

```text
Username|PasswordSalt|PasswordHash
```

Appointment records:

```text
DoctorId|PatientId|yyyy-MM-dd HH:mm:ss
```

## Technical Notes

- **Language:** C#
- **Framework:** .NET Framework 4.7.2
- **Storage:** UTF-8 flat files
- **Design:** Domain models separated from console workflow and file persistence
- **Authentication:** Role-based login with masked password input and PBKDF2 password hashing
- **Consistency:** Appointment relationships are linked to the main list, doctor and patient records

## Scope

This is an educational console application. A production hospital system would require a database, audited access control, encryption, privacy safeguards, concurrency handling and compliance with applicable health-information regulations.

## Author

Mohammad Abbas Tungeker  
[LinkedIn](https://linkedin.com/in/mohammad-tungeker-85b6b71b1)
