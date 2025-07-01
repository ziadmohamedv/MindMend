using Mind_Mend.Models.Diagnosis;
using Microsoft.Extensions.Configuration;

namespace Mind_Mend.Services;

public class DiagnosisService : IDiagnosisService
{
    public Task<InitialDiagnosisResponse> GetInitialDiagnosis(InitialDiagnosisRequest request)
    {
        // Analyze symptoms and create assessment
        var assessment = AnalyzeSymptoms(request);
        
        // Determine urgency level
        var urgencyLevel = DetermineUrgencyLevel(request, assessment);
        
        // Generate recommendations
        var (immediate, lifestyle) = GenerateRecommendations(assessment, urgencyLevel);

        return Task.FromResult(new InitialDiagnosisResponse
        {
            Summary = assessment.Summary,
            PossibleConditions = assessment.PossibleConditions,
            RecommendedSpecialist = DetermineSpecialist(assessment.PossibleConditions),
            UrgencyLevel = urgencyLevel,
            ImmediateRecommendations = immediate,
            LifestyleRecommendations = lifestyle,
            RequiresImmediate24HourSupport = urgencyLevel == "Emergency" || request.HasSuicidalThoughts
        });
    }

    private (string Summary, List<string> PossibleConditions) AnalyzeSymptoms(InitialDiagnosisRequest request)
    {
        var possibleConditions = new List<string>();
        var symptoms = new List<string>();

        // Analyze sleep patterns
        if (!string.IsNullOrEmpty(request.SleepPatterns))
        {
            if (request.SleepPatterns.Contains("too much") || request.SleepPatterns.Contains("12+ hours"))
            {
                symptoms.Add("hypersomnia");
                possibleConditions.Add("Major Depressive Disorder");
            }
            if (request.SleepPatterns.Contains("can't sleep") || request.SleepPatterns.Contains("lie awake"))
            {
                symptoms.Add("insomnia");
                possibleConditions.Add("Generalized Anxiety Disorder");
            }
        }

        // Analyze emotional state and symptoms
        if (request.MainSymptoms.Contains("emptiness") || 
            request.MainSymptoms.Contains("meaningless") || 
            request.MainSymptoms.Contains("tired") ||
            request.MainSymptoms.Contains("exhausted"))
        {
            symptoms.Add("depressive symptoms");
            if (!possibleConditions.Contains("Major Depressive Disorder"))
            {
                possibleConditions.Add("Major Depressive Disorder");
            }
        }

        if (request.MainSymptoms.Contains("worry") || 
            request.MainSymptoms.Contains("overthink") || 
            request.MainSymptoms.Contains("anxiety") ||
            request.MainSymptoms.Contains("stress"))
        {
            symptoms.Add("anxiety symptoms");
            if (!possibleConditions.Contains("Generalized Anxiety Disorder"))
            {
                possibleConditions.Add("Generalized Anxiety Disorder");
            }
        }

        // Analyze impact on daily life
        if (request.DailyLifeImpact.Contains("avoid social") || 
            request.DailyLifeImpact.Contains("isolate"))
        {
            symptoms.Add("social withdrawal");
            possibleConditions.Add("Social Anxiety Disorder");
        }

        // Check for physical symptoms
        if (request.MainSymptoms.Contains("chest") || 
            request.MainSymptoms.Contains("heart") || 
            request.MainSymptoms.Contains("breathing"))
        {
            symptoms.Add("physical anxiety symptoms");
            if (!possibleConditions.Contains("Panic Disorder"))
            {
                possibleConditions.Add("Panic Disorder");
            }
        }

        // Generate summary
        var summary = $"Based on the assessment, you are experiencing {string.Join(", ", symptoms)}. ";
        summary += "These symptoms appear to be significantly impacting your daily life. ";
        
        if (request.SymptomHistory.Contains("year") || request.SymptomHistory.Contains("months"))
        {
            summary += "The chronic nature of these symptoms suggests the need for professional evaluation. ";
        }
        else
        {
            summary += "While these symptoms are recent, they warrant professional attention. ";
        }

        return (summary, possibleConditions);
    }

    private string DetermineUrgencyLevel(InitialDiagnosisRequest request, (string Summary, List<string> PossibleConditions) assessment)
    {
        if (request.HasSuicidalThoughts)
        {
            return "Emergency";
        }

        // Check for severe impairment in daily functioning
        if (request.DailyLifeImpact.Contains("can't function") || 
            request.DailyLifeImpact.Contains("unable to work") ||
            request.DailyLifeImpact.Contains("can't take care"))
        {
            return "High";
        }

        // Check for moderate impairment
        if (request.DailyLifeImpact.Contains("struggle") || 
            request.MainSymptoms.Contains("severe") ||
            assessment.PossibleConditions.Count > 2)
        {
            return "Medium";
        }

        return "Low";
    }

    private (List<string> Immediate, List<string> Lifestyle) GenerateRecommendations(
        (string Summary, List<string> PossibleConditions) assessment,
        string urgencyLevel)
    {
        var immediate = new List<string>();
        var lifestyle = new List<string>();

        // Immediate recommendations based on urgency
        switch (urgencyLevel)
        {
            case "Emergency":
                immediate.Add("Contact emergency services or a crisis hotline immediately");
                immediate.Add("Do not remain alone - reach out to a trusted friend or family member");
                immediate.Add("Schedule an urgent appointment with a mental health professional");
                break;
            case "High":
                immediate.Add("Schedule an appointment with a mental health professional within the next few days");
                immediate.Add("Consider taking a break from work or stressful activities");
                immediate.Add("Reach out to your support system for immediate assistance");
                break;
            case "Medium":
                immediate.Add("Schedule an appointment with a mental health professional within the next two weeks");
                immediate.Add("Start keeping a symptom and mood journal");
                break;
            case "Low":
                immediate.Add("Schedule a regular appointment with a mental health professional");
                immediate.Add("Begin monitoring your symptoms and their triggers");
                break;
        }

        // Add condition-specific lifestyle recommendations
        if (assessment.PossibleConditions.Contains("Major Depressive Disorder"))
        {
            lifestyle.Add("Establish a regular sleep schedule");
            lifestyle.Add("Engage in daily physical activity, even if brief");
            lifestyle.Add("Set small, achievable daily goals");
        }

        if (assessment.PossibleConditions.Contains("Generalized Anxiety Disorder"))
        {
            lifestyle.Add("Practice deep breathing exercises daily");
            lifestyle.Add("Limit caffeine and alcohol intake");
            lifestyle.Add("Create a worry journal to track and challenge anxious thoughts");
        }

        if (assessment.PossibleConditions.Contains("Social Anxiety Disorder"))
        {
            lifestyle.Add("Start with small social interactions in comfortable settings");
            lifestyle.Add("Practice relaxation techniques before social situations");
        }

        if (assessment.PossibleConditions.Contains("Panic Disorder"))
        {
            lifestyle.Add("Learn and practice grounding techniques");
            lifestyle.Add("Create a calm-down kit for panic attacks");
            lifestyle.Add("Avoid caffeine and other stimulants");
        }

        // Add general wellness recommendations
        lifestyle.Add("Maintain a regular exercise routine");
        lifestyle.Add("Practice good sleep hygiene");
        lifestyle.Add("Develop a support network");
        lifestyle.Add("Consider mindfulness or meditation practices");

        return (immediate, lifestyle);
    }

    private string DetermineSpecialist(List<string> conditions)
    {
        if (conditions.Contains("Major Depressive Disorder") || conditions.Contains("Bipolar Disorder"))
        {
            return "Psychiatrist";
        }
        
        if (conditions.Count > 2)
        {
            return "Psychiatrist";
        }
        
        return "Psychologist or Licensed Therapist";
    }
}