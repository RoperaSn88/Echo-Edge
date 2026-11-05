# エンディング（EndScene）

ボスステージ（ウェーブCSVのクリア条件がボス撃破 `3, <敵ID>` のウェーブ）をクリアすると、
クリア後シナリオのあとにエンディング（`Assets/Scenes/End.unity`）を追加ロードし、アンロードされるまで待ってから準備シーン（Preparing）へ戻ります。

流れ:

1. ボスステージのクリア演出（報酬表示）。ボスステージでは「続ける」を出さず「休む」だけにする。
2. 「休む」→ 暗転 → クリア後シナリオ（あれば）→ Scenario シーンを閉じる。
3. End シーンを追加ロード。起動と同時に動画を再生し、シーン全体（`Panel` の CanvasGroup）を alpha 0→1 にして明転する。
   背後の MainGame は暗転したままなので、黒から明転して見える。
4. 明転が終わると「タイトルへ」（`ReturnButton` の PhaseButton）が押せるようになる。
5. 「タイトルへ」クリック → `Panel` を alpha 1→0 にして暗転 → 動画停止 → End シーンが自分をアンロード → 待機が終わる。
6. 従来どおり Preparing を読み込み、MainGame をアンロードする。

## このPRでシーン側に入れた変更

- `ResultManager`（仮で CreditSceneController が付いていたもの）を `EndSceneManager` に名前変更し、コンポーネントを `EndSceneManager` に差し替え。
  - `Video Player`: `video`、`Title Button`: `ReturnButton`、`Scene Group`: `Panel` の CanvasGroup
  - `Fade In Duration` / `Fade Out Duration`: 明転・暗転の秒数（1秒）
- `Canvas` の Sort Order を 0 → 100（MainGame の暗転パネルより手前に出すため）。
- Build Settings に `Assets/Scenes/End.unity` をインデックス 7 で追加（`GameScene.End = 7`）。

YAML を直接編集しているので、Unity で開いて参照が外れていないか確認してください。

## 注意

- End シーンには EventSystem・Camera（AudioListener）を置かない（System / MainGame 側のものを使う）。
- PhaseButton は仕様上、カーソルが乗っていないときは透明（alpha 0）です。

## 確認すること

- ボスステージのクリア時に「続ける」が出ず、「休む」だけが出る。
- 「休む」→（クリア後シナリオ）→ 動画が流れながら明転する。
- 明転が終わるまで「タイトルへ」は反応しない。終わった後クリックすると暗転し、準備シーン（タイトル）へ戻る。
- エンディング中に Esc でオプションが開かない。
- ボス以外のステージは従来どおり（「続ける」「休む」、エンディングなし）。
