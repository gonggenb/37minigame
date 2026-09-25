using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>Fades foreground roofs and vegetation around the player in the level two shader.</summary>
    public sealed class PingchuanTownVisibility : MonoBehaviour
    {
        public Transform player;
        private static readonly int PositionId=Shader.PropertyToID("_PingchuanPlayerPosition");
        private void LateUpdate()
        {
            if(player!=null)Shader.SetGlobalVector(PositionId,new Vector4(player.position.x,player.position.y,player.position.z,1));
        }
        private void OnDisable()=>Shader.SetGlobalVector(PositionId,Vector4.zero);
    }
}
