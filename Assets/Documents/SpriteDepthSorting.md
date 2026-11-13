# スプライトの前後を z（奥行き）で決める

## 何が起きていたか
- ユニットのスプライト（`Assets/Material/SpriteShader.mat`, シェーダー `Sprite-Lit-3D-Shadow-Cutoff`）は `ZWrite Off` の透明キューだった。
- 透明キューの描画順は **SortingLayer → SortingOrder → カメラからの距離** の順。z より SortingOrder が優先されるので、
  例えばエナジーウォール（SortingOrder -3、透明）の奥にいる敵（-1）でも、ウォールより手前に描かれていた。
- 同じ SortingOrder どうしでも距離は「オブジェクト中心」で比べるだけなので、大きいメッシュとはピクセル単位で正しくならない。

## 変更内容
- シェーダーに `Depth Write`（`_SpriteDepthWrite` / キーワード `_SPRITE_DEPTH_WRITE`）と `Depth Write Cutoff`（`_DepthCutoff`）を追加。
  ON のときはテクスチャのαが Cutoff 未満のピクセルを捨てて深度を書く。SpriteRenderer.color のαは見ないので、フェード中も形は残る。
- `SpriteShader.mat`: Depth Write ON、Render Queue **2450 (AlphaTest)**。不透明ブロックの後・透明物（エナジーウォール・パーティクル）の前に描かれ、
  以降は深度テストで z の前後がピクセル単位で決まる。このマテリアルのスプライトには SortingOrder は効かなくなる。
- `SpriteShaderTransparent.mat`（新規）: Depth Write OFF の従来どおりの半透明。`Depth Offset Units = 1` で同じ位置の深度を書くスプライトの少し奥に描く。
  - `AfterImageObject.prefab`（残像。αでフェードするので深度を書くと奥のものが消える）
  - `FlyingEnemy.prefab` の `sprite_back`（スカイアの後ろ側スプライト。本体と同じ z なので Offset で本体の後ろに回す）

## Unity で確認してほしいこと
1. `SpriteShader.mat` を選択し、Inspector で **Depth Write にチェック**、**Render Queue が 2450** になっていることを確認（シェーダー再コンパイル後に一度開くと確実）。
2. MainGame を再生し、以下を確認。
   - エナジーウォールの奥／手前にいる敵が正しく隠れる・重なる。
   - 敵どうし・プレイヤーと敵が重なったとき、奥の方が隠れる。
   - スプライトの縁がギザギザに見える場合は `Depth Write Cutoff` を下げる（0.1〜0.3）。縁に背景色が出る場合は上げる。
   - 死亡・点滅などのフェード中、重なった奥のスプライトや透明エフェクトが一瞬隠れることがある（フェード中もスプライトが深度を書くため）。気になる場合は教えてください。
   - スカイアの `sprite_back` が本体より奥に出ていること。
3. ほかに「深度を書かせたくない」半透明スプライト（エフェクト等）が `SpriteShader.mat` を使っていたら、`SpriteShaderTransparent.mat` に差し替える。
4. Addressables 側のブロックに **透明マテリアル** を使っているものがあれば、そのブロックとスプライトは「ブロックが後に描かれる」ので正しくなるはずだが、
   ブロックどうしの前後がおかしい場合は別途相談。
