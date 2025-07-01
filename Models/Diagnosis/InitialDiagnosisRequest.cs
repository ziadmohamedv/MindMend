using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Diagnosis;

public class InitialDiagnosisRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Age { get; set; }

    [Required]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MinLength(20)]
    public string MainSymptoms { get; set; } = string.Empty;

    [Required]
    [MinLength(20)]
    public string SymptomHistory { get; set; } = string.Empty;

    public string? PreviousDiagnoses { get; set; }

    public string? CurrentMedications { get; set; }

    [Required]
    [MinLength(20)]
    public string DailyLifeImpact { get; set; } = string.Empty;

    public string? FamilyHistory { get; set; }

    [Required]
    public bool HasSuicidalThoughts { get; set; }

    public string? StressTriggers { get; set; }

    public string? CopingMechanisms { get; set; }

    public string? SleepPatterns { get; set; }

    public string? SubstanceUse { get; set; }
} 