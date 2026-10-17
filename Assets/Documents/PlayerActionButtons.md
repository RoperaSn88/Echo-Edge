# プレイヤー行動ボタン（一閃・装備品・防御・回復）のシーン設定

プレイヤーフェーズの行動選択を、左右クリックから縦4つのボタンに変更しました。
スクリプトだけでは動かないため、MainGame シーンで以下の設定が必要です。
**この設定をしないと、ボタンが表示されず、従来のクリック操作（左: 一閃、右: 装備品）に戻ります。防御と回復は選べません。**

## 1. ボタンの配置

1. `Assets/Scenes/MainGame.unity` を開く。
2. 既存の `Canvas` の子に空の GameObject を作り、名前を `PlayerActionButtons` にする。
   - `RectTransform` は画面の右側など、盤面の操作を邪魔しない位置に置く（例: アンカー右中央）。
   - `Vertical Layout Group` を付けると縦並びにしやすい（Spacing 10〜20 程度）。
3. `PlayerActionButtons` に `CanvasGroup` を追加する。
4. `PlayerActionButtons` の子に `Button - TextMeshPro`（UI > Button - TextMeshPro）を4つ作り、上から順に以下の名前とラベルにする。

| 順番 | GameObject 名 | ラベル |
|---|---|---|
| 1 | AttackButton | 一閃 |
| 2 | WeaponButton | 装備品 |
| 3 | DefendButton | 防御 |
| 4 | HealButton | 回復 |

5. （任意）`HealButton` の中か横に `TextMeshPro - Text (UI)` を置き、回復の残り回数表示用にする（例: `HealCountText`）。「残り2回」のように表示されます。不要なら置かなくても動きます。
6. 回復を使い切るとボタンの `interactable` が false になるので、`HealButton` の Button コンポーネントの `Disabled Color` を押せないと分かる色にしておく。

## 2. PlayerPhaseController のアタッチ

1. `PlayerActionButtons` に `PlayerPhaseController`（`Assets/Scripts/Presenteation/UI/PlayerPhaseController.cs`）を追加する。
2. インスペクターで以下を割り当てる。

| フィールド | 割り当てるもの |
|---|---|
| Canvas Group | `PlayerActionButtons` の CanvasGroup |
| Attack Button | `AttackButton` |
| Weapon Button | `WeaponButton` |
| Defend Button | `DefendButton` |
| Heal Button | `HealButton` |
| Heal Count Text | `HealCountText`（置いた場合のみ。空でも可） |

- ボタンの OnClick はスクリプト側で登録するので、インスペクターで OnClick を設定する必要はありません。
- ボタンは起動時に自動で非表示になり、プレイヤーフェーズで行動を選ぶ間だけ表示されます。`PlayerActionButtons` の GameObject 自体は**アクティブのまま**にしてください（非アクティブだと `Awake` が呼ばれず、クリック操作に戻ります）。

## 3. 確認したほうがよいところ

- `OperateInfos` のプレイヤーフェーズ用の操作説明（`PhaseKinds.Player`）が「左クリック: 一閃 / 右クリック: 装備」のような内容なら、「ボタンで行動を選ぶ」内容に差し替える。
- チュートリアル（FirstBattle など）で左右クリックの説明をしている場合は、ボタン操作の説明に差し替える。
- 一閃準備・装備選択で右クリックすると、これまでどおりプレイヤーフェーズ（ボタン表示）に戻ります。

## 挙動のまとめ

| ボタン | 挙動 |
|---|---|
| 一閃 | これまでの左クリックと同じく、一閃の角度選択へ進む |
| 装備品 | これまでの右クリックと同じく、装備品の選択へ進む（使ってもターンは終わらない） |
| 防御 | 次のエネミーフェーズの被ダメージ半減、エナジー+2個、次のプレイヤーフェーズの反射回数+1。ターン終了 |
| 回復 | 最大HPの50%を回復。ステージ全体で2回まで（ウェーブをまたいでも回数は戻らない）。ターン終了 |
