# Realtime Graphic VJ — Codebase Implementation Plan

## 1. 現在のプロジェクト

- Unity: `6000.3.12f1`
- Render Pipeline: URP（PC / Mobile renderer assets）
- シーン: `Assets/qoooo/Scenes/Trip26.unity`。実質的に Main Camera のみの初期状態
- 既存アプリコード: なし
- 導入済み:
  - RosettaUI 2.0.0（PrefsGUI RosettaUI 1.4.0 経由）
  - Klak Spout 2.0.6
  - Klak Syphon 1.0.4
  - Minis 1.3.2
- 未導入または未確認:
  - uGUI / TextMeshPro
  - OSCライブラリ
  - Unity Test Framework

添付仕様のうちMIDI / OSC / BPMはMVPの描画パイプライン完成後に扱う。既存のGVM / MotionValue系実装はこのコードベースには存在しない。

## 2. 実装上の主要判断

### UI

操作UIはUI Toolkitを土台とし、Inspectorのフィールド生成にRosettaUIを使う。

- RosettaUI: InspectorのField / Slider / Fold / Dropdownと値Binding
- 素のUI Toolkit: 3ペインレイアウト、Layer Stack、ドラッグ並べ替え、Preview、Transform Handle
- Composition映像: 操作UIとは別のRenderTextureへ描画

RosettaUIだけでアプリ全体を構築しない。高頻度で再構築されるLayer StackやPointer操作は専用Viewにし、選択中オブジェクトのプロパティ編集にRosettaUIを限定する。

Runtime UIの階層は、画面上部のlauncher barを入口にする。設定群を直接rootへ並べず、`WindowLauncher -> Window -> Fold -> Field`の順で整理する。

```text
Launcher Bar
  ├─ Output Settings Window
  │    ├─ Final Texture
  │    ├─ Sender
  │    └─ Preferences
  ├─ Layers Window (Slice 1)
  ├─ Cameras Window (Slice 2)
  └─ Effects Window (Slice 3)
```

Previewと操作UIは別のUI Toolkit Panelにし、描画順を数値で固定する。

- Preview Panel: sorting order 0、pointer inputを受け取らない
- RosettaUI Controls Panel: sorting order 100

これによりWindowをPreviewより常に前面へ表示し、Preview方式と操作UI方式をUI Toolkitへ統一する。

### Runtime責務の分離

Scene上の1つのApplication componentへ処理を集中させず、次のMonoBehaviourへ分ける。

| Component | 責務 |
|---|---|
| `VjRenderLoop` | 毎フレームのComposition実行と、その結果のOutputへの受け渡し |
| `VjPreferencesController` | 出力設定の適用、PlayerPrefs Save / Load、UIとの接続 |
| `FinalCompositeRenderer` | Final RenderTextureの生成と描画 |
| `TextureOutputController` | Syphon / Spout adapterの生成とTexture送信 |
| `VjPreviewPresenter` | 低優先度Panel上のPreview表示 |
| `VjControlPanel` | RosettaUI window hierarchyとユーザー操作 |

各MonoBehaviourはUnityのゲームループを利用するが、他Componentの内部実装を持たない。UIは`IVjPreferencesController`、OutputはTextureという小さな境界を介して連携する。

### ProcessingとComposition

`Camera -> Capture RT` と `Layer -> Effect Stack -> Composite` を分離する。同じCapture RTを複数Layerが共有し、各Layerは独立したEffect設定を持つ。

EffectはデータとGPU実装を分離する。

```text
EffectData (JSON保存対象)
    -> IEffectPass (runtime GPU implementation)
    -> shared ping/pong RenderTexture
```

各Layerの処理結果を恒久RTとして保持せず、描画フレーム内で共有ping/pong RTを使い回す。LayerごとにEffect処理した結果を即座にFinal RTへ合成する。これで `Layer数 x Effect数` 分のRT常駐を避ける。

Final RTへの合成は、透過・Normal / Add / Multiply / Screenを明示的に扱う専用Composite shaderで行う。Unity Cameraの透明物ソートやGameObjectのZ位置をレイヤー順の真実にしない。

### Crop

MVPは`Subject Bounds`を採用する。

- Capture Cameraに対象Renderer群を登録
- 各Rendererのworld boundsの8頂点をViewportへ投影
- unionしたRectをpadding付きContentRectとしてGraphicSource metadataへ反映
- 画面外、背面、対象なしの場合はOriginal Rectへフォールバック
- Rectは指数平滑化し、急なジッターを抑える

Alpha Auto CropはGPU readback / reductionの設計が別途必要なのでMVP後とする。

### Text

TextMeshProをComposition専用カメラで描画する。現状パッケージがないため、Text Layer着手時に`com.unity.ugui`（TMPを含むUnity 6対応版）を追加する。操作UIは引き続きUI Toolkitを使う。

### Output

出力は`ITextureOutput`で抽象化する。

- Windows + D3D11/12: Klak Spout、CaptureMethod.Texture
- macOS開発時: Klak Syphon
- 未対応環境: Null output（Previewは継続）

Klak SpoutはWindows専用なので、macOS上ではコンパイルとFinal RTまでを検証し、実送信はSyphonで確認する。Windows実機でSpoutの受信確認を別の完了条件にする。

## 3. ディレクトリとassembly境界

```text
Assets/qoooo/
  Runtime/
    Core/              settings, ids, lifecycle, commands
    Model/             serializable scene/layer/effect data
    Capture/           capture cameras, registry, subject bounds
    Processing/        effect registry, passes, RT pool
    Composition/       layer stack, graphic source, compositor
    Output/            interface + Spout/Syphon/Null adapters
    UI/                UI Toolkit shell + RosettaUI inspector
  Shaders/
    Processing/        chroma key, colorize, halftone
    Composition/       layer blend/composite
  UI/
    VJPanelSettings.asset
    VJLayout.uxml
    VJTheme.uss
  Tests/
    EditMode/
    PlayMode/
  Scenes/
    Trip26.unity
```

Runtime本体、外部出力adapter、UI、EditMode testsをasmdefで分離する。Core / ModelはKlak・RosettaUIに依存させない。

## 4. データモデル

保存対象は純粋なserializable classとし、`Camera`、`Material`、`RenderTexture`などUnity runtime objectを直接保存しない。

```csharp
[Serializable]
public sealed class CompositionData
{
    public int schemaVersion;
    public OutputSettingsData output;
    public List<CaptureCameraData> cameras;
    public List<LayerData> layers;
}

[Serializable]
public sealed class LayerData
{
    public string id;
    public string name;
    public LayerType type;
    public bool visible = true;
    public bool solo;
    public bool locked;
    public TransformData transform;
    public string sourceId;
    public List<EffectData> effects;
    public CameraLayerData camera;
    public TextLayerData text;
    public SolidLayerData solid;
}
```

Unityの`JsonUtility`は多態的なEffect listに不向きなので、MVPでは`EffectData`をtype discriminator + 各効果の設定ブロックで表す。初期実装から`schemaVersion`を持たせ、Load時のmigration入口を用意する。

Runtimeでは次を唯一の変更経路にする。

```text
UI / MIDI / future OSC
        -> CompositionController commands
        -> Model mutation + Changed event
        -> Renderer / Views update
```

UIがCameraやMaterialを直接操作しない。`AddLayer`, `DeleteLayer`, `DuplicateLayer`, `MoveLayer`, `SetSelectedLayer`, `SetParameter`をController APIとして定義する。

## 5. レンダリング契約

### RenderTexture ownership

- CaptureManager: Capture CameraごとのRTを所有・解放
- RenderTexturePool: descriptor単位の一時RTを貸与・回収
- CompositeRenderer: Final RTを所有・解放
- Effect pass / UI / Output adapter: RTを所有しない
- 解像度・format変更時: フレーム境界で再生成

標準descriptor:

- Capture: configurable、初期値1280x720、ARGB32、depth 24
- Processing temporary: sourceと同解像度、ARGBHalfを第一候補
- Final: 1920x1080、Spout送信時はRGBA8へ最終変換
- filter: Bilinear、wrap: Clamp

### GraphicSource

`GraphicSource`はimmutableなframe snapshotとして、Texture参照、textureSize、contentRect、anchor、aspectRatio、valid flagを持つ。Source消失時はLayerDataを残し、runtime resolutionだけをMissing状態にする。

### Visibility rules

- soloが1つ以上: soloかつvisibleなLayerのみ描画
- soloなし: visibleなLayerを描画
- locked: 描画には影響せず、UI由来の編集commandを拒否
- source missing / RT invalid: checkerboard placeholderを描画

## 6. MVPスライス

### Slice 0 — Foundation / vertical smoke test

実装:

- asmdef、settings、bootstrap
- RT poolとFinal RT
- SolidColor Layer 1枚のComposite
- PreviewへのFinal RT表示
- Spout / Syphon / Null adapter

完了条件:

- Play Modeで色変更が即時Previewへ反映
- macOSではSyphon senderが見える
- WindowsではRGBA8 Final RTをSpoutで送れる構成になっている
- enable / disable、解像度変更、Play Mode再開でRT leakがない

### Slice 1 — Model / Layer Stack

実装:

- Camera / SolidのLayerDataとruntime resolver
- Add / Delete / Duplicate / Move
- select、visibility、solo、lock
- UI Toolkit Layer StackとRosettaUI Inspector

完了条件:

- すべてのLayer操作がController経由
- 重複時は新規ID、設定値はdeep copy
- solo / visibility規則のEditMode testsが通る

### Slice 2 — Capture / Subject Bounds

実装:

- CaptureCamera component、registry、専用RT
- Camera transform / FOV inspector
- GraphicSource metadata
- Renderer bounds projection + smoothing

完了条件:

- 1つのCamera Sourceを2つのLayerが共有できる
- camera削除時にLayerがMissing Sourceとして残る
- bounds対象なしでOriginal Rectへ安全に戻る

### Slice 3 — GPU effects

実装順:

1. Chroma Key: keyColor / threshold / softness / despill
2. Colorize: luminanceから3色gradientへmap
3. Halftone: cellSize / dotScale / rotation / threshold / contrast
4. Effect add / delete / duplicate / move / bypass

完了条件:

- 同一Camera Sourceの2 Layerで異なるEffect Stackが見える
- Effect順変更が次フレームに反映
- CPU readbackがない
- Material instanceを毎フレーム生成しない

### Slice 4 — Transform / blend / direct manipulation

実装:

- normalized canvas position、uniform scale、rotation、opacity、anchor
- Normal / Add / Multiply / Screen
- Preview上のmove / scale handle
- lock時の編集拒否

完了条件:

- Final解像度変更でレイアウトが維持される
- Inspector編集と直接操作が同じController commandへ到達する

### Slice 5 — Text / persistence

実装:

- TextMeshPro Layer
- text / font asset id / size / tracking / spacing / alignment / color
- JSON Save / Load、schema validation、missing reference recovery
- Saveは一時ファイル作成後に置換する

完了条件:

- Camera / Solid / Textを含む構成がround-tripする
- 不明Effect typeやMissing CameraでもLoad全体は失敗しない
- 壊れたJSONでは現在のscene stateを破壊しない

## 7. MVP後

- Group Layer
- Auto Crop（GPU reduction + 低頻度AsyncGPUReadback、または完全GPU metadata path）
- Image Layer
- Undo / Redo
- Parameter abstraction、LFO、BPM
- MinisによるMIDI Learn
- OSC library選定とadapter
- Effect / Layer presets
- shader pass fusion、解像度scale、performance presets

Groupは階層構造とstack orderの意味を先に確定しないと保存・並べ替えが複雑になるため、基本MVPの後に追加する。

## 8. テスト戦略

EditMode:

- Layer CRUD、deep duplicate、reorder境界
- solo / visible / locked
- ID uniqueness、source resolution、missing source
- JSON round-trip、migration、invalid JSON
- bounds projection（behind camera / partial offscreen / no renderer）

PlayMode:

- RT create / resize / release
- shaderごとの固定入力に対する代表pixel比較
- Effect順差分
- blend modeの代表pixel比較
- enable / disable後の再初期化

Performance acceptance scene:

- 1 / 4 / 8 / 16 layers
- 1 / 3 / 5 effects
- 720p / 1080p
- CPU frame time、GPU frame time、temporary RT peak、GC/frameを記録

目標は60 FPSを標準とするが、対象GPUが未指定のため現時点では合否基準ではなく計測項目とする。実装後に基準機を決めてbudget化する。

## 9. 実装前に確定が必要な項目

1. 主な本番OSはWindowsか。macOSも本番対象ならSpout / Syphon双方を正式対応する。
2. 操作UIは本番出力と同じwindowに表示するか、別display / 別windowを想定するか。
3. Scene JSONの保存先はローカルファイルでよいか。MVPは`Application.persistentDataPath`を想定する。
4. TextMeshPro / uGUI packageの追加を許可するか。
5. 初期性能基準に使うGPUと、最低維持fps。

上記のうち1〜4は該当Slice着手までに必要。Slice 0のCore / RT / Composite / Previewは、暫定値で先行実装できる。

## 10. Git運用

### Branch

`master`へ直接実装せず、原則として1 Sliceにつき1ブランチを使う。

```text
codex/vj-s0-foundation
codex/vj-s1-layer-stack
codex/vj-s2-capture
codex/vj-s3-effects
codex/vj-s4-transform-blend
codex/vj-s5-text-persistence
```

- 各Sliceの受け入れ条件を満たしたら、そのブランチを完了扱いにする
- 次ブランチは、完了Sliceがmasterへ統合された後のmasterから作る
- Sliceが大きくなった場合のみ、`codex/vj-s3-chroma-key`のように機能単位へ分割する
- 実装途中に無関係な変更を混ぜない
- ユーザーが作成した未追跡・未コミット変更は、明示的に対象としない限りcommitしない

### Commit

1コミットを「単独でコンパイルでき、目的を一文で説明できる変更」にする。目安は以下。

```text
chore: add runtime assembly boundaries
feat: add render texture pool lifecycle
feat: composite a solid color layer
feat: expose final texture in preview
test: cover layer visibility resolution
```

- 大きな一括コミットを作らない
- production codeと対応する小さなtestは、原則同じコミットへ含める
- formattingだけの変更は機能変更から分離する
- scene / prefab YAMLの大規模差分を避け、可能な限りbootstrap codeと小さなprefabへ分解する
- commit前にdiffと対象ファイルを確認する
- commit後、対応する検証が成功した時点で作業ブランチをoriginへpushする

### Integration

- 各Sliceの末尾で変更概要、test結果、手動確認項目、既知の制約を報告する
- masterへのmergeはSlice単位で行う
- merge後は次Slice用ブランチを新しく切り、長寿命の巨大ブランチを作らない
- 履歴の書き換えやforce pushは行わない

## 11. 自動テストと検証

ローカルにはプロジェクトと一致するUnity `6000.3.12f1`が存在するため、Unity CLIでテストを実行できる。

### 最初に整備するもの

- Unity Test Frameworkをmanifestへ追加
- EditMode / PlayMode test asmdef
- `TestResults/`とbatch logのgitignore
- macOS用の再現可能なtest commandまたはscript

### Commit前の検証レベル

変更内容に応じて、次の最小セットを実行する。

1. Model / Controllerのみ: Unity compile + EditMode tests
2. MonoBehaviour / lifecycle: Unity compile + EditMode +対象PlayMode tests
3. Shader / rendering: PlayMode tests +固定sceneの画像または代表pixel検証
4. Output adapter: Null/Syphon smoke test。SpoutはWindows実機で追加確認
5. Scene / UI: batch testに加えてEditor Play Modeで目視確認

テスト失敗中のコミットは原則pushしない。プラットフォーム固有でローカル検証不能な場合は、未検証箇所を明記してpushする。

### 実行結果の扱い

- test結果はコミットせず、成功数・失敗数と実行環境を作業報告に残す
- Unityが異常終了した場合はEditor logを確認し、単なるtimeoutとcompile failureを区別する
- GPU画像テストは色空間・format・許容誤差を固定して、機種差によるflaky testを抑える
- Final RTの視覚品質は自動pixel testだけで完結させず、Sliceごとに目視確認用sceneを維持する

### 現環境でできない検証

- Windows D3D11/12上のSpout送信と外部アプリ受信
- ユーザー所有のMIDI機器を使った実機入力
- 未選定OSCアプリとの相互接続

これらは実装とコンパイル条件までは検証し、該当環境が利用可能になった時点で受け入れテストを行う。
