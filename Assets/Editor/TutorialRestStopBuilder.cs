#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using Object=UnityEngine.Object;

namespace WuxiaRoguelite.EditorTools
{
    public static class TutorialRestStopBuilder
    {
        public const string ScenePath="Assets/Scenes/TutorialLevel.unity";
        public const string Art="Assets/Art/Environment/TutorialRestStop";
        [Serializable] private class MatEntry { public string name,source; public float roughness,emission; }
        [Serializable] private class MeshEntry { public string name,source_object,role; public int group; }
        [Serializable] private class Export { public MatEntry[] materials; public MeshEntry[] meshes; }
        [MenuItem("37 MiniGame/Adapt Tutorial Rest Stop")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath||scene.isDirty)throw new InvalidOperationException("Open the saved TutorialLevel scene before adapting.");
            AssetDatabase.ImportAsset(Art+"/TutorialRestStop.fbx",ImportAssetOptions.ForceSynchronousImport);
            var data=JsonUtility.FromJson<Export>(File.ReadAllText(Art+"/TutorialRestStopExport.json"));
            var shader=Shader.Find("Wuxia Roguelite/Tutorial Rest Stop Surface");
            if(shader==null)throw new InvalidOperationException("Import tutorial shader first.");
            Directory.CreateDirectory(Art+"/Materials");AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/TutorialRestStop.fbx");
            importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
            importer.isReadable=false;importer.generateSecondaryUV=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var e in data.materials)
            {
                string path=Art+"/Materials/"+e.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
                mat.shader=shader;mat.SetColor("_Color",Color.white);mat.SetFloat("_Smoothness",1-e.roughness);
                mat.SetFloat("_Emission",Mathf.Min(e.emission,2.2f));
                bool foliage=e.source.Contains("leaf")||e.source.Contains("bamboo");
                mat.SetFloat("_Foliage",foliage||e.source.Contains("roof")?1:0);
                mat.SetFloat("_Cull",foliage||e.source.Contains("cloth")?0:2);mat.enableInstancing=true;
                EditorUtility.SetDirty(mat);importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),e.name),mat);
            }
            importer.SaveAndReimport();
            foreach(string name in new[]{"3D Prototype Map","Tutorial Rest Stop Environment"})
            {var old=GameObject.Find(name);if(old!=null)Object.DestroyImmediate(old);}
            var env=new GameObject("Tutorial Rest Stop Environment");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/TutorialRestStop.fbx"));
            model.transform.SetParent(env.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;
                r.shadowCastingMode=r.name.StartsWith("TR_21")||r.name.StartsWith("TR_22")||r.name.StartsWith("TR_23")||r.name.StartsWith("TR_24")?ShadowCastingMode.Off:ShadowCastingMode.On;
            }
            var encounters=Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var e in encounters)
            {
                if(!new[]{"南桥药草","隐市岩洞","西路宝箱","山贼喽啰"}.Contains(e.name)){Object.DestroyImmediate(e.gameObject);continue;}
                e.ResetEncounter();e.transform.position=e.encounterType switch
                {
                    EncounterType.Herb=>new Vector3(-5.55f,0,-2.1f),
                    EncounterType.Treasure=>new Vector3(-5.2f,0,4.15f),
                    EncounterType.HiddenCave=>new Vector3(5.65f,0,4.1f),
                    _=>new Vector3(4.6f,0,-2.15f)
                };
                var collider=e.GetComponent<SphereCollider>();if(collider!=null){collider.radius=.62f;collider.center=new Vector3(0,.55f,0);}
                if(e.encounterType==EncounterType.NormalEnemy||e.encounterType==EncounterType.Treasure)e.cultivationReward=20;
                foreach(var r in e.GetComponentsInChildren<SpriteRenderer>())
                {
                    if(e.encounterType==EncounterType.NormalEnemy)r.transform.localPosition+=Vector3.up*(TutorialRestStopLayout.SurfaceHeight(e.transform.position.x,e.transform.position.z)-r.transform.localPosition.y+.7f);
                    else Object.DestroyImmediate(r.gameObject);
                }
                EditorUtility.SetDirty(e);
            }
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            var player=flow.playerController;player.transform.position=TutorialRestStopLayout.Spawn;
            player.groundY=0;player.followBambooValleyHeight=false;player.followTutorialRestStopHeight=true;
            flow.mainTimeLimit=LevelSequence.TutorialTimeLimitSeconds;flow.mainTimeRemaining=flow.mainTimeLimit;
            var camera=flow.cameraFollow!=null?flow.cameraFollow:Object.FindFirstObjectByType<WuxiaRoguelite.CameraTools.CameraFollow>();flow.cameraFollow=camera;camera.offset=new Vector3(4,16,-19);camera.portraitOffset=new Vector3(2,17,-20);
            camera.landscapeFieldOfView=40;camera.portraitFieldOfView=42;camera.lookAtHeight=.9f;
            camera.GetComponent<Camera>().farClipPlane=110;camera.GetComponent<Camera>().allowHDR=true;
            camera.transform.position=player.transform.position+camera.offset*.74f;camera.transform.LookAt(player.transform.position+Vector3.up*.9f);
            BuildCollisions(env.transform);
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.color=new Color(1,.83f,.62f);sun.intensity=.82f;sun.transform.rotation=Quaternion.Euler(48,-32,0);
            sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.23f,.31f,.38f);
            RenderSettings.ambientEquatorColor=new Color(.12f,.19f,.17f);RenderSettings.ambientGroundColor=new Color(.07f,.10f,.09f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.15f,.23f,.25f);
            RenderSettings.fogStartDistance=24;RenderSettings.fogEndDistance=68;
            foreach(var p in new[]{new Vector3(-6.7f,2.2f,3.7f),new Vector3(-3.7f,2.2f,3.7f),new Vector3(4.3f,1.5f,4.35f),new Vector3(7,1.5f,4.4f),new Vector3(5.85f,.75f,-2.5f),new Vector3(-2.4f,1.8f,-.7f),new Vector3(1.8f,1.7f,1.5f),new Vector3(-1.1f,1.4f,-6.5f)})
            {var o=new GameObject("Warm lantern light");o.transform.SetParent(env.transform);o.transform.position=p;var l=o.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.44f,.12f);l.intensity=1.7f;l.range=4;l.shadows=LightShadows.None;}
            Directory.CreateDirectory("Assets/Prefabs/Environment");
            PrefabUtility.SaveAsPrefabAssetAndConnect(env,"Assets/Prefabs/Environment/TutorialRestStop.prefab",InteractionMode.AutomatedAction);
            var sync=env.AddComponent<TutorialRestStopProps>();sync.player=player.transform;
            sync.chest=Object.FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None).First(e=>e.encounterType==EncounterType.Treasure);
            sync.herb=Object.FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None).First(e=>e.encounterType==EncounterType.Herb);
            var renderers=model.GetComponentsInChildren<Renderer>();
            sync.chestRenderers=renderers.Where(r=>data.meshes.Any(m=>m.name==r.name&&m.role=="chest")).ToArray();
            sync.herbRenderers=renderers.Where(r=>data.meshes.Any(m=>m.name==r.name&&m.source_object.Contains("medicinal plant"))).ToArray();
            if(sync.chestRenderers.Length==0||sync.herbRenderers.Length==0)throw new InvalidOperationException("Authored consumable mapping missing.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("TUTORIAL_REST_STOP_ADAPTED: four encounters, matching consumable models, bridge and pavilion heights, foliage visibility, 30-second tutorial.");
        }
        private static void Wall(Transform parent,string name,Vector3 pos,Vector3 size)
        {var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.AddComponent<BoxCollider>().size=size;}
        private static void BuildCollisions(Transform parent)
        {
            var root=new GameObject("Gameplay Collisions").transform;root.SetParent(parent,false);
            Wall(root,"North wooded boundary",new Vector3(0,1,8.3f),new Vector3(19,3,.5f));
            foreach(float x in new[]{-9.1f,9.1f})Wall(root,"Side wooded boundary",new Vector3(x,1,-.4f),new Vector3(.5f,3,18));
            Wall(root,"Entry end",new Vector3(0,1,-9.9f),new Vector3(19,3,.5f));
            foreach(float x in new[]{-5.1f,5.1f})Wall(root,"Stream bank",new Vector3(x,1,-7.85f),new Vector3(8.3f,3,3));
            foreach(float x in new[]{-1.04f,1.04f})Wall(root,"Bridge parapet",new Vector3(x,1,-7.8f),new Vector3(.17f,2,3.2f));
            Wall(root,"Central pine and furniture",new Vector3(-.25f,1,.2f),new Vector3(2.7f,3,2.65f));
            Wall(root,"Tent",new Vector3(6.2f,1,.5f),new Vector3(2.7f,3,2.55f));
            Wall(root,"Camp fire",new Vector3(5.85f,1,-2.5f),new Vector3(.9f,2,.9f));
            Wall(root,"Cart",new Vector3(7.1f,1,-3.85f),new Vector3(1.5f,2,2));
            foreach(float x in new[]{4.15f,7.15f})Wall(root,"Cave rock side",new Vector3(x,1,6),new Vector3(.8f,3,3.5f));
            Wall(root,"Cave rear",new Vector3(5.65f,1,7.6f),new Vector3(3.6f,3,.6f));
            foreach(float x in new[]{-6.85f,-3.55f})foreach(float z in new[]{3.68f,6.32f})Wall(root,"Pavilion column",new Vector3(x,1,z),new Vector3(.3f,3,.3f));
            foreach(float x in new[]{-6.82f,-3.58f})Wall(root,"Pavilion front ledge",new Vector3(x,1,3.13f),new Vector3(1.12f,2,.24f));
            foreach(float x in new[]{-7.4f,-3f})Wall(root,"Pavilion side ledge",new Vector3(x,1,5),new Vector3(.2f,2,3.6f));
            Wall(root,"Pavilion rear ledge",new Vector3(-5.2f,1,6.85f),new Vector3(4.4f,2,.2f));
        }
    }
}
#endif
