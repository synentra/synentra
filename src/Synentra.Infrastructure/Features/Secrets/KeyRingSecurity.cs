using System.Security.AccessControl;
using System.Security.Principal;

namespace Synentra.Infrastructure.Features.Secrets;

public static class KeyRingSecurity
{
    public static void ValidateEncryptedKeys(string path)
    {
        foreach (var key in Directory.EnumerateFiles(path, "key-*.xml"))
        {
            if (new FileInfo(key).Attributes.HasFlag(FileAttributes.ReparsePoint)) 
                throw new InvalidOperationException("Key-ring files cannot be reparse points.");

            using var reader = System.Xml.XmlReader.Create(key, 
                new System.Xml.XmlReaderSettings { 
                    DtdProcessing = System.Xml.DtdProcessing.Prohibit, 
                    XmlResolver = null, 
                    MaxCharactersInDocument = 1024 * 1024 
                });

            var document = System.Xml.Linq.XDocument.Load(reader);
            if (document.Descendants().Any(e => e.Name.LocalName == "masterKey"))
                throw new InvalidOperationException("Unencrypted existing key-ring material requires offline reprotection before production startup.");

            if (!document.Descendants().Any(e => e.Name.LocalName == "encryptedSecret"))
                throw new InvalidOperationException("Key-ring encryption format is not recognized.");
        }
    }

    public static void ValidateSecretFile(string path)
    {
        var info = new FileInfo(path);
        if (!Path.IsPathFullyQualified(path) || !info.Exists || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidOperationException("Secret file must be an absolute protected file.");

        if (OperatingSystem.IsWindows())
        {
            foreach (FileSystemAccessRule rule in info.GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && rule.FileSystemRights != 0 &&
                    rule.IdentityReference.Value is "S-1-1-0" or "S-1-5-11" or "S-1-5-32-545" or "S-1-5-32-546")
                    throw new InvalidOperationException("Secret file grants access to a broad principal.");
        }
        else if ((File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite)) != 0)
            throw new InvalidOperationException("Secret file grants access to group or other users.");
    }

    public static void Validate(string path)
    {
        var fullPath = Path.GetFullPath(path, AppContext.BaseDirectory);

        //if (!Path.IsPathFullyQualified(fullPath) || new DirectoryInfo(fullPath).Attributes.HasFlag(FileAttributes.ReparsePoint))
        //    throw new InvalidOperationException("Key-ring directory must be an absolute protected directory.");

        if (OperatingSystem.IsWindows())
        {
            var acl = new DirectoryInfo(fullPath).GetAccessControl();
            foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && rule.FileSystemRights != 0 &&
                    rule.IdentityReference.Value is "S-1-1-0" or "S-1-5-11" or "S-1-5-32-545" or "S-1-5-32-546")
                    throw new InvalidOperationException("Key-ring directory grants access to a broad principal.");
        }
        else if ((File.GetUnixFileMode(fullPath) & (UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute)) != 0)
            throw new InvalidOperationException("Key-ring directory must be accessible only to the service account.");
    }
}
