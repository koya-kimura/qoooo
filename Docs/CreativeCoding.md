# Creative Coding Guide

Trip26 の見た目を変更する入口である。[Architecture.md](Architecture.md) と [Rendering.md](Rendering.md) を先に読み、変更する領域の owner を確認する。

| やりたいこと               | 最初に見る場所                                                     | 守る境界                                            |
| -------------------------- | ------------------------------------------------------------------ | --------------------------------------------------- |
| Model source を変える      | `Rendering/Models/ModelManager.cs`                                 | source selection は `ModelManager` が所有する。     |
| Model arrangement を変える | `Rendering/Models/ModelArrangementController.cs`                   | arrangement は clone lifecycle を持たない。         |
| Model motion を変える      | `Rendering/Models/ModelController.cs`                              | time は `IBpmSource.Beat` から読む。                |
| Poster を構成する          | RosettaUI `Poster Composer`、`Rendering/Sources/PosterDocument.cs` | `PosterLibrary.Documents` が source of truth。      |
| Poster の描画を変える      | `Rendering/Sources/PosterCanvas.cs`                                | element list 先頭が見た目の前面。                   |
| Background を変える        | `Rendering/Sources/MaterialTextureSource.cs`                       | shared palette と output texture ownership を保つ。 |
| Layer effect を変える      | `Rendering/Composition/ProcessedTextureSource.cs`                  | `UV → Sampling → Color → Alpha` の順を保つ。        |
| ライブ操作を追加する       | 値を消費する component                                             | owner が typed Parameter を持つ。                   |
| MIDI 割当を変える          | RosettaUI `MIDI Bindings` / `MIDI Mapping`                         | Parameter ID と APC semantics を維持する。          |

表中のパスはすべて `Assets/qoooo/Scripts/` からの相対パスである。

`BpmManager` だけが clock を進める。BPM に同期する visual は `IBpmSource.Beat` を読み、clock state を新たに所有しない。

Trip26 の text output は `PosterDocument` の Text element で扱う。Legacy Text subsystem を最終 output 経路へ加える場合は、独立した変更として接続と ownership を明示する。
