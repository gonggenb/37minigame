using System;
using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>All positions are Unity meters; generated from the same curves as the Blender master.</summary>
    public static class PingchuanTownLayout
    {
        public const float Scale = .4f;
        public static readonly Vector3 Spawn = Point(-64,-103);
        [Serializable] public class River { public Vector2[] points; public float width; }
        [Serializable] public class Road : River { public string name; }
        [Serializable] public class Bridge
        {
            public string name; public Vector2 center; public float angle,length,width; public bool stone;
            public Vector3 Position => new Vector3(center.x,0,center.y);
            public Vector3 Forward => Quaternion.Euler(0,angle,0)*Vector3.forward;
            public Vector3 Right => Quaternion.Euler(0,angle,0)*Vector3.right;
            public Vector3 Local(Vector3 p) => Quaternion.Euler(0,-angle,0)*(p-Position);
        }
        [Serializable] public class LayoutData { public River[] rivers; public Road[] roads; public Bridge[] bridges; }
        private static LayoutData data;
        public static LayoutData Data => data ??= JsonUtility.FromJson<LayoutData>(Resources.Load<TextAsset>("PingchuanTownLayout").text);
        public static Vector3 Point(float x,float y)=>new Vector3(x*Scale,0,y*Scale);
        public static float Distance(Vector2 p, Vector2 a, Vector2 b)
        { Vector2 d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude))); }
        public static bool IsBridge(Vector3 p,float inset=0)
        {
            foreach(var b in Data.bridges){var l=b.Local(p);if(Mathf.Abs(l.x)<b.width*.5f-inset && Mathf.Abs(l.z)<b.length*.5f+.2f)return true;}
            return false;
        }
        public static bool IsRiver(Vector3 p,float padding=0)
        {
            if(IsBridge(p,padding))return false;
            Vector2 q=new Vector2(p.x,p.z);
            foreach(var r in Data.rivers)for(int i=1;i<r.points.Length;i++)
                if(Distance(q,r.points[i-1],r.points[i])<r.width*.5f+padding)return true;
            return false;
        }
        public static float TerrainHeight(float x,float z)
        { x/=Scale;z/=Scale;return Scale*(.38f+.13f*Mathf.Sin(x*.054f)*Mathf.Cos(z*.064f)+.1f*Mathf.Sin((x+z)*.043f)); }
        public static float SurfaceHeight(float x,float z)
        {
            Vector3 p=new Vector3(x,0,z);float h=TerrainHeight(x,z)+.06f;
            foreach(var b in Data.bridges)
            {
                var l=b.Local(p);
                if(Mathf.Abs(l.x)<b.width*.5f && Mathf.Abs(l.z)<b.length*.5f)
                    return TerrainHeight(b.center.x,b.center.y)+Scale*(.14f+.72f*Mathf.Cos(l.z/b.length*Mathf.PI)+(b.stone?.125f:.09f));
            }
            // Hand-laid street paving and the sparring platform are raised above the basin.
            if(x>-29*Scale && x<-22*Scale && z>-33*Scale && z<37*Scale)h+=.035f;
            if(x>36*Scale && x<44*Scale && z>-20*Scale && z<-13.4f*Scale)h+=.22f;
            return h;
        }
    }
}
