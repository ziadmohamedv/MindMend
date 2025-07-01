namespace Mind_Mend.Models.Users
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Therapist = "Therapist";
        public const string Patient = "Patient";
        public const string Doctor = "Doctor";
        
        public static readonly string[] All = new[] { Admin, Therapist, Patient, Doctor };
    }
}
