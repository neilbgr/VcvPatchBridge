using System.Text.Json.Nodes;

namespace VcvPatchBridge;

public sealed record DetectionResult(PatchOrigin Origin, List<ModuleSlug> CardinalOnlyModules, List<ModuleSlug> RackOnlyModules);

public static class PatchDetector
{
    public static DetectionResult Detect(PatchFile patch)
    {
        var cardinalHits = new List<ModuleSlug>();
        var rackHits = new List<ModuleSlug>();

        foreach (var module in EnumerateModules(patch.Root))
        {
            string? plugin = module["plugin"]?.GetValue<string>();
            string? model = module["model"]?.GetValue<string>();
            if (plugin is null || model is null)
                continue;

            // The "Cardinal" plugin slug is bundled by, and only exists inside, Cardinal itself.
            // Any module instance carrying it proves the patch was (re)saved by Cardinal.
            if (plugin == ModuleMap.CardinalPlugin)
            {
                cardinalHits.Add(new ModuleSlug(plugin, model));
            }
            else if (plugin == ModuleMap.CorePlugin && ModuleMap.AllCoreModels.Contains(model))
            {
                rackHits.Add(new ModuleSlug(plugin, model));
            }
        }

        PatchOrigin origin = cardinalHits.Count > 0
            ? PatchOrigin.Cardinal
            : rackHits.Count > 0
                ? PatchOrigin.Rack
                : PatchOrigin.Ambiguous;

        return new DetectionResult(origin, cardinalHits, rackHits);
    }

    public static IEnumerable<JsonObject> EnumerateModules(JsonObject root)
    {
        if (root["modules"] is not JsonArray modules)
            yield break;

        foreach (var m in modules)
        {
            if (m is JsonObject obj)
                yield return obj;
        }
    }
}
