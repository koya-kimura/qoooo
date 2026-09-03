# Realtime Graphic VJ — 現在の実装を読む手順

最終更新: 2026-09-04  
対象: PR #2 マージ後（`master` の `79f116d` 以降）

## 1. 最初に知っておくこと

このプロジェクトは、Unityの通常の`MonoBehaviour`ライフサイクルで動くリアルタイムVJアプリです。専用Bootstrapへ処理を集中させず、Scene上のComponentごとに責務を分けています。

現在のC#コードには`.asmdef`がありません。すべてUnity標準の`Assembly-CSharp`としてコンパイルされます。以前存在した自動テストも、テスト用`.asmdef`を全廃した時点で削除しています。現在の確認方法は、UnityコンパイルとPlay Modeのスモークテストです。

使用中の主な外部パッケージは次のとおりです。

- RosettaUI: Runtime操作UI
- Unity Simple Container: Scene内ComponentのDI
- Klak Spout / Klak Syphon: Final Textureの外部送信
- Unity CLI Loop: 別PCやCodexからのコンパイル、Play Mode、ログ確認

## 2. 別PCで最初に確認すること

1. リポジトリをcloneまたはpullする。
2. Unity HubでUnity `6000.3.12f1`を用意する。
3. Unityでプロジェクトを開き、Package Managerの依存解決が終わるまで待つ。
4. `Assets/qoooo/Scenes/Trip26.unity`を開く。
5. Consoleにcompile errorがないことを確認する。
6. Play Modeに入り、Preview、VJ Controls、Output、Layersを確認する。
7. Game Viewへフォーカスを置いてDキーを押し、開いている操作UIがすべて消え、再度Dキーで戻ることを確認する。

Unity CLI Loopを使える場合の最小確認は次のとおりです。

```text
uloop compile --timeout-seconds 180
uloop control-play-mode --action Play --timeout-seconds 180
uloop get-logs --log-type Error --max-count 50 --include-stack-trace
uloop control-play-mode --action Stop --timeout-seconds 180
```

`.codex/`と`.uloop/`はローカルツール用で、Git管理対象ではありません。

## 3. Sceneから全体像をつかむ

最初に`Assets/qoooo/Scenes/Trip26.unity`のHierarchyを見ます。

```text
VJ Runtime
  ├─ Composition
  │   ├─ CompositionController
  │   ├─ FinalCompositeRenderer
  │   ├─ VjRenderLoop
  │   └─ VjLayerStackUiTarget
  ├─ Output
  │   ├─ TextureOutputController
  │   └─ VjOutputUiTarget
  ├─ UI
  │   ├─ VjPreviewPresenter
  │   └─ UIBuilder
  └─ Preferences
      ├─ VjOutputSettingsController
      └─ VjPreferencesController

SceneContainer
ProjectContainer
```

`SceneContainer`はScene内のComponentを収集します。`IUiTarget`を実装したComponentは複数登録され、`UIBuilder.Construct(IEnumerable<IUiTarget>, ...)`へまとめて注入されます。

## 4. おすすめのコードリーディング順

### Step 1: 保存されるデータ

最初に次の2ファイルを読みます。

- `Assets/qoooo/Runtime/Model/LayerData.cs`
- `Assets/qoooo/Runtime/Model/CompositionData.cs`

`LayerData`は1枚のLayerの状態です。現在はCameraとSolidの種別、ID、名前、表示、Solo、Lock、Transform、Source ID、Solid色を保持します。`CompositionData`はLayer一覧を持つComposition全体のデータです。

ここにはCameraやRenderTextureなどの実行時Objectを保存しません。将来JSON保存へ進みやすくするためです。

### Step 2: Layerを変更する入口

次に`Assets/qoooo/Runtime/Composition/CompositionController.cs`を読みます。

UIは`LayerData`を勝手に変更せず、原則としてこのControllerを経由します。

- `AddLayer`
- `DeleteLayer`
- `DuplicateLayer`
- `MoveLayer`
- `SelectLayer`
- `SetVisible` / `SetSolo` / `SetLocked`
- `RenameLayer` / `SetSolidColor` / `SetSourceId`

変更後はrevisionが増えます。Layer Stack UIはrevisionの変化を見て表示を再構築します。

### Step 3: Final Textureの描画

次に以下を読みます。

- `Assets/qoooo/Runtime/Composition/FinalCompositeRenderer.cs`
- `Assets/qoooo/Shaders/Composition/SolidLayerComposite.shader`
- `Assets/qoooo/Runtime/Core/OwnedRenderTexture.cs`

`FinalCompositeRenderer`は出力解像度に合うRenderTextureを所有し、表示対象のSolid Layerを下から順番に合成します。Soloが1枚以上ある場合は、visibleかつsoloのLayerだけを描きます。

`OwnedRenderTexture`はRenderTextureの生成・サイズ変更・解放を担当します。UIやOutput側はFinal Textureを所有せず、参照するだけです。

Camera Layerはデータとして追加できますが、Camera映像を解決して合成する処理はまだ未実装です。

### Step 4: 毎フレームの処理

`Assets/qoooo/Runtime/Bootstrap/VjRenderLoop.cs`を読みます。

`LateUpdate`で次の順番に処理します。

```text
CompositionをFinal Textureへ描画
  -> Final TextureをOutputへ渡す
```

大きなApplicationクラスは置かず、Unityのゲームループをそのまま利用しています。

### Step 5: Spout / Syphon出力

以下を読みます。

- `Assets/qoooo/Runtime/Output/ITextureOutput.cs`
- `Assets/qoooo/Runtime/Output/TextureOutputController.cs`

`TextureOutputController`はOSとOutput Modeから出力方式を選びます。

- Windows: Spout
- macOS: Syphon
- 無効または利用不能: Null output

Autoモードでは実行OSに応じて選択します。Output Nameを変更すると送信Componentを作り直します。

### Step 6: Output設定とPrefs

以下を読みます。

- `Assets/qoooo/Runtime/Core/VjPreferences.cs`
- `Assets/qoooo/Runtime/Bootstrap/VjOutputSettingsController.cs`
- `Assets/qoooo/Runtime/Bootstrap/VjPreferencesController.cs`

`VjOutputSettingsController`が解像度、Output Name、Output Modeを実行中のComponentへ反映します。`VjPreferencesController`は起動時にPlayerPrefsを自動読込し、Save Prefs時に現在値を保存します。手動Loadボタンはありません。

### Step 7: DIとRosettaUI

最後にUIを読みます。

- `Assets/qoooo/Runtime/UI/IUiTarget.cs`
- `Assets/qoooo/Runtime/UI/UIBuilder.cs`
- `Assets/qoooo/Runtime/UI/VjOutputUiTarget.cs`
- `Assets/qoooo/Runtime/UI/VjLayerStackUiTarget.cs`
- `Assets/qoooo/Runtime/UI/VjLayerStackPanelBuilder.cs`
- `Assets/qoooo/Runtime/UI/VjPreviewPresenter.cs`

UIの流れは次のとおりです。

```text
VjOutputUiTarget ─┐
                  ├─ IUiTargetとしてSceneContainerへ登録
LayerStackUiTarget┘
                  ↓ IEnumerable<IUiTarget>を注入
               UIBuilder
                  ↓
        RosettaUIのRoot WindowをBuild
```

`UIBuilder`は`Order`順にWindow Launcherを縦へ並べ、その下にSave Prefsを置きます。Dキーで隠す際は、開いていた各Target Windowの状態を保存して明示的に閉じ、RosettaUIRootも無効にします。再表示時は以前開いていたWindowだけを復元します。

Previewは操作UIと別のUIDocumentです。

- Preview sorting order: 0
- Controls sorting order: 100

そのため操作UIはPreviewより前面に表示されます。

## 5. よくある変更の入口

| やりたいこと | 最初に見る場所 |
|---|---|
| Layerの項目を増やす | `LayerData.cs` |
| Layer操作を増やす | `CompositionController.cs` |
| 描画方法を変える | `FinalCompositeRenderer.cs`とShader |
| Output設定を増やす | `VjPreferences.cs`と`VjOutputSettingsController.cs` |
| 新しい設定Windowを増やす | `IUiTarget`実装Componentを追加 |
| UI全体の並びやDキーを変える | `UIBuilder.cs` |
| Spout/Syphon送信を変える | `TextureOutputController.cs` |

## 6. 現在の制約

- Camera Layerの映像CaptureとCompositeは未実装。
- Layerの並べ替えはUp / Downで、ドラッグ操作は未実装。
- Transform、Opacity、Blend Modeはデータの一部だけで描画へ未反映。
- Effect Stack、Chroma Key、Text Layer、JSON保存は未実装。
- `.asmdef`全廃に伴いUnity Test Runner用テストは現在ない。
- macOSではSyphonを確認できるが、Spoutの最終受信確認にはWindowsが必要。
- `TraceTransparentRays`のMetal警告はUnity Render Pipeline package内部から出るもので、プロジェクト固有Shaderのcompile errorではない。

## 7. 引き継ぎ時の変更ルール

- `master`へ直接実装せず、Sliceまたは小機能ごとに`codex/`ブランチを作る。
- 1コミットは単独で目的を説明できる大きさにする。
- Commit前にUnity compile、Play Mode、Console Errorを確認する。
- SceneやPrefabは可能ならUnity Editor経由で変更し、YAML参照を壊さない。
- Flowerは次のCamera Capture実装で初めてSceneへ配置するテストモデルとして扱う。
