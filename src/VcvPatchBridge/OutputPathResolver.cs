namespace VcvPatchBridge;

/// <summary>Derives an output patch path from the input path and the conversion target when none was given explicitly.</summary>
public static class OutputPathResolver
{
    public static string Resolve(string inputPath, PatchOrigin target, bool force)
    {
        string fullInput = Path.GetFullPath(inputPath);
        string dir = Path.GetDirectoryName(fullInput) ?? ".";
        string baseName = Path.GetFileNameWithoutExtension(fullInput);
        string ext = Path.GetExtension(fullInput);
        string slug = target switch
        {
            PatchOrigin.Cardinal => "cardinal",
            PatchOrigin.Rack => "rack",
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Target must be Cardinal or Rack."),
        };

        string candidate = Path.Combine(dir, $"{baseName}.{slug}{ext}");
        if (force || !File.Exists(candidate))
        {
            return candidate;
        }

        for (int n = 1; ; n++)
        {
            string numbered = Path.Combine(dir, $"{baseName}.{slug}.x{n}{ext}");
            if (!File.Exists(numbered))
            {
                return numbered;
            }
        }
    }
}