# Rendering / Text / Foundation Refactor 実装計画（草案）

## Objective

Trip26 の背景描画を正しく復旧し、増えたプロシージャルパターンとテキスト配置を UI / APC mini mk2 の共通 Parameter から操作できるようにする。同時に、BPM・乱数・MIDI parameter provider・ディレクトリ配置を責務に沿って整理する。

成功条件:

- `Custom/ProceduralPattern` を使う Background がマゼンタではなく、選択した2色と Pattern Type で描画される。
- Pattern Type は単一の数値対応表を持ち、UI / MIDI / C# / HLSL が同じ意味で扱う。
- Text の「配置（pattern）」と「glyph motion」を別概念として選択・編集でき、半径などの単位と対象が UI から分かる。
- 新規 `Text*Pattern` が Trip26 の Text 選択肢として実際に割り当てられる。
- BPM を使う各 component は `BpmManager` への Inspector 参照ではなく DI 経由の read-only tempo interface に依存する。
- `PcgHash` を廃止し、C# の乱数ハッシュは `qoooo.Util.Pcg` だけを使用する。
- `MidiParameterGroupName` と、実際に参照されない宣言を削除する。
- 既存 Scene / Material / MIDI profile の参照切れを起こさず、Unity compile と Play Mode で確認できる。

## Current State

調査対象は `cfc453f` に未コミット変更がある作業ツリーである。今回の実装計画では application code は変更しない。

- 実行中 Game View では Background が一面のマゼンタになった。Console の Error は0件、`Custom/ProceduralPattern` は `isSupported == true`、pass数は1である。従って「Console に出る通常の shader import error」は確認できていないが、描画結果は Unity の shader fallback と同じ見え方であり、実機バックエンドでの pass 実行を検証する必要がある。
- `Assets/qoooo/Scripts/Textures/MaterialTextureSource.cs` は `_ColorA` / `_ColorB` を設定する。一方 `Assets/qoooo/Scripts/Shaders/ProceduralPattern.shader` は `_MainColor` / `_SubColor` を CBUFFER と Properties に宣言する。少なくとも色の Uniform 契約は不整合である。また shader は `_Beat` を使うが Source は供給していない。
- `Assets/qoooo/Materials/ProceduralBackground.mat` にも旧 `_ColorA` / `_ColorB` が保存されている。Material 側の serialized property を移行しなければ、scriptを直しても Edit Mode の preview / 将来の別利用で色が一致しない。
- Pattern の値は HLSL `PatternDispatcher.hlsl` の0〜9、C# `ProceduralPatternType` の0〜9、`RadioParameter background.pattern` の10選択肢で重複定義されている。現時点では順番が一致するが、対応を機械的に検証する場所はない。
- `TextController` は `_patterns`（`TextPattern` の list）と indexを持つが、Pattern選択用 Parameter は持たない。Trip26には `Three Circles` だけが割り当てられている。
- `TextCirclePattern._radius` は、各 glyph の TMP local座標上の円中心からの距離である。文字数で半径は変わらず、文字数は円周上の角度間隔だけを変える。`TextPattern` entry の Offset / Scale / Rotation と `TextController` object の transform が後段で合成される。この関係はUIに表示されない。
- `TextGridPattern`、`TextRectangleLoopPattern`、`TextCornerRotatePattern`、`TextTypewriterPattern` は `ITextPattern`、すなわち文字の**配置をサンプリングする class**である。唯一の `TextRotateMotion` は `TextGlyphMotion`、すなわち配置済み glyphへ時間変化を加える classである。現在この二種類は用語上も Scene 設定上も混ざりやすい。
- `qoooo.Midi.PcgHash` は APC Random mode で使用される。`qoooo.Util.Pcg.Hash2D` は同じ2D PCG手順を既に実装しており、float bit patternを入力にする `Pcg.Pcg01(Vector2).x` は用途を置き換えられる。
- `IMidiParameterContainer.MidiParameterGroupName` は registry / UI / profile から読まれていない。`ModelController`、`TextController`、`TextureLayer` の宣言は dead API である。
- `BpmManager` は純粋な `BpmClock` を所有する MonoBehaviour だが、`MidiSystemController`、`MidiParameterRuntime`、`ApcMiniMk2ParameterController`、`ApcMiniMk2MidiInput` が具体 classの serialized fieldで参照する。UnitySimpleContainer は Scene component が実装する interface を収集・注入できるため、BPMの read interfaceを実装すればDI化できる。
- `Assets/qoooo/Scripts` 直下の `Midi`、`Parameters`、`Pattern`、`Textures`、`Timing`、`Util` と、`Scripts/Shaders/{Pattern,Util}` は導入済みだが、MIDIの device I/O・APC surface・binding・profile・UIが単一 `Midi/` に混在する。Textは controller / manager / pattern に分散し、rendering sourceとshaderは `Textures` / `Shaders` に分かれる。

## Problem

描画契約、Parameter model、BPM依存、ランダム関数、フォルダ責務が部分的に移行途中である。そのため、背景が復旧できず、Textの表示を変える入口がなく、今後パターンや motion を増やすたびに Scene の手編集・数値同期・Inspector参照が必要になる。

## Requirements

### Functional

- Backgroundの material property名を shader / source / `.mat` で一致させる。`_Beat` には BPM source の Beat を毎frame設定する。
- Pattern ID 0〜9を `Noise Texture` から `Psychedelic Ring` まで固定し、UI Radio、APC Radio、shader dispatchで同じ選択を描画する。
- Textの配置選択を `RadioParameter` として登録し、選択値で `TextController` が一つの `TextPattern` を用いる。
- Trip26へ現在存在する Text pattern component を選択肢として割り当てる。既存の `Three Circles` は失わない。
- Text motion は layout / sampling 後に適用されるものだけを `TextGlyphMotion` と呼び、個別 entry に設定する。
- APC Random modeを含む既存の決定的乱数は移行前後で同一入力に対して同一値を返す。
- Parameter registry の重複検出、MIDI profile復元、APC mapping UIの挙動を維持する。

### Non-functional

- shader値の未設定・不正Pattern IDは `Debug.LogError` または `Debug.LogWarning` で特定可能にする。毎frame logは出さない。
- 時間更新は `Time.unscaledDeltaTime` を所有する `BpmManager` だけが行う。
- class名と namespace は移動後も維持し、Unity asset GUID / Scene referenceを切らない。

### Acceptance Criteria

- Play Modeの背景はマゼンタではなく、Color A / B を変更すると即時に色が変わる。
- 10種類すべてをUIから選択でき、APCにRadio mappingを設定した場合も同じ番号・同じ見た目になる。
- Backgroundの Beat依存 patternはBPM Tap / SetBpmに追従する。
- Text UIには選択中の layout 名、`Radius (TMP local units)` 等の対象・単位、entry transform、motion listが表示される。
- Textの各 layout（Three Circles / Grid / Rectangle Loop / Corner Rotate / Typewriter）は少なくとも1回Trip26で選択・描画できる。
- `PcgHash.cs` と `MidiParameterGroupName` の参照が0件になる。
- `BpmManager` を必要とする利用側に `BpmManager` serialized fieldが残らず、BPM component未発見時は明確なDIエラーで起動を停止する。
- EditMode tests、Unity compile、Play Modeで error / warning が0件になる。

## Non-Goals

- 新規の MIDI message type、外部 MIDI clock、BPM保存仕様の変更。
- Text content editor、複数Text object切替UI、Text glyphごとの色・font effect。
- Patternを ScriptableObject asset化すること。今回は code / HLSL catalogue を維持する。
- 全projectの namespace rename、Assembly Definition 導入、package構成の変更。
- 既存APC mapping profileの削除・初期化。

## Proposed Architecture

```
IBpmSource <--- BpmManager (BpmClock owner, only time writer)
    |                    |
    |                    +--> MaterialTextureSource.SetUniforms(_Beat)
    +--> MidiParameterRuntime / APC parameter controller

TextureLayer (MIDI provider)
    +--> MaterialTextureSource (nested IMidiParameterSource)
             +--> RadioParameter background.pattern
             +--> ProceduralPatternId -> material _PatternType
             +--> _MainColor / _SubColor / _Beat

TextController (MIDI provider)
    +--> RadioParameter text.layout
    +--> TextPatternDefinition (layout sampler entries)
             +--> ITextPattern: glyph positions / order
             +--> TextGlyphMotion: changes each sampled glyph over time
```

### Rendering

`ProceduralPatternId` をC#側の唯一の公開 enumとする。HLSLには同一の整数定数を `PatternIds.hlsl` として置く。C# enumとHLSLは言語を跨ぐため自動共有はできない。EditMode testが enum値、Radio option順、HLSL constant名の期待値を検査し、番号のずれをCI前に検出する。

`MaterialTextureSource` は `IBpmSource` をDIで受け、material instanceへ `_PatternType`、`_MainColor`、`_SubColor`、`_Tiling`、`_Offset`、`_Speed`、`_Opacity`、`_Beat`を設定する。`Material.HasProperty` による一度だけの契約検査を Awake / OnValidate で行い、欠けていれば対象名を含む `Debug.LogError` を一度だけ出す。マゼンタ調査では各 target graphics APIでshader compile logと `Graphics.Blit`結果を確認し、対応しない HLSL構文があればその箇所を対応構文へ置換する。

### Text

Textは次の二層を厳密に分ける。

| 層 | interface | 責務 | 例 |
| --- | --- | --- | --- |
| Layout | `ITextPattern` | glyphの文字index、位置、初期回転、初期scale、順序を生成 | Circle / Grid / Rectangle Loop / Corner Rotate / Typewriter |
| Motion | `TextGlyphMotion` | layout後の個々の glyph sampleへ時間変化を加える | Rotate、将来のPulse / Wave等 |

`TextPattern` を `TextLayoutDefinition` に改名するかは、既存Sceneのserialized typeを壊すため本計画では**改名しない**。代わりに fieldとUIの表示名を `Layouts` / `Layout Entries` に統一する。`_radius` は `Radius (TMP local units)` に表示変更し、Tooltipで「Circle中心からglyph中心まで。文字数は角度間隔だけを変える」と説明する。serialized field名を変える場合は必ず `[FormerlySerializedAs("_radius")]` を付ける。

`TextController` は `text.layout` RadioParameterを所有する。選択肢は `_patterns` の名前から editor-timeに同期し、名前重複は OnValidateでError、空のlistはUI HelpBox、選択indexは範囲へclampする。登録後の option reorder は保存済みindexを変えるため、今回のprofile schema versionを上げ、旧indexを現在の同じ名前へ移す。名前が消えた場合は0番を採用しWarningを一度出す。

### BPM / MIDI / Utility

`IBpmSource` は `Bpm`、`Time`、`DeltaTime`、`Beat`、`BeatFloor`、`BeatModulo`、`IsBeatModulo` の読み取りAPIだけを持つ。`SetBpm`、Tap、Syncは `BpmManager` の操作APIのままとし、Tempoを変更する未来のUI / inputは `IBpmController` を別途要求する。今回の既存MIDI設定は `BpmManager` を唯一の制御先とする。

`BpmManager` が `IBpmSource` を実装する。SimpleContainerがinterface実装componentを登録するため、consumerは `[Inject] Construct(IBpmSource bpm)` で取得する。AwakeとInjectの順序差を避けるため、consumerは `Configure` / `Initialize` をinject後まで開始しない。Sceneの古いserialized BPM fieldsは、コンパイル後にInspectorから除去し、null fallbackのbeat=0は残さない。

`PcgHash.Pcg01(double x, double y)` を `Pcg.Pcg01(new Vector2((float)x, (float)y)).x` に置換する。実装順序とfloat castは旧実装と同じなので、境界beat・fader indexを含むテストで値の完全一致を確認してから `PcgHash.cs` を削除する。

`IMidiParameterContainer` は `MidiParameters` だけへ縮小する。Group名を使うUIを後で必要にした場合は、provider metadataではなく `MidiParameterRegistry` が owner component / DisplayNameから表示用groupを作る別機能として設計する。

## Files to Change

### 新規

| Path | Responsibility |
| --- | --- |
| `Assets/qoooo/Scripts/Timing/IBpmSource.cs` | BPM read interface。 |
| `Assets/qoooo/Scripts/Rendering/Procedural/ProceduralPatternId.cs` | C# Pattern ID enumとoption表示名の固定catalogue。 |
| `Assets/qoooo/Scripts/Shaders/Rendering/Procedural/PatternIds.hlsl` | HLSL側のPattern integer constant。 |
| `Assets/qoooo/Tests/EditMode/Rendering/ProceduralPatternContractTests.cs` | C# catalogueとshader property契約の検証。 |
| `Assets/qoooo/Tests/EditMode/Midi/PcgCompatibilityTests.cs` | 旧PcgHashと統一Pcgの決定的出力検証（削除前だけ存在）。 |
| `Assets/qoooo/Tests/EditMode/Text/TextLayoutSelectionTests.cs` | layout option、選択clamp、profile migrationの検証。 |

### 変更

| Path | Change |
| --- | --- |
| `Assets/qoooo/Scripts/Textures/MaterialTextureSource.cs` | Shader property名、IBpmSource DI、Pattern catalogue参照、material契約検査、UIの色名・pattern UIを更新。 |
| `Assets/qoooo/Materials/ProceduralBackground.mat` | `_ColorA/_ColorB` を `_MainColor/_SubColor` へ移行し、旧saved propertyを除く。 |
| `Assets/qoooo/Scripts/Shaders/ProceduralPattern.shader` | Property / CBUFFER contractを確定し、共通IDsとpattern dispatcherをinclude。 |
| `Assets/qoooo/Scripts/Shaders/Pattern/PatternDispatcher.hlsl` | `PatternIds.hlsl` のIDを使用。 |
| `Assets/qoooo/Scripts/Shaders/Pattern/PatternBasic.hlsl` | categoryごとのHLSLへ分割する場合の共通basic patternのみを残す。 |
| `Assets/qoooo/Scripts/Textures/TextureLayer.cs` | source Parameterの列挙は維持しつつGroup API削除。 |
| `Assets/qoooo/Scripts/Parameters/IMidiParameterContainer.cs` | `MidiParameterGroupName` を削除。 |
| `Assets/qoooo/Scripts/Parameters/RadioParameter.cs` | editor-time option同期・profile migrationで必要な、安全なoption置換APIを追加。 |
| `Assets/qoooo/Scripts/Midi/MidiProfileData.cs`, `MidiProfileStore.cs` | text.layout option順変更に耐えるprofile version migrationを追加。 |
| `Assets/qoooo/Scripts/Controller/TextController.cs` | `text.layout` parameter、UI metadata、layout選択、DI BPM（時間同期を採用するmotionで必要な場合）を追加。 |
| `Assets/qoooo/Scripts/Pattern/TextCirclePattern.cs` | radiusの単位・対象を明示するfield名とTooltip。 |
| `Assets/qoooo/Scripts/Pattern/TextGridPattern.cs`, `TextRectangleLoopPattern.cs`, `TextCornerRotatePattern.cs`, `TextTypewriterPattern.cs` | layout naming / validationを統一。Time sourceは採用決定後にIBpmSourceまたは明示的なunscaled timeへ統一。 |
| `Assets/qoooo/Scripts/Pattern/TextRotateMotion.cs` | motion説明と時間sourceを統一。 |
| `Assets/qoooo/Scripts/Midi/ApcMiniMk2ParameterController.cs`, `MidiParameterRuntime.cs`, `MidiSystemController.cs`, `MinisMidiInput.cs` | serialized BpmManager参照をIBpmSource注入に移行し、初期化順を整える。 |
| `Assets/qoooo/Scripts/Midi/ApcMiniMk2Controller.cs` | `PcgHash` を `Util.Pcg` へ置換。 |
| `Assets/qoooo/Scenes/Trip26.unity` | 新pattern components / layouts / `text.layout` parameter、DI済み参照削除、Background material参照を保存。 |

### 削除

| Path | Change |
| --- | --- |
| `Assets/qoooo/Scripts/Midi/PcgHash.cs` | compatibility test合格後に削除。 |

### 将来の整理候補（今回の実装対象外）

| Path | Change |
| --- | --- |
| `Assets/qoooo/Scripts/Shaders/Pattern/*` | `Assets/qoooo/Scripts/Shaders/Rendering/Procedural/{Core,Patterns}/` へUnity metaを伴って移動。 |
| `Assets/qoooo/Scripts/Pattern/*` | `Assets/qoooo/Scripts/Text/{Layouts,Motions,Model}/` へUnity metaを伴って移動。namespaceは今回維持。 |
| `Assets/qoooo/Scripts/Midi/*` | `Assets/qoooo/Scripts/Midi/{Input,Output,Apc,Binding,Profile}/` へ責務単位で移動。 |

## Interfaces / Data Structures

```csharp
public interface IBpmSource
{
    double Bpm { get; }
    double Time { get; }
    double DeltaTime { get; }
    double Beat { get; }
    long BeatFloor { get; }
    int BeatModulo(int count);
    bool IsBeatModulo(int count, int target);
}

public enum ProceduralPatternId
{
    NoiseTexture = 0,
    DiagonalStripe = 1,
    VerticalStripe = 2,
    HorizontalStripe = 3,
    WaveStripe = 4,
    Checkerboard = 5,
    PolkaDot = 6,
    Sunburst = 7,
    GridLine = 8,
    PsychedelicRing = 9,
}
```

`MaterialTextureSource` の `RadioParameter` は IDを `background.pattern` のまま維持する。Text layoutは `text.layout` を新設する。両者の option値は `ProceduralPatternId` / `TextPattern` list順と一致し、範囲外値は0へclampする。変更通知は既存 `RadioParameter.Changed` のみを使用する。

## Implementation Steps

1. **現象を固定し、shader契約を最小修正する。**
   - 対象: `MaterialTextureSource.cs`、`ProceduralPattern.shader`、`ProceduralBackground.mat`。
   - `_MainColor/_SubColor` と `_Beat` を正しく供給し、material assetの旧propertyを移行する。`Material.HasProperty`を一度だけ検証する。
   - Play Modeで10パターンを順にBlitし、マゼンタが消え、Color変更が反映されることを確認する。マゼンタが残る場合はMetal/現在のgraphics APIのshader compile outputを採取し、その具体的構文を修正してから次へ進む。

2. **Pattern catalogueを固定し、shaderを分割する。**
   - 対象: `ProceduralPatternId.cs`、`PatternIds.hlsl`、`PatternDispatcher.hlsl`、`PatternBasic.hlsl`、新しい `Patterns/` directory。
   - enum / option名 / HLSL IDsの契約testを作る。Pattern functionは `Noise`、`Stripes`、`Grid`、`Radial` 等のcategory fileへ移し、dispatcherだけがswitchする。
   - 全IDに実装があり、未知IDはNoiseへfallbackすることを確認する。

3. **BPM read DIを導入する。**
   - 対象: `IBpmSource.cs`、`BpmManager.cs`、全BPM consumer、`Trip26.unity`。
   - `BpmManager : MonoBehaviour, IBpmSource` とし、consumerのserialized `BpmManager` fieldsを `[Inject]` methodへ置換する。MIDI composition rootはInject完了前には構成しない。
   - Tap、SetBpm、Sequence、shader `_Beat` が同一beatを読むことをPlay Modeで検証する。

4. **PCGとMIDI provider APIを整理する。**
   - 対象: `PcgHash.cs`、`Pcg.cs`、`ApcMiniMk2Controller.cs`、`IMidiParameterContainer.cs`、provider classes。
   - 代表値と大量入力で旧新Pcg結果を比較してからcall siteを置換し、旧classを削除する。Group APIと実装を削除する。
   - APC Random / Fader Randomの再現値が変わらず、registry数・mappingが維持されることを確認する。

5. **Text layout selectionをParameter化する。**
   - 対象: `TextController.cs`、`TextPattern.cs`、`RadioParameter.cs`、`MidiProfileData.cs`、`MidiProfileStore.cs`、`Trip26.unity`。
   - `text.layout` をregistryへ公開する。各layout名をradio optionへ同期し、profile index migrationを実装する。TextControllerは選択layoutだけをsampleする。
   - UI / APCの双方でlayoutを切り替え、Save→再起動後に同じlayoutが選ばれることを確認する。

6. **Textの単位表示とScene配置を整える。**
   - 対象: Text layout / motion classes、`Trip26.unity`、Text parameter UI。
   - Circle radiusをTMP local unitとして明示し、Grid size、Rectangle size、Corner margin、Typewriter spacingも同じ座標系としてlabel / tooltipを統一する。新規 layout componentsをTextRendererへ置き、`Three Circles`、Grid、Rectangle Loop、Corner Rotate、TypewriterのdefinitionをSceneに作成する。
   - 各layoutを手動選択し、glyph数0/1、長い文字列、未設定samplerでも例外が出ないことを確認する。

7. **ディレクトリ移動を最後に行う。**
   - 対象: `Scripts/Midi`、`Scripts/Pattern`、`Scripts/Shaders/Pattern` と関連meta。
   - Unityを停止し、`.cs` と `.meta` を同時に移動する。namespaceを変えず、shader includeの相対pathだけ更新する。移動後にSceneを開きMissing Script / Missing Materialがないことを確認する。
   - compile、EditMode tests、Play Mode、Background / Text / MIDIのsmoke testが全て通ってから完了とする。

## Edge Cases

- `_material == null`、shader不一致、必要property欠落: Blitしないまたは明示fallbackを使い、対象property名を一度だけLogする。毎frame Logは禁止。
- Pattern IDが範囲外: Noise（0）へ正規化する。
- Text layout listが空、名前重複、samplerがnull: radioを無効化または0へclampし、Textを空にしてWarningを一度だけ出す。
- Text profile内の廃止layout名: 0番へfallbackし、保存は次回Saveで新schemaへ書き戻す。
- DIがBPM sourceを見つけられない: beat=0で静かに動かさず、composition rootがErrorを出してMIDI runtimeの構成を中止する。
- input未接続、MIDI profileの未解決mapping: existing recovery / unresolved binding behaviorを変更しない。
- PCGのNaN / Infinity / very large beat: 既存どおりfloat castしたbit patternを使う。違いが出る場合は削除せずcompatibility wrapperを残す。

## Verification

- Unity compile: `uloop compile --timeout-seconds 180`。
- EditMode tests: Pattern契約、PCG互換、Text layout選択・migrationを追加して `uloop run-tests` で実行。
- Play Mode: Backgroundの10 patternをUIとAPCの両方で操作し、Color A/B、BPM変更、再接続後LEDを確認。
- Game View screenshot: 背景マゼンタがないことを確認する。
- Text: 5 layoutをUI / APCで切替、radius等のUI表記、保存復元、文字列長0/1/長文を確認。
- DI: Scene開始後にBpmSourceが各consumerへ一つだけ注入され、Inspector Bpm referenceが残っていないことを確認。
- Directory: Scene reopen後にMissing Script / Shader errorなし、Console error / warning 0件。

## Risks

- **マゼンタの根本原因がproperty名不整合以外にもある。** Property名の修正だけではfallback shaderを直せないため、Step 1でtarget APIのcompile outputを必ず取る。
- **Text layoutのindex保存。** 名前変更・並び替えでprofileが別layoutを選ぶため、schema migrationを同じStepで実装する。
- **DI lifecycle。** UnityのAwakeはInjectより先に起こり得る。初期化の入口をInject完了後へまとめる。
- **ファイル移動。** Unity GUIDを失うとScene参照が壊れる。metaを維持してUnity停止中に移動し、最後に行う。
- **Pcg互換。** C#のdouble→float変換順序を変えると現在のRandom sequenceが変わる。旧新比較testを先に作る。

## Assumptions

- Trip26が唯一の対象Sceneであり、現在のAPC profile (`trip26`) を互換対象とする。
- Backgroundのピンクは意図的な色ではなく復旧すべき描画異常である。
- 新しい `TextGridPattern`、`TextRectangleLoopPattern`、`TextCornerRotatePattern`、`TextTypewriterPattern` は「motion」ではなくlayout候補として扱うのが現コードと一致する。
- namespaceを維持したdirectory整理で十分であり、外部利用APIのrenameは今回不要である。

## Open Questions

なし。以下を合意済みとする。

- TextのMotionはPattern内部でLayoutと組み合わせる概念として扱う。新規 `Text*Pattern` はLayoutであり、`TextGlyphMotion` とは区別する。
- Text layout選択は現在の一つの `TextRenderer` が所有する `text.layout` Radioとする。
- Backgroundのユーザー向け色名は `Main Color / Sub Color` とする。
- ディレクトリの物理移動は今回のスコープ外とする。Files to Changeの「削除または移動」節は将来の整理候補であり、この計画の実装対象ではない。
