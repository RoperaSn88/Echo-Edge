# 装備品パネル（weaponview）の右ずれ修正

展示会フィードバック「weaponviewの位置がおかしい、画面右側にズレる」への対応。

## 原因

- MainGame シーンの `Canvas` の CanvasScaler が **Constant Pixel Size（倍率1）** だった。UI はピクセル固定のため、1920x1080 以外の解像度では位置・大きさが変わる。
- 装備品パネル `Weapons/WeaponPanel`・`Weapons/WeaponPanel (1)` は **右端アンカー**（x=-546）だった。1920x1080 より大きい画面（2560x1440 や 4K）では、パネルが画面中央から離れて右端に寄って見える。カメラが映すプレイヤーは画面中央基準なので、ずれが目立つ。
- ビルドは `FullScreenWindow`（モニターのネイティブ解像度）で起動するため、展示会のモニター解像度でずれが出た。

## 修正内容（シーンYAMLを直接変更済み）

1. `Canvas` の CanvasScaler
   - UI Scale Mode: Constant Pixel Size → **Scale With Screen Size**
   - Reference Resolution: 800x600 → **1920x1080**
   - Match: 0 → **1（Height）**（カメラは縦FOV基準なので、盤面と同じく高さに合わせて拡大縮小する）
2. `WeaponPanel` と `WeaponPanel (1)` の RectTransform
   - Anchor: (1, 0.5) → **(0.5, 0.5)**（中央）
   - Pos X: -546 → **414**（1920x1080 では今までと同じ位置）

1920x1080 での見た目は変わりません。

## 確認手順（ロペさん）

1. MainGame シーンを開き、Game ビューを 1920x1080 にして装備品パネル（右クリック）がこれまでと同じ位置に出ることを確認。
2. Game ビューを 2560x1440・3840x2160・1280x720・1920x1200（16:10）などに変えて、パネルがプレイヤーの右隣に同じ見た目で出ることを確認。
3. 同じ Canvas の他のUI（操作説明・コンボ・クリア条件・行動ボタンなど）も解像度に合わせて拡大縮小されるようになります。崩れているものがあれば教えてください。
4. 16:9 より横長の画面では左右の端寄りUIは画面端に付いたまま、横が狭い画面（4:3など）では右端アンカーのUIとパネルが重なる可能性があります。

## 補足

- `Canvas (1)` や Preparing シーンの Canvas も Constant Pixel Size のままです。同じ症状が出るなら同様に Scale With Screen Size に変更してください。
