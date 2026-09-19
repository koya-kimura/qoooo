# Documentation

このディレクトリには、現行実装を説明するarchitecture文書と、設計経緯を残す
Implementation Planを置く。プロジェクト全体のREADMEは[../README.md](../README.md)を参照する。

現行アーキテクチャ文書は、現在の `master` のコードと
`Assets/qoooo/Scenes/Trip26.unity` を基準にしている。

## 読む順番

1. [Architecture.md](Architecture.md) - システム全体、依存方向、主要データフロー
2. [Architecture/Runtime.md](Architecture/Runtime.md) - Scene構成、DI、初期化、フレーム実行順
3. [Architecture/ParametersAndMidi.md](Architecture/ParametersAndMidi.md) - 型付きParameter、APC binding、MIDI I/O、Prefs
4. [Architecture/RenderingAndText.md](Architecture/RenderingAndText.md) - 描画pipeline、Text layout/motion
5. [Architecture/Development.md](Architecture/Development.md) - 開発環境、テスト、変更時の確認事項

## 文書の位置づけ

- 上記の文書は現行実装の説明であり、コード変更時に更新する。
- `*ImplementationPlan.md` は設計判断と移行経緯を残す履歴資料である。
- 計画書と現行コードが食い違う場合は、現行コードとこの索引から辿る
  アーキテクチャ文書を優先する。
- 未実装の構想は現行仕様へ混ぜず、[Architecture/PlanStatus.md](Architecture/PlanStatus.md)
  で実装状況を確認する。

## 履歴資料

- [ApcMiniMk2MidiInputImplementationPlan.md](ApcMiniMk2MidiInputImplementationPlan.md)
- [MidiInputConsumerAccessImplementationPlan.md](MidiInputConsumerAccessImplementationPlan.md)
- [RenderingTextAndFoundationRefactorImplementationPlan.md](RenderingTextAndFoundationRefactorImplementationPlan.md)
