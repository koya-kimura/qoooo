# Realtime Graphic VJ — 今後の実装計画

最終更新: 2026-09-04

## 1. 現在地

PR #2までで、Slice 0の基礎とSlice 1のLayerモデルが実装されています。

完了済み:

- Final RenderTextureの生成・サイズ変更・解放
- Preview表示
- Spout / Syphon / Null outputの切替
- 解像度、Output Name、Output ModeのRosettaUI設定
- PlayerPrefsへの保存と起動時自動復元
- Camera / Solid Layerのデータモデル
- Layerの追加、削除、複製、上下移動、選択、表示、Solo、Lock
- 複数Solid Layerの合成
- Unity Simple Containerによる`IUiTarget`収集
- Root Window、Output Window、Layers Window
- Dキーによる操作UI全体の表示切替

未実装の中心は「Camera Layerへ実際の映像を供給すること」です。次はSlice 2へ進みます。

## 2. 次のブランチ

この引き継ぎDocsブランチがmasterへマージされた後、最新masterから次を作ります。

```text
codex/vj-s2-camera-capture
```

Sliceが大きくなった場合は、完成を待たず次のように分けます。

```text
codex/vj-s2-capture-core
codex/vj-s2-camera-composite
codex/vj-s2-subject-bounds
```

`.asmdef`は再導入せず、当面はUnity標準`Assembly-CSharp`で実装します。

## 3. Slice 2 — Camera Capture

### 目標

Unity Cameraの映像をRenderTextureへ撮影し、Camera Layerの`sourceId`から解決してFinal Textureへ合成できるようにします。同じCamera Sourceを複数Layerから共有できる構造にします。

### Commit 1: Captureデータと境界を定義

追加候補:

```text
Runtime/Capture/CaptureCameraData.cs
Runtime/Capture/IGraphicSource.cs
Runtime/Capture/GraphicSource.cs
```

決める内容:

- Capture Sourceの一意ID
- 表示名
- Texture参照を含む実行時`GraphicSource`
- Texture size
- Content Rect
- Anchor
- Valid / Missing状態

この段階ではCameraをまだ動かさず、データと責務だけを確定します。

### Commit 2: Capture CameraとRenderTexture lifecycle

追加候補:

```text
Runtime/Capture/CaptureCamera.cs
Runtime/Capture/CaptureTextureOwner.cs
```

実装内容:

- Unity Cameraへ専用RenderTextureを割り当てる
- 解像度変更時に安全に再生成する
- Disable / Destroy時に解放する
- Cameraの既存Target Textureを破壊しない
- Texture format、filter、wrapを明示する

初期解像度は1280×720とし、固定値ではなく設定として保持します。

### Commit 3: Capture registryとsource resolver

追加候補:

```text
Runtime/Capture/CaptureCameraRegistry.cs
Runtime/Capture/IGraphicSourceResolver.cs
```

実装内容:

- Scene内のCapture CameraをIDで登録する
- `sourceId`から`GraphicSource`を解決する
- Camera削除時はLayerDataを消さずMissing Sourceを返す
- 同じSourceを複数Layerから参照可能にする

DIを使う場合も、`UIBuilder`と同様に小さなインターフェース境界で注入します。

### Commit 4: Camera LayerをFinal Textureへ合成

実装内容:

- `FinalCompositeRenderer`がSolidとCameraをLayer順に処理する
- Camera Textureのアスペクト比を保持する
- source missing時は安全にスキップ、または確認用placeholderを表示する
- visibility / soloの既存規則をCameraにも適用する
- CPU readbackを行わない

この時点でFlowerをSceneへ配置し、Capture CameraがFlowerを撮影する最小スモークシーンを作ります。Flowerはこのコミットより前にはSceneへ配置しません。

### Commit 5: Camera設定UI

追加候補:

```text
Runtime/UI/VjCameraUiTarget.cs
```

`IUiTarget`として次を提供します。

- Camera一覧
- Source ID /表示名
- Position / Rotation
- FOV
- Near / Far Clip
- Capture解像度

`UIBuilder`側へCamera固有ロジックを追加せず、DIで自動回収させます。

### Commit 6: Subject Bounds

実装内容:

- Capture Cameraごとに対象Renderer群を登録する
- Renderer boundsの8頂点をViewportへ投影する
- union Rectへpaddingを加える
- 対象なし、Camera背面、完全画面外ではOriginal Rectへ戻す
- Rectを指数平滑化してジッターを抑える

Auto CropのGPU readback方式はこのSliceでは行いません。

### Slice 2の完了条件

- FlowerがCapture Cameraに映る。
- Camera Layerを追加するとFinal Previewへ表示される。
- 同じCamera Sourceを2つのLayerが共有できる。
- Camera LayerとSolid Layerの上下順が描画へ反映される。
- visibility / soloがCamera Layerでも動く。
- Capture Cameraを無効化してもクラッシュせずMissing状態になる。
- 解像度変更、Play Mode再開、Component disableでRenderTexture leakがない。
- macOSのSyphonへCamera合成済みFinal Textureを送れる。

## 4. Slice 3 — GPU Effect Stack

Slice 2の後に着手します。

実装順:

1. EffectDataとEffect registry
2. shared ping/pong RenderTexture
3. Chroma Key
4. Colorize
5. Halftone
6. Effect追加、削除、複製、並べ替え、Bypass UI

同じCamera Sourceを参照する2つのLayerへ異なるEffect Stackを設定できることを完了条件にします。Materialを毎フレーム生成せず、CPU readbackも避けます。

## 5. Slice 4 — Transform / Opacity / Blend

実装内容:

- normalized canvas position
- rotation
- uniform scale
- anchor
- opacity
- Normal / Add / Multiply / Screen
- Preview上の直接操作

Inspector操作とPreview上の操作は、どちらも`CompositionController`の同じ変更APIへ到達させます。

## 6. Slice 5 — Text / JSON Persistence

実装内容:

- Text Layer
- TextMeshPro設定
- CompositionDataのJSON Save / Load
- schema versionとmigration
- Missing Camera / unknown Effectの復旧
- 一時ファイル作成後の置換による安全な保存

PlayerPrefsはアプリ設定用、JSONはCompositionデータ用として役割を分けます。

## 7. MVP後

- Group Layer
- Image Layer
- GPUベースAuto Crop
- Undo / Redo
- LFO / BPM
- MIDI Learn
- OSC adapter
- Effect / Layer preset
- Performance preset

## 8. 検証方針

現在は`.asmdef`を使わないためUnity Test Runner用テストはありません。各小コミットで最低限、次を実施します。

1. `uloop compile`でcompile error / warningを確認する。
2. 新規Playセッションを開始する。
3. `uloop get-logs --log-type Error`でRuntime errorを確認する。
4. `uloop execute-dynamic-code`でComponent、DI、RenderTextureの状態を読む。
5. UI入力が対象の場合はkeyboard / mouse simulationで実入力経路を通す。
6. 描画変更ではscreenshotまたは代表pixelを確認する。
7. Play Modeを停止し、再開時の状態も確認する。

プラットフォーム別:

- macOS: Compile、Preview、Syphon senderまで確認
- Windows: Spout senderと外部受信アプリまで確認

将来自動テストを再導入する場合は、`.asmdef`再導入の是非を先にユーザーと相談し、暗黙には追加しません。

## 9. Git運用

- 最新masterから`codex/`ブランチを作る。
- 1コミットは単独でcompileできる状態にする。
- データ、runtime、UI、Sceneを可能な限り別コミットへ分ける。
- 各コミット後にpushする。
- PR本文には「今回追加された仕様」「実装の読み方」「確認結果」「未実装」を書く。
- Sliceがmasterへマージされたら、次のSliceは新しいブランチから始める。
- force pushや履歴書き換えは行わない。
