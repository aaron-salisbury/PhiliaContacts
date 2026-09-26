using PhiliaContacts.Business.Modules.Integrations;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Integrations.Credentials;

/// <summary>
/// Stores PhiliaContacts credentials in the Linux desktop Secret Service through libsecret's secret-tool client.
/// </summary>
internal sealed class LinuxSecretServiceCredentialStore : ICredentialStore
{
    private const string ATTRIBUTE_NAME = "PhiliaContacts-credential";
    private const string SECRET_TOOL = "secret-tool";

    public async Task DeleteAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        ProcessResult result = await RunAsync(["clear", ATTRIBUTE_NAME, reference.Value], null, cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Secret Service failed to delete the PhiliaContacts credential: {result.Error}");
        }
    }

    public async Task<CredentialMaterial?> GetAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        ProcessResult result = await RunAsync(["lookup", ATTRIBUTE_NAME, reference.Value], null, cancellationToken);

        if (result.ExitCode != 0 || string.IsNullOrEmpty(result.Output))
        {
            return null;
        }

        return new CredentialMaterial(result.Output.TrimEnd('\r', '\n'));
    }

    public async Task StoreAsync(
        CredentialReference reference,
        CredentialMaterial material,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(material);

        ProcessResult result = await RunAsync(
            ["store", $"--label=PhiliaContacts ({reference.Value})", ATTRIBUTE_NAME, reference.Value],
            material.Value,
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Secret Service failed to store the PhiliaContacts credential: {result.Error}");
        }
    }

    private static async Task<ProcessResult> RunAsync(
        string[] arguments,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = SECRET_TOOL,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start secret-tool.");

            if (standardInput is not null)
            {
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken);
                process.StandardInput.Close();
            }

            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return new ProcessResult(process.ExitCode, output, error);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException(
                "Linux Secret Service support requires the libsecret secret-tool command.",
                exception);
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
