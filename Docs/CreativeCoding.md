# Creative Coding Guide

この文書は、Trip26の見た目を自分で変更するための最初の入口である。まずコード内の
`CREATIVE AREA`コメントを読み、次にこの文書、[Architecture.md](Architecture.md)、
[Architecture/](Architecture/)の順で読む。Implementation Planは設計履歴であり、現行仕様ではない。

## やりたいことから探す

| やりたいこと | 最初に見る場所 | 守る境界 |
| --- | --- | --- |
| Modelの動きや反応を変える | `Rendering/Models/ModelController.cs` | 時間は`IBpmSource.Beat`から読む |
| Modelを追加・選択する | `Rendering/Models/ModelManager.cs` | `model.source`のindex互換を確認する |
| Posterの構成を作る | RosettaUI `Poster Composer`、`Rendering/Sources/PosterDocument.cs` | `PosterDocument = Layout = Preset` |
| Posterの描画方法を変える | `Rendering/Sources/PosterCanvas.cs` | element list先頭が見た目の前面 |
| Poster内の文字を変える | `PosterDocument`のText element、`poster.main-text` | activeな文字表現はPoster側 |
| 独立したText表現を研究する | `Text/` | Legacy TextはTrip26でinactive |
| Rhythm表現を作る | `Foundation/Utilities/Rhythm.cs` | clock stateを持たせない |
| Background patternを作る | `Rendering/Sources/MaterialTextureSource.cs`とshader | enum、Radio options、HLSL IDを揃える |
| Layer effectを変える | `Rendering/Composition/TextureEffect.cs` | effectは各`TextureLayer`に属する |
| 操作値を追加する | 値を消費するcomponent | ownerがtyped Parameterを持つ |
| Keyboard入力を追加する | `Parameters/Runtime/KeyboardParameterController.cs` | Parameterをtriggerし、consumerを直接操作しない |
| MIDI割当を変える | RosettaUI `MIDI Bindings` / `MIDI Mapping` | Parameter IDとAPC semanticsを維持する |
| Live UIを変える | `Presentation/Runtime/UiBuilder.cs` | ownerと同じParameter instanceを表示する |

表中のパスはすべて`Assets/qoooo/Scripts/`からの相対パスである。

## Model Motionの参照形

`BpmManager`だけがclockを進め、creative codeはread-onlyなbeatを見た目へ変換する。
`ModelController`のStill / Spin / Pulseが最小の参照実装である。

```csharp
var beat = (float)(_bpmSource?.Beat ?? 0d);
var rhythmIndex = Rhythm.GetIndex(beat, cycleBeats);
var rhythmPhase = Rhythm.GetPhase(beat, cycleBeats, easeBeats);

// beat、rhythmIndex、rhythmPhaseからrotation、scale、色などを決める。
```

独自の経過時間を加算しない。速度や振幅のような作品固有の調整値は、まずserialized fieldで十分かを
判断する。RosettaUI、Keyboard、MIDI、Prefsからライブ操作する必要がある値だけをParameterにする。

## Rhythm API

- `GetIndex(t, interval)`: 現在のcycle indexを返す。
- `GetPhase(t, cycleLength, easeDuration)`: cycle内の補間済みphaseを返す。
- `GetLerpNoise(...)`: cycle間を補間する1次元noiseを返す。
- `GetLerpNoise3D(...)`: cycle間を補間する3次元noiseを返す。

`t`には通常`IBpmSource.Beat`を渡す。`Rhythm`は変換utilityであり、BPMやclockを所有しない。

## PosterとText

Trip26では`PosterLibrary`が可変長の`PosterDocument` listを持つ。Preset、Layout、Documentは別物ではなく、
同じ`PosterDocument`を指す。`poster.layout`はDocument indexであり、Editorial / Split / Modular / Customは
factory defaultにすぎない。Poster Composerで作成、複製、rename、削除、element編集を行う。

現在の出力に入る文字はPosterのText elementと`poster.main-text`である。`TextController`、Text Camera、
TextLayerからなるLegacy Text subsystemはSceneでinactiveで、`text.*`はlive Parameter、MIDI、Prefs stateに
含まれない。再利用するときはinactive subsystemとして明示的に再設計・再接続する。

## Parameterと入力

Parameterは値を使うMonoBehaviourが所有する。`MidiParameterRegistry`は検索とvalidationを行うindexで、
値のownerではない。RosettaUI、Keyboard、MIDIは同じtyped Parameterへ入力するadapterである。
consumerは入力元を知らず、Parameterの現在値だけを読む。

Parameterを追加するときは、stable ID、適切な型、ownerの`MidiParameters`公開、UI/APC/Prefsの必要性を
確認する。ID変更は保存済みmappingとstateの互換性を切るため、rename前にmigrationが必要である。

## UIの責務

- `LIVE CONTROLS`: 本番中に同じParameter instanceを操作する。
- `AUTHORING TOOLS`: Poster Documentのような構造データを編集する。
- `SETUP / DEBUG`: OutputとMIDI接続・割当・profileを管理する。

`MIDI Bindings`はParameterとPad/Faderのassignment、`MIDI Mapping`はprofile、Rebuild、Fader function、
Revert、Resetを扱う。MIDI UIにlive Parameter value editorを重複して作らない。

## 保存されるもの

保存はQoooo root windowの`Save`を押した時だけ行われる。Play停止時の自動保存はない。

1. 全`IPrefsSaveParticipant`が`PrepareSave()`する。
2. `Prefs.Save()`が実行される。
3. 成功時に全participantが`CommitSave()`する。
4. 例外時は`AbortSave()`とError logを実行する。

`MidiProfileStore`はAPC mappingと、Oneshot/Momentaryを除くParameter値を保存する。
`PosterComposer`は`qoooo.poster.library.v2`へDocument listを保存し、旧v1はload時にmigrationする。
Syphon名は`PrefsString`で保存される。APC current page、接続状態、Listen sessionは保存しない。
現行participantの`AbortSave()`はstaged Prefs cacheをrollbackしない。

## Creative regression checklist

- Consoleのcompile error、duplicate Parameter ID、exceptionが0件。
- MIDI未接続でもLive Controlsと描画が動く。
- Space / B / Shift+BがTap / Sync Beat / Sync Barとして動く。
- Model motionとBackgroundが同じBeatに追従する。
- Poster ComposerのCRUD後も`poster.layout`とDocument選択が一致する。
- Save後にmapping、保存対象Parameter、Poster Documentsが復元される。
- PreviewとSyphonが同じ最終textureを受け取る。
