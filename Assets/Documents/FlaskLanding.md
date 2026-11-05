# シーカーのフラスコ：滞空時間と着地パーティクル

設定はすべて `Assets/prefab/Enemy/flasco.prefab` のルートの `FlaskObject` で行います。

## 1. 滞空時間

- **Flight Duration**: 投げてから着地するまでの秒数（1.8666 → 0.8 に短縮済み）。
- **Jump Power**: 放物線の高さ。
- 複数本投げるときの間隔は、シーンの `FlaskThrower` の **Throw Interval**（既定 0.1 秒）です。

## 2. 着地パーティクル

**Landing Particle**（ParticleSystem）に、着地したときに出すパーティクルを割り当てます。位置の指定はなく、フラスコが最後にいた位置（着地位置）で出ます。未設定なら何も出ません。

- **フラスコの子のパーティクルを割り当てた場合**（現在は子の `AppearParticle` を割り当て済み）
  - 着地した瞬間に、その場で `Play` します。Play On Awake はオフにしておいてください。
  - フラスコは、アニメーションの終わり（`OnFlaskAnimationEnd`）に加えて、パーティクルが再生し終わるまで待ってからプールへ戻ります。
- **パーティクルのプレハブを割り当てた場合**
  - 着地位置に生成して再生し、「Start Delay + Duration + Start Lifetime」の最大値（最大5秒）が過ぎたら消します。フラスコの子にはならないので、フラスコがプールへ戻っても残ります。

## 3. 着地 SE

着地の SE はフラスコでは鳴らしません（シーカー側で鳴らす方針）。シーカーのアニメーションイベントで `SoundEffectPlay`（`BattleSeType`）を使ってください（[UnitSoundEffect.md](UnitSoundEffect.md)）。
