using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.Editor
{
    public static class WuxiaUiRegressionValidator
    {
        private readonly struct SafeAreaCase
        {
            public readonly string name;
            public readonly float width;
            public readonly float height;
            public readonly Rect safePixels;

            public SafeAreaCase(string name, float width, float height, Rect safePixels)
            {
                this.name = name;
                this.width = width;
                this.height = height;
                this.safePixels = safePixels;
            }
        }

        [MenuItem("37 MiniGame/Validate UI Safe Areas %#v")]
        public static void ValidateSafeAreas()
        {
            SafeAreaCase[] cases =
            {
                new SafeAreaCase("Portrait 540x960", 540f, 960f,
                    new Rect(0f, 34f, 540f, 892f)),
                new SafeAreaCase("Tall phone notch", 1179f, 2556f,
                    new Rect(0f, 102f, 1179f, 2320f)),
                new SafeAreaCase("Landscape 960x540", 960f, 540f,
                    new Rect(32f, 18f, 896f, 504f)),
                new SafeAreaCase("Wide phone cutout", 2556f, 1179f,
                    new Rect(132f, 63f, 2292f, 1074f))
            };

            foreach (SafeAreaCase item in cases)
            {
                Rect safe = ResponsiveGui.CalculateSafeArea(
                    item.safePixels, item.width, item.height);
                float logicalWidth = item.width /
                                     ResponsiveGui.CalculateScale(item.width, item.height);
                float logicalHeight = item.height /
                                      ResponsiveGui.CalculateScale(item.width, item.height);
                bool inside = safe.xMin >= -0.01f && safe.yMin >= -0.01f &&
                              safe.xMax <= logicalWidth + 0.01f &&
                              safe.yMax <= logicalHeight + 0.01f;
                bool touchRoom = safe.width >= 44f * 2f && safe.height >= 44f * 2f;
                if (!inside || !touchRoom)
                {
                    throw new InvalidOperationException(
                        $"Safe Area validation failed for {item.name}: {safe}");
                }
            }

            Debug.Log("UI Safe Area 校验通过：4 组横竖屏与异形屏安全区均保持在逻辑画布内，并保留 44x44 触摸空间。");
        }

        [MenuItem("37 MiniGame/Build WebGL UI Regression %#g")]
        public static void BuildWebGlUiRegression()
        {
            ValidateSafeAreas();
            PlayerSettings.WebGL.template = "PROJECT:WuxiaResponsive";
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("WebGL UI 回归构建没有可用场景。");
            }

            string output = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../Builds/WebGLUiRegression"));
            Directory.CreateDirectory(output);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"WebGL UI 回归构建失败：{report.summary.result}，" +
                    $"错误 {report.summary.totalErrors}，警告 {report.summary.totalWarnings}。");
            }

            Debug.Log(
                $"WebGL UI 回归构建通过：{output}，" +
                $"大小 {report.summary.totalSize} bytes，耗时 {report.summary.totalTime}。");
        }
    }
}
