using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using System.Xml;
using System.Xml.Linq;

namespace Synentra.Infrastructure.Features.Secrets;

public static class KeyRingSecurity
{
    internal const long MaximumXmlCharacters = 1024 * 1024;

    private const UnixFileMode PrivateDirectory =
        UnixFileMode.UserRead |
        UnixFileMode.UserWrite |
        UnixFileMode.UserExecute;

    internal const UnixFileMode PrivateFile =
        UnixFileMode.UserRead |
        UnixFileMode.UserWrite;

    private const UnixFileMode Forbidden =
        UnixFileMode.GroupRead |
        UnixFileMode.GroupWrite |
        UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead |
        UnixFileMode.OtherWrite |
        UnixFileMode.OtherExecute;

    public static string ResolvePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.GetFullPath(path, AppContext.BaseDirectory);
    }

    public static string EnsureKeyRingDirectory(string path)
    {
        var fullPath = ResolvePath(path);

        RejectLinks(fullPath);

        var directory = new DirectoryInfo(fullPath);

        if (!directory.Exists)
        {
            // Require deployment to provision the parent. Do not create an entire
            // unchecked directory tree under an arbitrary service identity.
            var parent = directory.Parent;

            if (parent is null || !parent.Exists)
                throw new InvalidOperationException("Provision the key-ring parent directory first.");

            if (OperatingSystem.IsWindows())
            {
                CreateWindowsDirectory(directory);
            }
            else
            {
                Directory.CreateDirectory(fullPath, PrivateDirectory);
            }
        }

        // Never repair permissions silently at startup.
        Validate(fullPath);

        return fullPath;
    }

    public static void Validate(string path)
    {
        var fullPath = ResolvePath(path);

        RejectLinks(fullPath);

        var directory = new DirectoryInfo(fullPath);

        if (!directory.Exists)
            throw new InvalidOperationException("Key-ring directory does not exist.");

        if (OperatingSystem.IsWindows())
        {
            ValidateWindowsAcl(directory.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner));
        }
        else
        {
            ValidateUnixPermissions(fullPath);
        }
    }

    public static void ValidateSecretFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Path.IsPathFullyQualified(path))
            throw new InvalidOperationException("Secret file path must be absolute.");

        var fullPath = Path.GetFullPath(path);

        RejectLinks(fullPath);

        var info = new FileInfo(fullPath);

        if (!info.Exists || (info.Attributes & FileAttributes.Directory) != 0)
            throw new InvalidOperationException("Secret file must be an existing file.");

        if (OperatingSystem.IsWindows())
        {
            ValidateWindowsAcl(info.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner));
        }
        else
        {
            ValidateUnixPermissions(fullPath);
        }
    }

    // Reject existing links in the complete path, including ancestor directories.
    // This is a path-based check, not a race-proof handle-based filesystem boundary.
    internal static void RejectLinks(string fullPath)
    {
        string? current = fullPath;

        while (current is not null)
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidOperationException("Protected paths cannot contain reparse points or symbolic links.");
                }
            }
            catch (FileNotFoundException)
            {
                // The path component does not exist yet.
            }
            catch (DirectoryNotFoundException)
            {
                // The path component does not exist yet.
            }

            current = Path.GetDirectoryName(current);
        }
    }

    private static void ValidateUnixPermissions(string path)
    {
        if ((File.GetUnixFileMode(path) & Forbidden) != 0)
        {
            throw new InvalidOperationException("Protected storage grants group or other permissions.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void CreateWindowsDirectory(DirectoryInfo directory)
    {
        var acl = new DirectorySecurity();

        acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        foreach (var principal in TrustedWindowsPrincipals())
        {
            acl.AddAccessRule(
                new FileSystemAccessRule(
                    principal,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit |
                    InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
        }

        directory.Create(acl);
    }

    [SupportedOSPlatform("windows")]
    private static SecurityIdentifier[] TrustedWindowsPrincipals()
    {
        using var identity = WindowsIdentity.GetCurrent();

        return
        [
            identity.User
                ?? throw new InvalidOperationException("Cannot resolve service identity."),

            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),

            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)
        ];
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateWindowsAcl(FileSystemSecurity acl)
    {
        var trusted = TrustedWindowsPrincipals()
            .Select(sid => sid.Value)
            .ToHashSet(StringComparer.Ordinal);

        var descriptor = new RawSecurityDescriptor(acl.GetSecurityDescriptorBinaryForm(), 0);

        if (descriptor.DiscretionaryAcl is null)
        {
            throw new InvalidOperationException("Protected storage cannot have a null DACL.");
        }

        if (acl.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner || !trusted.Contains(owner.Value))
        {
            throw new InvalidOperationException("Protected storage has an untrusted owner.");
        }

        // Conservative allow-list, not an effective-access calculation.
        // Reject unexpected allow ACEs even when a deny ACE might override them.
        foreach (FileSystemAccessRule rule in
            acl.GetAccessRules(
                includeExplicit: true,
                includeInherited: true,
                targetType: typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType == AccessControlType.Allow &&
                rule.FileSystemRights != 0 &&
                !trusted.Contains(rule.IdentityReference.Value))
            {
                throw new InvalidOperationException(
                    "Protected storage grants access to an untrusted principal. " +
                    $"SID: {rule.IdentityReference.Value}; " +
                    $"Rights: {rule.FileSystemRights}; " +
                    $"Inherited: {rule.IsInherited}.");
            }
        }
    }

    public static void ValidateEncryptedKeys(string path)
    {
        var fullPath = ResolvePath(path);

        Validate(fullPath);

        // Examine all repository XML, not only friendly filenames beginning 'key-'.
        foreach (var file in Directory.EnumerateFiles(fullPath, "*.xml"))
        {
            ValidateSecretFile(file);

            var element = ReadElement(file);

            ValidateKeyEncryption(element);
        }
    }

    internal static XElement ReadElement(string path)
    {
        using var reader = XmlReader.Create(
            path,
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumXmlCharacters
            });

        return XDocument.Load(reader).Root ?? throw new InvalidOperationException("Key-ring XML has no root element.");
    }

    internal static void ValidateKeyEncryption(XElement element)
    {
        // Revocation records are metadata and do not contain key material.
        if (element.Name == "revocation")
            return;

        if (element.Name != "key")
        {
            throw new InvalidOperationException("Unrecognized key-ring XML record.");
        }

        if (element.Descendants().Any(element => element.Name.LocalName == "masterKey"))
        {
            throw new InvalidOperationException("Plaintext keys require offline reprotection before startup.");
        }

        if (!element.Descendants().Any(element => element.Name.LocalName == "encryptedSecret"))
        {
            throw new InvalidOperationException("Key-ring encryption structure is not recognized.");
        }
    }
}