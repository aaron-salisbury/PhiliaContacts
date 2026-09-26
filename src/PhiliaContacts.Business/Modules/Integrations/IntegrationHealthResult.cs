namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record IntegrationHealthResult(IntegrationHealth Health, string Diagnostic)
{
    public static IntegrationHealthResult Healthy()
    {
        return new IntegrationHealthResult(IntegrationHealth.Healthy, string.Empty);
    }
}
