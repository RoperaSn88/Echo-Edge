# エナジーウォールの出現・消去演出

エナジーウォールを設置したとき、三角柱が**地面から生えるように伸びる**アニメーションとパーティクルを出すようにしました。
消えるとき（1フェーズ経過で消える場合と、貫通の一閃で壊された場合の両方）は、**地面へ沈むように縮む**アニメーションとパーティクルを出します。

- アニメーションはスクリプト（DOTween）で `Body` の Scale Y と Position Y を動かします。底面の高さは変わらないので、地面から生える／沈む見た目になります。
- 消えるときは、**盤面からの除去と当たり判定の無効化はすぐに行い**、見た目だけが演出のあとにプールへ戻ります（演出中の壁に一閃が当たったり、敵の移動を邪魔したりはしません）。
- プレビュー（向き選択中の半透明表示）では演出は出ません。左クリックで設置した瞬間に、いったん縮んでから生えます。
- ステージ CSV の斜面の壁（`SlopeWallStack`）には影響しません。

パーティクルはスクリプトから作れないため、以下のプレハブ編集が必要です。
**`EnergyWallEffectView` を付けない間は、従来どおり演出なしで即座に出現・消去します（エラーにはなりません）。**

## 1. プレハブにコンポーネントを付ける

`Assets/prefab/blocks/SlopeEnergyWall.prefab` を開いて編集します。

```
SlopeEnergyWall          … BuildingView + SlopeWallView + EnergyWallEffectView（追加）
├ Body                   … 三角柱（既存。触らない）
├ AppearParticle         … Particle System（追加。出現時）
└ DisappearParticle      … Particle System（追加。消去時）
```

1. ルートの `SlopeEnergyWall` に `EnergyWallEffectView` を付ける。
2. パーティクルは **`Body` の子ではなく、ルートの子** に置いてください（`Body` は伸び縮みするので、子にするとパーティクルまで潰れます）。

## 2. 出現パーティクル（AppearParticle）

ルートの子に Effects > Particle System で `AppearParticle` を作る。地面から光の粒が噴き上がるイメージです。

- Transform: Position (0, -0.5, 0)（三角柱の底面＝地面の高さ。`Body` の Scale Y を変えている場合は `-0.5 × Scale Y`）、Rotation (-90, 0, 0)（上向きに放出）
- Main
  - Duration 0.5、**Looping オフ**、**Play On Awake オフ**
  - Start Lifetime 0.4〜0.6、Start Speed 2〜4、Start Size 0.05〜0.15
  - Start Color: エナジー色（`#3e77dd` 付近、少し明るめ）
  - Simulation Space: **World**
- Emission: Rate over Time 0、Bursts に Time 0 / Count 30 程度
- Shape: **Box**、Scale (1, 1, 0.05)（回転 -90 なので、これで 1マス分の床面になります）。斜面に合わせたい場合は Shape を Mesh にして `EnergySlopeWall.obj` を割り当ててもOK
- Size over Lifetime: 1 → 0 に縮む曲線
- Color over Lifetime: アルファ 1 → 0
- Renderer: Material は既存のエフェクトで使っている加算系パーティクルのマテリアル（`Default-ParticleSystem` でも可）

## 3. 消去パーティクル（DisappearParticle）

`AppearParticle` を複製して `DisappearParticle` にし、少し変えると楽です。三角柱が砕けて散るイメージです。

- Transform: Position (0, 0, 0)（三角柱の中心）
- Main: Duration 0.5、**Looping オフ**、**Play On Awake オフ**、Start Speed 1〜3、Gravity Modifier 0.5 程度（散った粒が落ちる）
- Emission: Bursts に Time 0 / Count 40 程度
- Shape: **Mesh** で `EnergySlopeWall.obj` のメッシュ（または Box (1, 1, 1)）。破片が三角柱の形から飛び散ります
- Size over Lifetime / Color over Lifetime は出現と同じでOK

消去の演出は「沈みきる → パーティクルが消えきる」まで待ってからプールへ戻します。待ち時間は最大 `Max Disappear Particle Wait` 秒（既定 2 秒）です。
**Looping をオンにすると消えきらないので、必ずオフにしてください。**

## 4. EnergyWallEffectView の Inspector

| 項目 | 既定値 | 内容 |
|---|---|---|
| Appear Duration | 0.35 | 生えるのにかかる秒数（0 で伸びる演出なし） |
| Appear Ease | OutBack | 生えるときの動き。OutBack は少し伸びすぎてから戻る |
| Appear Particle | なし | 2 で作った `AppearParticle` |
| Disappear Duration | 0.3 | 沈むのにかかる秒数（0 で縮む演出なし） |
| Disappear Ease | InBack | 沈むときの動き。InBack は一瞬持ち上がってから沈む |
| Disappear Particle | なし | 3 で作った `DisappearParticle` |
| Max Disappear Particle Wait | 2 | 消去パーティクルを待つ最大秒数 |
| Mesh Bottom Offset | 0.5 | `Body` のローカル原点から底面までの高さ。`EnergySlopeWall.obj` は中心が原点で高さ 1 なので 0.5 のまま |

パーティクルはどちらも任意です。片方だけ割り当てても動きます。

## 確認してほしいこと

- 設置した瞬間に、三角柱が地面から生えてパーティクルが噴き上がる
- 次のエネミーフェーズ開始時に、三角柱が地面へ沈んでパーティクルが散る
- 貫通の一閃でエナジーウォールを壊したときも同じ消去演出が出て、一閃はそのまま進む
- 消去演出の直後に同じマスへ再設置しても、正しい大きさで生える
