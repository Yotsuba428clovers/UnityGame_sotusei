var stone=UnityEngine.GameObject.Find("StoneAttackVerification");
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
stone.transform.position=new UnityEngine.Vector3(0,0,0);stone.transform.rotation=UnityEngine.Quaternion.identity;player.transform.position=new UnityEngine.Vector3(0,0,5);
foreach(var b in player.GetComponents<UnityEngine.MonoBehaviour>()) b.enabled=false;
var enemy=stone.GetComponent<Enemy>();enemy.StopAllCoroutines();enemy.enabled=false;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(Enemy).GetField("attacking",flags).SetValue(enemy,false);typeof(Enemy).GetField("nextAttack",flags).SetValue(enemy,0f);
var anim=stone.GetComponentInChildren<UnityEngine.Animator>();anim.Play("Idle",0,0);anim.Update(0);
var cam=UnityEngine.Camera.main;cam.transform.position=new UnityEngine.Vector3(10,7,12);cam.transform.LookAt(new UnityEngine.Vector3(0,3,0));
var head=System.Linq.Enumerable.First(stone.GetComponentsInChildren<UnityEngine.Transform>(),t=>t.name=="Head");
var hand=System.Linq.Enumerable.First(stone.GetComponentsInChildren<UnityEngine.Transform>(),t=>t.name=="RightHand");
var report=new System.Text.StringBuilder();var nextSample=UnityEngine.Time.time;var start=nextSample;
UnityEditor.EditorApplication.CallbackFunction watcher=null;
watcher=()=>{if(!UnityEditor.EditorApplication.isPlaying||stone==null){UnityEditor.EditorApplication.update-=watcher;return;}if(UnityEngine.Time.time>=nextSample){nextSample+=.35f; var rt=UnityEngine.RenderTexture.GetTemporary(920,518,24); var previous=cam.targetTexture;var active=UnityEngine.RenderTexture.active;cam.targetTexture=rt;cam.Render();UnityEngine.RenderTexture.active=rt;var image=new UnityEngine.Texture2D(920,518,UnityEngine.TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,920,518),0,0);image.Apply();System.IO.File.WriteAllBytes("C:/Users/G2320/UnityGame_sotusei/prototype/.uloop/outputs/stone-frame-"+(UnityEngine.Time.time-start).ToString("F1")+".png",image.EncodeToPNG());cam.targetTexture=previous;UnityEngine.RenderTexture.active=active;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);var state=anim.GetCurrentAnimatorStateInfo(0);report.AppendLine("t="+(UnityEngine.Time.time-start).ToString("F2")+" attack="+state.IsName("Attack")+" normalized="+state.normalizedTime.ToString("F2")+" modelRot="+anim.transform.localEulerAngles+" head="+head.position+" hand="+hand.position);}if(UnityEngine.Time.time-start>=7){System.IO.File.WriteAllText("C:/Users/G2320/UnityGame_sotusei/prototype/.uloop/outputs/stone-runtime-results.txt",report.ToString());UnityEditor.EditorApplication.update-=watcher;UnityEditor.EditorApplication.isPaused=true;}};
UnityEditor.EditorApplication.update+=watcher;
enemy.StartCoroutine((System.Collections.IEnumerator)typeof(Enemy).GetMethod("AttackRoutine",flags).Invoke(enemy,null));
return "Natural Enemy AttackRoutine verification armed, 7 seconds";



