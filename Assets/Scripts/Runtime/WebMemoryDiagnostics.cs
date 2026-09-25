using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace WuxiaRoguelite.Runtime
{
    // Unity allocation figures are not the browser's total process/GPU/audio memory.
    public static class WebMemoryDiagnostics
    {
        [Serializable]
        public sealed class Snapshot
        {
            public string stage, scene;
            public int scenes, textures;
            public long allocatedBytes, reservedBytes, textureBytes;
            public float realtime;
        }
        public static readonly List<Snapshot> Samples = new List<Snapshot>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Samples.Clear();

        public static void Capture(string stage)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            long textureBytes = 0;
            foreach (var texture in textures) textureBytes += Profiler.GetRuntimeMemorySizeLong(texture);
            var sample = new Snapshot {
                stage = stage, scene = SceneManager.GetActiveScene().name,
                scenes = SceneManager.sceneCount, textures = textures.Length,
                allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                reservedBytes = Profiler.GetTotalReservedMemoryLong(), textureBytes = textureBytes,
                realtime = Time.realtimeSinceStartup
            };
            if (Samples.Count >= 64) Samples.RemoveAt(0);
            Samples.Add(sample);
            Debug.Log("WebMemory " + JsonUtility.ToJson(sample));
#endif
        }
    }
}
