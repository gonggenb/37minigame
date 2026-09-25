#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.Map;

namespace WuxiaRoguelite.EditorTools
{
    /// <summary>Flood sampled collision-free space at player radius; complementary to real Rigidbody checks.</summary>
    public static class PingchuanTownNavigationValidation
    {
        [Serializable] private class Report {public int reachableCells,clearCells,encounters;public float cellSize=.4f,playerRadius=.34f;public string[] unreachable;public string scope="Static collision flood; not a substitute for real player control.";}
        [MenuItem("37 MiniGame/Validate Pingchuan Town Reachability")]
        public static void Run()
        {
            const int nx=241,nz=226;const float step=.4f;
            Vector3 Point(int k)=>new Vector3(-48+(k%nx)*step,.7f,-45+(k/nx)*step);
            int Index(Vector3 p)=>Mathf.Clamp(Mathf.RoundToInt((p.z+45)/step),0,nz-1)*nx+Mathf.Clamp(Mathf.RoundToInt((p.x+48)/step),0,nx-1);
            bool[] clear=new bool[nx*nz],seen=new bool[nx*nz];Physics.SyncTransforms();
            var hero=GameObject.Find("Player").GetComponent<Collider>();bool active=hero.enabled;hero.enabled=false;
            try
            {
                for(int i=0;i<clear.Length;i++)clear[i]=!Physics.CheckSphere(Point(i),.34f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                var queue=new Queue<int>();int start=Index(PingchuanTownLayout.Spawn);if(!clear[start])throw new Exception("Spawn is blocked");queue.Enqueue(start);seen[start]=true;
                while(queue.Count>0)
                {
                    int k=queue.Dequeue(),x=k%nx,z=k/nx;
                    foreach(int dir in new[]{-1,1,-nx,nx})
                    {
                        if(dir==-1&&x==0||dir==1&&x==nx-1||dir==-nx&&z==0||dir==nx&&z==nz-1)continue;
                        int next=k+dir;if(seen[next]||!clear[next])continue;seen[next]=true;queue.Enqueue(next);
                    }
                }
                var es=UnityEngine.Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                var missing=es.Where(e=>!seen[Index(e.transform.position)]).Select(e=>e.name).ToArray();
                Directory.CreateDirectory("docs/validation/pingchuan_town");File.WriteAllText("docs/validation/pingchuan_town/navigation.json",JsonUtility.ToJson(new Report{reachableCells=seen.Count(v=>v),clearCells=clear.Count(v=>v),encounters=es.Length,unreachable=missing},true));
                if(missing.Length>0)throw new Exception("Unreachable encounters: "+string.Join(", ",missing));
                Debug.Log("PINGCHUAN_NAVIGATION_PASS: all "+es.Length+" encounter positions reachable from spawn at player radius.");
            }
            finally{hero.enabled=active;}
        }
    }
}
#endif
