using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml;
using System.Xml.Linq;

namespace Synentra.Infrastructure.Features.Secrets;

public sealed class PrivateKeyRingRepository : IXmlRepository
{
    private readonly string path;
    private readonly bool requireEncryption;

    public PrivateKeyRingRepository(string path, bool requireEncryption)
    {
        this.path = KeyRingSecurity.EnsureKeyRingDirectory(path);
        this.requireEncryption = requireEncryption;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        KeyRingSecurity.Validate(path);
        var elements = new List<XElement>();
        foreach (var file in Directory.EnumerateFiles(path, "*.xml"))
        {
            KeyRingSecurity.ValidateSecretFile(file);
            var element = KeyRingSecurity.ReadElement(file);
            if (requireEncryption) KeyRingSecurity.ValidateKeyEncryption(element);
            elements.Add(element);
        }
        return elements.AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        KeyRingSecurity.Validate(path);
        if (requireEncryption) KeyRingSecurity.ValidateKeyEncryption(element);

        // Ignore caller-supplied filenames. Publish complete XML via a same-directory rename.
        var id = Guid.NewGuid().ToString("N");
        var temporary = Path.Combine(path, id + ".tmp");
        var destination = Path.Combine(path, id + ".xml");
        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.WriteThrough
        };
        if (!OperatingSystem.IsWindows()) fileOptions.UnixCreateMode = KeyRingSecurity.PrivateFile;

        var created = false;
        try
        {
            using (var stream = new FileStream(temporary, fileOptions))
            {
                created = true;
                // Windows files inherit the validated directory's restricted ACL.
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
                {
                    Encoding = new System.Text.UTF8Encoding(false),
                    Indent = true,
                    CloseOutput = false
                }))
                    element.Save(writer);
                stream.Flush(flushToDisk: true);
            }

            KeyRingSecurity.ValidateSecretFile(temporary);
            // Apply the same parser limit on writes as on reads.
            _ = KeyRingSecurity.ReadElement(temporary);
            File.Move(temporary, destination, overwrite: false);
            KeyRingSecurity.ValidateSecretFile(destination);
        }
        finally
        {
            // Never remove a file we did not successfully create.
            if (created && File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
