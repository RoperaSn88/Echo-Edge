# エナジーウォール（斜面）

## 概要

エナジーウォールは、1マスの対角線に沿った「斜面の壁」です。
一閃は斜面の法線に沿って反射します（反射型は向きが90度変わります）。
見た目と当たり判定は専用のプレハブで持ち、ビルダーの壁（`WallStack`）とは別のプール（`EnergyWallStack`）で管理します。

## 操作

1. 装備品からエナジーウォールを選び、床マスを左クリックする
2. 選んだマスに半透明の斜面（プレビュー）が出る
3. マウスホイールの回転、またはホイールの押し込みで向き（／ ⇔ ＼）を切り替える
4. 左クリックで確定（ここでエナジーを消費）、右クリックで取り消してマス選択に戻る（エナジーは消費しない）

前回選んだ向きが、次に使うときの初期値になります。
すでに何かがあるマスを選んだ場合は、プレビューを出さずにマス選択に戻ります。

## Sceneの編集（必須）

### 1. 斜面のプレハブを作る

1. Project で `Assets/prefab/blocks/` を右クリック → Create → Prefab で `EnergySlopeWall.prefab` を作り、開く
2. ルートの GameObject（名前は `EnergySlopeWall`）に以下を付ける
   - `BuildingView`
   - `SlopeWallView`
3. ルートの子に 3D Object → Cube を作り、名前を `Body` にする
   - Transform: Position (0, 0, 0)、Rotation (0, 0, 0)、Scale (1.41, 1, 0.12)
     - Scale X の 1.41 はマスの対角線の長さ（√2）。Y は既存の壁の高さに合わせて調整してよい
   - Tag: `Wall`（攻撃のガイドラインが壁として扱うために必要）
   - Layer: `Block`（既存の壁と同じレイヤー。一閃の反射判定に必要）
   - BoxCollider は Cube に最初から付いているものをそのまま使う
   - MeshRenderer に、エナジーウォール専用のマテリアルを割り当てる（新しく作ってよい。プレビューを半透明にしたい場合は URP/Lit の Surface Type を Transparent にする）
4. ルートの `SlopeWallView` の Inspector で次を割り当てる
   - Body → 子の `Body` の Transform
   - Body Renderer → `Body` の MeshRenderer
   - Body Collider → `Body` の BoxCollider

向き（／・＼）はコードが `Body` の Y 軸回転を ±45° に変えて表します。プレハブ上の `Body` の回転は 0 のままにしてください。

### 2. シーンにプールを置く

1. MainGame シーンで、既存の `WallStack` と同じ親の下に空の GameObject `EnergyWallStack` を作る
2. `EnergyWallStack` コンポーネントを付け、次を割り当てる
   - Slope Wall Prefab → 1 で作った `EnergySlopeWall.prefab`
   - Walls Parent → 既存の `WallStack` の Walls Parent と同じ Transform（マップの基準位置に合わせるため）
   - Initial Pool Size → 4 程度

`EnergyWallStack` が無い場合、エナジーウォールは設置されず、警告ログが出ます（エナジーも消費しません）。

### 3. 任意

- **操作説明を出したい場合**: `OperateInfos` に向き選択用の操作説明（「ホイール: 向き変更 / 左クリック: 決定 / 右クリック: 戻る」）を追加してください。`PhaseKinds` に値を足し、`EnergyWall.SelectDirectionAsync` の冒頭で `OperateInfos.Instance.SetOperateInfo(...)` を呼ぶ形になります。

## 確認してほしいこと

- 真上視点で ／ と ＼ が見た目どおりの向きになっているか。逆になっていたら `SlopeWallView` の `SlashYaw` と `BackSlashYaw` の値を入れ替えてください。
- 一閃（反射・貫通・弧曲）が斜面で想定どおりに反射・通過するか。
