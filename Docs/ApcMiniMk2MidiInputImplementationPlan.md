# APC mini mk2 MIDI In / BPM 実装計画

## Objective

`Trip26` シーンで Akai APC mini mk2 の **MIDI input** を keijiro の Minis 1.3.2 経由で受信し、ページ付き 8 × 8 パッド、フェーダー、フェーダーボタンと 7 種のバインディング状態を、既存 VJ 機能から安全に読み取れるようにする。

併せて、提示された TypeScript の `BPMManager` と同じ状態・遷移契約を持つ BPM コンポーネントを追加する。APC の `Sequence`、`Random`、`Momentary` はこの BPM が供給する beat を使用する。

成功条件は次のとおり。

- APC 未接続でも `Trip26` は例外なく起動・描画を継続する。
- 接続後に 1 回操作すると Minis の `MidiDevice` を検出し、Channel 1（Minis の `channel == 0`）のイベントだけを 1 台分受信する。
- 同じシーン内マッピング、MIDI イベント列、beat 列を入力したとき、添付資料 `UnityMinisHandoff.md` の状態遷移と一致する。
- BPM は初期値 120、範囲 1–1000、Tap Tempo、拍境界での保留 BPM 適用、beat/bar 同期を実現する。
- MIDI out、LED 制御、RtMidi を直接使う出力コードは追加しない。

## Current State

調査対象は `master` の `cfc453f3ae22613429f5157dde0006119aea212a`（2026-09-14）である。

| 項目 | 現状と根拠 |
| --- | --- |
| Unity | Unity 6000.3.12f1。`ProjectSettings/ProjectVersion.txt`。 |
| MIDI 依存 | `Packages/manifest.json` に `jp.keijiro.minis: 1.3.2`、`Packages/packages-lock.json` に `com.unity.inputsystem: 1.19.0` が存在する。追加・更新しない。 |
| Input System | `ProjectSettings/ProjectSettings.asset` の `activeInputHandler: 1` で有効。 |
| Scene | 稼働シーンは `Assets/qoooo/Scenes/Trip26.unity`。`Managers` 配下に `ModelManager` と `TextManager`、別に `UiBuilder` がある。 |
| DI | `Assets/qoooo/Resources/ProjectContainer.prefab` は UnitySimpleContainer の自動バインドを利用し、`UiBuilder` は `IUiTarget` を列挙注入する。MIDI基盤は DI 登録を必要としない。 |
| 時間管理 | `Assets/qoooo/Scripts` に BPM/beat/tempo の所有者はない。各コンポーネントは必要に応じ `Time.time` を直接使う。 |
| テスト | プロジェクト固有の EditMode/PlayMode テスト、asmdef は存在しない。 |

Minis の実装も確認済みである。`MidiDevice` は MIDI メッセージ受信後に動的生成され、`onWillNoteOn`、`onWillNoteOff`、`onWillControlChange` は Input System の control state 更新前に発火する。そのため callback 引数の note/CC 番号と value だけを使用する。Minis の `description.product` はポート名に ` Channel 0` を付加した値であり、製品名は完全一致ではなく部分一致で照合する。

### Reference materials and adoption

今回追加で確認した資料は、定数の正と実装パターンの参考に限定して使う。添付コードが持つ LED 出力、RtMidi、全 MIDI デバイスの合成、UI エミュレーションを本プロジェクトへ移植するものではない。

| 参照 | 確認した事実・採用内容 | 今回採用しない内容 |
| --- | --- | --- |
| `APC mini mk2 - Communication Protocol - v1.0.pdf` | Port 0 / MIDI Channel 0 における Clip Launch `0x00..0x3F`、Track Button `0x64..0x6B`、Scene Launch `0x70..0x77`、Shift `0x7A`、Fader 1–8 `CC 0x30..0x37`、Master Fader `CC 0x38` を `ApcMiniMk2Constants` の根拠にする。 | LED、RGB SysEx、Device Enquiry、Introduction Message。 |
| `ApcMiniMk2.cs` | 機材固有の番号・範囲・座標変換を1つの静的クラスへ集約する。 | `IMidiOutput` extension、LED palette/behavior。 |
| `MidiInput.cs` | Minis callback は raw event を queue するだけにして、一定の早い `Update` で queue を drain し状態を確定する。二重 buffer で、処理中に届いたイベントを次回へ残す。 | 全デバイス/channels の合成、pitch bend/aftertouch/全CC monitor、RosettaUI monitor。 |
| `IMidiInput.cs`、`MidiBinding.cs`、`MidiCcBinding.cs` | 接続状態と入力状態を明示的な read API として出し、未割当・範囲外は安全な既定値を返す。 | MIDI learn、動的な任意 note/CC 割当。 |
| `MidiSurface.cs`、`MidiOutput.cs`、`IMidiSurface.cs`、`IMidiOutput.cs`、`MidiDiagnostics.cs` | lifecycle 時の購読解除、接続状態の観測、診断時に MIDI 処理を止めない方針を参考にする。 | 操作面UI、実機不在時のエミュレーション、LED flush、出力ポート管理、永続診断ログ。 |

## Problem

このプロジェクトには Minis は導入済みだが、APC mini mk2 固有の note/CC 変換、ページ、バインディング、再接続、beat 同期、BPM のいずれも実装されていない。入力 callback に VJ の具象コンポーネントを直接結びつけると、物理配線と状態遷移が混在し、実機なしの検証と将来のマッピング変更が困難になる。

## Requirements

### Functional requirements

- MIDI input は Minis の `MidiDevice` callback で受ける。`InputAction` / `PlayerInput` を主経路にしない。
- Minis callback は Controller や VJ を直接変更せず、番号・正規化 value・種別だけを private な入力 queue に追加する。`ApcMiniMk2MidiInput.Update` が queue を順に drain してから Controller を更新する。
- `ApcMiniMk2MidiInput` のシリアライズ済みフィールドに、`Trip26` 専用のマッピング定義を直接保存する。ScriptableObject、JSON、Prefs への保存は行わない。
- 受信対象は product 名の部分一致（既定値 `APC mini mk2 Control`）と Minis channel `0`。一致デバイスが複数あっても最初の 1 台だけを購読する。
- 公式プロトコルの Port 0 / MIDI Channel 0 の定数を使用する。Clip Launch `0..63`、Track Button `100..107`、Scene Launch `112..119`、Shift `122`、Fader 1–8 `CC 48..55`、Master Fader `CC 56` を添付資料の座標・状態遷移で扱う。
- `Toggle`、`Radio`、`Oneshot`、`Momentary`、`State`、`Sequence`、`Random` の全てをサポートする。
- MIDI 未接続、切断、再接続は非致命とし、切断中も論理状態と BPM を保持する。`MidiSuccess` は現在購読中かを表す。
- BPM は毎フレーム `Time.unscaledDeltaTime` を使用して進める。`Time.timeScale` の影響を受けないため、VJ 出力の停止/減速中も音楽的な拍を維持できる。
- `SetBpm` は即時変更ではなく、次の整数 beat を越えたフレームで反映する。Tap Tempo は直近 2 秒、最大 8 回を保持し、4 回以上で平均タップ間隔から保留 BPM を設定する。
- `SyncBeat` は現在 beat の小数部を捨てる。`SyncBar` は既定 4 bar × 4 beats の先頭へ戻す。`BeatModulo` と `IsBeatModulo` は負数にも正の剰余を返す。

### Acceptance criteria

- `ApcMiniMk2Controller` と `BpmClock` の全状態遷移を実機なしの EditMode テストで固定する。
- `Trip26` で APC 未接続起動、接続後の検出、USB 抜線、再接続後の単発処理を手動確認する。
- inspector 上でマッピングと BPM 初期値を確認・変更できる。
- 既存の `ModelManager`、`TextManager`、テクスチャ合成、Syphon、RosettaUI の既存挙動を変更しない。

## Non-Goals

- APC mini mk2 の LED、LED 色、ページ表示、MIDI output。
- RtMidi output API の直接利用。
- MIDI 状態・マッピング・BPM の永続化。
- p5/WebMIDI の移植、Web 権限 UI、他機種用の MIDI 抽象化。
- BPM にオーディオ解析、外部クロック、MIDI clock、BPM 自動検出を加えること。
- 今回の基盤を用いた既存 VJ 機能への具体的なパッド割当。利用側は後続変更で公開 API を読む。

## Proposed Architecture

```text
APC mini mk2
  -> Minis.MidiDevice callbacks
  -> ApcMiniMk2MidiInput.PendingMidiEvent queue
  -> ApcMiniMk2MidiInput.Update()（queue drain）
  -> ApcMiniMk2Controller (純粋 C#; 入力ルーティングと状態)
  -> ApcMiniMk2BindingRegistry (純粋 C#; 設定検証・セル索引・初期状態)
  -> VJ利用側（将来追加。読み取り API のみ使用）

Time.unscaledDeltaTime
  -> BpmManager (MonoBehaviour)
  -> BpmClock (純粋 C#; BPM/beat/tap tempo)
  -> ApcMiniMk2MidiInput.Update()
  -> ApcMiniMk2Controller.UpdateState(BpmManager.Beat)
```

`BpmManager` は `[DefaultExecutionOrder(-100)]`、`ApcMiniMk2MidiInput` は `[DefaultExecutionOrder(-90)]` とする。よって同じフレーム内で BPM を進め、queue を drain してから MIDI Controller の beat 依存状態を更新する。これにより callback の発火地点や Script Execution Order の手作業設定に依存しない。

`ApcMiniMk2MidiInput` は `Trip26` の新規 `MIDI` GameObject に付与し、同じ GameObject の inspector で product 名、channel、binding リスト、フェーダーボタン機能、`BpmManager` 参照を保持する。アダプタが `OnEnable` で Controller を初期化して Input System の device change を購読し、`OnDisable` で必ず解除する。再 enable は設定から初期状態を再構築する（添付仕様の `destroy()` 互換）。

`BpmManager` は同じ `MIDI` GameObject に配置し、初期 BPM を inspector に保持する。Controller や Minis を参照しないため、BPM を使う将来コンポーネントも `BpmManager` の読み取り API を参照できる。

## Design Decisions

| 決定 | 理由・影響 | 採用しない案 |
| --- | --- | --- |
| Mapping は `Trip26` の `ApcMiniMk2MidiInput` に直接シリアライズ | ユーザー合意済み。現在の単一シーン運用に合い、専用アセットを増やさない。 | ScriptableObject、JSON、Prefs。 |
| ドメイン状態は通常 C# class | Minis/Unity 実機なしで決定論的テストできる。 | MonoBehaviour に全ロジックを集約。 |
| BPM は `BpmManager` facade + `BpmClock` | Unity lifecycle と TypeScript 互換の純粋計算を分離する。 | MIDI adapter 内で beat を生成する。 |
| `unscaledDeltaTime` を BPM の時刻源にする | VJ の `timeScale` と独立し、外部音楽との同期を保つ。 | `deltaTime`。timeScale が変わると BPM が変わる。 |
| callback は event queue に蓄積して `Update` で確定 | callback と利用側 Update の順序を固定し、複数入力を取りこぼさず、Controller を Unity callback から分離する。 | callback 内で Controller と VJ state を直接更新。 |
| device change 時に一度全解除して再走査 | callback の重複購読を避け、切断・再接続を単純化する。 | 対象毎の部分的な購読差分管理。 |
| product 名は部分一致、channel は 0 に限定 | Minis の product 表記には ` Channel 0` が加わる。別 MIDI 機器の混入を防ぐ。 | 全 `MidiDevice` を無条件購読。 |
| APC物理定数は `ApcMiniMk2Constants` にだけ置く | PDFの Port/Channel/Note/CC 定義と、ドメインの page/row/col を分離して番号の散在を防ぐ。 | binding/adapter/scene に数値リテラルを散在。 |
| Random は TypeScript 互換 PCG | 同一 beat と cellKey に対する結果を再現する。 | `UnityEngine.Random`。 |

## Files to Change

| 種別 | パス | 内容 |
| --- | --- | --- |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Constants.cs` | PDF準拠の Port/Channel、Grid/Track/Scene/Shift の note 範囲、Fader 1–8 と Master Fader の CC、個数、範囲判定 helper。既定フェーダーボタン機能。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Layout.cs` | note ⇔ row/col、page 付き `cellKey` の純粋変換。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Binding.cs` | `[Serializable]` mapping 型、binding 種別、セル位置、登録後セル、フェーダーモード enum。polymorphic な7型を避け、共通の `BindingType` と全種別フィールドを持つ1定義型にする。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2BindingRegistry.cs` | mapping の検証、セル索引、初期値と deep copy を構築する純粋 class。 |
| 新規 | `Assets/qoooo/Scripts/Midi/IApcMiniMk2Controller.cs` | 利用側が依存する状態読み取り・入力更新契約。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Controller.cs` | note/CC/page/fader/beat/Momentary の純粋状態遷移。 |
| 新規 | `Assets/qoooo/Scripts/Midi/PcgHash.cs` | TypeScript 版の unsigned 32-bit / float32 互換 PCG ハッシュ。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2MidiInput.cs` | Minis adapter、device lifecycle、`MidiSuccess`、Controller facade、Scene 設定。 |
| 新規 | `Assets/qoooo/Scripts/Timing/BpmClock.cs` | 提示された TypeScript `BPMManager` と同じ純粋状態遷移。 |
| 新規 | `Assets/qoooo/Scripts/Timing/BpmManager.cs` | `BpmClock` を所有し、Unity の unscaled delta time を渡す MonoBehaviour facade。 |
| 新規 | `Assets/qoooo/Tests/EditMode/ApcMiniMk2LayoutTests.cs` | 座標・セルキーのテスト。 |
| 新規 | `Assets/qoooo/Tests/EditMode/ApcMiniMk2BindingRegistryTests.cs` | mapping 検証・初期値・deep copy のテスト。 |
| 新規 | `Assets/qoooo/Tests/EditMode/ApcMiniMk2ControllerTests.cs` | 全 binding、fader、beat、フォールバックのテスト。 |
| 新規 | `Assets/qoooo/Tests/EditMode/BpmClockTests.cs` | BPM、Tap Tempo、同期、剰余、拍境界反映のテスト。 |
| 変更 | `Assets/qoooo/Scenes/Trip26.unity` | `Managers` の子として `MIDI` GameObject を追加し、`BpmManager` と `ApcMiniMk2MidiInput`、初期 mapping をシリアライズ。 |

変更しないファイルは `Packages/manifest.json`、`Packages/packages-lock.json`、`ProjectSettings/ProjectSettings.asset`、既存 UI/レンダリング/出力コンポーネントである。依存はすでに導入済みである。

## Interfaces / Data Structures

### Scene-serialized configuration

`ApcMiniMk2MidiInput` の private serialized fields は少なくとも以下を持つ。

```csharp
[SerializeField] private string _productName = "APC mini mk2 Control";
[SerializeField] private int _channel = 0;
[SerializeField] private BpmManager _bpmManager;
[SerializeField] private List<ApcMiniMk2Binding> _bindings = new();
[SerializeField] private List<FaderButtonFunction> _faderButtonFunctions = new();
```

`ApcMiniMk2Binding` は `key`、`type`、`targets` に加え、`defaultBool`、`defaultInt`、`cycleLength`、`defaultSteps`、`radioKey` を持つ。`CellPosition` は `page (0..7)`、`row (0..7)`、`col (0..7)`。初期フェーダーボタン配列は `[Mute, Random, Mute, Random, Mute, Random, Mute, Random, Mute]` とする。

Inspector で `List<ApcMiniMk2Binding>` を編集できるよう、定義型は Unity の標準シリアライザだけで表せる fields に限定する。型に無関係な field は無視せず、Registry が warning/error の形で不整合を報告する。

### Public APIs

```csharp
public interface IApcMiniMk2Controller
{
    void ProcessNoteOn(int noteNumber, float velocity);
    void ProcessNoteOff(int noteNumber);
    void ProcessControlChange(int controlNumber, float value);
    void UpdateState(double beat);

    float FaderValue(int index);
    bool BooleanValue(string key);
    int RadioValue(string key);
    int StateValue(string key);
    bool SequenceActive(string key);
}

public sealed class BpmManager : MonoBehaviour
{
    public double Bpm { get; }
    public double Time { get; }
    public double DeltaTime { get; }
    public double Beat { get; }
    public long BeatFloor { get; }
    public int BeatModulo(int count);
    public bool IsBeatModulo(int count, int target);
    public void SetBpm(double bpm);
    public void TapTempo();
    public void SyncBeat();
    public void SyncBar(int barCount = 4, int beatsPerBar = 4);
}
```

`ApcMiniMk2MidiInput` は `MidiSuccess` と `IApcMiniMk2Controller` と同じ5つの読み取りメソッドを public に公開する。キー不明の bool/sequence は `false`、radio/state は `0`、無効な fader index は `0f` を返し、実行中入力で例外を出さない。今回は APC 専用の論理 binding API を公開するため、参考コードのような任意の raw note/CC input facade、MIDI learn、UI emulation API は追加しない。

### Invariants and lifecycle

- `key` は binding を識別する一意値。同一 binding の複数 target だけが同じ key を共有できる。
- 同一 `page,row,col` を複数 binding に登録しない。登録失敗をログに原因付きで出し、後勝ちしない。
- `Radio.defaultInt`、`State.defaultInt`、`State.cycleLength`、`Sequence.defaultSteps`、`Random.radioKey` は添付仕様の制約を Registry で検証する。
- `Random.radioKey` は既存 `Radio` だけを参照し、1 Radio への Random 割当は1件だけ。
- `OnEnable`: BPM 参照と mapping を検証して Controller を作り、`InputSystem.onDeviceChange` を購読、既存デバイスを走査する。
- `OnDisable`/`OnDestroy`: 対象 `MidiDevice` の3 callback と `InputSystem.onDeviceChange` を冪等に解除する。
- 再接続は購読だけを再構築し、Controller の論理状態は保持する。component の disable → enable は mapping 初期状態へ戻す。
- callback は `PendingMidiEvent { Type, Number, Value }` を `_pendingEvents` へ追加するだけとする。`Update` の先頭で frame flags を reset し、`_pendingEvents` と `_dispatchingEvents` を入れ替えて drain する。drain 中に来たイベントは `_pendingEvents` に残し、同一 callback を再入処理しない。

## Implementation Steps

### 1. 既存依存の実機前提を確認する

対象: `Packages/manifest.json`、`ProjectSettings/ProjectSettings.asset`、Minis package、APC 実機。

- package/version と Input System 有効状態を再確認する。追加の package は入れない。
- Play Mode で APC を 1 回操作し、Minis が作成する `MidiDevice.description.product` と `channel`、各 note/CC を一時ログで確認する。
- product 名が既定部分文字列と異なる場合だけ、Trip26 の `_productName` 初期値を実機値の安定部分へ更新する。

完了条件: Channel 0 の MidiDevice と実機 product 表記を確認済みで、入力番号に添付資料との差分がない。

### 2. BPM の純粋ロジックと Unity facade を実装する

対象: `Assets/qoooo/Scripts/Timing/BpmClock.cs`、`Assets/qoooo/Scripts/Timing/BpmManager.cs`、`Assets/qoooo/Tests/EditMode/BpmClockTests.cs`。

- TypeScript のフィールド、定数、`setBpm`、`tapTempo`、`syncBeat`、`syncBar`、`beatModulo`、`isBeatModulo` を `BpmClock` に移植する。
- `Update(double unscaledDeltaTime)` は delta/time/beat を更新し、整数 beat が変わったときだけ保留 BPM を反映する。無効 delta は `0` として安全に扱う。
- `BpmManager.Update` から `Time.unscaledDeltaTime` を1回渡し、読取・操作 API を委譲する。
- Tap Tempo は timeout 超過サンプルを除外し、最大8個、4個以上で平均 interval を使う。

完了条件: 指定 BPM の進行、上限下限 clamp、拍境界までの保留、Tap Tempo、bar 同期、負 beat 剰余が自動テストで通る。

### 3. APC 定数・座標・シリアライズ設定型を実装する

対象: `Assets/qoooo/Scripts/Midi/ApcMiniMk2Constants.cs`、`ApcMiniMk2Layout.cs`、`ApcMiniMk2Binding.cs`、`ApcMiniMk2LayoutTests.cs`。

- PDFで確認した `Port = 0`、`MidiChannel = 0`、`GridNote = 0..63`、`TrackButtonNote = 100..107`、`SceneLaunchNote = 112..119`、`ShiftNote = 122`、`FaderCc = 48..55`、`MasterFaderCc = 56` を named constant として定義する。`IsGridNote`、`IsTrackButtonNote`、`IsSceneLaunchNote`、`IsFaderCc`、`TryGetFaderIndex` をここに置く。
- note `0..63` を `row = 7 - note / 8`、`col = note % 8` へ変換し、逆変換と `cellKey = page * 64 + row * 8 + col` を実装する。物理座標 helper は PDF図の左下を `(col=0, physicalRow=0)` とし、アプリ座標への上下反転は `ApcMiniMk2Layout` だけが担う。
- binding、target、registered cell、binding/fader enum を定義し、Unity inspector のシリアライズに対応させる。
- note `0, 7, 56, 63`、page 境界、無効入力をテストで固定する。

完了条件: 四隅と全座標の往復変換、page 付き cellKey が一致する。

### 4. Registry と設定検証を実装する

対象: `ApcMiniMk2BindingRegistry.cs`、`ApcMiniMk2BindingRegistryTests.cs`。

- mapping から `cellKey -> RegisteredCell`、型別の初期状態、sequence steps の deep copy を構築する。
- 空 key/target、範囲外セル、重複セル、Radio/State/Sequence/Random の不正定義を収集して報告する。
- 初期化を失敗させる重大不整合と、設定値を互換フォールバックする `State.cycleLength/defaultInt` を明確に分ける。重大不整合があれば adapter はその mapping を有効化せず、エラーを出して初期状態のまま稼働する。

完了条件: 正常設定の初期値と、各不正設定がテストで検出できる。

### 5. Controller の入力・beat 状態遷移を実装する

対象: `IApcMiniMk2Controller.cs`、`ApcMiniMk2Controller.cs`、`PcgHash.cs`、`ApcMiniMk2ControllerTests.cs`。

- Note On の page 選択（Scene Launch 1–8 / note `112..119`）、グリッド lookup、fader button mode 切替（Track Button 1–8 / note `100..107` と Shift / note `122`）を順番どおり実装する。velocity `0` は Note Off として扱う。
- `Toggle`、`Radio`、`Oneshot`、`Momentary`、`State`、`Sequence`、`Random` の遷移を実装する。Note Off は Oneshot だけを false にする。
- CC `48..56` の正規化済み value を物理フェーダー値として保存し、Mute/Random/Normal の `FaderValue` 契約を実装する。
- `UpdateState(beat)` は整数 beat 変化時だけ sequence/random radio/random fader を更新する。Random は最初の target の cellKey を seed とする PCG を用い、beat 飛越し・逆行も最新 beat だけ処理する。
- Momentary は「前回予約を reset → 今回発火を次回 reset へ予約」の2段階 queue とする。利用側が同じフレームで1回読めることをテストで保証する。

完了条件: 7 binding、全9フェーダー、ページ、範囲外入力、Random 決定性、Momentary の観測回数を EditMode test で検証できる。

### 6. Minis adapter、入力 queue、接続 lifecycle を実装する

対象: `ApcMiniMk2MidiInput.cs`。

- `InputSystem.devices` を走査して `MidiDevice`、channel、product 部分一致で候補を選び、先頭1台の `onWillNoteOn`、`onWillNoteOff`、`onWillControlChange` を購読する。
- callback では `MidiNoteControl.noteNumber` / `MidiValueControl.controlNumber` と event value から `PendingMidiEvent` を enqueue する。control の現在値を読み直さず、Controller や利用側 event を callback 内で実行しない。
- `Update` の先頭で queue を二重 buffer で drain し、FIFO順に `ProcessNoteOn` / `ProcessNoteOff` / `ProcessControlChange` を呼ぶ。その後に1回だけ `Controller.UpdateState(BpmManager.Beat)` を呼ぶ。これが1フレームの入力確定点であり、Momentary を読む利用側はこの Update の後に読む。
- `InputSystem.onDeviceChange` で対象の追加・再接続・切断を受け、全解除後に再走査する。購読中の device が Removed/Disconnected になれば `MidiSuccess` を false にする。
- BPM 参照が未設定でも `0` beat を渡して例外にしないが、`OnValidate` と起動ログで設定不備を通知する。callback・queue・接続の例外は context を含む `Debug.LogWarning` に留め、既存 VJ を停止させない。

完了条件: callback の二重登録なし、同フレームに届く複数イベントの順序保持、切断後 false、再接続後の1操作で true・単発遷移、未接続でも稼働を満たす。

### 7. Trip26 Scene に組み込み、初期データを設定する

対象: `Assets/qoooo/Scenes/Trip26.unity`。

- `Managers` の子に `MIDI` GameObject を追加し、`BpmManager` と `ApcMiniMk2MidiInput` を付与する。
- `ApcMiniMk2MidiInput._bpmManager` を同一 GameObject の `BpmManager` に参照設定する。
- 実装時点で合意済みの binding 定義だけを inspector list に登録する。具体的な VJ 操作への割当は Non-Goals なので、未決の key を推測して登録しない。
- BPM 初期値は 120、product 名は `APC mini mk2 Control`、channel は 0、フェーダーボタンは既定配列に設定する。

完了条件: シーンを開くだけで component 参照が解決し、MIDI 未接続の Play Mode で警告以外の例外が出ない。

### 8. 検証・導入資料を完了する

対象: 新規テスト、`Docs/ApcMiniMk2MidiInputImplementationPlan.md`（本書の Verification を実施結果で補記する場合のみ）。

- Unity compile と全 EditMode test を実行する。
- APC 実機で四隅、全ページ、9フェーダー、9ボタン、抜線/再接続を確認する。
- `BpmManager` の API 呼出しによる Tap Tempo/BPM 変更/sync を一時的な debug UI または development log で確認し、検証用コードは残さない。

完了条件: Verification の必須項目がすべて合格し、既存シーンの Console に新規 Error がない。

## Edge Cases

| 条件 | 期待動作 |
| --- | --- |
| 起動時に APC 未接続 | 初期値のまま稼働。`MidiSuccess == false`。 |
| Minis がまだデバイスを生成していない | 接続済みでも `MidiSuccess == false`。初回操作後の device change で再走査する。 |
| 対象外 product/channel、複数候補 | 対象外を無視し、候補の先頭1台だけ購読する。 |
| Note On velocity 0 | Note Off と同じ遷移。 |
| page 変更後の Note Off | 添付仕様どおり、解放時の現在 page の Oneshot に作用する。 |
| 外部入力範囲外・未知 key | 例外にせず、無視または規定フォールバック。 |
| 無効 mapping | 原因をログに出し、入力を有効化しない。既存VJを停止させない。 |
| beat が同じ整数区間 | random 値を再生成しない。 |
| beat 飛越し・逆行 | 中間 beat を補間せず、渡された最新の `floor(beat)` で更新する。 |
| empty sequence | `SequenceActive` は false。 |
| Tap 間隔 0 以下 | BPM を更新しない。 |
| BPM/Sync 引数が範囲外 | BPM は 1–1000 へ clamp。barCount/beatsPerBar/count は 1 へ正規化。 |
| disable/destroy の重複 | event 解除は冪等。 |

## Verification

### Automated

実装後、Unity CLI Loop で次を実行する。

1. Console をクリアする。
2. Unity compile を実行し、新規 Error/Warning を確認する。
3. `Assets/qoooo/Tests/EditMode` の EditMode tests を実行する。

必須テストは以下。

- APC note/座標/cellKey の相互変換。
- PDF準拠の Track / Scene / Shift / Fader / Master Fader 定数と範囲判定。
- Registry の全検証規則、初期値、sequence deep copy。
- 全7 binding の Note On/Off 遷移。
- page、fader、Mute/Random、未知値のフォールバック。
- FIFO queue 内で同一フレームに届く Note On / CC / Note Off が受信順に Controller へ届き、drain 中の新規イベントが次フレームへ残ること。
- PCG の固定ベクトル、random radio/fader の決定性。
- Momentary の1更新だけの観測契約。
- BPM の clamp、経過、拍境界保留、Tap Tempo、beat/bar sync、正の剰余。

### Play Mode / hardware

1. APC 未接続で `Trip26` を起動し、描画・UI・Console を確認する。
2. APC を接続して1操作し、product/channel を確認して `MidiSuccess` が true になることを確認する。
3. グリッド四隅、Scene Launch 1–8 / note 112–119、Track Button 1–8 / note 100–107、Shift / note 122、Fader 1–8 / CC 48–55、Master Fader / CC 56 を操作する。
4. USB を抜き `MidiSuccess == false` を確認する。
5. 再接続・1回操作後に入力が復帰し、1操作が二重処理されないことを確認する。
6. BPM 120 で beat が進み、Tap Tempo と `SetBpm` が次の整数 beat でのみ変わることを確認する。

実施済みの検証は現時点で「依存パッケージと API の静的確認」のみ。実機検証、compile、テストは実装後に行う。

## Risks

| リスク | 緩和策 / rollback |
| --- | --- |
| OS により MIDI port/product 名が違う | 最初の実機操作で product をログ確認し、部分一致 field を調整する。 |
| Minis は操作前に device を公開しない | 未接続を非致命とし、初回操作を促す状態を公開する。 |
| 再接続で callback が重複する | 全解除→再走査→1台購読を徹底し、実機で単発遷移を確認する。 |
| callback と利用側 Update の順番で状態が揺れる | callback は queue への追加だけに限定し、`ApcMiniMk2MidiInput.Update` を唯一の入力確定点にする。 |
| TypeScript/C# の uint/float が異なる | PCG を `uint` と float32 bit 操作で移植し、固定値テストを追加する。 |
| シーン直列 mapping の編集競合 | 単一 `Trip26` シーンに限定し、mapping の変更は scene diff としてレビューする。 |
| BPM と MIDI の Update 順が変わる | `DefaultExecutionOrder` attribute で順序をコード化する。 |
| MIDI基盤が既存VJ機能を壊す | 既存クラスへ直接依存しない。rollback は `Trip26` の `MIDI` GameObject と新規 Scripts/Tests を戻すだけで可能。 |

## Assumptions

- 対象機種は Akai APC mini mk2、標準 MIDI Channel 1（Minis index 0）である。
- `jp.keijiro.minis` 1.3.2 と現行 Input System を継続利用できる。
- `Trip26` が今回の唯一の組込み先である。
- BPM は今回追加する `BpmManager` が唯一の beat 所有者となる。外部同期は今回扱わない。
- MIDI入力と BPM に関する状態永続化は不要である。
- mapping の具体的な VJ 機能割当は本スコープ外であり、Scene には空リストでも有効な基盤を配置する。

## Open Questions

なし。具体的な VJ 操作への mapping は明示的に Non-Goals とし、後続タスクで各 `key` と対象機能を合意してから追加する。
