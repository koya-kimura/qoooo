# qoooo Architecture

qoooo は RosettaUI と Akai APC mini mk2 で型付き Parameter を操作し、映像を preview と Syphon へ出力する Unity VJ アプリケーションである。主 Scene は `Assets/qoooo/Scenes/Trip26.unity`。

## Core Ownership

| 領域              | 所有者                           | 責務                                                                                                          |
| ----------------- | -------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Tempo             | `BpmManager`                     | BPM clock を更新し、`IBpmSource` として read-only 公開する。                                                  |
| Parameter         | 各 consumer                      | 値を使う component が型付き Parameter を所有する。`MidiParameterRegistry` は validation と index だけを担う。 |
| MIDI              | `ApcMiniMk2ParameterController`  | APC page、typed binding、Listen、pickup、fader mode を持ち、owner Parameter を操作する。                      |
| Model source      | `ModelManager`                   | model source catalog と選択を持つ。                                                                           |
| Model arrangement | `ModelArrangementController`     | active slot の arrangement、requested source、position を決める。                                             |
| Model instances   | `ModelInstanceCache`             | runtime clone の生成、再利用、破棄を管理する。                                                                |
| Model appearance  | `ModelController`                | active slot へ scale、motion、visibility を適用する。                                                         |
| Poster            | `PosterLibrary` / `PosterCanvas` | document とその Canvas 表現を管理する。                                                                       |
| Texture effects   | `ProcessedTextureSource`         | source ごとの effect Parameter と処理済み texture を持つ。                                                    |
| Composition       | `TextureCompositor`              | layer 合成用の ping-pong buffer と最終 texture を持つ。                                                       |
| Palette           | `PaletteProvider`                | shared palette catalog と active palette を持つ。                                                             |

UI、Keyboard、MIDI は同じ owner Parameter を操作する。consumer は入力元を参照せず、現在値だけを読む。

## Rendering Invariants

```text
ModelCamera
  ↓
PosterCamera
  ↓
TextureCompositor
  ↓
Preview / Syphon
```

`TextureCompositor` は PosterCamera の描画完了後に同じ frame の合成を開始する。PosterCamera より前に RenderTexture を読むと、前 frame の Poster を合成するため、この順序を崩さない。

Texture effect は任意順の stack ではなく、次の固定 pipeline で処理する。

```text
UV → Sampling → Color → Alpha
```

## Poster Placement Invariants

- Grid は cell layout ではなく grid point coordinate system である。
- Grid point は Element Anchor を合わせる位置である。
- Grid の Width / Height は element size の grid unit である。
- Free placement は normalized coordinate を使う。
- Anchor は 9-point anchor を使う。

`PosterLibrary.Documents` が document の source of truth であり、`poster.layout` は active document を選択する。

## Stable Boundaries

- Parameter ID は binding と保存データの identity である。変更時は互換性を明示的に扱う。
- MIDI mapping は Parameter identity を変更しない。Fader は pickup / soft takeover を使い、assignment 切替時の jump を防ぐ。
- Effect Parameter も通常の Parameter として registry に公開する。
- Palette は shared catalog として管理する。Poster の保存データは `PosterFontCatalog` の stable Font ID を保持する。
- Legacy Text subsystem は Trip26 の最終出力経路ではない。現在の Poster text は `PosterDocument` が所有する。

## Current Documentation

- [Development.md](Development.md): 開発と handoff の最小手順
- [ParametersAndMidi.md](ParametersAndMidi.md): Parameter、MIDI、保存の境界
- [Rendering.md](Rendering.md): rendering、Poster、palette の関係
- [CreativeCoding.md](CreativeCoding.md): 見た目を変更する入口
