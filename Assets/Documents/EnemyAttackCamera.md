# 敵の攻撃時にプレイヤーと敵を同時に映すカメラのシーン設定

敵が攻撃を始めるとき、攻撃する敵とプレイヤーを `CinemachineTargetGroup` に追加し、2人が同時に画面に収まるようにカメラを動かすようにしました。
ターゲットグループはスクリプトから作れないため、MainGame シーンで以下の設定が必要です。
**ターゲットグループを割り当てない場合も、攻撃開始時の位置で2人が映るよう一度だけ寄せます（警告ログが出ます）。ただし踏み込みなどで動いたときの追尾はされません。**

## 流れ

1. 敵の攻撃開始時（近距離の敵は踏み込んだ後）、`BaseUnitView.WaitToCameraZoomWithPlayer` が「敵自身・プレイヤー」の Transform を `ReadOnlySpan<Transform>` で `CameraManager.ActSetCameraTargets` に渡す
2. `CameraManager` がグループを空にしてから対象を追加し、全員が収まる距離（5〜13 の範囲）とグループ中心を計算して、0.25 秒でカメラを寄せる
3. 寄り終わった後は、対象が動いてもグループの中心を追い続ける
4. 攻撃が終わり `ActResetCameraTarget` が呼ばれると、グループを空に戻して追尾をやめ、カメラを元の位置へ戻す
   （他のカメラ操作 `ActSetCameraTarget` / `ActMoveCameraToTopAngle` などが呼ばれたときも同様に解除されます）

## 1. ターゲットグループの作成

1. `Assets/Scenes/MainGame.unity` を開く。
2. Hierarchy で右クリック > Create Empty で空の GameObject を作り、名前を `EnemyAttackTargetGroup` にする。
   - ※ メニューの「Cinemachine > Target Group Camera」はカメラまで作ってしまうので使わないでください。
3. Inspector の Add Component から **Cinemachine Target Group** を追加する。
4. 設定は以下のとおり（基本は初期値のままで OK）。
   - **Position Mode: Group Center**
   - **Rotation Mode: Manual**
   - **Update Method: Update**（初期値）
   - **Targets: 空のまま**（スクリプトが攻撃のたびに追加・削除します）

## 2. CameraManager への割り当て

1. `CameraManager` が付いている GameObject を選択する。
2. Inspector に増えた **Target Group** 欄へ、手順1で作った `EnemyAttackTargetGroup` をドラッグ＆ドロップする。

## 調整したいとき

`Assets/Scripts/Infrastructure/Camera/CameraManager.cs` の定数で調整できます。

| 定数 | 意味 | 初期値 |
| --- | --- | --- |
| `GroupMemberRadius` | 対象1人あたりの半径。大きくすると引き気味になる | 1 |
| `GroupFramingPadding` | 画面端の余白の倍率。大きくすると引き気味になる | 1.2 |
| `ZoomCameraDistance` / `DefaultCameraDistance` | カメラ距離の下限・上限 | 5 / 13 |

## 確認方法

1. MainGame を再生し、敵フェーズまで進める。
2. 遠距離の敵の攻撃で、敵とプレイヤーの両方が画面に入ることを確認する。
3. 近距離（プレイヤーの隣の列）の敵が踏み込んで攻撃するときも、2人が画面に入ったままになることを確認する。
4. 攻撃後にカメラが全体を見渡す位置へ戻ることを確認する。
