using System;

namespace Hospital_Management_System
{
    sealed class Appointment
    {
        public Doctor Doctor { get; private set; }
        public Patient Patient { get; private set; }
        public DateTime Date { get; private set; }

        public Appointment(Doctor doctor, Patient patient, DateTime date)
        {
            if (doctor == null)
                throw new ArgumentNullException("doctor");

            if (patient == null)
                throw new ArgumentNullException("patient");

            if (date == DateTime.MinValue)
                throw new ArgumentException("An appointment date is required.", "date");

            Doctor = doctor;
            Patient = patient;
            Date = date;
        }
    }
}
