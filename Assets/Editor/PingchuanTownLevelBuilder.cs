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
using WuxiaRoguelite.Cave;
using Object=UnityEngine.Object;

namespace WuxiaRoguelite.EditorTools
{
    /// <summary>Idempotent level-two replacement. Saves original encounter content as reusable templates.</summary>
    public static class PingchuanTownLevelBuilder
    {
        public const string ScenePath="Assets/Scenes/MainPrototype.unity";
        public const string ArtRoot="Assets/Art/Environment/PingchuanTown";
        public const string TemplatePath="Assets/Prefabs/Environment/PingchuanEncounterTemplates.prefab";
        private const string PrefabPath="Assets/Prefabs/Environment/PingchuanTown.prefab";
        [Serializable] private class MaterialEntry {public string name,source;public float roughness,emission;}
        [Serializable] private class ExportData {public MaterialEntry[] materials;}
        [Serializable] private class Placement {public string name,type,region;public Vector3 position;}
        [Serializable] private class PlacementReport {public int normal,elite,caves,treasures,herbs;public Placement[] placements;public string scope;}
        private static readonly List<Vector3> placed=new();
        private static readonly List<Placement> report=new();
        private static readonly List<BoxCollider> solids=new();
        public static IReadOnlyList<BoxCollider> SolidColliders=>solids;
        [MenuItem("37 MiniGame/Build Pingchuan Town Level 2")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building.");
            if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Save scene changes before building.");
            ConfigureModel();
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var originals=Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if(!File.Exists(TemplatePath))
            {
                var backup=new GameObject("Pingchuan Encounter Templates");
                foreach(var e in originals){var c=Object.Instantiate(e.gameObject,backup.transform);c.name=e.name;}
                PrefabUtility.SaveAsPrefabAsset(backup,TemplatePath);Object.DestroyImmediate(backup);
            }
            var templates=AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath).GetComponentsInChildren<EncounterTrigger>(true);
            foreach(var e in originals)if(e!=null)Object.DestroyImmediate(e.gameObject);
            foreach(string name in new[]{"3D Prototype Map","Pingchuan Town Environment","Pingchuan Encounters"})
            {var old=GameObject.Find(name);if(old!=null)Object.DestroyImmediate(old);}
            var environment=new GameObject("Pingchuan Town Environment");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArtRoot+"/PingchuanTown.fbx"));
            model.transform.SetParent(environment.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
            {r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;r.receiveShadows=true;}
            BuildCollisions(environment.transform);
            BuildNaturalCollisions(environment.transform,model);
            Physics.SyncTransforms();placed.Clear();report.Clear();
            var root=new GameObject("Pingchuan Encounters").transform;
            void Place(string template,float x,float y,string region,bool cave=false)
            {
                var source=templates.First(e=>e.name==template);Vector3 desired=PingchuanTownLayout.Point(x,y);
                Vector3 pos=cave?desired:FindSafe(desired);
                var c=Object.Instantiate(source.gameObject,root);c.name=template+" PC "+(report.Count+1);c.transform.position=pos;
                var e=c.GetComponent<EncounterTrigger>();e.ResetEncounter(rerollCaveContent:true);
                // The five Blender portals already supply cave artwork; retain their trigger and gameplay.
                if(cave){foreach(var r in c.GetComponentsInChildren<Renderer>())r.enabled=false;e.caveContent=CaveContentType.Random;}
                else c.transform.position+=Vector3.up*PingchuanTownLayout.SurfaceHeight(pos.x,pos.z);
                placed.Add(pos);report.Add(new Placement{name=c.name,type=e.encounterType.ToString(),region=region,position=c.transform.position});
            }
            string[] normal={"山贼喽啰","灰岩巨鼠","流寇","青衣快剑","紫衣毒客","青竹机关傀","机关弩车","赤骑枪客"};
            int n=0;
            void Normals(string region,Vector2[] points,int offset){foreach(var p in points)Place(normal[(n+++offset)%normal.Length],p.x,p.y,region);}
            Normals("Entry",new Vector2[]{new(-58,-79),new(-49,-63),new(-70,-43),new(-79,-12),new(-49,-22),new(-29,-30)},0);
            Normals("Town",new Vector2[]{new(-25,-14),new(-25,10),new(-25,31),new(-53,8),new(3,29),new(-49,24)},0);
            Normals("Plain",new Vector2[]{new(5,-15),new(21,-14),new(34,-10),new(50,-18),new(72,-3),new(36,20),new(36,40),new(23,10),new(52,-37),new(2,-38)},3);
            Normals("Camp",new Vector2[]{new(22,-50),new(43,-61),new(58,-66),new(65,-81),new(73,-70),new(89,-48),new(102,-43),new(54,-88)},5);
            Normals("Caves",new Vector2[]{new(-64,39),new(-82,17),new(-81,47),new(-48,69),new(-18,51),new(81,12)},1);
            Normals("Pass",new Vector2[]{new(4,83),new(25,73),new(27,93),new(31,107)},6);
            foreach(var p in new Vector2[]{new(-76,58),new(-64,67),new(0,66),new(85,25),new(39,-23),new(20,-30),new(65,-64),new(88,-79),new(27,84),new(-88,30)})
                Place(new[]{"黑风刀客","玄衣刀客","岩甲山魈","边城黑衣客","东关铁卫"}[report.Count%5],p.x,p.y,"Elite pockets");
            foreach(var p in new Vector2[]{new(-83.95f,54.11f),new(-66.28f,71.06f),new(0,73),new(90,22),new(-92.37f,31.24f)})Place("隐市岩洞",p.x,p.y,"Caves",true);
            foreach(var p in new Vector2[]{new(-64,-87),new(-26,43),new(-54,32),new(57,0),new(93,-60),new(39,77),new(-43,72),new(80,29)})Place("西路宝箱",p.x,p.y,"Supply");
            int h=0;foreach(var p in new Vector2[]{new(-55,-69),new(-25,-3),new(-80,6),new(14,-38),new(50,-49),new(-31,64),new(29,99)})Place(new[]{"南桥药草","北门药草","北坡轻身草","东村铁骨草"}[h++%4],p.x,p.y,"Recovery");
            Place("望气石·西",-44,-53,"Entry fork");Place("望气石·东",29,29,"North fork");Place("东郊无名奇草",79,-36,"Exploration");
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            flow.playerController.transform.position=PingchuanTownLayout.Spawn;flow.playerController.groundY=0;
            flow.playerController.followPingchuanTownHeight=true;flow.playerController.followBambooValleyHeight=false;flow.playerController.followTutorialRestStopHeight=false;
            environment.AddComponent<PingchuanTownVisibility>().player=flow.playerController.transform;
            flow.mainTimeLimit=60;flow.mainTimeRemaining=60;flow.midBossTriggerElapsedTime=30;
            var camera=flow.cameraFollow != null ? flow.cameraFollow : Object.FindFirstObjectByType<WuxiaRoguelite.CameraTools.CameraFollow>();flow.cameraFollow=camera;camera.offset=new Vector3(8,16,-20);camera.portraitOffset=new Vector3(6.5f,15,-18);
            camera.landscapeFieldOfView=40;camera.portraitFieldOfView=40;camera.GetComponent<Camera>().farClipPlane=180;camera.ResetVision();
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.intensity=.85f;sun.color=new Color(1,.88f,.71f);sun.transform.rotation=Quaternion.Euler(52,-35,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.25f,.30f,.33f);RenderSettings.ambientEquatorColor=new Color(.17f,.21f,.17f);RenderSettings.ambientGroundColor=new Color(.09f,.12f,.09f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.30f,.40f,.43f);RenderSettings.fogStartDistance=42;RenderSettings.fogEndDistance=125;
            PrefabUtility.SaveAsPrefabAssetAndConnect(environment,PrefabPath,InteractionMode.AutomatedAction);
            LevelTwoMonsterPackBuilder.ApplyToActiveScene();
            PingchuanTownRewardBuilder.ApplyToActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("docs/validation/pingchuan_town");
            File.WriteAllText("docs/validation/pingchuan_town/placements.json",JsonUtility.ToJson(new PlacementReport{normal=40,elite=10,caves=5,treasures=8,herbs=7,placements=report.ToArray(),scope="Authored placement; natural-run balance and device performance require separate validation."},true));
            Validate();Debug.Log("PINGCHUAN_LEVEL2_BUILT: 40 normal + 10 elite, 5 physical caves, 8 chests, 7 herbs, 2 vision, 1 mystery.");
        }
        private static void BuildNaturalCollisions(Transform environment,GameObject model)
        {
            var metadata=JsonUtility.FromJson<ExportData>(File.ReadAllText(ArtRoot+"/PingchuanTownExport.json"));
            string cliff=metadata.materials.First(m=>m.source=="cliff").name;
            string bark=metadata.materials.First(m=>m.source=="bark").name;
            var rocks=new List<MeshCollider>();
            foreach(var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                if(!mf.name.EndsWith(cliff)&&!mf.name.EndsWith(bark))continue;
                var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;
                if(mf.name.EndsWith(cliff))rocks.Add(c);
            }
            Physics.SyncTransforms();var root=new GameObject("Mountain collision proxies").transform;root.SetParent(environment,false);
            // The controller is planar. Only high rock mass obstructs it; low dressed slopes remain walkable.
            // Rasterize the upper rock footprint, reserving the authored roads, camp and cave approaches.
            for(float z=-43.6f;z<44;z+=.8f)for(float x=-46.8f;x<47; x+=.8f)
            {
                Vector2 p=new Vector2(x,z);bool road=PingchuanTownLayout.Data.roads.Any(r=>Enumerable.Range(1,r.points.Length-1).Any(i=>PingchuanTownLayout.Distance(p,r.points[i-1],r.points[i])<r.width*.5f+.65f));
                if(road)continue;
                bool reserved=false;
                foreach(var r in new[]{new Vector4(-25,5,34,52),new Vector4(66,-67,33,26),new Vector4(27,88,25,16),new Vector4(35,-10,26,27),new Vector4(-83,54,12,12),new Vector4(-67,71,9,9),new Vector4(0,73,11,10),new Vector4(90,22,10,10),new Vector4(-92,31,9,9)})
                    if(Mathf.Pow((x/.4f-r.x)/r.z,2)+Mathf.Pow((z/.4f-r.y)/r.w,2)<1)reserved=true;
                if(reserved)continue;
                var ray=new Ray(new Vector3(x,60,z),Vector3.down);
                if(rocks.Any(c=>c.bounds.min.x<=x&&c.bounds.max.x>=x&&c.bounds.min.z<=z&&c.bounds.max.z>=z&&c.Raycast(ray,out RaycastHit hit,65)&&hit.point.y>1.2f))
                    Box(root,"High mountain rock",new Vector3(x,1,z),new Vector3(.82f,3,.82f));
            }
            foreach(var c in rocks)Object.DestroyImmediate(c);
        }
        private static Vector3 FindSafe(Vector3 p)
        {
            for(float radius=0;radius<=5;radius+=.4f)for(int a=0;a<(radius==0?1:32);a++)
            {
                Vector3 q=p+new Vector3(Mathf.Cos(a*Mathf.PI/16),0,Mathf.Sin(a*Mathf.PI/16))*radius;
                if(PingchuanTownLayout.IsRiver(q,.45f)||placed.Any(v=>Vector3.Distance(v,q)<2.4f))continue;
                if(Physics.CheckSphere(q+Vector3.up*.7f,.65f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
                return q;
            }
            throw new InvalidOperationException("No safe placement near "+p);
        }
        private static void Box(Transform root,string name,Vector3 p,Vector3 size,float angle=0)
        {var o=new GameObject(name);o.transform.SetParent(root,false);o.transform.localPosition=p;o.transform.localRotation=Quaternion.Euler(0,angle,0);var c=o.AddComponent<BoxCollider>();c.size=size;solids.Add(c);}
        private static void Obstacle(Transform root,string name,float x,float y,float w,float d,float angle=0)
        {Box(root,name,PingchuanTownLayout.Point(x,y)+Vector3.up,new Vector3(w*.4f,3,d*.4f),angle);}
        private static void Fence(Transform root,Vector2[] pts)
        {for(int i=1;i<pts.Length;i++){Vector3 a=PingchuanTownLayout.Point(pts[i-1].x,pts[i-1].y),b=PingchuanTownLayout.Point(pts[i].x,pts[i].y);Box(root,"Timber fence",(a+b)*.5f+Vector3.up,new Vector3(.14f,2,Vector3.Distance(a,b)+.05f),Quaternion.LookRotation(b-a).eulerAngles.y);}}
        private static void BuildCollisions(Transform parent)
        {
            solids.Clear();var root=new GameObject("Pingchuan Collision").transform;root.SetParent(parent,false);
            foreach(float s in new[]{-1f,1f}){Box(root,"Map boundary",new Vector3(s*48,1,0),new Vector3(.6f,8,91));Box(root,"Map boundary",new Vector3(0,1,s*45),new Vector3(97,8,.6f));}
            foreach(var r in PingchuanTownLayout.Data.rivers)for(int i=1;i<r.points.Length;i++)
            {
                Vector2 a=r.points[i-1],b=r.points[i];Vector2 along=(b-a).normalized,right=new Vector2(along.y,-along.x);
                int steps=Mathf.CeilToInt(Vector2.Distance(a,b)/.12f),lanes=Mathf.CeilToInt((r.width+.12f)/.18f);
                float length=Vector2.Distance(a,b),laneWidth=(r.width+.12f)/lanes;
                for(int lane=0;lane<lanes;lane++)
                {
                    Vector2 offset=right*((lane+.5f)*laneWidth-(r.width+.12f)*.5f);int run=-1;
                    for(int j=0;j<=steps;j++)
                    {
                        Vector2 q=Vector2.Lerp(a,b,(j+.5f)/steps)+offset;
                        bool solid=j<steps&&!PingchuanTownLayout.IsBridge(new Vector3(q.x,1,q.y),-.22f);
                        if(solid&&run<0)run=j;
                        if(!solid&&run>=0)
                        {
                            Vector2 center=Vector2.Lerp(a,b,(run+j)*.5f/steps)+offset;
                            Box(root,"River bank",new Vector3(center.x,1,center.y),new Vector3(laneWidth+.015f,2,(j-run)*length/steps+.025f),Quaternion.LookRotation(new Vector3(along.x,0,along.y)).eulerAngles.y);run=-1;
                        }
                    }
                }
            }

            foreach(var b in PingchuanTownLayout.Data.bridges)foreach(float side in new[]{-1f,1f})
                Box(root,"Bridge rail "+b.name,b.Position+b.Right*(side*(b.width*.5f+.015f))+Vector3.up,new Vector3(.10f,2,b.length),b.angle);
            foreach(float side in new[]{-1f,1f})
            {
                for(int i=0;i<7;i++)Obstacle(root,"Town shop",-25+side*11.8f,-26+i*9.6f,6.7f,7.5f);
                for(int i=0;i<5;i++)Obstacle(root,"Courtyard home",-25+side*25,-21+i*12,7.3f,6.2f);
            }
            Obstacle(root,"Guild hall",-25,48,13.5f,9.45f);Obstacle(root,"Command tent",67,-56,9,8);
            foreach(var p in new Vector2[]{new(49,-72),new(49,-59),new(79,-76),new(80,-65),new(61,-83),new(76,-49)})Obstacle(root,"Quarter tent",p.x,p.y,5.8f,5.3f);
            Obstacle(root,"Pass tent",38,79,5.2f,5.5f);
            foreach(float x in new[]{19.5f,34.5f})Obstacle(root,"Pass pier",x,89,5,1.5f);
            foreach(float x in new[]{11.5f,42.5f})Obstacle(root,"Pass wall",x,90,9,1.5f);
            foreach(var p in new Vector2[]{new(15,12),new(43,-83),new(87,-51),new(44,95)})Obstacle(root,"Watchtower",p.x,p.y,3.2f,3.2f);
            foreach(var p in new Vector2[]{new(-30,-21),new(-20,-15),new(-30,-2),new(-20,5),new(-30,17),new(-20,25),new(-39,7),new(-46,8),new(-15,30),new(56,-5),new(-66,-88)})Obstacle(root,"Market stall",p.x,p.y,3.2f,2.8f);
            foreach(var p in new Vector2[]{new(-83,58),new(-67,75),new(0,77),new(-91,35)})Obstacle(root,"Cave rear",p.x,p.y+5,12,9);
            Obstacle(root,"East cave rear",100,22,9,12);
            Fence(root,new Vector2[]{new(40,-86),new(40,-70),new(42,-54),new(51,-48)});
            Fence(root,new Vector2[]{new(59,-89),new(75,-89),new(92,-84),new(95,-66)});
            Fence(root,new Vector2[]{new(58,-47),new(71,-43),new(84,-44)});
            foreach(var range in new[]{new Vector2(12,72),new Vector2(108,162),new Vector2(198,252),new Vector2(288,342)})
            {var pts=new List<Vector2>();for(float a=range.x;a<=range.y;a+=6)pts.Add(new Vector2(35+18*Mathf.Cos(a*Mathf.Deg2Rad),-10+20*Mathf.Sin(a*Mathf.Deg2Rad)));Fence(root,pts.ToArray());}
        }
        private static void ConfigureModel()
        {
            string path=ArtRoot+"/PingchuanTown.fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var data=JsonUtility.FromJson<ExportData>(File.ReadAllText(ArtRoot+"/PingchuanTownExport.json"));
            var shader=Shader.Find("Wuxia Roguelite/Pingchuan Town Surface");if(shader==null)throw new Exception("Missing Pingchuan shader");
            Directory.CreateDirectory(ArtRoot+"/Materials");AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=false;importer.generateSecondaryUV=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var e in data.materials)
            {
                string mp=ArtRoot+"/Materials/"+e.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,mp);}
                mat.shader=shader;mat.SetColor("_Color",Color.white);mat.SetFloat("_Smoothness",1-e.roughness);mat.SetFloat("_Emission",Mathf.Min(1.4f,e.emission));
                bool leaf=e.source.StartsWith("leaf")||e.source.Contains("cloth")||e.source.Contains("roof")||e.source=="plaster";
                mat.SetFloat("_Foliage",leaf?1:0);mat.SetFloat("_Cull",e.source.StartsWith("leaf")||e.source.Contains("cloth")?0:2);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),e.name),mat);
            }
            importer.SaveAndReimport();
        }
        [MenuItem("37 MiniGame/Validate Pingchuan Town Level 2")]
        public static void Validate()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open MainPrototype first");
            var es=Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var expected=new Dictionary<EncounterType,int>{{EncounterType.NormalEnemy,40},{EncounterType.EliteEnemy,10},{EncounterType.HiddenCave,5},{EncounterType.Treasure,8},{EncounterType.Herb,7},{EncounterType.VisionRelic,2},{EncounterType.MysteryHerb,1}};
            foreach(var kv in expected)if(es.Count(e=>e.encounterType==kv.Key)!=kv.Value)throw new Exception("Encounter count mismatch: "+kv.Key);
            var coll=GameObject.Find("Pingchuan Collision").GetComponentsInChildren<BoxCollider>();
            foreach(var e in es)
            {
                var p=new Vector3(e.transform.position.x,.7f,e.transform.position.z);
                if(PingchuanTownLayout.IsRiver(p,.15f)||coll.Any(c=>Vector3.Distance(c.ClosestPoint(p),p)<.34f))throw new Exception("Blocked encounter: "+e.name+" "+p);
            }
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            if(flow.mainTimeLimit!=60||flow.midBossTriggerElapsedTime!=30||!flow.playerController.followPingchuanTownHeight)throw new Exception("Level 2 flow mismatch");
            Debug.Log("PINGCHUAN_STATIC_PASS: 73 encounters, five cave mouths; dry clear spawn points; level 2 timers retained.");
        }
    }
}
#endif
