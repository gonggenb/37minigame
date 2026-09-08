using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>Only the third-level vegetation shader reads this visibility corridor.</summary>
    public sealed class BambooValleyVisibility : MonoBehaviour
    {
        public Transform player;
        private static readonly int PositionId=Shader.PropertyToID("_BambooPlayerPosition");
        private void LateUpdate()
        {
            if(player!=null)Shader.SetGlobalVector(PositionId,new Vector4(player.position.x,player.position.y,player.position.z,1));
        }
        private void OnDisable()=>Shader.SetGlobalVector(PositionId,Vector4.zero);
    }
}
