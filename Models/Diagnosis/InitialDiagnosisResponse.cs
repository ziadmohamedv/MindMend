namespace Mind_Mend.Models.Diagnosis;

public class InitialDiagnosisResponse
{
    public string Summary { get; set; } = string.Empty;
    
    public List<string> PossibleConditions { get; set; } = new();
    
    public string RecommendedSpecialist { get; set; } = string.Empty;
    
    public string UrgencyLevel { get; set; } = string.Empty;
    
    public List<string> ImmediateRecommendations { get; set; } = new();
    
    public List<string> LifestyleRecommendations { get; set; } = new();
    
    public bool RequiresImmediate24HourSupport { get; set; }
    
    public string DisclaimerNote { get; set; } = 
        "This is an AI-generated initial assessment and should not be considered a final diagnosis. " +
        "Please consult with a mental health professional for a proper evaluation and treatment plan.";
}