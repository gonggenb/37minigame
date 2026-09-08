using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>Keep the authored reward geometry synchronized with encounter consumption and replay.</summary>
    public sealed class TutorialRestStopProps : MonoBehaviour
    {
        public EncounterTrigger chest;
        public EncounterTrigger herb;
        public Renderer[] chestRenderers;
        public Renderer[] herbRenderers;
        public Transform player;
        private static readonly int PositionId=Shader.PropertyToID("_RestStopPlayerPosition");
        private void LateUpdate()
        {
            SetVisible(chestRenderers,chest!=null&&!chest.consumed);
            SetVisible(herbRenderers,herb!=null&&!herb.consumed);
            if(player!=null)Shader.SetGlobalVector(PositionId,new Vector4(player.position.x,player.position.y,player.position.z,1));
        }
        private static void SetVisible(Renderer[] renderers,bool visible)
        {if(renderers!=null)foreach(var r in renderers)if(r!=null)r.enabled=visible;}
        private void OnDisable()=>Shader.SetGlobalVector(PositionId,Vector4.zero);
    }
}
