# ボスBGM

ボス戦では、通常BGMとは違う方法でピンチを表現する。

| | 通常BGM | ボスBGM |
|---|---|---|
| 再生する曲 | `Battle` | `Boss` |
| ピンチ（HP 30%未満）になったとき | `Pinch` を同じ再生位置から重ねてフェードイン | `BossPinch` に曲ごと切り替え（最初から再生） |
| ピンチを脱したとき | `Pinch` を0.5秒でフェードアウト（`Battle` はそのまま） | `Boss` を最初から再生し直す |

## ボス戦の判定

クリア条件が「ボス撃破」（`StageClearConditionType.DefeatBoss`）のウェーブをボス戦として扱う。

- ステージ開始時（`StartPhase`）: 最初のウェーブがボス戦なら `Boss`、それ以外は `Battle` を再生する。
- 次のウェーブへ進んだとき（`NextWavePhase`）: 通常⇔ボスが切り替わる場合だけ曲を変える。同じ種類のウェーブが続くときは流し続ける。
  - 通常→ボス: 重ねていた `Pinch` を止め、ピンチ中なら `BossPinch`、そうでなければ `Boss` を再生する。
  - ボス→通常: `Battle` を再生し、ピンチ中なら `Pinch` を重ねる。

処理は `Assets/Scripts/Applicatiton/Battle/Main/BattleBgmController.cs` にまとまっている。

## Unity での作業

1. ボスBGMの音源を2つ用意し、次の名前で置く（`AudioManager` は `BgmAudioType` の名前から Addressables のアドレスを作る）。
   - `Assets/Addressables/Audio/BGM/Boss.wav`（通常時のボス曲）
   - `Assets/Addressables/Audio/BGM/BossPinch.wav`（ピンチ時のボス曲）
2. 2つのファイルを選択し、Inspector の **Addressable** にチェックを入れる。アドレスはファイルパスのまま（`Assets/Addressables/Audio/BGM/Boss.wav` など）にする。既存の `Battle.wav` / `Pinch.wav` と同じグループに入れればよい。
3. 両方の AudioClip の Import Settings で、既存の `Battle.wav` と同じ設定（Load Type など）にしておく。ループ再生はコード側で行うので、曲の頭と終わりがループでつながるように作っておく。
4. ボス戦にしたいウェーブのCSVの1行目のクリア条件を「ボス撃破」（`DefeatBoss`）にする。ボス撃破のクリア判定は PR #254 で実装中。マージ前でもクリア条件の種類は設定されるので、BGMの切り替えは確認できる（クリア判定だけ全滅扱いになる）。

## 確認手順

1. ボス撃破のウェーブを含むステージを始め、ボスウェーブで `Boss` が流れることを確認する。
2. 被弾してHPを30%未満にすると、`BossPinch` に切り替わる（フェードで重ならない）ことを確認する。
3. 回復でHPを30%以上に戻すと、`Boss` が最初から再生されることを確認する。
4. 通常ウェーブでは、今までどおり `Pinch` がフェードインで重なることを確認する。
