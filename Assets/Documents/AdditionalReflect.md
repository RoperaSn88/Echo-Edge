# 追加反射（分度器QTE）

仕様: Notion「1-1-5. 反射」

## 動き

- 反射の剣（通常・高速一閃）で、【反射回数】分は今までどおり QTE なしで反射します。
- 【反射回数】を使い切って壁に当たったとき、分度器QTE（`ReflectGauge`）を表示します。
  - 分度器は弧の真ん中が壁の面の法線を向くように回転します（上下左右に加えて、斜面なら45度刻みで斜め向き）。
  - 青いエリアは反射角を中心に表示します。幅は最初 `Initial Area Width`（45度）で、追加反射のたびに半分（22.5 → 11.25 …）になります。
  - 反射角が分度器の端（0度・180度付近）に寄ってエリアがはみ出す場合は、反射する側（左右）を中心にした分度器に回して収めます。
  - 針はエリアの反対側の端から片道だけ回り、クリックで止まります。エリア内なら追加で1回反射し、次の壁でまた QTE が出ます。エリア外、または反対の端まで行ったら一閃はそこで終了です。
  - 追加反射の回数に上限はありません。
  - QTE 中はカメラをプレイヤーへ近づけます（距離 5、0.2 秒。`CameraManager.ActReflectGaugeZoomIn` / `ActReflectGaugeZoomOut`）。終わるとズーム前の距離に戻します。
  - 敵に攻撃が当たったときのズームも、攻撃（被弾・撃破演出）が終わったらズーム前に戻します（`ActAttackHitZoomIn` / `ActAttackHitZoomOut`）。
- 攻撃QTE（敵に最初に当たったときのもの）が出ている間は、それが終わってから分度器QTEを出します。
- 追加反射の分も反射ダメージ倍率（1 + 0.1 × 反射数）に入ります（通常の一閃）。
- 予測線は今までどおり【反射回数】分だけ描画します。

## 角度の表し方

`RadialFillRange` の値（Min Angle / Max Angle / Origin Angle）は度で指定します。
`ReflectGaugeView` は起動時に `correct-area` の Origin Angle = 90、Clockwise = オフ に設定するので、
エリアの角度は「分度器の右端 = 0度、真上 = 90度、左端 = 180度」になります。

## シーン（MainGame）でやったこと

スクリプトから直接 YAML を編集済みです。Unity で開いて以下を確認してください。

1. `Canvas/ReflectGauge` に `ReflectGaugeView` コンポーネントを追加し、参照を設定
   - Canvas Group: `ReflectGauge` の CanvasGroup
   - Content: `ReflectGauge/content`
   - Area: `content/correct-area` の RadialFillRange
   - Needle: `content/needle`（回転軸の空オブジェクト。子の `needle (1)` ではない）
2. `content` の Pivot を (0.5, 0)、Pos Y を -200 に変更（見た目の位置は同じ。分度器の中心で回転させるため）
3. `correct-area` の RadialFillRange を角度表記の値に変更（Min 0 / Max 90 / Origin 90 / Clockwise オフ）

`ReflectGauge` の GameObject はアクティブのままにしてください（起動時に CanvasGroup の alpha を 0 にして隠します）。
`ReflectGaugeView` が無い場合、追加反射は発生せず今までどおりの動きになります。

## 調整できる値（ReflectGaugeView）

| 項目 | 既定値 | 内容 |
| --- | --- | --- |
| Initial Area Width | 45 | 最初のエリアの幅（度）。追加反射のたびに半分 |
| Needle Speed | 240 | 針の速さ（度/秒）。端から端まで 0.75 秒 |
| Max Sweep Count | 1 | 針が端から端まで動く回数の上限。1 で片道。0 で止めるまで往復 |
| Max Additional Reflect Count | 0 | 1回の一閃での追加反射の上限。0 で上限なし |
| Fade Duration | 0.15 | 表示・非表示のフェード時間 |
| Result Duration | 0.35 | 針を止めてから消えるまでの時間 |

## 確認ポイント

- 分度器の回転は Unity の Z 回転（反時計回りが正）です。仕様の表「入射135 / 反射225」の行は +90 になります。
- 盤面の x を画面の右、z を画面の上として角度を計算しています。カメラの向きが違う場合は教えてください。
- 表示位置は画面中央（シーンの `ReflectGauge` の位置）です。
