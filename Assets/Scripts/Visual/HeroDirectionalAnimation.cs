using System;
using UnityEngine;

namespace WuxiaRoguelite.Visual
{
    // Screen-space directions, counter-clockwise from right; shared by world and cave rendering.
    public static class HeroDirectionalArt
    {
        public const string ResourceRoot = "Characters/HeroEightDirections/";
        public static readonly string[] Directions =
            { "right", "up_right", "up", "up_left", "left", "down_left", "down", "down_right" };
        private static Sprite[][] runs;
        private static Sprite[][] idles;
        private static bool loaded;
        public static bool Available { get { Load(); return runs != null; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { loaded = false; runs = null; idles = null; }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            var newRuns = new Sprite[8][];
            var newIdles = new Sprite[8][];
            for (int i = 0; i < 8; i++)
            {
                newRuns[i] = Resources.LoadAll<Sprite>(ResourceRoot + "spr_hero_run_" + Directions[i] + "_8f_v01");
                newIdles[i] = Resources.LoadAll<Sprite>(ResourceRoot + "spr_hero_idle_" + Directions[i] + "_1f_v01");
                Array.Sort(newRuns[i], (a, b) => string.CompareOrdinal(a.name, b.name));
                if (newRuns[i].Length != 8 || newIdles[i].Length != 1)
                {
                    Debug.LogError("Hero directional art is incomplete: " + Directions[i] + ". Reimport Hero Eight Directions art.");
                    return;
                }
            }
            runs = newRuns;
            idles = newIdles;
        }

        public static Sprite Frame(int direction, bool moving, int frame)
        {
            if (!Available) return null;
            return moving ? runs[direction][frame % 8] : idles[direction][0];
        }
    }

    // Changing direction preserves stride phase; stopping resets it.
    public sealed class HeroDirectionalPlayback
    {
        public int Direction { get; private set; } = 6;
        public bool IsMoving { get; private set; }
        public float FrameClock { get; private set; }
        public Sprite CurrentSprite => HeroDirectionalArt.Frame(Direction, IsMoving, Mathf.FloorToInt(FrameClock));
        public void Reset() { Direction = 6; IsMoving = false; FrameClock = 0f; }

        public void Tick(Vector2 input, float deltaTime, float speedRatio = 1f)
        {
            bool moving = input.sqrMagnitude > 0.01f && speedRatio > 0.001f;
            if (input.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
                // Five degrees of hysteresis prevent stick noise at diagonal boundaries.
                if (Mathf.Abs(Mathf.DeltaAngle(Direction * 45f, angle)) > 27.5f)
                    Direction = (Mathf.RoundToInt(angle / 45f) + 8) % 8;
            }
            if (!moving || !IsMoving) FrameClock = 0f;
            else FrameClock = (FrameClock + Mathf.Max(0f, deltaTime) * 12f * speedRatio) % 8f;
            IsMoving = moving;
        }
    }
}
