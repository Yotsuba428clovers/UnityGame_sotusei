var flow=UnityEngine.Object.FindFirstObjectByType<SceneFlowUI>();
var settings=flow.GetComponent<SettingsMenu>();
var panel=flow.transform.Find("Settings Popup/Settings Panel");
if(UiLanguage.English)throw new System.Exception("Default Japanese not retained");
if(panel.Find("Settings Heading").GetComponent<UnityEngine.UI.Text>().text!="設定")throw new System.Exception("Japanese heading missing");
foreach(var text in panel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
    if(text.gameObject.name.EndsWith(" Value")&&string.IsNullOrEmpty(text.text))throw new System.Exception("Setting value missing: "+text.name);
panel.Find("LANGUAGE Choice").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
if(!UiLanguage.English||panel.Find("Settings Heading").GetComponent<UnityEngine.UI.Text>().text!="SETTINGS")throw new System.Exception("English switch failed");
settings.Close();flow.ToggleHelp();
if(!flow.help.transform.Find("Controls").GetComponent<UnityEngine.UI.Text>().text.Contains("Settings: Esc / OPTIONS"))throw new System.Exception("English help incomplete");
flow.ToggleHelp();
UnityEngine.SceneManagement.SceneManager.LoadScene("battleScene");
return "PASS: Japanese title/settings, numeric values, English switch, help; entering battle";
