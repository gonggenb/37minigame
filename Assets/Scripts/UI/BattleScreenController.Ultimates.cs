using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.UI
{
    public partial class BattleScreenController
    {
        private int observedUltimateSerial = -1;
        private int observedUltimateSequence;
        private float ultimateStartedAt = -100f;
        private int currentUltimateIndex = -1;
        public string CurrentUltimateArtId => currentUltimateIndex < 0 ? null : MartialUltimateCatalog.ArtIds[currentUltimateIndex];
        public float UltimateProgress => Mathf.Clamp01((Time.unscaledTime-ultimateStartedAt)/MartialUltimateCatalog.PresentationDuration);
        public bool IsUltimatePlaying => battleManager != null && battleManager.IsBattleActive &&
            currentUltimateIndex >= 0 && UltimateProgress < 1f;
        public bool IsUltimatePosePlaying => IsUltimatePlaying && HeroUltimateArt.Frames(currentUltimateIndex).Length == 8;
        public int CurrentUltimatePoseFrame => IsUltimatePosePlaying ? HeroUltimateArt.FrameIndex(UltimateProgress) : -1;

        private void TrackUltimate()
        {
            if (battleManager == null) return;
            if (!battleManager.IsBattleActive || observedUltimateSerial != battleManager.UltimateBattleSerial)
            {
                observedUltimateSerial = battleManager.UltimateBattleSerial;
                observedUltimateSequence = 0;
                currentUltimateIndex = -1;
                ultimateStartedAt = -100f;
            }
            if (!battleManager.IsBattleActive || observedUltimateSequence == battleManager.UltimateVisualSequence) return;
            observedUltimateSequence = battleManager.UltimateVisualSequence;
            currentUltimateIndex = MartialUltimateCatalog.IndexOf(battleManager.LastUltimateArtId);
            ultimateStartedAt = Time.unscaledTime;
            ClearHeroSkillVfx();
        }

        private void DrawHeroUltimateLayer(Rect stage, Rect player, Rect enemy, bool foreground)
        {
            if (!IsUltimatePlaying || Event.current.type != EventType.Repaint) return;
            float p=UltimateProgress;
            float reveal=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(p/.17f));
            float fade=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.67f,1f,p));
            float alpha=reveal*fade;
            Color tint=MartialUltimateCatalog.Tint(currentUltimateIndex);
            GUI.BeginGroup(stage);
            Rect localPlayer=OffsetRect(player,-stage.x,-stage.y);
            Rect localEnemy=OffsetRect(enemy,-stage.x,-stage.y);
            Vector2 from=new Vector2(localPlayer.center.x,localPlayer.y+localPlayer.height*.60f);
            Vector2 to=new Vector2(localEnemy.center.x,localEnemy.y+localEnemy.height*.60f);
            Vector2 foot=new Vector2(from.x,localPlayer.y+localPlayer.height*.875f);
            // Bound the core by the space above the actor's foot, not by the tall
            // portrait stage: the apparition must stand with its caster.
            float size=Mathf.Min(stage.height-34f,stage.width*.86f,Mathf.Max(80f,(foot.y-48f)/.80f));
            bool guarding=currentUltimateIndex==2 || currentUltimateIndex==3;
            Vector2 center=Vector2.Lerp(from,to,guarding ? .06f : .25f);
            center.x=Mathf.Clamp(center.x,size*.50f+4f,stage.width-size*.50f-4f);
            center.y=foot.y-size*.37f;
            if (!foreground)
            {
                FillRect(new Rect(0,0,stage.width,stage.height),new Color(.025f,.032f,.045f,alpha*.48f));
                DrawUltimateCore(center,size,p,alpha);
                float groundRadius=size*(.26f+.27f*Mathf.Clamp01(p/.6f));
                DrawHeroArc(foot,groundRadius,groundRadius*.19f,0,360,3f,WithAlpha(tint,alpha*.62f));
                DrawUltimateMotes(center,size,p,WithAlpha(tint,alpha));
            }
            else
            {
                float release=Mathf.Clamp01((p-.18f)/.56f);
                if (p>.18f) DrawUltimateRelease(localPlayer,localEnemy,from,to,foot,size,release,WithAlpha(tint,alpha));
                // Boss telegraphs retain the shared upper-stage title space.
                float bossBannerAge=battleManager.IsBossBattle && battleManager.IsFinalBossActionActive
                    ? battleManager.FinalBossActionElapsed : Time.unscaledTime-battleManager.LastBossSkillTriggeredAt;
                bool bossBanner=battleManager.IsBossEncounter && battleManager.LastBossSkill!=BossSkillId.None &&
                    bossBannerAge>=0f && bossBannerAge<=1.15f;
                if (p<.66f && !bossBanner)
                {
                    float titleWidth=Mathf.Min(280f,stage.width-24f);
                    Rect title=new Rect((stage.width-titleWidth)*.5f,2f,titleWidth,43f);
                    Color previous=GUI.color;
                    GUI.color=new Color(1,1,1,alpha);
                    WuxiaUiTheme.DrawCompactSurface(title,new Color(.035f,.030f,.025f,.90f),Gold);
                    ResponsiveGui.DrawSingleLineLabel(new Rect(title.x+8,title.y+1,title.width-16,15),
                        GameTextCatalog.UltimateTierName,skillCalloutCaptionStyle,9);
                    ResponsiveGui.DrawSingleLineLabel(new Rect(title.x+8,title.y+16,title.width-16,25),
                        CurrentUltimateArtId,skillCalloutStyle,14);
                    GUI.color=previous;
                }
            }
            GUI.EndGroup();
        }

        private void DrawUltimateCore(Vector2 center,float size,float p,float alpha)
        {
            Texture2D texture=HeroUltimateArt.Texture(currentUltimateIndex);
            if(texture==null)return;
            float scale=Mathf.Lerp(.62f,1f,Mathf.SmoothStep(0f,1f,Mathf.Clamp01(p/.23f)))+p*.08f;
            float rotation=currentUltimateIndex==3 ? -20f+p*60f : currentUltimateIndex==4 ? -24f+p*95f :
                currentUltimateIndex==1 ? -5f+p*12f : 0f;
            Matrix4x4 previousMatrix=GUI.matrix;
            Color previousColor=GUI.color;
            GUIUtility.RotateAroundPivot(rotation,center);
            float backdropStrength=IsUltimatePosePlaying ? .50f : 1f;
            GUI.color=new Color(1f,1f,1f,alpha*(currentUltimateIndex==2 ? .83f : .92f)*backdropStrength);
            float s=size*scale;
            GUI.DrawTexture(new Rect(center.x-s*.5f,center.y-s*.5f,s,s),texture,ScaleMode.StretchToFill,true);
            GUI.matrix=previousMatrix;GUI.color=previousColor;
        }

        private void DrawUltimateRelease(Rect player,Rect enemy,Vector2 from,Vector2 to,Vector2 foot,
            float size,float p,Color tint)
        {
            float burst=Mathf.Sin(p*Mathf.PI);
            Color core=new Color(1f,.96f,.79f,tint.a*burst);
            switch(currentUltimateIndex)
            {
                case 0:
                    // Seven individually staggered sword rays converge toward the opponent.
                    for(int i=0;i<7;i++)
                    {
                        float t=Mathf.Clamp01((p-i*.055f)/.50f);
                        Vector2 origin=from+new Vector2(-size*.23f+i*size*.09f,-size*(.32f+.08f*Mathf.Sin(i)));
                        Vector2 end=to+new Vector2((i-3)*size*.024f,(i%3-1)*size*.07f);
                        Vector2 tip=Vector2.Lerp(origin,end,t);
                        Vector2 tail=Vector2.Lerp(origin,end,Mathf.Max(0,t-.25f));
                        float a=tint.a*Mathf.Sin(t*Mathf.PI);
                        DrawHeroLine(tail,tip,7f,WithAlpha(tint,a*.55f));
                        DrawHeroLine(tail,tip,2f,WithAlpha(core,a));
                    }
                    DrawHeroArc(to,size*(.10f+p*.35f),size*(.10f+p*.24f),0,360,3f,WithAlpha(tint,tint.a*burst*.58f));
                    break;
                case 1:
                    for(int i=0;i<3;i++)
                    {
                        float radius=size*(.12f+p*.25f+i*.05f);
                        DrawHeroArc(to,radius,radius*.57f,30f+i*120f+p*100f,120f+i*120f+p*100f,
                            4f,WithAlpha(tint,tint.a*burst*.65f));
                    }
                    DrawUltimateMotes(to,size*.8f,p,WithAlpha(tint,tint.a*burst));
                    break;
                case 2:
                    for(int i=0;i<3;i++)
                    {
                        float t=Mathf.Clamp01((p-i*.13f)/.60f);
                        float radius=size*(.17f+t*.72f);
                        DrawHeroArc(foot,radius,radius*.23f,0,360,4f,WithAlpha(tint,tint.a*(1f-t)*.65f));
                    }
                    break;
                case 3:
                    Sprite[] frames=HeroAttackArt.Idle;
                    if(!IsUltimatePosePlaying && frames.Length>0)
                    {
                        Color previous=GUI.color;
                        for(int i=0;i<4;i++)
                        {
                            GUI.color=WithAlpha(tint,tint.a*burst*(.35f-i*.055f));
                            DrawSprite(OffsetRect(player,(i-1.5f)*size*.19f,-Mathf.Sin(p*Mathf.PI)*size*.06f),frames[0],false);
                        }
                        GUI.color=previous;
                    }
                    DrawHeroArc(from,size*.36f,size*.36f,-140f+p*110f,30f+p*110f,5f,WithAlpha(tint,tint.a*burst));
                    break;
                case 4:
                    DrawSlashTrail(enemy,p,tint,-48f,2.1f);
                    if(p>.20f)DrawSlashTrail(enemy,(p-.20f)/.8f,core,42f,1.8f);
                    DrawHeroArc(to,size*(.18f+p*.32f),size*(.12f+p*.25f),-130f+p*80f,100f+p*80f,6f,
                        WithAlpha(tint,tint.a*burst*.72f));
                    break;
            }
        }

        private static void DrawUltimateMotes(Vector2 center,float size,float p,Color color)
        {
            for(int i=0;i<20;i++)
            {
                float angle=(i*137.5f+p*38f)*Mathf.Deg2Rad;
                float radius=size*(.15f+Mathf.Repeat(i*.173f+p*.55f,1f)*.45f);
                Vector2 point=center+new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*.70f);
                float side=i%4==0 ? 4f : 2f;
                FillRect(new Rect(Mathf.Round(point.x),Mathf.Round(point.y),side,side),WithAlpha(color,color.a*.55f));
            }
        }
    }
}
