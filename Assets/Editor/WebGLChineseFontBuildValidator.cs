using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WuxiaRoguelite.Editor
{
    /// <summary>
    /// WebGL cannot fall back to an operating-system Chinese font. Keep every
    /// runtime-visible non-ASCII glyph inside both bundled Noto Sans subsets,
    /// and stop the build before a missing glyph can become garbled text.
    /// </summary>
    public sealed class WebGLChineseFontBuildValidator : IPreprocessBuildWithReport
    {
        private const string RegularFontPath =
            "Assets/Resources/Fonts/NotoSansCJKsc-Regular-Subset.ttf";
        private const string BoldFontPath =
            "Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf";

        private static readonly string[] RuntimeTextRoots =
        {
            "Assets/Scripts",
            "Assets/Scenes",
            "Assets/Resources"
        };

        private static readonly HashSet<string> SerializedTextExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".asset", ".json", ".txt", ".unity"
            };

        private static readonly Regex CSharpStringLiteral = new Regex(
            "(?:\\$?@|@\\$)?\"(?:\"\"|\\\\.|[^\"])*\"",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex EscapedUnicode = new Regex(
            @"\\u([0-9a-fA-F]{4})",
            RegexOptions.Compiled);

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                ValidateOrThrow();
            }
        }

        [MenuItem("37 MiniGame/Validate WebGL Chinese Fonts")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log("WebGL 中文字体校验通过：运行时文本所需字形已全部打包。");
        }

        public static void ValidateOrThrow()
        {
            Font regular = LoadBundledFont(RegularFontPath);
            Font bold = LoadBundledFont(BoldFontPath);
            SortedSet<char> requiredCharacters = CollectRequiredCharacters();

            string missingRegular = FindMissingCharacters(regular, requiredCharacters);
            string missingBold = FindMissingCharacters(bold, requiredCharacters);
            if (missingRegular.Length == 0 && missingBold.Length == 0)
            {
                Debug.Log(
                    $"WebGL 中文字体校验通过：检查 {requiredCharacters.Count} 个非 ASCII 字形。" +
                    "Noto Sans SC 常规体与粗体均已覆盖。");
                return;
            }

            StringBuilder message = new StringBuilder(
                "WebGL 构建已停止：内置中文字体缺少运行时字形。WebGL 无法使用系统字体兜底。\n");
            if (missingRegular.Length > 0)
            {
                message.AppendLine($"常规体缺少：{FormatMissingCharacters(missingRegular)}");
            }

            if (missingBold.Length > 0)
            {
                message.AppendLine($"粗体缺少：{FormatMissingCharacters(missingBold)}");
            }

            message.Append(
                "请按 Assets/Resources/Fonts/README.md 更新两个字体子集后重新构建。");
            throw new BuildFailedException(message.ToString());
        }

        private static Font LoadBundledFont(string path)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null)
            {
                throw new BuildFailedException(
                    $"WebGL 构建已停止：缺少内置中文字体资源 {path}");
            }

            TrueTypeFontImporter importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (importer == null || !importer.includeFontData)
            {
                throw new BuildFailedException(
                    $"WebGL 构建已停止：字体 {path} 必须启用 Include Font Data。");
            }

            return font;
        }

        private static SortedSet<char> CollectRequiredCharacters()
        {
            SortedSet<char> characters = new SortedSet<char>();
            foreach (string root in RuntimeTextRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(path);
                    if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        CollectCSharpStringCharacters(File.ReadAllText(path), characters);
                    }
                    else if (SerializedTextExtensions.Contains(extension))
                    {
                        CollectVisibleCharacters(File.ReadAllText(path), characters);
                    }
                }
            }

            return characters;
        }

        private static void CollectCSharpStringCharacters(
            string source,
            ISet<char> characters)
        {
            MatchCollection matches = CSharpStringLiteral.Matches(source);
            foreach (Match match in matches)
            {
                CollectVisibleCharacters(match.Value, characters);
            }
        }

        private static void CollectVisibleCharacters(string text, ISet<char> characters)
        {
            foreach (char character in text)
            {
                if (character > 127 && !char.IsSurrogate(character))
                {
                    characters.Add(character);
                }
            }

            MatchCollection escapedCharacters = EscapedUnicode.Matches(text);
            foreach (Match match in escapedCharacters)
            {
                char character = (char)Convert.ToInt32(match.Groups[1].Value, 16);
                if (character > 127 && !char.IsSurrogate(character))
                {
                    characters.Add(character);
                }
            }
        }

        private static string FindMissingCharacters(Font font, IEnumerable<char> required)
        {
            StringBuilder missing = new StringBuilder();
            foreach (char character in required)
            {
                if (!font.HasCharacter(character))
                {
                    missing.Append(character);
                }
            }

            return missing.ToString();
        }

        private static string FormatMissingCharacters(string characters)
        {
            StringBuilder formatted = new StringBuilder();
            foreach (char character in characters)
            {
                if (formatted.Length > 0)
                {
                    formatted.Append(' ');
                }

                formatted.Append(character);
                formatted.Append("(U+");
                formatted.Append(((int)character).ToString("X4"));
                formatted.Append(')');
            }

            return formatted.ToString();
        }
    }
}
