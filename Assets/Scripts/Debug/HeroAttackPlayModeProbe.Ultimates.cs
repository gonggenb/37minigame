#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

public sealed partial class HeroAttackPlayModeProbe
{
    private IEnumerator CheckUltimates(BattleScreenController screen, BattleManager battle,
        PlayerStats player, CombatantStats enemy)
    {
        const string output="docs/validation/hero_ultimates";
        const string poseOutput="docs/validation/hero_ultimate_poses";
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(poseOutput);
        // Keep fixture time away from the map's real midboss threshold. The suite
        // below resets it to 60 and separately exercises all three clock rules.
        F.mainTimeRemaining=600f;
        for(int i=0;i<MartialUltimateCatalog.ArtIds.Length;i++)
        {
            string id=MartialUltimateCatalog.ArtIds[i];
            Texture2D texture=HeroUltimateArt.Texture(i);
            Check(texture!=null && texture.width==512 && texture.height==512 &&
                texture.filterMode==FilterMode.Point,id+": imported 512px point-filtered Resources core");
            Sprite[] poses=HeroUltimateArt.Frames(i);
            Check(poses.Length==8,id+": eight dedicated character-and-effects frames imported");
            foreach(Sprite pose in poses)
                Check(pose.rect.size==new Vector2(256,256) && pose.pivot==new Vector2(128,32) &&
                    pose.pixelsPerUnit==160 && pose.texture.filterMode==FilterMode.Point,
                    pose.name+": normalized cells, foot pivot and pixel filtering");
            player.ResetRun();
            for(int rank=0;rank<3;rank++)
            {
                if(rank>0)player.ApplyMartialArt(id);
                int before=battle.UltimateVisualSequence;
                Invoke(battle,"RegisterMartialArtActivation",id);
                Check(battle.UltimateVisualSequence==before,id+": rank "+rank+" cannot present an ultimate");
            }
        }
        player.ResetRun();
        for(int rank=0;rank<3;rank++)
        { player.ApplyMartialArt("剑气诀"); player.ApplyMartialArt("无影连环剑"); }
        Invoke(battle,"RegisterMartialArtActivation","剑气诀");
        Check(battle.UltimateVisualSequence==0,"a full ordinary art cannot borrow another full capstone's eligibility");

        foreach(bool portrait in new[]{false,true})
        {
            Resize(portrait ? 540 : 960,portrait ? 960 : 540);
            yield return new WaitForSecondsRealtime(.2f);
            for(int i=0;i<MartialUltimateCatalog.ArtIds.Length;i++)
            {
                string id=MartialUltimateCatalog.ArtIds[i];
                battle.CancelBattle();
                player.ResetRun();
                if(player.equipment!=null)player.equipment.Unequip(EquipmentSlot.Weapon);
                for(int rank=0;rank<3;rank++)player.ApplyMartialArt(id);
                if(i==1)player.ApplyMartialArt("毒砂掌");
                player.runtimeStats.maxHealth=1000000;player.runtimeStats.currentHealth=1000000;
                player.runtimeStats.attack=1f;player.runtimeStats.critChance=i==4 ? 1f : 0f;
                player.runtimeStats.dodgeChance=0f;player.runtimeStats.attackSpeed=.2f;
                int serial=battle.UltimateBattleSerial;
                Invoke(F,"BeginNormalBattle",enemy.Clone(),0,0,EncounterType.NormalEnemy);
                battle.StopAllCoroutines();
                Check(battle.UltimateBattleSerial>serial,id+": new battle clears prior visual cooldown");
                if(i==0)
                {
                    typeof(BattleManager).GetProperty("PlayerSuccessfulHits").SetValue(battle,2);
                    Invoke(battle,"DoAttack",player.runtimeStats,battle.currentEnemy);
                }
                else if(i==1)
                {
                    Invoke(battle,"DoAttack",player.runtimeStats,battle.currentEnemy);
                    Check(battle.UltimateVisualSequence==0,"poison ultimate waits for actual poison tick");
                    Invoke(battle,"ApplyPoisonTick");
                }
                else if(i==3)
                {
                    typeof(BattleManager).GetProperty("EnemyAttackAttempts").SetValue(battle,3);
                    Invoke(battle,"DoAttack",battle.currentEnemy,player.runtimeStats);
                }
                else if(i==4)Invoke(battle,"DoAttack",player.runtimeStats,battle.currentEnemy);
                // Iron guard must already have arrived via the actual opening shield.
                Check(battle.UltimateVisualSequence==1 && battle.LastUltimateArtId==id,
                    id+": real max-rank skill hook records exactly one ultimate "+portrait);
                string randomState=JsonUtility.ToJson(Random.state);
                Invoke(screen,"TrackUltimate");Invoke(screen,"TrackHeroAttack");
                Check(screen.IsUltimatePlaying && screen.CurrentUltimateArtId==id,id+": UI consumes the independent event");
                Sprite[] bound=(Sprite[])Invoke(screen,"CurrentHeroAttackFrames");
                Check(screen.IsUltimatePosePlaying && ReferenceEquals(bound,HeroUltimateArt.Frames(i)),
                    id+": real skill activation replaces the character pose, including guard and dodge");
                float timeScale=Time.timeScale;
                int sequence=battle.UltimateVisualSequence;
                Invoke(battle,"RegisterMartialArtActivation",id);
                Check(battle.UltimateVisualSequence==sequence,id+": repeated activation respects visual cooldown");
                Check(JsonUtility.ToJson(Random.state)==randomState && timeScale==1f,
                    id+": presentation preserves gameplay randomness and time scale");
                float started=(float)screen.GetType().GetField("ultimateStartedAt",Flags).GetValue(screen);
                int attackSequence=battle.PlayerAttackVisualSequence;
                for(int n=0;n<8;n++)
                {
                    Invoke(battle,"DoAttack",player.runtimeStats,battle.currentEnemy);
                    Invoke(battle,"DoAttack",battle.currentEnemy,player.runtimeStats);
                    if(i==1)Invoke(battle,"ApplyPoisonTick");
                    Invoke(screen,"TrackHeroAttack");Invoke(screen,"TrackUltimate");
                }
                Check(battle.PlayerAttackVisualSequence>attackSequence && screen.IsUltimatePosePlaying &&
                    ReferenceEquals(bound,Invoke(screen,"CurrentHeroAttackFrames")) &&
                    (float)screen.GetType().GetField("ultimateStartedAt",Flags).GetValue(screen)==started,
                    id+": continuing player attacks, enemy attacks and poison cannot interrupt or restart the cast");
                Check(screen.ActiveHeroSelfVfxCount==0 && screen.ActiveHeroSkillVfxCount==0,
                    id+": baked effects replace mismatched ordinary self effects during the cast");
                if(portrait)
                {
                    float[] sample={.02f,.10f,.21f,.32f,.46f,.63f,.80f,.94f};
                    for(int frame=0;frame<8;frame++)
                    {
                        Set(screen,"ultimateStartedAt",Time.time-sample[frame]*MartialUltimateCatalog.PresentationDuration);
                        Check(screen.CurrentUltimatePoseFrame==frame,id+": authored cast frame "+frame);
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(poseOutput+"/pose_"+MartialUltimateCatalog.TextureIds[i]+"_"+frame+".png");
                        yield return null;
                    }
                }
                yield return new WaitForSecondsRealtime(.10f);
                Set(screen,"ultimateStartedAt",Time.time-.60f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(output+"/"+MartialUltimateCatalog.TextureIds[i]+
                    (portrait ? "_portrait" : "_landscape")+".png");
                yield return new WaitForSecondsRealtime(1f);
                Check(!screen.IsUltimatePlaying,id+": presentation completes while battle remains active");
                Check(!screen.IsUltimatePosePlaying && screen.CurrentUltimatePoseFrame==-1 &&
                    !ReferenceEquals(bound,Invoke(screen,"CurrentHeroAttackFrames")),
                    id+": character naturally returns to current normal animation after the cast");
                Invoke(battle,"RegisterMartialArtActivation",id);
                Check(battle.UltimateVisualSequence==sequence,id+": visual cooldown outlasts animation");
                Set(battle,"nextUltimateVisualAt",Time.time-.01f);
                Invoke(battle,"RegisterMartialArtActivation",id);Invoke(screen,"TrackUltimate");
                Check(battle.UltimateVisualSequence==sequence+1 && screen.IsUltimatePlaying,
                    id+": next real activation can rearm after visual cooldown");
                // A second maxed art in the same frame may resolve gameplay, but cannot
                // overwrite the already accepted visual or consume the next battle's slot.
                string other=MartialUltimateCatalog.ArtIds[(i+1)%5];
                for(int rank=0;rank<3;rank++)player.ApplyMartialArt(other);
                Invoke(battle,"RegisterMartialArtActivation",other);
                Check(battle.UltimateVisualSequence==sequence+1 && battle.LastUltimateArtId==id,
                    id+": same-frame mixed capstones keep first presentation stable");
                battle.CancelBattle();Invoke(screen,"TrackUltimate");
                Check(!screen.IsUltimatePlaying && !screen.IsUltimatePosePlaying && screen.CurrentUltimatePoseFrame==-1 &&
                    screen.CurrentUltimateArtId==null && battle.UltimateVisualSequence==0,
                    id+": cancellation clears all ultimate presentation state");
                Invoke(battle,"RegisterMartialArtActivation",id);
                Check(battle.UltimateVisualSequence==0,id+": inactive battle rejects stale activations");
            }
        }
    }
}
#endif
