# 分度器QTEの成功・失敗テキスト

分度器QTE（追加反射、`ReflectGaugeView`）で針を止めたとき（時間切れを含む）に、結果のテキストを表示します。

## 動き

- 成功（エリア内で止めた）: `Reflect!!`
- 失敗（エリア外で止めた・反対の端まで行った）: `Failed...`
- 表示の演出は攻撃QTEの結果テキスト（`QTEResultText`）と同じです（フェードイン → 0.3 秒 → フェードアウト、拡大しながら）。
- ステージクリアで分度器QTEが打ち切られたときは表示しません。

## シーン作業

**必須の作業はありません。** `Result Text` が未設定のときは、攻撃QTEの結果テキスト（`UIPresenter` の `QTEResultText`）をそのまま使います。

位置や見た目を分度器用に変えたい場合のみ、以下を行ってください。

1. `Assets/prefab/UI/QTEText.prefab` を `Canvas/ReflectGauge` の外（Canvas 直下など）に置く
   - `ReflectGauge` の子にすると、分度器の回転・フェードに巻き込まれるので避けてください
2. 位置・フォント・色を調整する
3. `Canvas/ReflectGauge` の `ReflectGaugeView` → `Result Text` にその `QTEResultText` を設定

## 調整できる値（ReflectGaugeView）

| 項目 | 既定値 | 内容 |
| --- | --- | --- |
| Result Text | なし | 結果を出す `QTEResultText`。未設定なら攻撃QTEと同じもの |
| Success Text | Reflect!! | 成功時の文言 |
| Failed Text | Failed... | 失敗時の文言。空にすると表示しない（Success Text も同様） |
