UnityEngine.Application.runInBackground=true;
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var pc=player.GetComponent<PlayerController>();var anim=player.GetComponentInChildren<UnityEngine.Animator>();
foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(UnityEngine.FindObjectsSortMode.None))e.gameObject.SetActive(false);
foreach(var s in UnityEngine.Object.FindObjectsByType<EnemySpawner>(UnityEngine.FindObjectsSortMode.None))s.enabled=false;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(PlayerCombat).GetProperty("Hp").SetValue(player,player.maxHp);typeof(PlayerCombat).GetProperty("IsDead").SetValue(player,false);typeof(PlayerCombat).GetField("invincibleUntil",flags).SetValue(player,0f);pc.Locked=false;
var cc=player.GetComponent<UnityEngine.CharacterController>();cc.enabled=false;player.transform.position=new UnityEngine.Vector3(0,.1f,0);cc.enabled=true;player.transform.rotation=UnityEngine.Quaternion.Euler(0,180,0);
var cam=UnityEngine.Camera.main;foreach(var b in cam.GetComponents<UnityEngine.MonoBehaviour>())b.enabled=false;cam.transform.position=new UnityEngine.Vector3(2.8f,1.8f,-3.5f);cam.transform.LookAt(player.transform.position+UnityEngine.Vector3.up*1f);
anim.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;anim.Play("Idle",0,0);anim.Update(0);
var pres=CombatPresentation.Instance;var flash=System.Linq.Enumerable.First(pres.GetComponentsInChildren<UnityEngine.UI.Image>(),i=>i.name=="Damage Flash");
var report=new System.Text.StringBuilder();float start=UnityEngine.Time.unscaledTime;float next=start;int captures=0;bool sawStop=false,sawDamage=false,sawFlash=false;var before=player.Hp;var startPosition=player.transform.position;
bool inflicted=false;int armedFrame=UnityEngine.Time.frameCount;
UnityEditor.EditorApplication.CallbackFunction watch=null;
watch=()=>{if(!UnityEditor.EditorApplication.isPlaying||player==null){UnityEditor.EditorApplication.update-=watch;return;}if(!inflicted){if(UnityEditor.EditorApplication.isPaused || UnityEngine.Time.frameCount<armedFrame+3 || UnityEngine.Time.deltaTime>.05f)return;inflicted=true;start=UnityEngine.Time.unscaledTime;next=start;player.TakeDamage(32);player.TakeDamage(32);report.AppendLine("canAttack="+(UnityEngine.Time.time >= (float)typeof(PlayerCombat).GetField("nextActionTime",flags).GetValue(player))+" noStagger="+(!pc.IsStaggered));report.AppendLine("Accepted damage once="+(before-player.Hp)+" stagger="+pc.IsStaggered+" raycast="+flash.raycastTarget);}var elapsed=UnityEngine.Time.unscaledTime-start;if(UnityEngine.Time.unscaledTime>=next){next+=.025f;var state=anim.GetCurrentAnimatorStateInfo(0);sawStop|=UnityEngine.Time.timeScale<.1f;sawDamage|=state.IsName("Damage");sawFlash|=flash.color.a>.05f;report.AppendLine("t="+elapsed.ToString("F3")+" scale="+UnityEngine.Time.timeScale+" damage="+state.IsName("Damage")+" stagger="+pc.IsStaggered+" alpha="+flash.color.a.ToString("F3")+" position="+player.transform.position);}
if(captures==0&&elapsed>.035f){UnityEngine.ScreenCapture.CaptureScreenshot("C:/Users/G2320/UnityGame_sotusei/prototype/.uloop/outputs/player-damage-flash.png");captures++;}
if(captures==1&&elapsed>.22f){UnityEngine.ScreenCapture.CaptureScreenshot("C:/Users/G2320/UnityGame_sotusei/prototype/.uloop/outputs/player-damage-pose.png");captures++;}
if(elapsed>=1.15f){report.AppendLine("PASS noHitStop="+(!sawStop)+" motion="+sawDamage+" red="+sawFlash+" recovered="+(!pc.IsStaggered&&!anim.GetCurrentAnimatorStateInfo(0).IsName("Damage"))+" clear="+(flash.color.a==0)+" timeRestored="+(UnityEngine.Time.timeScale==1));System.IO.File.WriteAllText("C:/Users/G2320/UnityGame_sotusei/prototype/.uloop/outputs/player-damage-results.txt",report.ToString());UnityEditor.EditorApplication.update-=watch;UnityEditor.EditorApplication.isPaused=true;}}
;UnityEditor.EditorApplication.update+=watch;return "Damage verification armed";





