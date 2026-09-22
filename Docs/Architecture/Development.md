# Development Guide

## Environment

| Item               | Current value |
| ------------------ | ------------- |
| Unity              | 6000.3.12f1   |
| Render pipeline    | URP 17.3.0    |
| RosettaUI          | 2.0.1         |
| PrefsGUI RosettaUI | 1.4.0         |
| Minis              | 1.3.2         |
| Klak.Syphon        | 1.0.4         |
| Klak.Spout         | 2.0.6         |
| Test Framework     | 1.6.0         |

主要Sceneは`Assets/qoooo/Scenes/Trip26.unity`である。runtime script用asmdefはなく、
project codeは`Assembly-CSharp`へ入る。

## Repository layout

```text
Assets/qoooo/
  Materials/        Material assets
  Scenes/           Trip26 scene
  Scripts/
    Foundation/     時間と汎用処理
    Parameters/     型付きParameter
    Midi/           Transport、APC、Binding、Profile、Runtime
    Rendering/      Source、Camera、Layer、Compositor、Preview
    Text/           Layout、Motion、Model、Runtime
    Presentation/   UI contractとUiBuilder
    Integration/    Syphon等の外部連携
    Editor/         Editor専用処理
  Tests/EditMode/   pure logicのEditMode test source
Docs/               現行architectureと履歴計画
```

creative workは[CreativeCoding.md](../CreativeCoding.md)を入口にし、そこで示すownerと境界を確認してから
詳細architectureを読む。

## Build and validation

通常のcompile確認:

```sh
dotnet build qoooo.sln --no-restore
```

Unity側のcompile確認にはプロジェクトで導入済みのuLoopを使う。

```sh
uloop compile --timeout-seconds 180
```

現在のtest source:

- `BpmClockTests`: BPM境界、modulo、tap tempo
- `ApcMiniMk2LayoutTests`: grid座標変換、9 fader定数

`Assets/qoooo/Tests/EditMode`には専用asmdefがない。Unity Test Runnerでtestが発見されない場合、
まずassembly構成を確認する。文書だけの変更でもMarkdown link checkと`git diff --check`を行う。

## Parameterを追加する

1. 機能を所有するcomponentへ適切なParameter型をserialized fieldで追加する。
2. IDは意味のあるstable stringにし、既存IDと重複させない。
3. ownerの`IMidiParameterContainer.MidiParameters`から返す。
4. 入れ子のtexture sourceなら`IMidiParameterSource`から返し、`TextureLayer`に中継させる。
5. consumerはParameter値を直接読み、MIDI controllerを参照しない。
6. `Trip26`のScene defaultと必要なAPC mappingを設定する。
7. Save/Revert/Reset、UI direct control、APC入力、LEDを確認する。

Parameter ID変更はPrefs stateとmappingの互換性を切る。renameが必要な場合はmigrationを先に用意する。

## MIDIを変更する

- raw device処理は`Midi/Transport`へ置く。
- APC note/CC番号は`Midi/Apc`のconstants/layoutへ集約する。
- Parameter kind別mapping DTOは`Midi/Bindings`へ置く。
- application側へMIDI型や文字列key lookupを持ち込まない。
- native output処理からUnity APIを呼ばない。
- disconnect、focus復帰、Domain Reload無効時のevent重複を確認する。
- fader変更ではpickup成立前のjumpがないことを確認する。

## Rendering sourceを追加する

1. `ITextureContainer`を実装し、生成textureのownershipを明確にする。
2. UIが必要なら`IParameterContainer`、MIDI Parameterが必要なら`IMidiParameterSource`を実装する。
3. `TextureLayer._sourceComponent`へ割り当てる。
4. 解像度は`IResolutionContainer`から取得する。
5. 作成した`RenderTexture`をdisable/destroy時に解放する。

## Text layoutを追加する

この手順はTrip26でinactiveなLegacy Text subsystemを再利用する場合にだけ適用する。現在のPoster内Textは
`PosterDocument`のText elementとして編集する。

1. `ITextPattern`を実装するMonoBehaviourを`Text/Layouts`へ追加する。
2. `TextPattern.Entries`のsamplerとしてSceneへ設定する。
3. `TextPattern.Name`を一意に保つ。
4. subsystemを再びlive Parameter化する場合、layout listの順序変更と保存indexの互換を確認する。
5. motionはlayout samplerへ混ぜず、`TextGlyphMotion`としてentryへ追加する。

## Sceneとassetの変更

- `.cs`、shader、Material、Sceneの参照はGUIDで結ばれる。
- ファイル移動時は対応する`.meta`を必ず同時に移動する。
- Scene YAMLの`m_EditorClassIdentifier`は過去namespaceを保持することがあるため、script GUIDを正本とする。
- `Trip26`を開き、Missing Script / Missing Material / shader errorを確認する。
- Play ModeではMIDI未接続、inputのみ、output再接続の各状態を確認する。

## Documentation policy

- 現在の構造を変えたらこのディレクトリの文書を同じ変更で更新する。
- 実装予定はArchitectureへ断定形で書かず、Implementation Planへ置く。
- 計画が完了または方針変更されたら`PlanStatus.md`を更新する。
