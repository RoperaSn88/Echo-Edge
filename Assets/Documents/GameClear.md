# ゲームクリア後の選択肢（続ける / 休む）

仕様: Notion「1-メインゲーム」>「ゲームクリア」

ゲームクリア時、報酬パネル（`GameClears` / `GameClearRewardPresenter`）に加えて次の2つを表示します。

- **続ける**: ステージナンバーを+1し、MainGame シーンを読み込み直して次のステージを始める。最終ステージ（`StageData.MaxLevel`）では表示しない。
- **休む**: 準備シーン（Preparing）へ戻る（従来の動き）。

コードは入っていますが、ボタンのUIはシーン編集が必要です。以下の手順で MainGame シーンに追加してください。
UIが無い場合は、従来どおり「クリックで準備シーンへ戻る」動きになります。

## 手順（MainGame シーン）

1. `GameClears`（GameClearRewardPresenter が付いているオブジェクト）と同じ Canvas の下に、空の UI オブジェクト `GameClearChoice` を作る。
2. `GameClearChoice` に `CanvasGroup` と `GameClearChoicePresenter`（`Assets/Scripts/Presenteation/UI/GameClearChoicePresenter.cs`）を付ける。
3. `GameClearChoice` の子に `Button - TextMeshPro` を2つ作り、テキストを「続ける」「休む」にする。
   - 置き場所は報酬パネルと重ならない位置（例: 画面下中央に横並び）がおすすめです。
4. `GameClearChoicePresenter` のインスペクターで以下を割り当てる。
   - `Canvas Group`: 手順2の CanvasGroup
   - `Continue Button`: 「続ける」ボタン
   - `Rest Button`: 「休む」ボタン
5. **`GameClearChoice` はシーン上でアクティブのまま保存する。** 起動時の `Awake` で自分を非表示にするため、最初から非アクティブだと参照が取れません（`GameClearRewardPresenter` と同じ仕組みです）。
6. クリア中は `Time.timeScale = 0` ですが、uGUI のボタンはそのまま押せます（MainGame に EventSystem があることを確認済み）。

## 確認すること

- ステージ1をクリア →「続ける」でステージ2が始まる（ステージ選択画面の表示も2になっている）。
- 「休む」で従来どおり準備シーンへ戻る。
- 最終ステージのクリア時は「休む」だけが表示される。
- 開始前/クリア後シナリオがあるステージで「続ける」を選ぶと、クリア後シナリオ → 次ステージの開始前シナリオ → 次ステージの順に進む。
- 「続ける」の後、BGMが鳴る・UI（HP等）が表示される・2回目以降のクリアでも選択肢が出る。
