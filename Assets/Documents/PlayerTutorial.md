# プレイヤーフェーズの行動選択チュートリアル (Player)

プレイヤーフェーズで行動選択ボタン（一閃 / 装備品 / 防御 / 回復）が初めて表示されたときに、
`TutorialKinds.Player` のチュートリアルを再生します。

- ボタンのスライドイン（初回）／フェードイン表示が終わってから再生します。
- 再生中はボタンを表示したまま押せません。チュートリアルを閉じたクリックはボタン入力として扱いません。
- 既存のチュートリアルと同じく、初回のみ再生されます（`TutorialCompletedPlayer`）。チュートリアルをスキップすると以降は再生されません。

## エディタでの手順

テキストデータはリポジトリ外（`Assets/Addressables` は .gitignore 対象）のため、手動で用意してください。

1. `Assets/Addressables/Tutorials/` に `TextBoxMasterData_Player.asset` を作成する
   （既存の `TextBoxMasterData_Attack.asset` などを複製して中身を書き換えるのが簡単です）。
2. Addressables に登録し、アドレスを `Assets/Addressables/Tutorials/TextBoxMasterData_Player.asset` にする
   （他の TextBoxMasterData と同じグループ・同じアドレス形式）。
3. 動作確認で再生済みフラグを消したい場合は、PlayerPrefs の `TutorialCompletedPlayer` を削除する。

アセットが無い場合はロードに失敗した旨の警告を出して、ゲームはそのまま進みます。
