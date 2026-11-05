# エナジー獲得トレイルのプレハブ・シーン設定

敵に一閃が当たったとき（撃破時も含む）、敵の位置から UI 上にトレイルを出し、PlayerInfo の Energy 表示まで飛ばすようにしました。

- 演出の量はエナジーの獲得量に関係なく一定です（1ヒットにつきトレイル1本）。
- **エナジーの加算と表示の反映は、トレイルの先端が Energy 表示に着いた時点で行います。**
- 着いたときに Energy 表示が少し弾みます（`Arrival Punch Scale` を 0 にすると無効）。
- ヒットストップ中（timeScale ≒ 0）でも動くよう、移動とパーティクルは実時間で動きます（スクリプトが子の Particle System の `Use Unscaled Time` を自動でオンにします）。
- 座標変換は Canvas の Render Mode（Overlay / Camera）に依存しません。

パーティクルはスクリプトから作れないため、以下の設定が必要です。
**`UIPresenter` の `Energy Trail Pool` を割り当てない間は、従来どおり敵の位置で光ってすぐ加算されます（エラーにはなりません）。**

## 1. トレイルのプレハブを作る

`Assets/Scenes/MainGame.unity` の Canvas 上で作ってからプレハブ化するのがおすすめです（既存の `PlayerPhaseViewController/AttackButton/ButtonbackGround/UIParticle` と同じ作り方です）。

```
EnergyTrail            … RectTransform + EnergyTrailPresenter
└ Head                 … RectTransform + UIParticle（先端。これが動く）
   ├ Trail             … Particle System（トレイル本体）
   └ Arrival           … Particle System（到着時のはじけ。任意）
```

1. Canvas の子に空の GameObject `EnergyTrail` を作り、`EnergyTrailPresenter` を付ける。
   - RectTransform は Anchor を中央、Position (0, 0, 0) のままでOK（位置はスクリプトが毎回計算します）。
2. `EnergyTrail` の子に空の GameObject `Head` を作り、`UIParticle`（Coffee.UIExtensions）を付ける。
   - Scale: 既存のボタン演出と同じ 10 から試してください。軌跡が先端からずれて見える場合は **1** にして、パーティクルの Size を 10 倍にしてください。
   - Maskable はオフ。
3. `Head` の子に Effects > Particle System で `Trail` を作る。
   - Main: **Looping オン / Play On Awake オフ / Simulation Space: World / Start Speed 0**、Start Lifetime 0.3〜0.5 程度、Start Color はエナジー色（`#3e77dd` 付近）。
   - Emission: Rate over Time 0、**Rate over Distance** で粒の密度を決める（Scale 10 なら 1〜3、Scale 1 なら 0.1〜0.3 くらいから調整）。
   - Size over Lifetime: 1 → 0 に縮む曲線にすると尾が細くなります。
   - Renderer の Material は既存の UIParticle で使っている加算系のものでOK。
   - 線状にしたい場合は Trails モジュールをオンにして Renderer の Trail Material も設定してください（UIParticle 側で Trail 用マテリアルも描画されます）。
4. （任意）`Head` の子に `Arrival` を作る。
   - Main: **Looping オフ / Play On Awake オフ**、Burst で 10〜20 粒を散らす程度。
5. `Head` の `UIParticle` のインスペクターで「Refresh」等を押し、`Trail`・`Arrival` が Particles に入っていることを確認する。
6. `EnergyTrailPresenter` に割り当てる。

| フィールド | 割り当てるもの / 目安 |
|---|---|
| Head | `Head` の RectTransform |
| Trail Particle | `Trail` |
| Arrival Particle | `Arrival`（無ければ空でOK） |
| Flight Time | 0.7（敵から Energy に着くまでの秒数） |
| Curve Rate | 0.35（軌道の膨らみ。左右はランダム） |
| Ease | InQuad（最初ゆっくり→加速して吸い込まれる） |
| Arrival Punch Scale | 0.15（0 で弾み無効） |
| Max Fade Out Time | 2（到着後に残った粒が消えるのを待つ上限秒数） |

7. `EnergyTrail` を `Assets/Prefabs` 以下などにドラッグしてプレハブ化し、シーン上のものは削除する。

## 2. プールを置いて UIPresenter に割り当てる

1. MainGame シーンの Canvas の子（例: `Canvas/Infos` の兄弟）に空の GameObject `EnergyTrailPool` を作る。
   - **必ず Canvas の子にしてください**（既存の `EnergyPool` は UIPresenter の子なので流用できません）。
   - RectTransform は全画面ストレッチ（Anchor Min (0,0) / Max (1,1)、Offset 0）がおすすめ。
   - 他の UI より手前に出したい場合は、Hierarchy で PlayerInfo より下（後ろ）に置いてください。
2. `EnergyTrailPool` に既存の `EnergyPool` コンポーネントを付ける（プールの処理は共通です）。
   - Object To Pool: 1 で作った `EnergyTrail` プレハブ
   - Init Size: 6 程度（足りなければ自動で増えます）
3. `UIPresenter` の `Energy Trail Pool` に `EnergyTrailPool` を割り当てる。
   - 到着先は既存の `Destination`（`Canvas/Infos/PlayerInfo/content/Energy/Energy_Frame/Energy_Frame (1)`）を使います。

## 確認ポイント

- 一閃が敵に当たる／倒すたびにトレイルが1本ずつ Energy へ飛ぶ。
- Energy のゲージ・個数は、トレイルが Energy に着いた瞬間に増える（飛んでいる間は増えない）。
- 反射で複数回当てたとき、ヒットストップ中でもトレイルが止まらず飛ぶ。
- エナーの犠牲など、エナジー報酬が無い被弾ではトレイルが出ない。
