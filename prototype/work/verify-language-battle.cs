if(!UiLanguage.English||GameSettings.GetInt("Language",0)!=1)throw new System.Exception("Language did not persist into battle");
var gm=GameManager.Instance;
var rewards=gm.GetComponent<UpgradeManager>();
if(!rewards.Offer())throw new System.Exception("Reward unavailable");
foreach(var data in rewards.Choices)
    if(System.Text.RegularExpressions.Regex.IsMatch(UiLanguage.UpgradeName(data)+UiLanguage.UpgradeDescription(data),@"[\u3040-\u30ff\u4e00-\u9fff]"))throw new System.Exception("English upgrade incomplete");
var settings=gm.GetComponent<SettingsMenu>();settings.Open();
var panel=gm.transform.Find("Settings Popup/Settings Panel");
panel.Find("LANGUAGE Choice").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
if(UiLanguage.English)throw new System.Exception("Japanese switch failed");
settings.Close();
if(UnityEngine.Time.timeScale!=0)throw new System.Exception("Reward pause not preserved after settings");
UnityEngine.Time.timeScale=1;
UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
return "PASS: saved language across scenes, English upgrades, Japanese switch, nested settings pause";
