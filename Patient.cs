using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hospital_Management_System
{
    sealed class Patient
    {
        private readonly List<Appointment> appointments;
        private readonly ReadOnlyCollection<Appointment> readOnlyAppointments;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Username { get; private set; }
        internal string PasswordSalt { get; private set; }
        internal string PasswordHash { get; private set; }
        public IReadOnlyList<Appointment> Appointments
        {
            get { return readOnlyAppointments; }
        }

        public Patient(int id, string name, string username, string passwordSalt, string passwordHash)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException("id", "Patient ID must be greater than zero.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Patient name is required.", "name");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Patient username is required.", "username");

            if (!PasswordHasher.IsValidHashData(passwordSalt, passwordHash))
                throw new ArgumentException("Patient password data is invalid.");

            Id = id;
            Name = name.Trim();
            Username = username.Trim();
            PasswordSalt = passwordSalt;
            PasswordHash = passwordHash;

            appointments = new List<Appointment>();
            readOnlyAppointments = appointments.AsReadOnly();
        }

        public bool MatchesCredentials(string username, string password)
        {
            return string.Equals(Username, username, StringComparison.OrdinalIgnoreCase)
                && PasswordHasher.Verify(password, PasswordSalt, PasswordHash);
        }

        public bool HasAppointmentAt(DateTime date)
        {
            return appointments.Any(appointment => appointment.Date == date);
        }

        internal void AddAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException("appointment");

            if (appointment.Patient.Id != Id)
                throw new InvalidOperationException("The appointment does not belong to this patient.");

            if (appointments.Contains(appointment))
                return;

            if (HasAppointmentAt(appointment.Date))
                throw new InvalidOperationException("The patient already has an appointment at this time.");

            appointments.Add(appointment);
        }

        internal void RemoveAppointment(Appointment appointment)
        {
            if (appointment != null)
                appointments.Remove(appointment);
        }
    }
}
