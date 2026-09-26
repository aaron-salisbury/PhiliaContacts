using PhiliaContacts.Business.Modules.Integrations;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Integrations.Credentials;

/// <summary>
/// Stores PhiliaContacts credentials in Windows Credential Manager.
/// </summary>
/// <remarks>
/// Uses the Win32 CredWriteW, CredReadW, CredDeleteW, and CredFree APIs documented by Microsoft.
/// </remarks>
internal sealed class WindowsCredentialStore : ICredentialStore
{
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;
    private const uint CRED_TYPE_GENERIC = 1;
    private const int ERROR_NOT_FOUND = 1168;
    private const string TARGET_PREFIX = "PhiliaContacts:";

    public Task DeleteAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!CredDelete(Target(reference), CRED_TYPE_GENERIC, 0))
        {
            int error = Marshal.GetLastWin32Error();

            if (error != ERROR_NOT_FOUND)
            {
                throw new Win32Exception(error);
            }
        }

        return Task.CompletedTask;
    }

    public Task<CredentialMaterial?> GetAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!CredRead(Target(reference), CRED_TYPE_GENERIC, 0, out IntPtr credentialPointer))
        {
            int error = Marshal.GetLastWin32Error();

            if (error == ERROR_NOT_FOUND)
            {
                return Task.FromResult<CredentialMaterial?>(null);
            }

            throw new Win32Exception(error);
        }

        try
        {
            NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            byte[] bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            string value = Encoding.UTF8.GetString(bytes);
            Array.Clear(bytes);
            return Task.FromResult<CredentialMaterial?>(new CredentialMaterial(value));
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public Task StoreAsync(CredentialReference reference, CredentialMaterial material, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(material);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes = Encoding.UTF8.GetBytes(material.Value);
        IntPtr blob = Marshal.AllocHGlobal(bytes.Length);

        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);

            NativeCredential credential = new()
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = Target(reference),
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blob,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = "PhiliaContacts"
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Array.Clear(bytes);
            Marshal.FreeHGlobal(blob);
        }

        return Task.CompletedTask;
    }

    private static string Target(CredentialReference reference)
    {
        return TARGET_PREFIX + reference.Value;
    }

#pragma warning disable SYSLIB1054 // DllImport is retained until PhiliaContacts adopts an explicit policy for source-generated native interop and unsafe code.
    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree")]
    private static extern void CredFree(IntPtr buffer);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential credential, uint flags);
#pragma warning restore SYSLIB1054

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }
}
