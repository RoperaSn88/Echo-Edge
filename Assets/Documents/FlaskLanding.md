# シーカーのフラスコ：滞空時間と着地演出

フラスコの滞空時間の調整と、着地時のエフェクト・SE の土台です。設定はすべて `Assets/prefab/Enemy/flasco.prefab` のルートの `FlaskObject` で行います。

## 1. 滞空時間

- **Flight Duration**: 投げてから着地するまでの秒数。1.8666 → **0.8** に短くしました。好みで調整してください。
- **Jump Power**: 放物線の高さ。滞空時間を短くして山が急に見える場合は下げてください。
- **Fit Animation To Flight**（既定オン）: フラスコの回転アニメーション（`flasco/idle.anim`、2秒）を、滞空時間にちょうど収まる速さで再生します。滞空時間を変えても、アニメーションが途中で切れたり余ったりしません。オフにすると等速で再生され、着地の時点で切れます。
- 複数本投げるときの間隔は、シーンの `FlaskThrower` の **Throw Interval**（既定 0.1 秒）です。

## 2. 着地 SE

1. 着地 SE の wav を `Assets/Addressables/Audio/SE/Battle/FlaskLanding.wav` に置く（ファイル名は `BattleSeType.FlaskLanding` と同じにする）。他の Battle SE と同じく Addressables に登録してください。
2. `FlaskObject` の **Play Landing Se**（既定オン）で鳴らすかどうかを切り替えられます。

wav が無いうちは、着地のたびに「SE がロードされていません: Battle/FlaskLanding」の警告が出ます。気になる場合は Play Landing Se をオフにしてください。

## 3. 着地エフェクト

1. 着地エフェクトのプレハブを作る（例: `Assets/prefab/Effect/FlaskLanding.prefab`）。ParticleSystem は **Play On Awake** をオン、**Looping** はオフにする。
2. `FlaskObject` の **Landing Effect Prefab** に割り当てる。
   - **Landing Effect Offset**: 着地位置からのずらし（床にめり込む・浮く場合に調整）。
   - **Landing Effect Lifetime**: エフェクトを消すまでの秒数。0 なら、エフェクト内の ParticleSystem の「Start Delay + Duration + Start Lifetime」の最大値で自動で消えます（最大5秒）。
3. エフェクトはプレハブの回転のまま、着地した位置に出ます（フラスコの子にはならないので、フラスコがプールへ戻っても残ります）。

未設定ならエフェクトは出ません。

## 実装メモ

- 着地の瞬間（放物線の移動が終わったとき）に `FlaskObject.PlayLandingEffect` で SE とエフェクトを出します。エナーが現れるのはその後（全部のフラスコが着地してから）です。
- 新しい SE `FlaskLanding` は `BattleSeType` の末尾に追加しています。
