# ステージの斜面の壁（SlopeWall）

## 概要

ステージ／ウェーブの CSV から、普通の壁（Wall）と同じように斜面の壁（三角柱）を配置できます。
形と反射の仕方はエナジーウォール（`EnergyWallSlope.md`）と同じで、`SlopeWallView` をそのまま使います。
エナジーウォールと違って時間で消えず、ウェーブ切り替えで普通の壁と一緒に片付けられます。
高速の貫通一閃やステージクリア時の壁消去（`TryRemoveWallAt`）でも、普通の壁と同じように壊れます。

## CSV の書き方

```
SlopeWall, 縦, 横, 角度
```

- 縦・横は Wall 行と同じマス座標（縦 0〜8、横 0〜15）
- 角度は斜面（三角形の斜辺）が向いている方向。真上から見て右を 0° とした反時計回りで、次の4つのどれか

| 角度 | 斜面の向き | 直角の角 |
| --- | --- | --- |
| 45 | 右上 | 左下 |
| 135 | 左上 | 右下 |
| -135 | 左下 | 右上 |
| -45 | 右下 | 左上 |

例:

```
Wall, 3, 5
SlopeWall, 4, 7, 135
```

角度が無い・上の4つ以外のときは、その行を飛ばして警告ログを出します。

## Sceneの編集（必須）

### 1. 斜面の壁のプレハブを用意する

作り方は `EnergyWallSlope.md` の「1. 三角柱のプレハブを作る」と同じです（ルートに `BuildingView` と `SlopeWallView`、子の `Body` に Tag `Wall`・Layer `Block`・MeshFilter・MeshRenderer・Convex の MeshCollider）。
見た目をエナジーウォールと変えたい場合は、`EnergySlopeWall.prefab` を複製して（例: `StageSlopeWall.prefab`）マテリアルだけ差し替えるのが簡単です。同じプレハブを使っても動きます。

### 2. シーンにプールを置く

1. MainGame シーンで、既存の `WallStack` と同じ親の下に空の GameObject `SlopeWallStack` を作る
2. `SlopeWallStack` コンポーネントを付け、次を割り当てる
   - Slope Wall Prefab → 1 のプレハブ
   - Walls Parent → 既存の `WallStack` の Walls Parent と同じ Transform（マップの基準位置に合わせるため）
   - Initial Pool Size → ステージで使う最大数くらい（足りなければ自動で増えます）
3. `BuildingManager` の Inspector の Slope Wall Stack に 2 の `SlopeWallStack` を割り当てる
   - 未割り当てでもシーン内の `SlopeWallStack` を自動で探しますが、明示しておくのがおすすめです

**注意**: `EnergyWallStack` コンポーネントは `SlopeWallStack` を継承しています。ステージ用には必ず `SlopeWallStack` を付けてください（`EnergyWallStack` を付けるとエナジーウォール側に拾われることがあります）。
既存の `EnergyWallStack` の設定はそのまま引き継がれるので、触る必要はありません。

`SlopeWallStack` が無い場合、斜面の壁は表示されず、エラーログが出ます（マスの占有だけは登録されます）。

## 確認してほしいこと

- 角度 45 で斜面が右上（画面の右奥）を向くか。向きが合わない場合は `StageLayoutLoader.TryGetSlopeDirection` の対応を直してください。
- 既存の `EnergyWallStack` の Inspector の値（プレハブ・親・プール数）が消えていないか。
- 一閃（反射・貫通・弧曲）が斜面で想定どおりに反射・通過するか。
