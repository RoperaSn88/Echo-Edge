# 1. コミットメッセージのフォーマットについて
| タイプ       | 説明                    | 例                             |
| --------- | --------------------- | ----------------------------- |
| feat:     | 新機能の追加                | feat: ログイン画面にソーシャルログインを追加     |
| fix:      | バグ修正                  | fix: APIのリクエスト時にエラーが発生する問題を修正 |
| refactor: | リファクタリング（動作に影響なし）     | refactor: ユーザー認証ロジックを整理       |
| chore:    | 設定ファイルの変更、ライブラリ更新     | chore: eslint のルールを調整         |
| docs:     | ドキュメント修正              | docs: READMEにセットアップ手順を追加      |
| style:    | コードのフォーマット修正（動作に影響なし） | style: prettier を適用           |
| test:     | テストコードの追加・修正          | test: ユーザー登録APIのテストを追加        |
| perf:     | パフォーマンス改善             | perf: 画像の遅延読み込みを実装            |
| ci:       | CI/CD関連の変更            | ci: GitHub Actions のワークフローを修正 |

# 2. ざっくりルール
git: チームでよく使うやつ。ファイルを共有し、変更とかした履歴を残るもの。
[参考リンク](https://qiita.com/a_goto/items/0fe40b17105d1ac1c40b)

# 3. ライブラリ検査（このプロジェクトで使っている主なもの）
- Unity Package Manager: `/home/runner/work/GameCreateProject-2025/GameCreateProject-2025/Packages/manifest.json`
  - com.cysharp.unitask
  - com.unity.2d.tilemap.extras
  - com.unity.addressables
  - com.unity.ai.navigation
  - com.unity.cinemachine
  - com.unity.inputsystem
  - com.unity.postprocessing
  - com.unity.render-pipelines.universal
  - com.unity.test-framework
  - jp.andantetribe.utils
- 追加プラグイン（Assets 配下）
  - DOTween: `/home/runner/work/GameCreateProject-2025/GameCreateProject-2025/Assets/Plugins/Demigiant/DOTween`
  - NuGet 関連ファイル: `/home/runner/work/GameCreateProject-2025/GameCreateProject-2025/Assets/NuGet`
