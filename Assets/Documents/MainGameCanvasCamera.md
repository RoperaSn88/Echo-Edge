# MainGame の UI を Screen Space - Camera にする（Option/Credit/License を手前に出す）

Option / Credit / License シーンの UI は Screen Space - Camera で描いています。
MainGame の UI が Screen Space - Overlay のままだと、Overlay は全カメラの描画後に描かれるため、
Option などをどう設定しても MainGame の UI より手前に出せません。
そこで MainGame の UI キャンバスを Screen Space - Camera にし、同じ `UICamera` で描くようにします。
同じカメラで描くキャンバス同士は **Sort Order が大きいほど手前** になります。

## コード側の対応（このブランチで実装済み）

Overlay では「UI のワールド座標 = スクリーン座標」でしたが、Camera モードでは一致しません。
そのため、スクリーン座標から UI を置いていた箇所を `UIScreenPosition`（`Assets/Scripts/Presenteation/UI/UIScreenPosition.cs`）経由に直しました。
キャンバスが Overlay でも Camera でも正しく動くので、シーン設定より先にマージしても問題ありません。

- `TextObject`（ダメージ数字）: 敵の位置のスクリーン座標にダメージ数字を置く処理
- `TextBoxPresenter`（チュートリアルのコーチマーク）: MainGame の UI を囲むときの位置計算

## 1. MainGame のキャンバスを Camera モードにする

1. `Assets/Scenes/MainGame.unity` を開く。
2. Hierarchy の `Canvas` を選択し、Canvas コンポーネントを以下に変更する。
   - **Render Mode: Screen Space - Camera**
   - **Render Camera: `UICamera`**（同じシーンの UICamera をドラッグ＆ドロップ）
   - **Plane Distance: 50**（Option などの 1 より奥。near 0.3〜far 1000 の範囲なら OK）
   - **Sort Order: 0**（今のまま）
3. `Canvas (1)` も同じように変更する（**Sort Order: 1** は今のまま）。

## 2. Option / Credit / License の Sort Order を上げる

MainGame の UI（Sort Order 0, 1）より大きくします。

1. `Assets/Scenes/Option.unity` の Canvas の **Sort Order を 10** にする（今は 3）。
2. `Assets/Scenes/Credit.unity` の Canvas の **Sort Order を 10** にする（今は 0）。
3. `Assets/Scenes/License.unity` の Canvas の **Sort Order を 10** にする（今は 0）。

※ System シーンのカーソル（PointerCanvas, Overlay, Sort Order 999）とチュートリアルのキャンバスは Overlay のままなので、引き続き一番手前に出ます。

## 3. 確認すること

- バトル中に Esc で Option を開き、MainGame の UI より手前に Option が出ること。
- ダメージ数字が敵の位置に出ること。
- チュートリアルのコーチマークが、指定した MainGame の UI を正しく囲むこと。
- 照準時（真上視点で直交投影になるとき）も UI が崩れないこと。
  UICamera は Cinemachine の Brain に制御されていない別カメラ（透視投影のまま）なので、影響しない想定です。

## 注意

- UICamera にポストエフェクト（Post Processing）を有効にしている場合、MainGame の UI にもかかるようになります。
- UICamera の Culling Mask は UI レイヤーのみです。MainGame の UI オブジェクトは UI レイヤーのままにしてください。
