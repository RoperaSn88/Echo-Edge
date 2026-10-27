# ユニットの SE をアニメーションイベントで鳴らす

`BaseUnitView` は `ISoundEffectEventReceiver` を実装していて、アニメーションイベントから `SoundEffectPlay(BattleSeType)` を呼ぶと、`AudioManager` 経由で SE が鳴ります。`FlyingBaseUnitView` など `BaseUnitView` を継承した View でも同じように使えます。

敵の Animator は `BaseEnemy` / `FlyingEnemy` プレハブのルート（`BaseUnitView` と同じ GameObject）に付いているので、プレハブの変更は不要です。

## 使い方（Animation ウィンドウ）

1. SE を鳴らしたいモーション（例: `Assets/Animation/<敵>/Attack.anim`）を Animation ウィンドウで開く。
   プレビュー対象に `BaseEnemy` プレハブ（またはそれを置いたシーン上のオブジェクト）を選んでおくと、Function の候補が出ます。
2. 鳴らしたいフレームで **Add Event** を押す。
3. Inspector で **Function** に `SoundEffectPlay` を選ぶ（手入力する場合も `SoundEffectPlay`）。
4. 引数のドロップダウン（`BattleSeType`）から鳴らす SE を選ぶ。

## SE を増やすとき

1. `Assets/Scripts/Infrastructure/Audio/BattleSeType.cs` の enum に名前を足す。
2. `Assets/Addressables/Audio/SE/Battle/<名前>.wav` に音声を置き、Addressables に登録する（アドレスはパスそのまま）。

注意: アニメーションイベントの引数は enum の **番号（int）** で保存されます。`BattleSeType` の途中に値を挿入したり並べ替えたりすると、既存のイベントが別の SE を鳴らすようになるので、新しい SE は必ず末尾に追加してください。

## プレイヤーの被ダメージ SE

プレイヤーがダメージを受けたとき（`BattleManager.PlayerDamage`）に `BattleSeType.Damage` を鳴らします。
`Assets/Addressables/Audio/SE/Battle/Damage.wav` を置いて Addressables に登録してください。ファイルが無い間は、コンソールに「SE がロードされていません: Battle/Damage」と警告が出て音は鳴りません。
