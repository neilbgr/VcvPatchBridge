using System.Formats.Tar;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ZstdSharp;

namespace VcvPatchBridge;

/// <summary>
/// Reads/writes a .vcv patch file. A .vcv is either:
///   - raw JSON (used for patches shipped inside the Cardinal repo), or
///   - a zstd-compressed tar (pax) archive containing "patch.json" (+ optional extra files),
///     which is what Rack/Cardinal actually write when a user saves a patch from the UI.
/// Detection matches Rack's own check (src/patch.cpp): first 4 bytes == zstd magic.
/// </summary>
public sealed class PatchFile
{
    private static readonly byte[] ZstdMagic = { 0x28, 0xB5, 0x2F, 0xFD };

    public JsonObject Root { get; }

    /// <summary>Any non-"patch.json" files found in the source archive, preserved byte-for-byte on save.</summary>
    public List<(string Name, byte[] Data)> ExtraEntries { get; }

    public bool WasArchive { get; }

    private PatchFile(JsonObject root, List<(string Name, byte[] Data)> extraEntries, bool wasArchive)
    {
        Root = root;
        ExtraEntries = extraEntries;
        WasArchive = wasArchive;
    }

    public static PatchFile Load(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        bool isArchive = bytes.Length >= 4 && bytes.AsSpan(0, 4).SequenceEqual(ZstdMagic);

        if (!isArchive)
        {
            var root = ParseJson(bytes, path);
            return new PatchFile(root, new List<(string, byte[])>(), wasArchive: false);
        }

        using var compressedStream = new MemoryStream(bytes);
        using var tarStream = new DecompressionStream(compressedStream);
        using var tarCopy = new MemoryStream();
        tarStream.CopyTo(tarCopy);
        tarCopy.Position = 0;

        JsonObject? patchJson = null;
        var extras = new List<(string Name, byte[] Data)>();

        using var tarReader = new TarReader(tarCopy);
        TarEntry? entry;
        while ((entry = tarReader.GetNextEntry()) is not null)
        {
            if (entry.DataStream is null)
                continue;

            using var entryData = new MemoryStream();
            entry.DataStream.CopyTo(entryData);
            byte[] data = entryData.ToArray();

            if (NormalizeEntryName(entry.Name) == "patch.json")
                patchJson = ParseJson(data, path);
            else
                extras.Add((entry.Name, data));
        }

        if (patchJson is null)
            throw new InvalidDataException($"'{path}' is a tar+zstd archive but contains no patch.json entry.");

        return new PatchFile(patchJson, extras, wasArchive: true);
    }

    /// <summary>Strips a leading "./" (or repeated ones), matching tar writers that emit paths relative to "./".</summary>
    private static string NormalizeEntryName(string name)
    {
        while (name.StartsWith("./", StringComparison.Ordinal))
            name = name[2..];
        return name;
    }

    private static JsonObject ParseJson(byte[] bytes, string path)
    {
        JsonNode? node = JsonNode.Parse(bytes)
            ?? throw new InvalidDataException($"'{path}': empty or invalid JSON.");
        return node as JsonObject
            ?? throw new InvalidDataException($"'{path}': root JSON value is not an object.");
    }

    /// <summary>Always writes back as a tar(pax)+zstd archive, matching what Rack/Cardinal produce when saving from the UI.</summary>
    public void Save(string path)
    {
        byte[] patchJsonBytes = Encoding.UTF8.GetBytes(Root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        using var tarBuffer = new MemoryStream();
        using (var tarWriter = new TarWriter(tarBuffer, TarEntryFormat.Pax, leaveOpen: true))
        {
            var patchEntry = new PaxTarEntry(TarEntryType.RegularFile, "patch.json")
            {
                DataStream = new MemoryStream(patchJsonBytes),
            };
            tarWriter.WriteEntry(patchEntry);

            foreach (var (name, data) in ExtraEntries)
            {
                var extraEntry = new PaxTarEntry(TarEntryType.RegularFile, name)
                {
                    DataStream = new MemoryStream(data),
                };
                tarWriter.WriteEntry(extraEntry);
            }
        }

        tarBuffer.Position = 0;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var outFile = File.Create(path);
        using var compressionStream = new CompressionStream(outFile, level: 19);
        tarBuffer.CopyTo(compressionStream);
    }
}
