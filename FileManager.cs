using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Hospital_Management_System
{
    static class FileManager
    {
        private const char Separator = '|';
        private const string AppointmentDateFormat = "yyyy-MM-dd HH:mm:ss";

        private static readonly string DataDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string PatientsFile = Path.Combine(DataDirectory, "Patients.txt");
        private static readonly string DoctorsFile = Path.Combine(DataDirectory, "Doctors.txt");
        private static readonly string AdministratorsFile = Path.Combine(DataDirectory, "Administrators.txt");
        private static readonly string AppointmentsFile = Path.Combine(DataDirectory, "Appointments.txt");

        public static void EnsureDataFilesExist()
        {
            Directory.CreateDirectory(DataDirectory);

            if (!File.Exists(PatientsFile))
                WriteLines(PatientsFile, CreateDefaultPatientLines());

            if (!File.Exists(DoctorsFile))
                WriteLines(DoctorsFile, CreateDefaultDoctorLines());

            if (!File.Exists(AdministratorsFile))
                WriteLines(AdministratorsFile, CreateDefaultAdministratorLines());

            if (!File.Exists(AppointmentsFile))
                WriteLines(AppointmentsFile, new string[0]);
        }

        public static List<Patient> LoadPatients()
        {
            List<Patient> patients = new List<Patient>();
            HashSet<int> ids = new HashSet<int>();
            HashSet<string> usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int lineNumber = 0;
            foreach (string rawLine in File.ReadAllLines(PatientsFile))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string[] parts = rawLine.Split(Separator);
                if (parts.Length != 5)
                    throw InvalidRecord(PatientsFile, lineNumber, "expected five fields");

                int id;
                if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0)
                    throw InvalidRecord(PatientsFile, lineNumber, "patient ID must be a positive number");

                string name = parts[1].Trim();
                string username = parts[2].Trim();
                string salt = parts[3].Trim();
                string hash = parts[4].Trim();

                ValidatePersonRecord(PatientsFile, lineNumber, name, username, salt, hash);

                if (!ids.Add(id))
                    throw InvalidRecord(PatientsFile, lineNumber, "duplicate patient ID");

                if (!usernames.Add(username))
                    throw InvalidRecord(PatientsFile, lineNumber, "duplicate patient username");

                patients.Add(new Patient(id, name, username, salt, hash));
            }

            return patients.OrderBy(patient => patient.Id).ToList();
        }

        public static List<Doctor> LoadDoctors()
        {
            List<Doctor> doctors = new List<Doctor>();
            HashSet<int> ids = new HashSet<int>();
            HashSet<string> usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int lineNumber = 0;
            foreach (string rawLine in File.ReadAllLines(DoctorsFile))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string[] parts = rawLine.Split(Separator);
                if (parts.Length != 5)
                    throw InvalidRecord(DoctorsFile, lineNumber, "expected five fields");

                int id;
                if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0)
                    throw InvalidRecord(DoctorsFile, lineNumber, "doctor ID must be a positive number");

                string name = parts[1].Trim();
                string username = parts[2].Trim();
                string salt = parts[3].Trim();
                string hash = parts[4].Trim();

                ValidatePersonRecord(DoctorsFile, lineNumber, name, username, salt, hash);

                if (!ids.Add(id))
                    throw InvalidRecord(DoctorsFile, lineNumber, "duplicate doctor ID");

                if (!usernames.Add(username))
                    throw InvalidRecord(DoctorsFile, lineNumber, "duplicate doctor username");

                doctors.Add(new Doctor(id, name, username, salt, hash));
            }

            return doctors.OrderBy(doctor => doctor.Id).ToList();
        }

        public static List<Appointment> LoadAppointments(List<Doctor> doctors, List<Patient> patients)
        {
            if (doctors == null)
                throw new ArgumentNullException("doctors");

            if (patients == null)
                throw new ArgumentNullException("patients");

            List<AppointmentRecord> records = new List<AppointmentRecord>();
            HashSet<string> doctorSlots = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> patientSlots = new HashSet<string>(StringComparer.Ordinal);

            int lineNumber = 0;
            foreach (string rawLine in File.ReadAllLines(AppointmentsFile))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string[] parts = rawLine.Split(Separator);
                if (parts.Length != 3)
                    throw InvalidRecord(AppointmentsFile, lineNumber, "expected three fields");

                int doctorId;
                int patientId;
                DateTime date;

                if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out doctorId) || doctorId <= 0)
                    throw InvalidRecord(AppointmentsFile, lineNumber, "doctor ID is invalid");

                if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out patientId) || patientId <= 0)
                    throw InvalidRecord(AppointmentsFile, lineNumber, "patient ID is invalid");

                if (!DateTime.TryParseExact(
                    parts[2].Trim(),
                    AppointmentDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date))
                {
                    throw InvalidRecord(AppointmentsFile, lineNumber, "appointment date is invalid");
                }

                Doctor doctor = doctors.Find(item => item.Id == doctorId);
                Patient patient = patients.Find(item => item.Id == patientId);

                if (doctor == null || patient == null)
                    throw InvalidRecord(AppointmentsFile, lineNumber, "appointment references a missing doctor or patient");

                string doctorSlot = BuildSlotKey(doctorId, date);
                string patientSlot = BuildSlotKey(patientId, date);

                if (!doctorSlots.Add(doctorSlot))
                    throw InvalidRecord(AppointmentsFile, lineNumber, "doctor has more than one appointment at the same time");

                if (!patientSlots.Add(patientSlot))
                    throw InvalidRecord(AppointmentsFile, lineNumber, "patient has more than one appointment at the same time");

                records.Add(new AppointmentRecord(doctor, patient, date));
            }

            List<Appointment> appointments = new List<Appointment>();
            foreach (AppointmentRecord record in records.OrderBy(item => item.Date))
            {
                Appointment appointment = new Appointment(record.Doctor, record.Patient, record.Date);
                record.Doctor.AddAppointment(appointment);
                record.Patient.AddAppointment(appointment);
                appointments.Add(appointment);
            }

            return appointments;
        }

        public static void SaveAll(
            IEnumerable<Patient> patients,
            IEnumerable<Doctor> doctors,
            IEnumerable<Appointment> appointments)
        {
            if (patients == null)
                throw new ArgumentNullException("patients");

            if (doctors == null)
                throw new ArgumentNullException("doctors");

            if (appointments == null)
                throw new ArgumentNullException("appointments");

            List<Patient> patientList = patients.ToList();
            List<Doctor> doctorList = doctors.ToList();
            List<Appointment> appointmentList = appointments.ToList();

            ValidateSystemData(patientList, doctorList, appointmentList);

            List<PendingWrite> writes = new List<PendingWrite>
            {
                new PendingWrite(PatientsFile, BuildPatientLines(patientList)),
                new PendingWrite(DoctorsFile, BuildDoctorLines(doctorList)),
                new PendingWrite(AppointmentsFile, BuildAppointmentLines(appointmentList))
            };

            WriteTransaction(writes);
        }

        public static void ValidateAccountData(List<Patient> patients, List<Doctor> doctors)
        {
            if (patients == null)
                throw new ArgumentNullException("patients");

            if (doctors == null)
                throw new ArgumentNullException("doctors");

            List<AdministratorRecord> administrators = LoadAdministrators();
            if (administrators.Count == 0)
                throw new InvalidDataException("Administrators.txt must contain at least one administrator account.");

            HashSet<string> usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Patient patient in patients)
            {
                if (patient == null)
                    throw new InvalidDataException("The patient list contains an empty record.");

                if (!usernames.Add(patient.Username))
                    throw new InvalidDataException("The username '" + patient.Username + "' is used by more than one account.");
            }

            foreach (Doctor doctor in doctors)
            {
                if (doctor == null)
                    throw new InvalidDataException("The doctor list contains an empty record.");

                if (!usernames.Add(doctor.Username))
                    throw new InvalidDataException("The username '" + doctor.Username + "' is used by more than one account.");
            }

            foreach (AdministratorRecord administrator in administrators)
            {
                if (!usernames.Add(administrator.Username))
                    throw new InvalidDataException("The username '" + administrator.Username + "' is used by more than one account.");
            }
        }

        public static bool ValidateAdministratorCredentials(string username, string password)
        {
            foreach (AdministratorRecord administrator in LoadAdministrators())
            {
                if (string.Equals(administrator.Username, username, StringComparison.OrdinalIgnoreCase)
                    && PasswordHasher.Verify(password, administrator.PasswordSalt, administrator.PasswordHash))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool AdministratorUsernameExists(string username)
        {
            return LoadAdministrators().Any(administrator =>
                string.Equals(administrator.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        public static void CreatePasswordHash(string password, out string salt, out string hash)
        {
            PasswordHasher.CreateHash(password, out salt, out hash);
        }

        public static int NextPatientId(IEnumerable<Patient> patients)
        {
            return GetNextId(patients.Select(patient => patient.Id), "patient");
        }

        public static int NextDoctorId(IEnumerable<Doctor> doctors)
        {
            return GetNextId(doctors.Select(doctor => doctor.Id), "doctor");
        }

        private static void ValidateSystemData(
            List<Patient> patients,
            List<Doctor> doctors,
            List<Appointment> appointments)
        {
            if (patients.Any(patient => patient == null))
                throw new InvalidDataException("The patient list contains an empty record.");

            if (doctors.Any(doctor => doctor == null))
                throw new InvalidDataException("The doctor list contains an empty record.");

            if (appointments.Any(appointment => appointment == null))
                throw new InvalidDataException("The appointment list contains an empty record.");

            ValidateAccountData(patients, doctors);

            if (patients.Select(patient => patient.Id).Distinct().Count() != patients.Count)
                throw new InvalidDataException("Patient IDs must be unique.");

            if (doctors.Select(doctor => doctor.Id).Distinct().Count() != doctors.Count)
                throw new InvalidDataException("Doctor IDs must be unique.");

            HashSet<string> doctorSlots = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> patientSlots = new HashSet<string>(StringComparer.Ordinal);
            HashSet<Appointment> appointmentSet = new HashSet<Appointment>(appointments);

            foreach (Appointment appointment in appointments)
            {
                Doctor doctor = doctors.Find(item => item.Id == appointment.Doctor.Id);
                Patient patient = patients.Find(item => item.Id == appointment.Patient.Id);

                if (doctor == null || !object.ReferenceEquals(doctor, appointment.Doctor))
                    throw new InvalidDataException("An appointment references a doctor that is not in the doctor list.");

                if (patient == null || !object.ReferenceEquals(patient, appointment.Patient))
                    throw new InvalidDataException("An appointment references a patient that is not in the patient list.");

                if (!doctorSlots.Add(BuildSlotKey(doctor.Id, appointment.Date)))
                    throw new InvalidDataException("A doctor has more than one appointment at the same time.");

                if (!patientSlots.Add(BuildSlotKey(patient.Id, appointment.Date)))
                    throw new InvalidDataException("A patient has more than one appointment at the same time.");

                if (!doctor.Appointments.Contains(appointment) || !patient.Appointments.Contains(appointment))
                    throw new InvalidDataException("An appointment is not linked to both its doctor and patient.");
            }

            foreach (Doctor doctor in doctors)
            {
                if (doctor.Appointments.Any(appointment =>
                    !appointmentSet.Contains(appointment) || appointment.Doctor.Id != doctor.Id))
                {
                    throw new InvalidDataException("A doctor contains an appointment that is not in the main appointment list.");
                }
            }

            foreach (Patient patient in patients)
            {
                if (patient.Appointments.Any(appointment =>
                    !appointmentSet.Contains(appointment) || appointment.Patient.Id != patient.Id))
                {
                    throw new InvalidDataException("A patient contains an appointment that is not in the main appointment list.");
                }
            }
        }

        private static List<AdministratorRecord> LoadAdministrators()
        {
            List<AdministratorRecord> administrators = new List<AdministratorRecord>();
            HashSet<string> usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int lineNumber = 0;
            foreach (string rawLine in File.ReadAllLines(AdministratorsFile))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string[] parts = rawLine.Split(Separator);
                if (parts.Length != 3)
                    throw InvalidRecord(AdministratorsFile, lineNumber, "expected three fields");

                string username = parts[0].Trim();
                string salt = parts[1].Trim();
                string hash = parts[2].Trim();

                if (!IsValidUsername(username))
                    throw InvalidRecord(AdministratorsFile, lineNumber, "administrator username is invalid");

                if (!PasswordHasher.IsValidHashData(salt, hash))
                    throw InvalidRecord(AdministratorsFile, lineNumber, "password data is invalid");

                if (!usernames.Add(username))
                    throw InvalidRecord(AdministratorsFile, lineNumber, "duplicate administrator username");

                administrators.Add(new AdministratorRecord(username, salt, hash));
            }

            return administrators;
        }

        private static string[] BuildPatientLines(IEnumerable<Patient> patients)
        {
            return patients
                .OrderBy(patient => patient.Id)
                .Select(patient => JoinFields(
                    patient.Id.ToString(CultureInfo.InvariantCulture),
                    patient.Name,
                    patient.Username,
                    patient.PasswordSalt,
                    patient.PasswordHash))
                .ToArray();
        }

        private static string[] BuildDoctorLines(IEnumerable<Doctor> doctors)
        {
            return doctors
                .OrderBy(doctor => doctor.Id)
                .Select(doctor => JoinFields(
                    doctor.Id.ToString(CultureInfo.InvariantCulture),
                    doctor.Name,
                    doctor.Username,
                    doctor.PasswordSalt,
                    doctor.PasswordHash))
                .ToArray();
        }

        private static string[] BuildAppointmentLines(IEnumerable<Appointment> appointments)
        {
            return appointments
                .OrderBy(appointment => appointment.Date)
                .ThenBy(appointment => appointment.Doctor.Id)
                .ThenBy(appointment => appointment.Patient.Id)
                .Select(appointment => JoinFields(
                    appointment.Doctor.Id.ToString(CultureInfo.InvariantCulture),
                    appointment.Patient.Id.ToString(CultureInfo.InvariantCulture),
                    appointment.Date.ToString(AppointmentDateFormat, CultureInfo.InvariantCulture)))
                .ToArray();
        }

        private static string[] CreateDefaultPatientLines()
        {
            string salt;
            string hash;
            PasswordHasher.CreateHash("Password1", out salt, out hash);
            return new[] { JoinFields("1", "Patient One", "Patient1", salt, hash) };
        }

        private static string[] CreateDefaultDoctorLines()
        {
            string salt;
            string hash;
            PasswordHasher.CreateHash("Password1", out salt, out hash);
            return new[] { JoinFields("1", "Doctor One", "Doctor1", salt, hash) };
        }

        private static string[] CreateDefaultAdministratorLines()
        {
            string salt;
            string hash;
            PasswordHasher.CreateHash("1234", out salt, out hash);
            return new[] { JoinFields("Admin1", salt, hash) };
        }

        private static void ValidatePersonRecord(
            string path,
            int lineNumber,
            string name,
            string username,
            string salt,
            string hash)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw InvalidRecord(path, lineNumber, "name is missing");

            if (name.Length > 100 || ContainsLineBreak(name))
                throw InvalidRecord(path, lineNumber, "name is invalid");

            if (!IsValidUsername(username))
                throw InvalidRecord(path, lineNumber, "username is invalid");

            if (!PasswordHasher.IsValidHashData(salt, hash))
                throw InvalidRecord(path, lineNumber, "password data is invalid");
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

        private static int GetNextId(IEnumerable<int> ids, string recordType)
        {
            List<int> idList = ids.ToList();
            if (idList.Count == 0)
                return 1;

            int highestId = idList.Max();
            if (highestId == int.MaxValue)
                throw new InvalidOperationException("No more " + recordType + " IDs are available.");

            return highestId + 1;
        }

        private static string BuildSlotKey(int id, DateTime date)
        {
            return id.ToString(CultureInfo.InvariantCulture)
                + ":"
                + date.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        private static string JoinFields(params string[] fields)
        {
            foreach (string field in fields)
            {
                if (field == null)
                    throw new InvalidDataException("A data field cannot be null.");

                if (field.IndexOf(Separator) >= 0 || ContainsLineBreak(field))
                    throw new InvalidDataException("Data fields cannot contain '|', carriage returns or line breaks.");
            }

            return string.Join(Separator.ToString(), fields);
        }

        private static bool ContainsLineBreak(string value)
        {
            return value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0;
        }

        private static void WriteLines(string path, IEnumerable<string> lines)
        {
            File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
        }

        private static void WriteTransaction(List<PendingWrite> writes)
        {
            bool saveCompleted = false;
            bool rollbackCompleted = true;

            try
            {
                foreach (PendingWrite write in writes)
                    WriteLines(write.TemporaryPath, write.Lines);

                foreach (PendingWrite write in writes)
                {
                    if (write.DestinationExisted)
                    {
                        File.Replace(
                            write.TemporaryPath,
                            write.DestinationPath,
                            write.BackupPath,
                            true);
                    }
                    else
                    {
                        File.Move(write.TemporaryPath, write.DestinationPath);
                    }

                    write.Committed = true;
                }

                saveCompleted = true;
            }
            catch (Exception saveError)
            {
                Exception rollbackError;
                rollbackCompleted = TryRollback(writes, out rollbackError);

                if (!rollbackCompleted)
                {
                    throw new IOException(
                        "Saving failed and the original data could not be fully restored. Backup files have been left beside the data files.",
                        new AggregateException(saveError, rollbackError));
                }

                throw;
            }
            finally
            {
                foreach (PendingWrite write in writes)
                {
                    TryDelete(write.TemporaryPath);

                    if (saveCompleted || rollbackCompleted)
                        TryDelete(write.BackupPath);
                }
            }
        }

        private static bool TryRollback(List<PendingWrite> writes, out Exception rollbackError)
        {
            rollbackError = null;

            for (int index = writes.Count - 1; index >= 0; index--)
            {
                PendingWrite write = writes[index];
                if (!write.Committed)
                    continue;

                try
                {
                    if (write.DestinationExisted)
                    {
                        if (!File.Exists(write.BackupPath))
                            throw new IOException("A required backup file is missing: " + write.BackupPath);

                        File.Copy(write.BackupPath, write.DestinationPath, true);
                    }
                    else
                    {
                        if (File.Exists(write.DestinationPath))
                            File.Delete(write.DestinationPath);
                    }
                }
                catch (Exception ex)
                {
                    rollbackError = ex;
                    return false;
                }
            }

            return true;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static InvalidDataException InvalidRecord(string path, int lineNumber, string reason)
        {
            return new InvalidDataException(string.Format(
                CultureInfo.InvariantCulture,
                "Invalid record in {0} on line {1}: {2}.",
                Path.GetFileName(path),
                lineNumber,
                reason));
        }

        private sealed class AppointmentRecord
        {
            public Doctor Doctor { get; private set; }
            public Patient Patient { get; private set; }
            public DateTime Date { get; private set; }

            public AppointmentRecord(Doctor doctor, Patient patient, DateTime date)
            {
                Doctor = doctor;
                Patient = patient;
                Date = date;
            }
        }

        private sealed class AdministratorRecord
        {
            public string Username { get; private set; }
            public string PasswordSalt { get; private set; }
            public string PasswordHash { get; private set; }

            public AdministratorRecord(string username, string passwordSalt, string passwordHash)
            {
                Username = username;
                PasswordSalt = passwordSalt;
                PasswordHash = passwordHash;
            }
        }

        private sealed class PendingWrite
        {
            public string DestinationPath { get; private set; }
            public string TemporaryPath { get; private set; }
            public string BackupPath { get; private set; }
            public string[] Lines { get; private set; }
            public bool DestinationExisted { get; private set; }
            public bool Committed { get; set; }

            public PendingWrite(string destinationPath, string[] lines)
            {
                string token = Guid.NewGuid().ToString("N");

                DestinationPath = destinationPath;
                TemporaryPath = destinationPath + "." + token + ".tmp";
                BackupPath = destinationPath + "." + token + ".bak";
                Lines = lines;
                DestinationExisted = File.Exists(destinationPath);
            }
        }
    }

    static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static void CreateHash(string password, out string saltText, out string hashText)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(salt);
            }

            byte[] hash;
            using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                hash = deriveBytes.GetBytes(HashSize);
            }

            saltText = Convert.ToBase64String(salt);
            hashText = Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string saltText, string hashText)
        {
            if (password == null || !IsValidHashData(saltText, hashText))
                return false;

            byte[] salt = Convert.FromBase64String(saltText);
            byte[] expectedHash = Convert.FromBase64String(hashText);
            byte[] actualHash;

            using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                actualHash = deriveBytes.GetBytes(HashSize);
            }

            int difference = actualHash.Length ^ expectedHash.Length;
            int length = Math.Min(actualHash.Length, expectedHash.Length);

            for (int index = 0; index < length; index++)
                difference |= actualHash[index] ^ expectedHash[index];

            return difference == 0;
        }

        public static bool IsValidHashData(string saltText, string hashText)
        {
            if (string.IsNullOrWhiteSpace(saltText) || string.IsNullOrWhiteSpace(hashText))
                return false;

            try
            {
                return Convert.FromBase64String(saltText).Length == SaltSize
                    && Convert.FromBase64String(hashText).Length == HashSize;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
