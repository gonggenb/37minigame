using System;
using System.Reflection;
using NUnit.Framework;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class PresentationLayoutResolverTests
    {
        [TestCase(720, 1280, "Portrait")]
        [TestCase(1080, 1920, "Portrait")]
        [TestCase(1000, 1000, "Portrait")]
        [TestCase(1280, 720, "Landscape")]
        [TestCase(1920, 1080, "Landscape")]
        public void Resolve_UsesPortraitForTallOrSquareScreens(
            int width,
            int height,
            string expected)
        {
            Type resolver = Type.GetType(
                "WuxiaRoguelite.Application.Presentation.PresentationLayoutResolver, WuxiaRoguelite.Application");
            Assert.That(resolver, Is.Not.Null, "缺少 PresentationLayoutResolver。");

            MethodInfo resolve = resolver.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
            Assert.That(resolve, Is.Not.Null, "PresentationLayoutResolver.Resolve 必须是公开静态方法。");

            object result = resolve.Invoke(null, new object[] { width, height });
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ToString(), Is.EqualTo(expected));
        }
    }
}
