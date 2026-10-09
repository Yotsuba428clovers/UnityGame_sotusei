UnityEngine.Application.runInBackground=true;
foreach(var sp in UnityEngine.Object.FindObjectsByType<EnemySpawner>(UnityEngine.FindObjectsSortMode.None))sp.enabled=false;
foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(UnityEngine.FindObjectsSortMode.None))e.gameObject.SetActive(false);
var gm=GameManager.Instance;var pc=UnityEngine.Object.FindFirstObjectByType<PlayerController>();var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
var report=new System.Text.StringBuilder();report.AppendLine("battleUiNavigation="+UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents);
int stage=0;int nextFrame=UnityEngine.Time.frameCount+3;float deadline=0;bool sawDodge=false;int managerId=gm.GetInstanceID();
UnityEditor.EditorApplication.CallbackFunction watch=null;
watch=()=>{if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=watch;UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);return;}
if(pc!=null)sawDodge|=pc.IsDodging;
if(UnityEngine.Time.frameCount<nextFrame)return;
if(stage==0){UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));stage=1;nextFrame=UnityEngine.Time.frameCount+3;}
else if(stage==1){report.AppendLine("A dodge="+sawDodge+" battleStayed="+(gm!=null&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="battleScene"));UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());stage=2;nextFrame=UnityEngine.Time.frameCount+30;}
else if(stage==2){gm.GameOver();UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));stage=3;nextFrame=UnityEngine.Time.frameCount+3;}
else if(stage==3){report.AppendLine("A noRestart="+(gm!=null&&GameManager.Instance.GetInstanceID()==managerId));UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());stage=4;nextFrame=UnityEngine.Time.frameCount+3;}
else if(stage==4){UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.Start));stage=5;nextFrame=UnityEngine.Time.frameCount+3;deadline=UnityEngine.Time.realtimeSinceStartup+8;}
else if(stage==5 && ((GameManager.Instance!=null&&GameManager.Instance.GetInstanceID()!=managerId)||UnityEngine.Time.realtimeSinceStartup>deadline)){report.AppendLine("Options restarted="+(GameManager.Instance!=null&&GameManager.Instance.GetInstanceID()!=managerId&&GameManager.Instance.State==GameManager.GameState.Playing));System.IO.File.WriteAllText("C:/Users/G2320/UnityGame_sotusei/prototype/.uloop/outputs/pad-restart-results.txt",report.ToString());UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);UnityEditor.EditorApplication.update-=watch;UnityEditor.EditorApplication.isPaused=true;}};
UnityEditor.EditorApplication.update+=watch;return "Virtual gamepad A and Options test armed";
