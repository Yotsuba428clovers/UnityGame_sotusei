# 完全リセット型ローグライト：第1段階

これは第1段階時点の記録です。追加実装と最新の確認結果はRoguelite-Implementation.mdを参照してください。

## 今回の実装

- GameManagerのAwakeでRunManagerを自動追加。シーンの手動設定は不要。
- RunManagerはPreparing → Playing → Cleared/Failed → Restartingを管理。
- PlayerBuildはラン内データ専用。開始時に元のHP・通常/強攻撃力・ゲージ獲得量を設定し、HP満タン、ゲージ0、炎属性、属性バフなしで開始。
- 死亡・クリアは既存GameManagerから通知。RunResultManagerが撃破数、最大コンボ、経過時間、成否を一度だけ記録。
- SaveManagerはPlayerPrefsの専用キーPrototype.Roguelite.Records.v1に戦績だけを保存。能力・強化・通貨は保存しない。既存設定は変更しない。
- 再挑戦は既存のRキー・再挑戦ボタンからRunManagerへ接続。シーン再読み込みでプレイヤー、敵、スポナー、演出を再生成。DontDestroyOnLoadは使用しない。
- ボス撃破時の既存勝利演出・リザルトUIは維持。死亡時はプレイヤーの戦闘と新規スポーンを終了。

## 既存構成の調査結果

Unity 2022.3.0f1 / URP 14.0.7 / Input System 1.5.1。
TitleSceneとbattleSceneはBuild Settingsに登録済み。
QuickSetupが実行時にプレイヤー・カメラ・GameManager・スポナー・HUDを生成。
PlayerCombatは通常4段、強攻撃、属性切替、共通ゲージ、4属性技を実装済み。
PlayerControllerが移動と回避を担当。PlayerInputsがコードでInputActionを構築。
Enemyは共通AIで、近接と範囲攻撃を実装。EnemySpawnerは連続出現と撃破数によるボス出現を担当。
HUDとSceneFlowUIが戦闘表示・結果・シーン遷移を担当。

## Unityでの動作確認（未実施）

1. battleSceneを再生。ConsoleのRun startedを確認し、HP満タン・ゲージ0・炎属性であることを確認。
2. 攻撃してゲージを溜め、属性を変更し、被弾してHPを減らす。
3. 死亡。Run endedが一度だけ出て、追加スポーンが止まることを確認。
4. Rキーまたは再挑戦ボタン。HP・ゲージ・属性・撃破数・コンボ・敵・ボス進行が初期状態に戻ることを確認。
5. ボス撃破でもRun endedが一度だけ出て、既存の勝利演出とリザルトが表示されることを確認。
6. リザルトから再挑戦、タイトル経由の再開始、回避/属性バフ中の終了も確認。
7. エディター再起動後、戦績のみ残り、開始性能が変化しないことを確認。
8. 設定メニューの開閉後に終了・再挑戦し、時間停止や音声停止が残らないことを確認。

静的検証：既存Assembly-CSharpの参照・ソースと追加4ファイルをC#コンパイラでコンパイルし成功。Unity起動のログは取得できず、Unity上のコンパイル・プレイ確認は未完了。

## 次の段階

3択UpgradeManager、UpgradeData(ScriptableObject)、RewardUI、EnemyWaveManagerは未実装。第1段階確認後に追加する。既存EnemySpawnerと二重に出現制御を行わない構成にする。
PlayerBuildにはまだ強化を獲得するAPIを設けていない。強化データと重複ルールを導入する段階で追加する。

仕様書との差分：現在は回避が南ボタン、必殺技が東ボタン。ジャンプ・ロックオン・0.12秒のジャスト回避専用判定・ブレイクゲージは未実装。現在の回避は全時間で被ダメージ無効時にゲージを獲得する。氷は回復付き凍結、雷は強攻撃ダッシュ強化、風は12秒/1.7倍の既存仕様。QuickSetupのカメラ初期値は一人称。今回はこれらの既存挙動を変更していない。

仕様書末尾の解放型推奨より、ユーザー指定の完全リセット型を優先。保存戦績は攻略性能へ反映しない。
