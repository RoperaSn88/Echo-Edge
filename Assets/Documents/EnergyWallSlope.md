# エナジーウォール（三角柱）

## 概要

エナジーウォールは、1マスを対角線で半分に切った「三角柱の壁」です。
真上から見ると直角二等辺三角形で、斜辺の面（斜面）に当たった一閃は斜面の法線に沿って反射します（反射型は向きが90度変わります）。
残りの2面はマスの辺に沿った普通の壁として反射します。
見た目と当たり判定は専用のプレハブで持ち、ビルダーの壁（`WallStack`）とは別のプール（`EnergyWallStack`）で管理します。

## 操作

1. 装備品からエナジーウォールを選ぶと、真上視点になり、カーソルの下のマスに半透明の三角柱（プレビュー）が付いてくる
2. マウスホイールの回転で向きを 90° ずつ回す（上で時計回り、下で反時計回り）。ホイールの押し込みでも時計回りに回る
   - 向きは斜面が向く方向で、右上 → 右下 → 左下 → 左上 の4つ。最初はプレイヤー側を向く左上
3. 左クリックでそのマスに設置する（ここでエナジーを消費）。右クリックで装備選択に戻る（エナジーは消費しない）

前回選んだ向きが、次に使うときの初期値になります。
敵や壁がいるマスと盤面の外ではプレビューが消え、そこを左クリックしても何も起きません。

## Sceneの編集（必須）

### 1. 三角柱のプレハブを作る

三角柱のメッシュは `Assets/prefab/blocks/EnergySlopeWall.obj` として入れてあります（1マス分、高さ1、中心が原点）。

1. Project で `Assets/prefab/blocks/` を右クリック → Create → Prefab で `EnergySlopeWall.prefab` を作り、開く
2. ルートの GameObject（名前は `EnergySlopeWall`）に以下を付ける
   - `BuildingView`
   - `SlopeWallView`
3. ルートの子に空の GameObject を作り、名前を `Body` にする
   - Transform: Position (0, 0, 0)、Rotation (0, 0, 0)、Scale (1, 1, 1)
     - Scale Y は既存の壁の高さに合わせて調整してよい（X と Z は 1 のまま。1マスぴったりの大きさです）
   - Tag: `Wall`（攻撃のガイドラインが壁として扱うために必要）
   - Layer: `Block`（既存の壁と同じレイヤー。一閃の反射判定に必要）
   - `MeshFilter` を付け、Mesh に `EnergySlopeWall.obj` の中のメッシュを割り当てる
   - `MeshRenderer` を付け、エナジーウォール専用のマテリアルを割り当てる（新しく作ってよい。プレビューを半透明にしたい場合は URP/Lit の Surface Type を Transparent にする）
   - `MeshCollider` を付け、Mesh に同じメッシュを割り当て、**Convex にチェック**を入れる
4. ルートの `SlopeWallView` の Inspector で次を割り当てる
   - Body → 子の `Body` の Transform
   - Body Renderer → `Body` の MeshRenderer
   - Body Collider → `Body` の MeshCollider

向きはコードが `Body` の Y 軸回転を 0° / 90° / 180° / 270° に変えて表します。プレハブ上の `Body` の回転は 0 のままにしてください。
回転 0 のとき、斜面は右上（+X, +Z）を向き、直角の角はマスの左下に来ます。

### 2. シーンにプールを置く

1. MainGame シーンで、既存の `WallStack` と同じ親の下に空の GameObject `EnergyWallStack` を作る
2. `EnergyWallStack` コンポーネントを付け、次を割り当てる
   - Slope Wall Prefab → 1 で作った `EnergySlopeWall.prefab`
   - Walls Parent → 既存の `WallStack` の Walls Parent と同じ Transform（マップの基準位置に合わせるため）
   - Initial Pool Size → 4 程度

`EnergyWallStack` が無い場合、エナジーウォールは設置されず、警告ログが出ます（エナジーも消費しません）。

### 3. 任意

- **操作説明を出したい場合**: 床マス選択（`PhaseKinds.Equip`）の操作説明に「ホイール: 向き変更」を足してください。エナジーウォール以外の装備品でもこの説明が出るので、分けたい場合は `PhaseKinds` に値を足し、`PlayerEquipPhase.WaitPhase` の冒頭で `SelectedEffect is EnergyWall` のときだけそちらを出す形になります。

## 確認してほしいこと

- `Body` の回転が 0 のとき、斜面が右上（+X, +Z）を向いているか。OBJ は取り込み時に X が反転する前提で作っています。斜面が左上を向いていたら、`SlopeWallView` の `BaseYaw` を 270 に変えてください（この三角柱では左右反転と 270° 回転が同じ形になります）。
- ホイールを上に回したときに時計回りに回るか。逆なら `EnergyWall.OnScroll` の `1` と `-1` を入れ替えてください。
- 一閃（反射・貫通・弧曲）が斜面で想定どおりに反射・通過するか。
