using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>Authored metre coordinates: Blender X,Y,Z become Unity X,Z,Y.</summary>
    public static class TutorialRestStopLayout
    {
        public static readonly Vector3 Spawn = new Vector3(0,0,-9.2f);
        public static float SurfaceHeight(float x,float z)
        {
            if(Mathf.Abs(x)<.95f && z>=-9.4f && z<=-6.25f)
                return .24f+.22f*Mathf.Sin(Mathf.Clamp01((z+9.25f)/2.805f)*Mathf.PI);
            if(x>-7.4f && x<-3f && z>3.22f && z<6.85f)return .70f;
            if(Mathf.Abs(x+5.2f)<.95f && z>2.27f && z<=3.22f)
                return .21f+Mathf.Clamp(Mathf.Floor((z-2.27f)/.27f),0,3)*.13f;
            return .22f+.035f*Mathf.Sin(x*.9f)*Mathf.Cos(z*.6f);
        }
    }
}
