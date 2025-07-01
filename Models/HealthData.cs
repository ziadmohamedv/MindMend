using System;

namespace Mind_Mend.Models
{
    public class HealthData
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public double HeartRate { get; set; }
        public int Steps { get; set; }
        public double CaloriesBurned { get; set; }
        public double Distance { get; set; }
        public int SleepDuration { get; set; } // in minutes
        public double StressLevel { get; set; }
        public required string Source { get; set; } // e.g., "CMF_WATCH_PRO_2"
    }
} 