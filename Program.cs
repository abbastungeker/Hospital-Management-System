using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hospital_Management_System
{
    class Program
    {
        private const string AppointmentInputFormat = "yyyy-MM-dd HH:mm";

        private static List<Patient> patients = new List<Patient>();
        private static List<Doctor> doctors = new List<Doctor>();
        private static List<Appointment> appointments = new List<Appointment>();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Hospital Management System";

            if (!TryLoadData())
                return;

            ShowMainMenu();
        }

        private static bool TryLoadData()
        {
            try
            {
                FileManager.EnsureDataFilesExist();
                patients = FileManager.LoadPatients();
                doctors = FileManager.LoadDoctors();
                FileManager.ValidateAccountData(patients, doctors);
                appointments = FileManager.LoadAppointments(doctors, patients);
                return true;
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidDataException)
            {
                Console.WriteLine("The application could not load its data files.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("\nCheck the text files beside the application and try again.");
                Pause();
                return false;
            }
        }

        private static bool TrySaveData()
        {
            try
            {
                FileManager.SaveAll(patients, doctors, appointments);
                return true;
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidDataException)
            {
                Console.WriteLine("\n  The changes could not be saved.");
                Console.WriteLine("  " + ex.Message);
                return false;
            }
        }

        private static void ShowMainMenu()
        {
            while (true)
            {
                ClearScreen();
                Console.WriteLine("+--------------------------------------+ ");
                Console.WriteLine("|      Hospital Management System      |");
                Console.WriteLine("+--------------------------------------+ ");
                Console.WriteLine("|  1. Login as Patient                 |");
                Console.WriteLine("|  2. Login as Doctor                  |");
                Console.WriteLine("|  3. Login as Administrator           |");
                Console.WriteLine("|  4. Exit                             |");
                Console.WriteLine("+--------------------------------------+ ");
                Console.Write("\n  Choice: ");

                switch (ReadInt())
                {
                    case 1:
                        HandlePatientLogin();
                        break;
                    case 2:
                        HandleDoctorLogin();
                        break;
                    case 3:
                        HandleAdministratorLogin();
                        break;
                    case 4:
                        Console.WriteLine("\n  Goodbye!");
                        return;
                    default:
                        ShowInvalidChoice(1, 4);
                        break;
                }
            }
        }

        private static void HandlePatientLogin()
        {
            string username;
            string password;
            ReadCredentials(out username, out password);

            Patient patient = patients.Find(item => item.MatchesCredentials(username, password));
            if (patient == null)
            {
                ShowInvalidLogin();
                return;
            }

            ShowPatientMenu(patient);
        }

        private static void HandleDoctorLogin()
        {
            string username;
            string password;
            ReadCredentials(out username, out password);

            Doctor doctor = doctors.Find(item => item.MatchesCredentials(username, password));
            if (doctor == null)
            {
                ShowInvalidLogin();
                return;
            }

            ShowDoctorMenu(doctor);
        }

        private static void HandleAdministratorLogin()
        {
            string username;
            string password;
            ReadCredentials(out username, out password);

            try
            {
                if (!FileManager.ValidateAdministratorCredentials(username, password))
                {
                    ShowInvalidLogin();
                    return;
                }
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidDataException)
            {
                Console.WriteLine("\n  Administrator credentials could not be read.");
                Console.WriteLine("  " + ex.Message);
                Pause();
                return;
            }

            ShowAdministratorMenu();
        }

        private static void ReadCredentials(out string username, out string password)
        {
            Console.Write("\n  Username: ");
            username = (Console.ReadLine() ?? string.Empty).Trim();

            Console.Write("  Password: ");
            password = ReadMaskedPassword();
        }

        private static void ShowInvalidLogin()
        {
            Console.WriteLine("\n  Invalid username or password.");
            Pause();
        }

        private static string ReadMaskedPassword()
        {
            StringBuilder password = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo key;
                try
                {
                    key = Console.ReadKey(true);
                }
                catch (InvalidOperationException)
                {
                    return Console.ReadLine() ?? string.Empty;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return password.ToString();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Length--;
                        Console.Write("\b \b");
                    }

                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
        }

        private static void ShowPatientMenu(Patient patient)
        {
            while (true)
            {
                ClearScreen();
                Console.WriteLine("  Patient Portal - Welcome, " + patient.Name);
                Console.WriteLine("  " + new string('-', 42));
                Console.WriteLine("  1. View My Details");
                Console.WriteLine("  2. View My Appointments");
                Console.WriteLine("  3. Book an Appointment");
                Console.WriteLine("  4. Logout");
                Console.Write("\n  Choice: ");

                switch (ReadInt())
                {
                    case 1:
                        DisplayPatientDetails(patient);
                        Pause();
                        break;
                    case 2:
                        DisplayPatientAppointments(patient);
                        Pause();
                        break;
                    case 3:
                        BookAppointment(patient);
                        break;
                    case 4:
                        return;
                    default:
                        ShowInvalidChoice(1, 4);
                        break;
                }
            }
        }

        private static void DisplayPatientDetails(Patient patient)
        {
            Console.WriteLine("\n  Patient ID   : " + patient.Id);
            Console.WriteLine("  Patient Name : " + patient.Name);
            Console.WriteLine("  Username     : " + patient.Username);
        }

        private static void DisplayPatientAppointments(Patient patient)
        {
            List<Appointment> patientAppointments = patient.Appointments
                .OrderBy(appointment => appointment.Date)
                .ToList();

            if (patientAppointments.Count == 0)
            {
                Console.WriteLine("\n  No appointments found.");
                return;
            }

            Console.WriteLine("\n  Appointments for " + patient.Name + ":");
            Console.WriteLine("  " + new string('-', 50));

            foreach (Appointment appointment in patientAppointments)
            {
                Console.WriteLine("  Doctor : Dr. " + appointment.Doctor.Name);
                Console.WriteLine("  Date   : " + appointment.Date.ToString(AppointmentInputFormat));
                Console.WriteLine("  Status : " + GetAppointmentStatus(appointment.Date));
                Console.WriteLine("  " + new string('-', 50));
            }
        }

        private static void BookAppointment(Patient patient)
        {
            if (doctors.Count == 0)
            {
                Console.WriteLine("\n  No doctors are currently available.");
                Pause();
                return;
            }

            Console.WriteLine("\n  Available Doctors:");
            Console.WriteLine("  " + new string('-', 50));
            foreach (Doctor doctor in doctors.OrderBy(item => item.Id))
                Console.WriteLine("  ID: {0,-5} Dr. {1}", doctor.Id, doctor.Name);
            Console.WriteLine("  " + new string('-', 50));

            Console.Write("\n  Enter Doctor ID: ");
            int doctorId = ReadInt();
            Doctor selectedDoctor = doctors.Find(item => item.Id == doctorId);

            if (selectedDoctor == null)
            {
                Console.WriteLine("\n  Doctor not found.");
                Pause();
                return;
            }

            Console.Write("  Enter appointment date and time ({0}): ", AppointmentInputFormat);
            string dateInput = (Console.ReadLine() ?? string.Empty).Trim();

            DateTime appointmentDate;
            if (!DateTime.TryParseExact(
                dateInput,
                AppointmentInputFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out appointmentDate))
            {
                Console.WriteLine("\n  Invalid date. Use the format " + AppointmentInputFormat + ".");
                Pause();
                return;
            }

            if (appointmentDate <= DateTime.Now)
            {
                Console.WriteLine("\n  Appointments must be scheduled in the future.");
                Pause();
                return;
            }

            if (selectedDoctor.HasAppointmentAt(appointmentDate))
            {
                Console.WriteLine("\n  The selected doctor already has an appointment at this time.");
                Pause();
                return;
            }

            if (patient.HasAppointmentAt(appointmentDate))
            {
                Console.WriteLine("\n  You already have an appointment at this time.");
                Pause();
                return;
            }

            Appointment appointment = new Appointment(selectedDoctor, patient, appointmentDate);

            try
            {
                LinkAppointment(appointment);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("\n  The appointment could not be booked.");
                Console.WriteLine("  " + ex.Message);
                Pause();
                return;
            }

            if (!TrySaveData())
            {
                UnlinkAppointment(appointment);
                Pause();
                return;
            }

            Console.WriteLine("\n  Appointment booked successfully.");
            Pause();
        }

        private static void ShowDoctorMenu(Doctor doctor)
        {
            while (true)
            {
                ClearScreen();
                Console.WriteLine("  Doctor Portal - Welcome, Dr. " + doctor.Name);
                Console.WriteLine("  " + new string('-', 42));
                Console.WriteLine("  1. View My Details");
                Console.WriteLine("  2. View My Patients");
                Console.WriteLine("  3. View My Appointments");
                Console.WriteLine("  4. Logout");
                Console.Write("\n  Choice: ");

                switch (ReadInt())
                {
                    case 1:
                        DisplayDoctorDetails(doctor);
                        Pause();
                        break;
                    case 2:
                        DisplayDoctorPatients(doctor);
                        Pause();
                        break;
                    case 3:
                        DisplayDoctorAppointments(doctor);
                        Pause();
                        break;
                    case 4:
                        return;
                    default:
                        ShowInvalidChoice(1, 4);
                        break;
                }
            }
        }

        private static void DisplayDoctorDetails(Doctor doctor)
        {
            Console.WriteLine("\n  Doctor ID   : " + doctor.Id);
            Console.WriteLine("  Doctor Name : Dr. " + doctor.Name);
            Console.WriteLine("  Username    : " + doctor.Username);
        }

        private static void DisplayDoctorPatients(Doctor doctor)
        {
            List<Patient> assignedPatients = doctor.GetPatients();
            if (assignedPatients.Count == 0)
            {
                Console.WriteLine("\n  No patients registered.");
                return;
            }

            Console.WriteLine("\n  Patients of Dr. " + doctor.Name + ":");
            Console.WriteLine("  " + new string('-', 50));

            foreach (Patient patient in assignedPatients)
                Console.WriteLine("  Patient ID: {0,-5} Name: {1}", patient.Id, patient.Name);

            Console.WriteLine("  " + new string('-', 50));
        }

        private static void DisplayDoctorAppointments(Doctor doctor)
        {
            List<Appointment> doctorAppointments = doctor.Appointments
                .OrderBy(appointment => appointment.Date)
                .ToList();

            if (doctorAppointments.Count == 0)
            {
                Console.WriteLine("\n  No appointments scheduled.");
                return;
            }

            Console.WriteLine("\n  Appointments for Dr. " + doctor.Name + ":");
            Console.WriteLine("  " + new string('-', 50));

            foreach (Appointment appointment in doctorAppointments)
            {
                Console.WriteLine("  Patient : " + appointment.Patient.Name);
                Console.WriteLine("  Date    : " + appointment.Date.ToString(AppointmentInputFormat));
                Console.WriteLine("  Status  : " + GetAppointmentStatus(appointment.Date));
                Console.WriteLine("  " + new string('-', 50));
            }
        }

        private static void ShowAdministratorMenu()
        {
            while (true)
            {
                ClearScreen();
                Console.WriteLine("  Administrator Portal");
                Console.WriteLine("  " + new string('-', 42));
                Console.WriteLine("  1. List All Doctors");
                Console.WriteLine("  2. List All Patients");
                Console.WriteLine("  3. List All Appointments");
                Console.WriteLine("  4. Add Doctor");
                Console.WriteLine("  5. Add Patient");
                Console.WriteLine("  6. Remove Doctor");
                Console.WriteLine("  7. Remove Patient");
                Console.WriteLine("  8. Logout");
                Console.Write("\n  Choice: ");

                switch (ReadInt())
                {
                    case 1:
                        ListAllDoctors();
                        Pause();
                        break;
                    case 2:
                        ListAllPatients();
                        Pause();
                        break;
                    case 3:
                        ListAllAppointments();
                        Pause();
                        break;
                    case 4:
                        AddDoctor();
                        break;
                    case 5:
                        AddPatient();
                        break;
                    case 6:
                        RemoveDoctor();
                        break;
                    case 7:
                        RemovePatient();
                        break;
                    case 8:
                        return;
                    default:
                        ShowInvalidChoice(1, 8);
                        break;
                }
            }
        }

        private static void ListAllDoctors()
        {
            if (doctors.Count == 0)
            {
                Console.WriteLine("\n  No doctors on record.");
                return;
            }

            Console.WriteLine("\n  Doctors:");
            Console.WriteLine("  " + new string('-', 62));
            Console.WriteLine("  {0,-8} {1,-28} {2}", "ID", "Name", "Username");
            Console.WriteLine("  " + new string('-', 62));

            foreach (Doctor doctor in doctors.OrderBy(item => item.Id))
                Console.WriteLine("  {0,-8} {1,-28} {2}", doctor.Id, "Dr. " + doctor.Name, doctor.Username);

            Console.WriteLine("  " + new string('-', 62));
        }

        private static void ListAllPatients()
        {
            if (patients.Count == 0)
            {
                Console.WriteLine("\n  No patients on record.");
                return;
            }

            Console.WriteLine("\n  Patients:");
            Console.WriteLine("  " + new string('-', 62));
            Console.WriteLine("  {0,-8} {1,-28} {2}", "ID", "Name", "Username");
            Console.WriteLine("  " + new string('-', 62));

            foreach (Patient patient in patients.OrderBy(item => item.Id))
                Console.WriteLine("  {0,-8} {1,-28} {2}", patient.Id, patient.Name, patient.Username);

            Console.WriteLine("  " + new string('-', 62));
        }

        private static void ListAllAppointments()
        {
            if (appointments.Count == 0)
            {
                Console.WriteLine("\n  No appointments scheduled.");
                return;
            }

            Console.WriteLine("\n  Appointments:");
            Console.WriteLine("  " + new string('-', 78));

            foreach (Appointment appointment in appointments.OrderBy(item => item.Date))
            {
                Console.WriteLine("  Doctor  : Dr. " + appointment.Doctor.Name);
                Console.WriteLine("  Patient : " + appointment.Patient.Name);
                Console.WriteLine("  Date    : " + appointment.Date.ToString(AppointmentInputFormat));
                Console.WriteLine("  Status  : " + GetAppointmentStatus(appointment.Date));
                Console.WriteLine("  " + new string('-', 78));
            }
        }

        private static void AddDoctor()
        {
            string name;
            string username;
            string password;

            if (!TryReadNewAccount("doctor", out name, out username, out password))
            {
                Pause();
                return;
            }

            if (UsernameExists(username))
            {
                Console.WriteLine("\n  That username is already in use.");
                Pause();
                return;
            }

            string salt;
            string hash;
            FileManager.CreatePasswordHash(password, out salt, out hash);

            int id = FileManager.NextDoctorId(doctors);
            Doctor doctor = new Doctor(id, name, username, salt, hash);
            doctors.Add(doctor);

            if (!TrySaveData())
            {
                doctors.Remove(doctor);
                Pause();
                return;
            }

            Console.WriteLine("\n  Dr. {0} added successfully (ID: {1}).", name, id);
            Pause();
        }

        private static void AddPatient()
        {
            string name;
            string username;
            string password;

            if (!TryReadNewAccount("patient", out name, out username, out password))
            {
                Pause();
                return;
            }

            if (UsernameExists(username))
            {
                Console.WriteLine("\n  That username is already in use.");
                Pause();
                return;
            }

            string salt;
            string hash;
            FileManager.CreatePasswordHash(password, out salt, out hash);

            int id = FileManager.NextPatientId(patients);
            Patient patient = new Patient(id, name, username, salt, hash);
            patients.Add(patient);

            if (!TrySaveData())
            {
                patients.Remove(patient);
                Pause();
                return;
            }

            Console.WriteLine("\n  Patient {0} added successfully (ID: {1}).", name, id);
            Pause();
        }

        private static bool TryReadNewAccount(
            string role,
            out string name,
            out string username,
            out string password)
        {
            name = string.Empty;
            username = string.Empty;
            password = string.Empty;

            Console.Write("\n  Enter new {0}'s name: ", role);
            name = (Console.ReadLine() ?? string.Empty).Trim();

            if (!IsValidName(name))
            {
                Console.WriteLine("\n  Name must be between 2 and 100 characters and cannot contain '|'.");
                return false;
            }

            Console.Write("  Enter a username: ");
            username = (Console.ReadLine() ?? string.Empty).Trim();

            if (!IsValidUsername(username))
            {
                Console.WriteLine("\n  Username must be 3-30 characters and may use letters, numbers, '.', '_' or '-'.");
                return false;
            }

            Console.Write("  Enter a password: ");
            password = ReadMaskedPassword();

            if (password.Length < 8 || password.Length > 128)
            {
                Console.WriteLine("\n  Password must be between 8 and 128 characters.");
                return false;
            }

            Console.Write("  Confirm password: ");
            string confirmation = ReadMaskedPassword();

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                Console.WriteLine("\n  Passwords do not match.");
                return false;
            }

            return true;
        }

        private static void RemoveDoctor()
        {
            if (doctors.Count == 0)
            {
                Console.WriteLine("\n  No doctors on record.");
                Pause();
                return;
            }

            ListAllDoctors();
            Console.Write("\n  Enter Doctor ID to remove: ");
            int id = ReadInt();

            Doctor doctor = doctors.Find(item => item.Id == id);
            if (doctor == null)
            {
                Console.WriteLine("\n  Doctor not found.");
                Pause();
                return;
            }

            if (!Confirm("Remove Dr. " + doctor.Name + " and all related appointments?"))
            {
                Console.WriteLine("\n  Removal cancelled.");
                Pause();
                return;
            }

            List<Appointment> relatedAppointments = appointments
                .Where(appointment => appointment.Doctor.Id == doctor.Id)
                .ToList();

            foreach (Appointment appointment in relatedAppointments)
                UnlinkAppointment(appointment);

            doctors.Remove(doctor);

            if (!TrySaveData())
            {
                doctors.Add(doctor);
                RestoreAppointments(relatedAppointments);
                Pause();
                return;
            }

            Console.WriteLine("\n  Dr. {0} removed successfully.", doctor.Name);
            Pause();
        }

        private static void RemovePatient()
        {
            if (patients.Count == 0)
            {
                Console.WriteLine("\n  No patients on record.");
                Pause();
                return;
            }

            ListAllPatients();
            Console.Write("\n  Enter Patient ID to remove: ");
            int id = ReadInt();

            Patient patient = patients.Find(item => item.Id == id);
            if (patient == null)
            {
                Console.WriteLine("\n  Patient not found.");
                Pause();
                return;
            }

            if (!Confirm("Remove " + patient.Name + " and all related appointments?"))
            {
                Console.WriteLine("\n  Removal cancelled.");
                Pause();
                return;
            }

            List<Appointment> relatedAppointments = appointments
                .Where(appointment => appointment.Patient.Id == patient.Id)
                .ToList();

            foreach (Appointment appointment in relatedAppointments)
                UnlinkAppointment(appointment);

            patients.Remove(patient);

            if (!TrySaveData())
            {
                patients.Add(patient);
                RestoreAppointments(relatedAppointments);
                Pause();
                return;
            }

            Console.WriteLine("\n  Patient {0} removed successfully.", patient.Name);
            Pause();
        }

        private static void RestoreAppointments(IEnumerable<Appointment> items)
        {
            foreach (Appointment appointment in items.OrderBy(item => item.Date))
                LinkAppointment(appointment);
        }

        private static void LinkAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException("appointment");

            if (appointments.Contains(appointment))
                return;

            appointment.Doctor.AddAppointment(appointment);

            try
            {
                appointment.Patient.AddAppointment(appointment);
            }
            catch
            {
                appointment.Doctor.RemoveAppointment(appointment);
                throw;
            }

            appointments.Add(appointment);
        }

        private static void UnlinkAppointment(Appointment appointment)
        {
            if (appointment == null)
                return;

            appointments.Remove(appointment);
            appointment.Doctor.RemoveAppointment(appointment);
            appointment.Patient.RemoveAppointment(appointment);
        }

        private static bool UsernameExists(string username)
        {
            if (patients.Any(patient =>
                string.Equals(patient.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (doctors.Any(doctor =>
                string.Equals(doctor.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            try
            {
                return FileManager.AdministratorUsernameExists(username);
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidDataException)
            {
                Console.WriteLine("\n  Administrator usernames could not be checked.");
                Console.WriteLine("  " + ex.Message);
                return true;
            }
        }

        private static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                && name.Length >= 2
                && name.Length <= 100
                && name.IndexOf('|') < 0
                && name.IndexOf('\r') < 0
                && name.IndexOf('\n') < 0;
        }

        private static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 30)
                return false;

            return username.All(character =>
                char.IsLetterOrDigit(character)
                || character == '.'
                || character == '_'
                || character == '-');
        }

        private static string GetAppointmentStatus(DateTime date)
        {
            return date >= DateTime.Now ? "Upcoming" : "Completed";
        }

        private static bool Confirm(string message)
        {
            Console.Write("\n  {0} (y/n): ", message);
            string response = (Console.ReadLine() ?? string.Empty).Trim();

            return response.Equals("y", StringComparison.OrdinalIgnoreCase)
                || response.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int ReadInt()
        {
            int value;
            string input = (Console.ReadLine() ?? string.Empty).Trim();
            return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : -1;
        }

        private static void ShowInvalidChoice(int minimum, int maximum)
        {
            Console.WriteLine(
                "\n  Invalid choice. Please select an option from {0} to {1}.",
                minimum,
                maximum);
            Pause();
        }

        private static void ClearScreen()
        {
            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                // Redirected consoles may not support clearing the screen.
            }
        }

        private static void Pause()
        {
            Console.Write("\n  Press any key to continue...");

            try
            {
                Console.ReadKey(true);
            }
            catch (InvalidOperationException)
            {
                Console.ReadLine();
            }
        }
    }
}
