namespace ATI.Services.Consul;

public sealed class K8sServiceOptions
{
    /// <summary>
    /// Url локального прокси k8s
    /// </summary>
    public required string BaseUrl { get; init; }

    public required string ServiceNameHeaderKey { get; init; }
}