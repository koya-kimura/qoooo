# Implementation Plan Status

この文書は既存Implementation Planと現行コードの関係を示す。計画書は背景と設計判断を
残すため削除しないが、現行仕様の参照先にはしない。

## Status summary

| Plan                                                                                                                  | 現在の位置づけ                                                                     |
| --------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| [ApcMiniMk2MidiInputImplementationPlan.md](../ApcMiniMk2MidiInputImplementationPlan.md)                               | 初期の文字列keyベースAPC実装の履歴。対応コードはlegacyとして残るがTrip26では非配置 |
| [MidiInputConsumerAccessImplementationPlan.md](../MidiInputConsumerAccessImplementationPlan.md)                       | 型付きParameter中心への移行計画。主要部分は実装済み、一部は簡略化または未実装      |
| [RenderingTextAndFoundationRefactorImplementationPlan.md](../RenderingTextAndFoundationRefactorImplementationPlan.md) | Rendering/Text/Foundation再編計画。主要な修正と物理配置変更は概ね反映済み          |

## 型付きParameter / MIDI計画

実装済み:

- 7種の型付きParameterとowner側のserialized field
- `IMidiParameterContainer` / `IMidiParameterSource` / Registry
- Minis raw inputとRtMidi output workerの分離
- 型別Binding DTO、8 page、Fader pickup、Mute/Random
- Pad/Fader Listen、型別RosettaUI、APC LED出力
- Scene default mappingとPrefs mapping/stateのSave/Revert/Reset
- MIDI未接続時にも成立するParameterとapplication処理

現行実装が計画より簡素な箇所:

- APC Surface、Listen Controller、Binding Registryの独立serviceはなく、
  `ApcMiniMk2ParameterController`へ統合されている。
- profile persistence abstractionとconnection settings recordはない。
- mapping/stateのorphan保持、競合record全件のresolution statusはない。
- `AbortSave`はPrefs cacheをrollbackしない。
- LEDはdesired/current dirty queueではなく、15Hz pollと前回値比較で差分送信する。
- input callback queueはlockされていない。
- `poster.model-layout`、`poster.typography-layout`とLegacy `text.*`はTrip26のlive Parameter / MIDI pathからretire済みである。

## Rendering / Text / Foundation計画

実装済み:

- `IBpmSource`と`BpmManager`のread-only tempo境界
- procedural materialの`_MainColor` / `_SubColor` / `_Beat` contract
- `background.pattern`の10 option Parameter
- 複数Legacy Text layout sampler（Trip26のlive Parameter / MIDI pathからはretire済み）
- layoutと`TextGlyphMotion`の分離
- APC Randomで`qoooo.Foundation.Utilities.Pcg`を使用
- `MidiParameterGroupName`の削除
- Scriptsの責務別directory / namespace整理
- PosterDocument / Poster Composerによる最終Poster compositionとLegacy Text runtimeのTrip26停止

未完了または追加検証が必要:

- C# enum、Radio option、HLSL IDを照合するcontract test
- Text layout名変更・並べ替えに対するprofile migration
- Text motionとobject transformのBPM同期
- Parameter、Binding、Profile、LEDを対象にした計画記載のEditMode test群

## Legacy code

次の型は現行`Trip26` Sceneの実行経路ではない。

- `qoooo.Midi.Runtime.ApcMiniMk2MidiInput`
- `qoooo.Midi.Bindings.IApcMiniMk2Controller`
- `qoooo.Midi.Bindings.ApcMiniMk2Controller`
- `qoooo.Midi.Bindings.ApcMiniMk2BindingRegistry`
- union形式の`ApcMiniMk2Binding`

削除時はScene/Prefab参照が0件であること、現行controllerが同等の利用箇所をすべて置換していること、
既存Prefs keyの扱いを確認する。
