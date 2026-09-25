#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.CameraTools;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.Cave;
using Object = UnityEngine.Object;

namespace WuxiaRoguelite.EditorTools
{
    public static class BambooValleyLevelBuilder
    {
        public const string ScenePath = "Assets/Scenes/BambooValleyLevel.unity";
        public const string ArtRoot = "Assets/Art/Environment/BambooValley";
        public const string ModelPath = ArtRoot + "/BambooValley.fbx";
        public const string PrefabPath = "Assets/Prefabs/Environment/BambooValley.prefab";
        [Serializable] private class MaterialEntry { public string name; public string source; public float[] color; public float roughness; public float emission; }
        [Serializable] private class ExportData { public MaterialEntry[] materials; }

        [MenuItem("37 MiniGame/Build Bamboo Valley Level 3")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building Level 3.");
            if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save current scene edits before building Level 3.");
            if (!File.Exists(ModelPath)) throw new FileNotFoundException("Export BambooValley.fbx from Blender first.");
            ConfigureModel();
            var original = EditorSceneManager.OpenScene("Assets/Scenes/MainPrototype.unity", OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(original, ScenePath, true)) throw new IOException("Cannot create Level 3 scene copy.");
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var originalEncounters = Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform encounters = new GameObject("Bamboo Valley Encounters").transform;
            void Place(string templateName, Vector2 point)
            {
                var stableTemplates = AssetDatabase.LoadAssetAtPath<GameObject>(PingchuanTownLevelBuilder.TemplatePath);
                EncounterTrigger template = (stableTemplates != null ? stableTemplates.GetComponentsInChildren<EncounterTrigger>(true) : originalEncounters).First(e => e.name == templateName);
                GameObject clone = Object.Instantiate(template.gameObject, encounters);
                clone.name = templateName + " BV " + encounters.childCount;
                clone.transform.position = new Vector3(point.x, BambooValleyLayout.SurfaceHeight(point.x, point.y), point.y);
                clone.GetComponent<EncounterTrigger>().ResetEncounter(rerollCaveContent: true);
            }
            string[] normal = { "山贼喽啰", "灰岩巨鼠", "流寇", "青衣快剑", "紫衣毒客", "青竹机关傀", "机关弩车", "赤骑枪客" };
            Vector2[] points = { new(-17,-13), new(-14.5f,-9), new(-12,-6.3f), new(-15,0), new(-15.5f,4.3f), new(-14,7.5f), new(-12,10), new(-3.2f,-5.8f), new(3,-3), new(3,1.5f), new(2,5.5f), new(8,6), new(12.5f,7.5f), new(9,-5.8f), new(14,-7), new(18,-5) };
            for (int i = 0; i < points.Length; i++) Place(normal[i % normal.Length], points[i]);
            Place("黑风刀客", new(-16,9)); Place("玄衣刀客", new(0,7)); Place("岩甲山魈", new(15,12));
            foreach (Vector2 p in new Vector2[] { new(-18,11.5f), new(4.8f,6.3f), new(17,11), new(13,-8.8f) }) Place("西路宝箱",p);
            foreach (Vector2 p in new Vector2[] { new(-18,-10), new(-17.5f,1), new(4,-6), new(9,5) }) Place("南桥药草",p);
            Place("望气石·西",new(-14,-3)); Place("望气石·东",new(5.5f,4.5f));
            Place("隐市岩洞",new(20,-2.8f));
            encounters.GetComponentsInChildren<EncounterTrigger>().Last(e=>e.encounterType==EncounterType.HiddenCave).caveContent=CaveContentType.Random;
            foreach (EncounterTrigger old in originalEncounters) Object.DestroyImmediate(old.gameObject);
            foreach(string oldName in new[]{"Pingchuan Town Environment","Pingchuan Encounters"})
            {var old=GameObject.Find(oldName);if(old!=null)Object.DestroyImmediate(old);}
            GameObject map = GameObject.Find("3D Prototype Map");
            if (map != null) Object.DestroyImmediate(map);

            GameObject environment = new GameObject("Bamboo Valley Environment");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            model.transform.SetParent(environment.transform,false);
            // Verified with imported Anchor_Start: FBX import gives (20,.55,16).
            model.transform.localRotation=Quaternion.Euler(0,180,0);
            model.transform.localPosition=new Vector3(0,-.4f,0);
            foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>())
            {
                r.lightProbeUsage=LightProbeUsage.Off;
                r.reflectionProbeUsage=ReflectionProbeUsage.Off;
                r.receiveShadows=true;
                r.shadowCastingMode = r.name.StartsWith("BV_18") || r.name.StartsWith("BV_19")
                    ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
            BuildCollisions(environment.transform);
            ExtendForest(environment.transform,model);
            Directory.CreateDirectory("Assets/Prefabs/Environment"); AssetDatabase.Refresh();
            PrefabUtility.SaveAsPrefabAssetAndConnect(environment,PrefabPath,InteractionMode.AutomatedAction);

            var flow=Object.FindFirstObjectByType<GameFlowController>();
            flow.mainTimeLimit=60; flow.mainTimeRemaining=60;
            flow.midBossTriggerElapsedTime=0; // Level 2's timed gatekeeper remains Level 2 specific.
            var music = Object.FindFirstObjectByType<WuxiaRoguelite.Audio.MainMapMusicController>();
            if (music != null) music.midBossMusic = null; // This level never enters the timed gatekeeper phase.
            flow.playerController.transform.position=BambooValleyLayout.Spawn;
            flow.playerController.groundY=0;
            flow.playerController.followBambooValleyHeight=true;
            flow.playerController.followPingchuanTownHeight=false;
            environment.AddComponent<BambooValleyVisibility>().player=flow.playerController.transform;
            flow.bossIntroNarration="竹影摇动，石台之上，九道狐火照亮最后的对手。";
            CameraFollow camera=flow.cameraFollow != null ? flow.cameraFollow : Object.FindFirstObjectByType<CameraFollow>();
            flow.cameraFollow=camera;
            camera.offset=new Vector3(8,16,-20); camera.portraitOffset=new Vector3(6.5f,15,-18);
            camera.landscapeFieldOfView=40;camera.portraitFieldOfView=40;
            camera.transform.position=flow.playerController.transform.position+camera.offset*.74f;
            camera.transform.LookAt(flow.playerController.transform.position+Vector3.up*.72f);
            Camera worldCamera=camera.GetComponent<Camera>();worldCamera.farClipPlane=130;
            Light sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.intensity=1.05f;sun.color=new Color(1,.87f,.68f);sun.transform.rotation=Quaternion.Euler(55,-30,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.42f,.49f,.52f);
            RenderSettings.ambientEquatorColor=new Color(.25f,.31f,.27f);
            RenderSettings.ambientGroundColor=new Color(.14f,.18f,.13f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.14f,.21f,.20f);RenderSettings.fogStartDistance=30;RenderSettings.fogEndDistance=80;
            AddBackdrop();
            var builds=EditorBuildSettings.scenes.ToList();
            if (!builds.Any(s=>s.path==ScenePath)) builds.Add(new EditorBuildSettingsScene(ScenePath,true));
            else builds.First(s=>s.path==ScenePath).enabled=true;
            EditorBuildSettings.scenes=builds.ToArray();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("BAMBOO_LEVEL3_BUILT: 16 normal enemies, 3 elites, 4 treasures, 4 herbs, 2 vision relics, 1 cave; 60-second exploration.");
        }

        private static void ConfigureModel()
        {
            AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport);
            var data=JsonUtility.FromJson<ExportData>(File.ReadAllText(ArtRoot+"/BambooValleyExport.json"));
            var shader=Shader.Find("Wuxia Roguelite/Bamboo Valley Vertex Surface");
            if (shader==null) throw new InvalidOperationException("Bamboo Valley shader was not imported.");
            Directory.CreateDirectory(ArtRoot+"/Materials");AssetDatabase.Refresh();
            var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
            importer.isReadable=false;importer.generateSecondaryUV=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var entry in data.materials)
            {
                string path=ArtRoot+"/Materials/"+entry.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
                mat.shader=shader;mat.SetColor("_Color",Color.white);
                mat.SetFloat("_Smoothness",1-entry.roughness);
                mat.SetFloat("_Emission",Mathf.Min(2,entry.emission));
                mat.SetFloat("_Foliage",entry.source.Contains("Foliage") ? 1 : 0);
                mat.enableInstancing=true;
                mat.SetFloat("_Cull",entry.source.Contains("Foliage") || entry.source.Contains("Fabric") ? 0 : 2);
                EditorUtility.SetDirty(mat);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),entry.name),mat);
            }
            importer.SaveAndReimport();
        }

        private static void Wall(Transform parent,string name,Vector3 position,Vector3 size,Quaternion rotation)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localRotation=rotation;
            obj.AddComponent<BoxCollider>().size=size;
        }

        private static void BuildCollisions(Transform parent)
        {
            Transform root=new GameObject("Bamboo Valley Collisions").transform;root.SetParent(parent,false);
            // Follow the superellipse inside the visible cliff line.
            Vector3 Bound(float a)=>new Vector3(23*Mathf.Sign(Mathf.Cos(a))*Mathf.Sqrt(Mathf.Abs(Mathf.Cos(a))),1,20*Mathf.Sign(Mathf.Sin(a))*Mathf.Sqrt(Mathf.Abs(Mathf.Sin(a))));
            for(int i=0;i<96;i++)
            {
                Vector3 a=Bound(i*Mathf.PI*2/96),b=Bound((i+1)*Mathf.PI*2/96);
                Wall(root,"Valley boundary "+i,(a+b)*.5f,new Vector3(.65f,4,Vector3.Distance(a,b)+.25f),Quaternion.LookRotation(b-a));
            }
            // Continuous river volumes; exact bridge-width gaps are guarded by side rails.
            for(float z=-21;z<21;z+=.25f)
            {
                bool gap=false;
                for(int i=0;i<BambooValleyLayout.BridgeZ.Length;i++)
                    if(Mathf.Abs(z+.125f-BambooValleyLayout.BridgeZ[i])<BambooValleyLayout.BridgeWidths[i]*.5f+.13f)gap=true;
                if(gap)continue;
                Wall(root,"River barrier",new Vector3(BambooValleyLayout.RiverX(z+.125f),1,z+.125f),new Vector3(4.7f,3,.32f),Quaternion.identity);
            }
            for(int i=0;i<BambooValleyLayout.BridgeZ.Length;i++)
                foreach(float side in new[]{-1f,1f})
                    Wall(root,"Bridge rail "+i,new Vector3(BambooValleyLayout.RiverX(BambooValleyLayout.BridgeZ[i]),1,BambooValleyLayout.BridgeZ[i]+side*(BambooValleyLayout.BridgeWidths[i]*.5f)),new Vector3(7.2f,2,.18f),Quaternion.identity);
            Wall(root,"Martial hall walls",new Vector3(0,1,12),new Vector3(8.6f,3,5.6f),Quaternion.identity);
            Wall(root,"Camp tent west",new Vector3(-17,1,13),new Vector3(3.2f,3,3),Quaternion.identity);
            Wall(root,"Camp tent north",new Vector3(-12.6f,1,15.2f),new Vector3(3.2f,3,3),Quaternion.identity);
            Wall(root,"Cave rear",new Vector3(20,1,2.5f),new Vector3(7,4,4),Quaternion.identity);
            foreach(float x in new[]{17.4f,22.6f})Wall(root,"Cave rock side",new Vector3(x,1,-.3f),new Vector3(1.1f,4,3.4f),Quaternion.identity);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3+Mathf.PI/6;
                Wall(root,"Pavilion column",new Vector3(2.28f*Mathf.Cos(a),1,-2+2.28f*Mathf.Sin(a)),new Vector3(.34f,3,.34f),Quaternion.identity);
            }
        }

        private static void AddBackdrop()
        {
            string path=ArtRoot+"/Materials/BV_Backdrop.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=new Color(.11f,.17f,.10f);mat.SetFloat("_Glossiness",0);
            GameObject ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Distant valley floor";
            ground.transform.position=new Vector3(0,-.9f,0);ground.transform.localScale=new Vector3(20,1,20);
            Object.DestroyImmediate(ground.GetComponent<Collider>());ground.GetComponent<Renderer>().sharedMaterial=mat;
        }

        private static void ExtendForest(Transform parent,GameObject model)
        {
            var root=new GameObject("Distant bamboo continuation").transform;root.SetParent(parent,false);
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                if(!r.name.StartsWith("BV_18"))continue;
                Vector3 center=r.bounds.center;
                float edge=Mathf.Max(Mathf.Abs(center.x)/23,Mathf.Abs(center.z)/20);
                if(edge<.78f)continue;
                Vector3 outward=new Vector3(center.x/23,0,center.z/20).normalized;
                for(int i=1;i<=2;i++)
                {
                    var clone=Object.Instantiate(r.gameObject,root,true);
                    clone.name=r.name+" distant "+i;
                    clone.transform.position+=outward*(i*6f)-Vector3.up*.3f;
                    clone.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
            }
        }

        [MenuItem("37 MiniGame/Validate Bamboo Valley Level 3")]
        public static void Validate()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open BambooValleyLevel before validation.");
            var issues=new List<string>();
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            if(flow==null||flow.mainTimeLimit!=60||!flow.playerController.followBambooValleyHeight)issues.Add("flow/height binding");
            foreach(var e in Object.FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None))
                if(!BambooValleyLayout.IsInsideBounds(e.transform.position,.2f)||BambooValleyLayout.IsInsideRiver(e.transform.position,.45f))issues.Add("unsafe encounter: "+e.name);
            foreach(var r in GameObject.Find("Bamboo Valley Environment").GetComponentsInChildren<Renderer>())
                if(r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader"))issues.Add("material: "+r.name);
            Physics.SyncTransforms();
            foreach(float z in BambooValleyLayout.BridgeZ)
                if(Physics.Linecast(new Vector3(BambooValleyLayout.RiverX(z)-4,1,z),new Vector3(BambooValleyLayout.RiverX(z)+4,1,z),out _,~0,QueryTriggerInteraction.Ignore))issues.Add("blocked bridge: "+z);
            if(!Physics.Linecast(new Vector3(BambooValleyLayout.RiverX(0)-4,1,0),new Vector3(BambooValleyLayout.RiverX(0)+4,1,0),out _,~0,QueryTriggerInteraction.Ignore))issues.Add("river lacks blocker");
            if(issues.Count>0)throw new InvalidOperationException(string.Join("; ",issues));
            Debug.Log("BAMBOO_LEVEL3_VALID: materials, encounters, two bridge passages, river blocker, timing config.");
        }
    }
}
#endif
