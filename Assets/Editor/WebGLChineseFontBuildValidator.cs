using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WuxiaRoguelite.Editor
{
    /// <summary>
    /// Keep every runtime-visible non-ASCII glyph inside both bundled Noto Sans
    /// subsets, and stop the build before a missing glyph can become garbled text.
    /// WebGL has no operating-system fallback, while the other targets must not
    /// silently depend on a machine-specific fallback either.
    /// Font fingerprints also prevent Unity from serving a stale native import.
    /// </summary>
    [InitializeOnLoad]
    public sealed class WebGLChineseFontBuildValidator : IPreprocessBuildWithReport
    {
        private const string RegularFontPath =
            "Assets/Resources/Fonts/NotoSansCJKsc-Regular-Subset.ttf";
        private const string BoldFontPath =
            "Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf";

        private static readonly HashSet<string> SerializedTextExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".asset", ".json", ".prefab", ".txt", ".unity", ".uss", ".uxml"
            };

        private static readonly Regex CSharpStringLiteral = new Regex(
            "(?:\\$?@|@\\$)?\"(?:\"\"|\\\\.|[^\"])*\"",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex EscapedUnicode = new Regex(
            @"\\u([0-9a-fA-F]{4})",
            RegexOptions.Compiled);

        private static readonly string[] CorruptedTextMarkers =
        {
            "\uFFFD",
            "锟斤拷",
            "烫烫烫",
            "屯屯屯",
            "â€"
        };

        private static readonly string[] DeprecatedRuntimeTerms =
        {
            "九尾妖姬"
        };

        static WebGLChineseFontBuildValidator()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += ValidateAfterDomainReload;
            }
        }

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        [MenuItem("37 MiniGame/Validate Chinese Fonts")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow(logSuccess: true);
        }

        public static void ValidateOrThrow()
        {
            ValidateOrThrow(logSuccess: false);
        }

        private static void ValidateOrThrow(bool logSuccess)
        {
            List<string> textIssues = FindRuntimeTextIssues();
            if (textIssues.Count > 0)
            {
                throw new BuildFailedException(
                    "构建已停止：运行时文案包含乱码标记、无效 UTF-8 或过期专名。\n" +
                    string.Join("\n", textIssues));
            }

            Font regular = LoadBundledFont(RegularFontPath);
            Font bold = LoadBundledFont(BoldFontPath);
            SortedSet<char> requiredCharacters = CollectRequiredCharacters();

            string missingRegular = FindMissingCharacters(regular, requiredCharacters);
            string missingBold = FindMissingCharacters(bold, requiredCharacters);
            if (missingRegular.Length == 0 && missingBold.Length == 0)
            {
                if (logSuccess)
                {
                    Debug.Log(
                        $"中文字体校验通过：检查 {requiredCharacters.Count} 个非 ASCII 字形。" +
                        "Wuxia Sans SC 常规体与粗体均已覆盖；运行时文本完整性检查通过。");
                }

                return;
            }

            StringBuilder message = new StringBuilder(
                "构建已停止：内置中文字体缺少运行时字形，不能依赖系统字体兜底。\n");
            if (missingRegular.Length > 0)
            {
                message.AppendLine($"常规体缺少：{FormatMissingCharacters(missingRegular)}");
            }

            if (missingBold.Length > 0)
            {
                message.AppendLine($"粗体缺少：{FormatMissingCharacters(missingBold)}");
            }

            message.Append(
                "请运行 Tools/update_chinese_font_subset.py 更新两个字体子集后重新构建。" +
                "详细步骤见 Assets/Resources/Fonts/README.md。");
            throw new BuildFailedException(message.ToString());
        }

        private static void ValidateAfterDomainReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ValidateAfterDomainReload;
                return;
            }

            try
            {
                ValidateOrThrow();
            }
            catch (BuildFailedException exception)
            {
                Debug.LogError(
                    "检测到中文字体可能显示为方框或乱码。请在继续测试前修复。\n" +
                    exception.Message);
            }
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

            string expectedFingerprint = BuildFontFingerprint(path);
            if (!string.Equals(importer.userData, expectedFingerprint, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"构建已停止：字体 {path} 的 Unity 导入指纹已过期。" +
                    "请运行 Tools/update_chinese_font_subset.py，或重新导入字体后再构建。");
            }

            return font;
        }

        private static string BuildFontFingerprint(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                return "wuxia-font-sha256=" +
                       BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static List<string> FindRuntimeTextIssues()
        {
            List<string> issues = new List<string>();
            const string root = "Assets";
            if (!Directory.Exists(root))
            {
                return issues;
            }

            UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);
            foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (IsEditorOnlyPath(path))
                {
                    continue;
                }

                string extension = Path.GetExtension(path);
                if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
                    !SerializedTextExtensions.Contains(extension))
                {
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(path, strictUtf8);
                }
                catch (DecoderFallbackException)
                {
                    issues.Add($"{path}：文件不是有效 UTF-8。统一保存为 UTF-8 后重试。");
                    continue;
                }

                foreach (string marker in CorruptedTextMarkers)
                {
                    if (text.Contains(marker))
                    {
                        issues.Add($"{path}：检测到疑似乱码标记「{marker}」。");
                    }
                }

                foreach (string term in DeprecatedRuntimeTerms)
                {
                    if (text.Contains(term))
                    {
                        issues.Add($"{path}：检测到过期专名「{term}」，请改用 GameTextCatalog。");
                    }
                }
            }

            return issues;
        }

        private static SortedSet<char> CollectRequiredCharacters()
        {
            SortedSet<char> characters = new SortedSet<char>();
            const string root = "Assets";
            if (!Directory.Exists(root))
            {
                return characters;
            }

            foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (IsEditorOnlyPath(path))
                {
                    continue;
                }

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

            return characters;
        }

        private static bool IsEditorOnlyPath(string path)
        {
            string normalized = path.Replace('\\', '/');
            return normalized.StartsWith("Assets/Editor/", StringComparison.OrdinalIgnoreCase) ||
                   normalized.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0;
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
