using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using qoooo.Rendering.Sources;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace qoooo.Editor
{
    public static class PosterFontImporter
    {
        private const string MenuPath = "Qoooo/Fonts/Import Poster Fonts";
        private const string GeneratedFolder = "Assets/qoooo/Fonts/Generated";

        [MenuItem(MenuPath)]
        private static void ImportSelectedFonts()
        {
            var sources = Selection.objects
                .Where(value => value is Font or TMP_FontAsset)
                .Distinct()
                .ToArray();
            if (sources.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Import Poster Fonts",
                    "Select one or more .ttf/.otf Font assets or existing TMP Font Assets in the Project window.",
                    "OK");
                return;
            }

            var catalog = ResolveCatalog();
            if (catalog == null) return;

            Undo.RecordObject(catalog, "Import Poster Fonts");
            var added = 0;
            var skipped = 0;
            foreach (var source in sources)
            {
                var fontAsset = source switch
                {
                    TMP_FontAsset existing => existing,
                    Font font => GetOrCreateFontAsset(font),
                    _ => null
                };
                if (fontAsset == null || catalog.Contains(fontAsset))
                {
                    skipped++;
                    continue;
                }

                var label = source.name;
                var id = UniqueId(catalog, Slug(label));
                if (catalog.TryAdd(id, label, fontAsset)) added++;
                else skipped++;
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = catalog;
            Debug.Log($"[Poster Fonts] Added {added} font(s) to '{catalog.name}'; skipped {skipped}.", catalog);
        }

        private static PosterFontCatalog ResolveCatalog()
        {
            var selected = Selection.objects.OfType<PosterFontCatalog>().FirstOrDefault();
            if (selected != null) return selected;

            var guids = AssetDatabase.FindAssets("t:PosterFontCatalog");
            if (guids.Length == 1)
                return AssetDatabase.LoadAssetAtPath<PosterFontCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));

            EditorUtility.DisplayDialog(
                "Import Poster Fonts",
                guids.Length == 0
                    ? "No Poster Font Catalog was found. Create one with Assets > Create > Qoooo > Poster Font Catalog."
                    : "Multiple Poster Font Catalogs were found. Select the target catalog together with the fonts.",
                "OK");
            return null;
        }

        private static TMP_FontAsset GetOrCreateFontAsset(Font source)
        {
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var extension = Path.GetExtension(sourcePath);
            if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[Poster Fonts] '{source.name}' is not a .ttf or .otf asset.", source);
                return null;
            }

            EnsureFolder(GeneratedFolder);
            var guid = AssetDatabase.AssetPathToGUID(sourcePath);
            var suffix = guid.Length >= 8 ? guid[..8] : guid;
            var assetPath = $"{GeneratedFolder}/{SafeFileName(source.name)}-{suffix} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var fontAsset = TMP_FontAsset.CreateFontAsset(source);
            if (fontAsset == null)
            {
                Debug.LogError($"[Poster Fonts] Failed to create a TMP Font Asset from '{source.name}'.", source);
                return null;
            }

            fontAsset.name = $"{source.name} SDF";
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            for (var index = 0; index < fontAsset.atlasTextures.Length; index++)
            {
                var atlas = fontAsset.atlasTextures[index];
                if (atlas == null) continue;
                atlas.name = $"{fontAsset.name} Atlas{(index == 0 ? string.Empty : $" {index + 1}")}";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAsset.name} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static string UniqueId(PosterFontCatalog catalog, string baseId)
        {
            var candidate = baseId;
            for (var suffix = 2; catalog.ContainsId(candidate); suffix++)
                candidate = $"{baseId}-{suffix}";
            return candidate;
        }

        private static string Slug(string value)
        {
            var builder = new StringBuilder();
            var pendingSeparator = false;
            foreach (var character in (value ?? string.Empty).Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
                if (character <= 127 && char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0) builder.Append('-');
                    builder.Append(char.ToLowerInvariant(character));
                    pendingSeparator = false;
                }
                else
                {
                    pendingSeparator = true;
                }
            }
            return builder.Length == 0 ? "font" : builder.ToString();
        }

        private static string SafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string((value ?? "Font")
                .Select(character => invalid.Contains(character) || character is '/' or '\\' ? '-' : character)
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "Font" : sanitized.Trim();
        }
    }
}