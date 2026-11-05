# エンディング（EndScene）

ボスステージ（ウェーブCSVのクリア条件がボス撃破 `3, <敵ID>` のウェーブ）をクリアすると、
クリア後シナリオのあとにエンディング（EndScene）を追加ロードし、アンロードされるまで待ってから準備シーン（Preparing）へ戻ります。

流れ:

1. ボスステージのクリア演出（報酬表示）。ボスステージでは「続ける」を出さず「休む」だけにする。
2. 「休む」→ 暗転 → クリア後シナリオ（あれば）→ Scenario シーンを閉じる。
3. EndScene を追加ロード。起動と同時に動画を再生し、画面全体が明転する。
4. 明転が終わると「タイトルへ」（PhaseButton）が押せるようになる。
5. 「タイトルへ」クリック → 暗転 → 暗転完了で EndScene が自分をアンロード → 待機が終わる。
6. 従来どおり Preparing を読み込み、MainGame をアンロードする。

コード側は `EndSceneManager`（`Assets/Scripts/Applicatiton/Scene/EndSceneManager.cs`）と、
`GameClearManager.StartGameClearSequenceAsync` の組み込み、`GameScene.End = 7` の追加まで入っています。
シーンの作成・登録は以下の手順でお願いします。

## 手順

### 1. Build Settings に登録する

1. EndScene を `Assets/Scenes/EndScene.unity` に置く（名前は任意）。
2. File > Build Profiles（Build Settings）の Scene List に追加し、**インデックスを 7**（Scenario の次）にする。
   - `GameScene.End = 7` で読み込むので、番号がずれると別のシーンが開きます。
   - 登録されていない場合はエラーログを出してエンディングを飛ばし、そのまま Preparing へ戻ります。

### 2. シーンの中身

EndScene は MainGame（暗転したまま）の上に重ねて表示します。次の点に注意してください。

- **EventSystem・AudioListener（Camera）は置かない。** System / MainGame 側のものが既にあるため、重複すると警告や入力の二重処理になります。
  - カメラが必要な場合は Camera を置き、AudioListener コンポーネントだけ外してください。
- **Canvas は Screen Space - Overlay、Sort Order を MainGame の Canvas より大きく（例: 100）する。** MainGame の暗転パネルより手前に出すためです。Canvas に `Graphic Raycaster` が付いていることを確認してください。

1. Canvas の子に、画面全体を覆う `RawImage`（動画表示用）を置く。
2. `VideoPlayer` の設定:
   - **Render Mode: Render Texture** にし、RenderTexture アセットを作って Target Texture に割り当て、手順1の RawImage の Texture にも同じものを入れる。
     - Camera Far/Near Plane にすると、Overlay の MainGame 暗転パネルの下に描かれて見えなくなります。
   - **Play On Awake はオフ**（`EndSceneManager` が起動時に `Play()` します。オンでも動きます）。
   - 音声は Audio Output Mode: Direct で大丈夫です（BGM はクリア時にフェードアウト済み）。
3. Canvas の子に PhaseButton（既存のもの）を置き、`Rect Transform` / `Canvas Group` / `Text` を割り当てる。
   - PhaseButton は仕様上、カーソルが乗っていないときは透明（alpha 0）です。常に見せたい場合は教えてください。
4. Canvas の一番下（最前面）に、画面全体を覆う黒い `Image` を置く（明転・暗転用のフェードパネル）。
   - 色は黒、Raycast Target はオンのままで構いません（明転中のクリックを防ぎます。明転後は非アクティブになります）。
5. 空の GameObject `EndSceneManager` を作り、`EndSceneManager` を付けて以下を割り当てる。
   - `Video Player`: 手順2の VideoPlayer
   - `Title Button`: 手順3の PhaseButton
   - `Fade Panel`: 手順4の黒い Image
   - `Fade In Duration` / `Fade Out Duration`: 明転・暗転の秒数（初期値 1 秒）
   - `Title Button Text`: ボタンの文字（初期値「タイトルへ」。空にするとシーン上の文字のまま）

## 確認すること

- ボスステージのクリア時に「続ける」が出ず、「休む」だけが出る。
- 「休む」→（クリア後シナリオ）→ 動画が流れながら明転する。
- 明転が終わるまで「タイトルへ」は反応しない。終わった後クリックすると暗転し、準備シーン（タイトル）へ戻る。
- エンディング中に Esc でオプションが開かない。
- ボス以外のステージは従来どおり（「続ける」「休む」、エンディングなし）。
