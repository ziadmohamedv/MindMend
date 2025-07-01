using Mind_Mend.Models.Diagnosis;

namespace Mind_Mend.Services;

public interface IDiagnosisService
{
    Task<InitialDiagnosisResponse> GetInitialDiagnosis(InitialDiagnosisRequest request);
} 