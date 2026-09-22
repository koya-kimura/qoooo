# qoooo

Unityで動作するVJ / リアルタイム映像合成アプリケーションです。複数のtexture sourceを
合成し、RosettaUIとAkai APC mini mk2から型付きParameterを操作します。合成結果は
previewとSyphonへ出力できます。

## 主な機能

- procedural background、model、textをlayerとして合成
- RosettaUIによるParameterと映像layerの操作
- APC mini mk2のPad/Fader mapping、LED feedback、MIDI profile保存
- BPM駆動のSequenceとFader Random mode
- macOS Syphon出力

## 動作環境

- Unity `6000.3.12f1`
- macOS
- Universal Render Pipeline 17.3.0
- 任意: Akai APC mini mk2、Syphon対応ソフトウェア

使用パッケージにはRosettaUI、PrefsGUI、Minis、Klak.Syphon、RtMidiが含まれます。

## 開始方法

1. Git LFSを有効化してcloneする。

   ```sh
   git lfs install
   git clone https://github.com/koya-kimura/qoooo.git
   cd qoooo
   git lfs pull
   ```

2. Unity HubでこのディレクトリをUnity `6000.3.12f1`として開く。
3. `Assets/qoooo/Scenes/Trip26.unity` を開いてPlayする。
4. APC mini mk2を接続する場合は、Input / Outputの接続状態をRosettaUIの
   `APC mini mk2` windowで確認する。

MIDI機器が未接続でも、RosettaUI、描画、Parameterの操作は動作します。

## Architecture

アーキテクチャの入口は [Docs/README.md](Docs/README.md) です。

- [Creative Coding Guide](Docs/CreativeCoding.md)
- [全体構成](Docs/Architecture.md)
- [Scene構成とlifecycle](Docs/Architecture/Runtime.md)
- [ParameterとMIDI](Docs/Architecture/ParametersAndMidi.md)
- [RenderingとText](Docs/Architecture/RenderingAndText.md)
- [開発・検証ガイド](Docs/Architecture/Development.md)

既存のImplementation Planは設計経緯を残す履歴資料です。現行仕様と計画の差分は
[PlanStatus](Docs/Architecture/PlanStatus.md) を参照してください。

## Asset policy

`Assets/qoooo/Models` 配下のモデル本体と高解像度textureはGit LFSで管理します。
Unityの`.meta`、Scene、Material、C#、shaderは通常Gitで管理します。

新しい大型assetを追加する前にLFS対象か確認してください。

```sh
git lfs ls-files
git check-attr filter -- Assets/qoooo/Models/path/to/asset.png
```

GitHubは通常Gitの単一blobを100MBまでしか受け付けません。制限未満でも、大量の8K textureを
通常Gitへまとめてpushするとpackが肥大化するため、モデルassetはLFSへ追加します。

## Validation

通常のC# compile確認:

```sh
dotnet build qoooo.sln --no-restore
```

Unity compile確認:

```sh
uloop compile --timeout-seconds 180
```

詳細な検証手順は[開発・検証ガイド](Docs/Architecture/Development.md)にまとめています。
