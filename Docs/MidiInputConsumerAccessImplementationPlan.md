# 型付きParameterを中心としたAPC mini mk2入出力・Binding実装計画

## Objective

各VJ機能が所有する型付きの論理Parameterを、RosettaUI、APC mini mk2、将来のKeyboard入力から共通して操作できる基盤を実装する。

現在の `ApcMiniMk2MidiInput` が文字列key別に論理状態を所有する構造は廃止する。MIDIは状態の所有者ではなく、Scene内のMIDI対応Parameterを物理controlへ接続するadapterとする。

成功条件:

- 利用classはMIDI APIや文字列keyではなく、自身が所有する型付きParameterから値を取得する。
- RosettaUIからParameter値とAPC Bindingを編集できる。
- Mapping定義と実行状態をPrefsへ分離保存できる。
- Binding単位・slot単位のListenでPad/Faderを割り当てられる。
- APCのLEDが論理Parameterの状態を自動表示する。
- MIDI入出力、APC固有処理、Binding、Parameter、永続化の責務が分離される。
- MIDI機器が未接続でもRosettaUIとアプリ本体が通常動作する。

## Current State

調査対象は `master` の `cfc453f3ae22613429f5157dde0006119aea212a` に、未コミットのAPC MIDI/BPM実装を加えた作業ツリーである。

- `Assets/qoooo/Scripts/Midi/ApcMiniMk2MidiInput.cs` がMinis接続、event queue、APC filtering、Binding Controller生成を所有する。
- `Assets/qoooo/Scripts/Midi/ApcMiniMk2Binding.cs` はToggle、Radio、Oneshot、Momentary、State、Sequence、Radio用Randomの設定を一classへ集約する。
- 標準Inspectorには選択Typeに不要な `Default Bool`、`Default Int`、`Cycle Length`、`Default Steps`、`Radio Key` まで同時表示される。
- `ApcMiniMk2Controller` は文字列keyのDictionaryに論理状態を保持し、`BooleanValue(string)` などで公開する。
- `Trip26.unity` の `_bindings` と `_faderButtonFunctions` は現在空で、旧形式から移行すべきMappingはない。
- `BpmManager` がbeatを生成し、SequenceとFader Random modeに利用される。
- `UiBuilder` はUnitySimpleContainerから `IUiTarget` を収集し、RosettaUI windowと既存 `Prefs.Save` buttonを構築する。
- PrefsGUI 3.6.1とPrefsGUI RosettaUI 1.4.0が導入済みである。
- Minis 1.3.2は入力専用だが、依存するRtMidi 2.2.0の `MidiOut` を直接利用できる。
- 添付実装のraw I/O分離、APC定数、座標変換、Binding単位Listen、worker出力、LED dirty管理は採用候補である。Radio、State、Sequenceを欠く論理層、UI emulation、永続ファイル診断はそのまま採用しない。

## Problem

現状は「物理MIDI入力から作る論理状態」と「アプリが使用するParameter」の依存方向が逆になっている。

- アプリ側が `BooleanValue("key")` のようにMIDI固有storeへ問い合わせる。
- Bindingごとに文字列keyと論理仕様を入力するため、typo、重複、型不一致が起きる。
- 一つの汎用Binding classが全Typeのfieldを持つため編集画面が読みにくい。
- RosettaUI、MIDI、将来のKeyboardで同じ値を操作する共通点がない。
- 現classへMIDI outを足すと接続、Binding、LED、Prefs、Listenが集中する。
- Scene保存だけではビルド後の変更を保持できず、Prefsだけでは標準Mappingがない。

## Requirements

### Parameter

- MIDI対応Parameterは各利用classがserialized fieldとして所有する。
- Parameterはstable ID、表示名、既定値、現在値、型固有metadataを持つ。
- stable IDはParameter宣言時に意味のある文字列として一度だけ指定し、Binding編集時には入力させない。
- Parameterは現在値のpollと `Changed` eventを提供し、同値再設定ではeventを発火しない。
- MIDI、RosettaUI、コードの変更は同じvalidationと通知経路を通り、最後の変更を採用する。
- MIDI対応Parameterだけが `IMidiBindableParameter` を実装する。
- owner classは `IMidiParameterProvider` として所有Parameterを明示的に列挙する。
- `FloatParameter` はアプリケーション値のmin/maxを所有し、APC Faderの0..1と相互変換する。
- Sequenceのbeat更新とMomentaryのframe resetはMIDI接続に依存しない `MidiParameterRuntime` が行う。

### Binding

- Type dropdownを廃止し、対象ParameterのinterfaceからBinding Typeを決定する。
- Bindingを型別class/listへ分割し、無関係なfieldを持たせない。
- 一つのParameterにつきAPC Bindingは一つだけとする。
- 一つの物理controlは一つのBinding slotだけに所属できる。
- 使用済みcontrolをListenした場合、旧slotから新slotへ移動する。
- RadioとSequenceはoption/stepごとに個別slotを持つ。
- Radioを自動選択するRandom Bindingは削除する。Fader Random modeは維持する。

### Listen

- 同時にListenできるslotは一つ。
- Button slotは次のNote Onで確定する。
- Fader slotはListen開始後のCC移動量が0.05以上になった時点で確定する。
- 確定Note OnはParameter操作へ伝播しない。
- 確定CCはPickup基準だけに使い、Parameter値を変更しない。
- 別slotのListen開始、Cancel、component disable、Scene終了、Mapping resetで現在sessionを解除する。
- 自動timeoutは設けない。

### Prefs

- Scene serialized fieldを初回・reset時の標準Mappingと接続設定にする。
- 起動時にScene標準値のruntime copyを作り、互換Prefsがあれば上書きする。
- Mapping、Runtime State、Connection Settingsを別々のversion付きrecordとして保存する。
- RosettaUI編集は即時反映し、既存 `Prefs.Save` で永続化する。
- `Revert`、`Reset Mapping`、`Reset Values` を提供する。reset後にSaveした場合だけ永続化する。
- 欠損Parameter IDのBindingは未解決状態で保持し、RosettaUIから再割当または削除できる。

### MIDI out

- RtMidiのOpen、Send、Disposeをworker threadで実行し、main threadではqueueへ積むだけにする。
- Parameter/page変更時に該当LEDをdirty化する。
- 一定間隔・一定件数でdirty LEDを送信する。
- 再接続時とMapping変更時は現在状態から全LEDを再描画する。
- disable/終了時は全消灯をbest effortで送り、長時間joinしない。
- 診断はmain threadの `Debug.Log`、`Debug.LogWarning`、`Debug.LogError` を使う。

### Acceptance criteria

- MIDI未接続でもRosettaUIからParameterを変更でき、利用classへ即時反映される。
- MIDI対応ParameterだけがBinding Editorへ表示される。
- RadioではRadio slot、Sequenceではstep slotだけが表示され、無関係なdefault設定は出ない。
- ListenしたPad/Faderが即時に入力とLEDへ反映される。
- Save後の再起動でMappingと保存対象の論理値が復元される。
- Revertで最後の保存状態、ResetでScene標準状態へ戻る。
- Parameter削除後も未解決Bindingを失わず修復できる。
- UI操作後に物理Faderを動かしてもPickup成立前に値がjumpしない。
- MIDI出力port切断でmain threadが停止せず、再接続後にLEDが復元される。

## Non-Goals

- Keyboard Binding実装。拡張点だけ残す。
- 仮想APC Surface、8x8 Pad emulator、MIDI monitor UI。
- Radio選択肢をbeatごとにランダム変更する旧Random Binding。
- BindingごとのLED色設定。Type共通色を使う。
- MIDI clock送受信、外部clock同期。
- 複数の名前付きProfile、Scene別Profileの管理UI。当面は `trip26` 一つ。
- MIDI LearnによるParameter自動生成。
- MIDI非対応fieldを含む全projectの一括Parameter化。
- 添付実装のUI emulationと永続ファイル診断ログ。

## Proposed Architecture

```text
                         ┌─ RosettaUI
                         ├─ application code
                         └─ future Keyboard adapter
                                   ↕
Owner MonoBehaviour ── typed Parameter ── ParameterRegistry (index only)
                                   ↕                    ↕
                              typed Binding ─── Binding Editor / Prefs
                                   ↕
MinisMidiInput ──→ ApcMiniMk2Surface ──→ ApcMiniMk2LedRenderer
                                           ↓
                                      RtMidiOutput worker
```

### Ownership and dependencies

- 利用classがParameterを所有して直接読む。
- Parameter RegistryはUnitySimpleContainerからProviderを受け取り、ID検索と一覧だけを担う。
- `MidiParameterRuntime` はRegistry内のMomentary/Sequence Parameterを毎frame更新し、MIDIがなくても論理時間を進める。
- `MidiSystemController` がcomposition rootとなり、各serviceを決めた順番で初期化・逆順破棄する。
- APC Surfaceはraw input番号をGrid(row/column)、Fader(index)、Track(index)、Scene(index)、Shiftの物理eventへ変換するだけで、pageや論理状態を所有しない。
- Binding ControllerはSurface eventを受け、current page、Fader mode、Pickupを所有して対象Parameterを変更する。
- LED RendererはParameter、Binding、page、Fader modeをAPC表示へ投影する。
- アプリclassはMIDI I/O、Surface、Binding Registryへ依存しない。

```csharp
public sealed class CameraController : MonoBehaviour, IMidiParameterProvider
{
    [SerializeField]
    private RadioParameter _cameraMode = new(
        "camera.mode", "Camera Mode",
        new[] { "Front", "Side", "Top", "Auto" }, 0);

    public IEnumerable<IMidiBindableParameter> MidiParameters
    {
        get { yield return _cameraMode; }
    }

    private void Update() => ApplyCameraMode(_cameraMode.Value);
}
```

static `MidiInput.Current` はアプリ側の値取得口として導入しない。ownerがParameterを直接読むことで、偽singleton、文字列lookup、MIDIへの逆依存を不要にする。Provider collectionはUnitySimpleContainer、MIDI subsystem内のconcrete componentは `MidiSystemController` のserialized referenceで解決する。

### Initialization and frame order

```text
Composition:
  SceneContainer.Awake       -9999 (bind/inject)
  MidiSystemController.Awake -9000 (explicit initialize)
  ownerのScene defaults
  -> ParameterRegistry validation
  -> runtime Mapping copy
  -> Prefs Mapping / State / Settings load
  -> Binding resolve
  -> raw MIDI I/O start

Frame:
  BpmManager                 -120
  Parameter frame begin      -110
  MinisMidiInput             -100
    -> Surface/Binding event  -100 (same call stack)
  APC Controller beat update  -90
  normal consumers              0
  LED flush                 LateUpdate/worker queue
```

`MidiParameterRuntime(-110)` はMomentaryの前frame flagをresetしてSequenceへ現在beatを渡す。その後 `MinisMidiInput(-100)` がeventを発火し、同じcall stackでSurface/Controllerが当該frameの入力を適用するため、通常consumerはMomentaryを一度読める。`ApcMiniMk2Runtime(-90)` はControllerへbeatを渡してFader Randomだけを更新する。

UnitySimpleContainerの `[Inject]` methodでは参照をfieldへ保存するだけにし、他serviceの状態を読んだりevent購読したりしない。SceneContainerのComponent注入順に依存しないよう、`[DefaultExecutionOrder(-9000)]` の `MidiSystemController.Awake` が次を明示実行する。

1. `MidiParameterRegistry.Initialize(providers)`
2. `PrefsMidiProfilePersistence` を3つの固定Prefs keyで生成し、`MidiProfileStore.Initialize(registry, persistence)` を呼ぶ。Scene default copyとPrefs loadを行う。
3. `MidiParameterRuntime.Initialize(registry, bpmManager, ApplyPendingChanges)`
4. `ApcMiniMk2BindingRegistry.Rebuild(workingMapping, registry)`
5. `ApcMiniMk2ListenController.Initialize(...)`
6. `ApcMiniMk2Surface.Initialize(midiInput)`
7. `ApcMiniMk2Controller.Initialize(surface, bindingRegistry, listen, bpmManager)`
8. `ApcMiniMk2Runtime.Initialize(controller, bpmManager, ApplyPendingChanges)`
9. `ApcMiniMk2LedRenderer.Initialize(midiOutput, controller, bindingRegistry)`。Parameter `Changed` を購読する。
10. `MinisMidiInput.StartInput(settings)`
11. `RtMidiOutput.StartOutput(settings)`

shutdownは単純な逆順にせず、次の順に固定する。

1. `MinisMidiInput.StopInput()` で新規入力を止める。
2. Listen cancel、ControllerのOneshot releaseとmode/pressed state clear。
3. `ClearLedsOnDisable` がtrueかつoutput接続中ならRendererが64 Pad Off、8 Track Off、8 Scene Offをqueueする。
4. `RtMidiOutput.StopOutput(drainPending: ClearLedsOnDisable)`。drainは最大100msで打ち切る。
5. Renderer → APC Runtime → Controller → Surface → Listen → Parameter Runtime → Profile Store → Parameter Registryの順にDisposeする。

全serviceの `Initialize`、`Dispose`、`StartInput/Output`、`StopInput/Output` は二重呼出し安全にする。Binding rebuild時はController/Rendererの旧event購読を解除してから新Registryを差し替える。

実際の停止入口は `MidiSystemController.OnDisable` とし、`OnDestroy` は停止済みでも安全に同じ `Shutdown()` を呼ぶ。再enable時は `OnEnable` から `EnsureInitializedAndStarted()` を呼ぶ。初回はAwakeと直後のOnEnableの両方から呼ばれても `_started` guardで一度だけ実行する。

Surface eventを購読するのは `ApcMiniMk2Controller` 一つだけとする。Controllerは各Note/CCを最初に `ApcMiniMk2ListenController.TryConsume(...)` へ渡し、trueなら通常Binding処理を行わない。event購読順へListen抑制を依存させない。

OneshotはGrid Note On時に解決したParameterをphysical note別のpressed tableへ保持し、Note Offではcurrent pageや現Mappingを再検索せずtableのParameterをfalseへ戻す。page変更ではtableを維持し、Mapping rebuild、input disconnect、Controller disposeではtable内を全てfalseへ戻してclearする。

通常の利用classは実行順0以降の `Awake` でPrefs適用済みParameterを読める。Parameter getterはEditModeやDI失敗時にも安全なserialized defaultを返す。Profile適用後、値が変わったParameterだけ `Changed` を一回発火する。

各serviceの公開lifecycle APIを次に固定する。

| Service | API |
| --- | --- |
| `MidiParameterRegistry` | `Initialize(IEnumerable<IMidiParameterProvider>)`, `TryGet(string, out IMidiBindableParameter)`, `Descriptors`, `Dispose()` |
| `MidiProfileStore` | serialized `_defaultMapping` / `_defaultSettings`, `Initialize(registry, persistence)`, `WorkingMapping`, `WorkingSettings`, `PrepareSave()`, `CommitSave()`, `AbortSave()`, `Revert()`, `ResetMapping()`, `ResetValues()`, `Dispose()` |
| `MidiParameterRuntime` | `Initialize(registry, bpmManager, applyPendingChanges)`, `Update(-110)` 冒頭でpending適用後にframe reset/Sequence更新, `Dispose()` |
| `MinisMidiInput` | `StartInput(settings)`, `ApplySettings(settings)`, `StopInput()` |
| `RtMidiOutput` | `StartOutput(settings)`, `ApplySettings(settings)`, `StopOutput(bool drainPending)` |
| `ApcMiniMk2Surface` | `Initialize(input)`, semantic control events, `Dispose()` |
| `ApcMiniMk2ListenController` | `Initialize(profileStore)`, `Begin(parameterId, slotIndex, kind)`, `TryConsume(controlEvent)`, `Cancel(reason)`, `Dispose()` |
| `ApcMiniMk2Controller` | `Initialize(surface, bindingRegistry, listen, bpm)`, `Rebind(registry)`, LED/UI用状態query, `Dispose()` |
| `ApcMiniMk2Runtime` | `Initialize(controller, bpmManager, applyPendingChanges)`, `Update(-90)` 冒頭でpending適用後にbeat更新, `Dispose()` |
| `ApcMiniMk2LedRenderer` | `Initialize(output, controller, registry)`, `InvalidateAll()`, `FlushIfDue()`, `Dispose()` |

上表以外からservice内部list、event delegate、RtMidi handleを直接変更しない。

Binding UIとListen Controllerは `WorkingMapping` のlistを直接変更せず、Profile Storeの `AddBinding(parameterId)`、`DeleteBinding(parameterId)`、`ReassignBinding(oldParameterId, newParameterId)`、`AssignButtonSlot(parameterId, slotIndex, slot)`、`AssignFader(parameterId, faderIndex)`、`ClearSlot(parameterId, slotIndex)`、`SetFaderFunction(...)`、`SetFaderRandomDivision(...)` を使う。各methodはkind一致、一意性、旧controlからの移動、revision increment、`MappingChanged` 発火を一transactionで行う。失敗時はfalseと理由を返しworking copyを変更しない。

## Design Decisions

| 決定 | 理由 | 採用しない案 |
| --- | --- | --- |
| owner所有の型付きParameter | UI/MIDI/Keyboardの共通状態となり、MIDIなしでも成立する。 | MIDIの文字列Dictionaryを状態ownerにする。 |
| stable semantic ID | renameに耐え、人間が読め、Prefs復元に使える。 | GUID、GameObject path自動生成。 |
| 明示的Provider登録 | ownership/lifecycleが明確。 | static自己登録、reflection。 |
| Parameter型からBinding Typeを決定 | 不正組合せとType dropdownをなくす。 | Typeを別選択。 |
| Binding型別list | 不要fieldを構造上なくす。 | union class、`SerializeReference` 多態list。 |
| Scene default + Prefs override | Unityの初期値と実行時編集を両立する。 | Sceneのみ、Prefsのみ、コード直書き。 |
| 用途別version付きrecord | migration/reset境界と一括整合性を保つ。 | itemごとのPrefs key。 |
| last change wins | 複数入力を対等に扱う。 | 固定優先順位。 |
| Fader Pickup | UI/復元値から物理位置へのjumpを防ぐ。 | 即時追従、relative。 |
| Oneshot/Momentary名称維持 | 単独利用の既存理解を優先する。 | Gate/Triggerへrename。 |
| raw I/OとAPC Surface分離 | MIDI out、Listen、LED追加後も責務を保つ。 | 一classへ集約。 |
| worker MIDI out | native callからmain threadを守る。 | 同期送信。 |
| 自動LED投影 | 入力と表示の不一致を防ぐ。 | consumerがLEDを直接操作。 |
| 単一 `trip26` Profile | 現用途に十分。schemaは将来拡張可能にする。 | 初回からProfile管理UI。 |

## Files to Change

新規パスは実装時に作成する提案パスである。

### Parameter

| 種別 | パス | 責務 |
| --- | --- | --- |
| 新規 | `Assets/qoooo/Scripts/Parameters/IParameterValue.cs` | read/write値interfaceと変更通知。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/IMidiBindableParameter.cs` | ID、表示名、kind、reset/save contract。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/IMidiParameterProvider.cs` | ownerのParameter列挙。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/BoolParameter.cs` | Toggle値。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/RadioParameter.cs` | option名、index validation。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/StateParameter.cs` | state countと循環。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/OneshotParameter.cs` | Note OnからOffまでの状態。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/MomentaryParameter.cs` | 1 frame flag、event、発火回数。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/SequenceParameter.cs` | 固定steps、pattern、division、active step。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/FloatParameter.cs` | min/max付き連続値と正規化変換。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/MidiParameterRegistry.cs` | Provider収集、ID validation/index。 |
| 新規 | `Assets/qoooo/Scripts/Parameters/MidiParameterRuntime.cs` | Momentary frame resetとSequence beat更新。 |

### MIDI transport and APC

| 種別 | パス | 責務 |
| --- | --- | --- |
| 新規 | `Assets/qoooo/Scripts/Midi/IMidiInput.cs` | raw Note/CCのframe確定済みAPI。 |
| 新規 | `Assets/qoooo/Scripts/Midi/IMidiOutput.cs` | raw Note/CC/SysEx送信API。 |
| 新規 | `Assets/qoooo/Scripts/Midi/MinisMidiInput.cs` | Minis接続、filter/channel、queue drain。 |
| 新規 | `Assets/qoooo/Scripts/Midi/RtMidiOutput.cs` | worker、port filter、reconnect、bounded queue。 |
| 変更 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Constants.cs` | LED behaviour、palette、出力定数を追加。 |
| 変更 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Layout.cs` | control名と安全な座標/Note変換。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Surface.cs` | raw番号からAPC物理control eventへの変換とListen source。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2LedRenderer.cs` | 共通色、dirty、page/reconnect再描画。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Runtime.cs` | `Update(-90)` でControllerのFader Random beat更新。 |
| 新規 | `Assets/qoooo/Scripts/Midi/MidiSystemController.cs` | subsystemの明示初期化、更新開始、逆順破棄。 |
| 変更 | `Assets/qoooo/Scripts/Timing/BpmManager.cs` | 実行順を-120へ変更しParameter Runtimeより先にbeat更新。 |

### Binding, persistence and UI

| 種別 | パス | 責務 |
| --- | --- | --- |
| 変更 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Binding.cs` | union型を型別Binding DTO/slotへ置換。 |
| 変更 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2BindingRegistry.cs` | 型別list統合、一意性、Parameter解決、未解決保持。 |
| 変更 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2Controller.cs` | 文字列storeを削除しParameterを操作。Pickup/Fader mode管理。 |
| 削除 | `Assets/qoooo/Scripts/Midi/IApcMiniMk2Controller.cs` | 旧文字列getter/write API。新Controllerは内部concrete型として使用。 |
| 新規 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2ListenController.cs` | 単一Listen session、移動、cancel、入力抑制。 |
| 新規 | `Assets/qoooo/Scripts/Midi/MidiProfileData.cs` | version付きMapping/State/Settings DTO。 |
| 新規 | `Assets/qoooo/Scripts/Midi/IMidiProfilePersistence.cs` | Profile Storeが使用するload/stage/rollback境界。 |
| 新規 | `Assets/qoooo/Scripts/Midi/PrefsMidiProfilePersistence.cs` | 3つの `PrefsAny<T>` を包むproduction adapter。 |
| 新規 | `Assets/qoooo/Scripts/Midi/MidiProfileStore.cs` | Scene default copy、Prefs load/save/revert/reset。 |
| 新規 | `Assets/qoooo/Scripts/Midi/MidiBindingUi.cs` | `MidiSystemController` が所有するplain C# UI builder。型別RosettaUI、Listen、未解決修復。 |
| 新規 | `Assets/qoooo/Scripts/Midi/MidiSettingsUi.cs` | `MidiSystemController` が所有するplain C#接続設定UI builder。 |
| 新規 | `Assets/qoooo/Scripts/View/IPrefsSaveParticipant.cs` | `Prefs.Save` 前後のsnapshot準備・確定hook。 |
| 変更 | `Assets/qoooo/Scripts/View/UiBuilder.cs` | save participantを注入し、Prepare → Prefs.Save → Commitの順で実行。 |
| 置換後削除 | `Assets/qoooo/Scripts/Midi/ApcMiniMk2MidiInput.cs` | 移行中のcomposition facade。新構造統合後に削除。 |
| 変更 | `Assets/qoooo/Scenes/Trip26.unity` | 新components、標準Mapping、接続設定。 |

### Tests

| 種別 | パス | 責務 |
| --- | --- | --- |
| 新規 | `Assets/qoooo/Tests/EditMode/ParameterTests.cs` | 型別validation、Changed、reset、Momentary。 |
| 新規 | `Assets/qoooo/Tests/EditMode/MidiBindingRegistryTests.cs` | 一意性、型解決、未解決、移動。 |
| 新規 | `Assets/qoooo/Tests/EditMode/MidiProfileStoreTests.cs` | round-trip、version、revert/reset。 |
| 新規 | `Assets/qoooo/Tests/EditMode/ApcMiniMk2ControllerTests.cs` | Binding、Listen、Pickup、page、Fader mode。 |
| 新規 | `Assets/qoooo/Tests/EditMode/ApcMiniMk2LedRendererTests.cs` | Type表示、dirty、page、reconnect。 |
| 新規 | `Assets/qoooo/Scripts/qoooo.Runtime.asmdef` | runtime scriptsをtest assemblyから参照可能にする。 |
| 新規 | `Assets/qoooo/Scripts/AssemblyInfo.cs` | `InternalsVisibleTo("qoooo.EditModeTests")` でinternal coreをtest可能にする。 |
| 新規 | `Assets/qoooo/Tests/EditMode/qoooo.EditModeTests.asmdef` | NUnit test discoveryを有効化する。 |

既存VJ classは、具体的なMIDI割当を行うclassだけ同じ変更単位でParameter化する。

## Interfaces / Data Structures

```csharp
public interface IReadOnlyValue<out T>
{
    T Value { get; }
}

public interface IValue<T> : IReadOnlyValue<T>
{
    event Action<T> Changed;
    bool TrySetValue(T value);
}

public interface IMidiBindableParameter
{
    string Id { get; }
    string DisplayName { get; }
    MidiParameterKind Kind { get; }
    void InitializeFromDefault();
    void ResetToDefault();
}

public interface IMidiParameterProvider
{
    string MidiParameterGroupName { get; }
    IEnumerable<IMidiBindableParameter> MidiParameters { get; }
}
```

`MidiParameterKind` は `Toggle, Radio, Oneshot, Momentary, State, Sequence, Float` の7値に固定する。各具象Parameterは対応するkindを定数で返し、外部から変更できない。

型固有interface:

- `IBoolValue`: bool値。
- `IRadioValue`: int値、表示名付きOptions、範囲設定。
- `IStateValue`: int値、StateCount、循環。
- `IOneshotValue`: Note Onでtrue、Note Offでfalse。
- `IMomentaryValue`: `WasTriggeredThisFrame`、`Triggered` event、同frame発火回数。
- `ISequenceValue`: readonly steps、固定StepCount、Division、ActiveStep、step toggle/set。
- `IFloatValue`: Value、Min、Max、`NormalizedValue`。Fader入力は `NormalizedValue` を変更する。

IDは空白禁止、Registry内で一意、大文字小文字を区別する。重複IDは両ParameterをBinding対象外にしてowner付き `Debug.LogError` を一回出す。

`SequenceParameter.Changed` はpatternまたはDivision変更時だけ発火する。beat進行によるActiveStep変更は永続状態をdirtyにしないよう、別の `ActiveStepChanged(int)` eventで通知する。LED Rendererは両eventを購読する。

### Parameter serialization contract

全Parameter classは `[Serializable] sealed`、`ISerializationCallbackReceiver` 実装、public引数なしconstructor付きとし、Unityが保存するfieldとruntime fieldを分ける。

```csharp
[Serializable]
public sealed class FloatParameter : IFloatValue, IMidiBindableParameter
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private float _defaultValue;
    [SerializeField] private float _min;
    [SerializeField] private float _max = 1f;

    [NonSerialized] private float _value;
    [NonSerialized] private bool _initialized;

    public float Value => _value;
    public float NormalizedValue => Mathf.InverseLerp(_min, _max, _value);
}
```

- Registry構築時に全Parameterの `InitializeFromDefault()` を一度呼び、その後Prefs stateを適用する。
- constructorはコード生成時の便宜、serialized fieldはScene defaultのsource of truthとする。
- `OnAfterDeserialize` ではevent delegate、runtime flag、current valueを保持しない。
- `TrySetValue` は未初期化なら先にdefaultで初期化し、clamp/normalize後の値が変わった場合だけ `Changed` を発火する。
- Radioは `_options: List<string>`、Stateは `_stateCount >= 1`、Sequenceは `_defaultSteps: List<bool>` と `_division` をserialized fieldに持つ。
- SequenceのStepCountは `_defaultSteps.Count` とし、空配列はvalidation errorでBinding対象外にする。
- Oneshot/Momentaryはcurrent stateをserialized fieldへ書き戻さない。

型別normalize/validation規則:

- 全型: IDがnull/空白ならRegistry対象外。DisplayNameが空なら表示だけIDへfallbackする。
- Radio: options nullは空listへ直す。0件はRegistry対象外。空option名は `Option {index}` と表示する。default/current indexは0..Count-1へclampする。
- State: StateCountは最低1、default/currentは0..StateCount-1へclampする。
- Sequence: default steps null/0件はRegistry対象外。invalid Division enum値はQuarterへ戻す。
- Float: NaN/Infinityは0へ戻す。`Max <= Min` の場合は `Max = Min + 1`、default/currentはMin..Maxへclampする。
- Oneshot/Momentary: defaultは常にfalse、初期化/Resetでfalseと発火count 0へ戻す。

自動補正した項目はParameter ID付きwarningを初期化時に一回だけ出す。Registry対象外Parameterはアプリ側では安全なdefaultを返すがBinding dropdownには出さない。

### Registry and DI contract

`MidiParameterRegistry` はScene上の `MonoBehaviour` とする。`MidiSystemController` だけが `[Inject] Construct(IEnumerable<IMidiParameterProvider> providers)` でProvider collectionを受け取り、`Awake(-9000)` から `registry.Initialize(providers)` を呼ぶ。SceneContainerは `BindType.Interfaces` でProvider interfaceをcollection登録できるためmanual binder/static登録は不要である。

- Providerは `string MidiParameterGroupName` と `IEnumerable<IMidiBindableParameter> MidiParameters` を返す。
- Registryは注入された順序へ依存せず、UI表示時にgroup name、parameter display name、IDの順でsortする。
- null Provider/Parameterはskipしてerrorを一回記録する。
- Registryは `Revision` を持ち、再構築、Parameter追加/削除、解決状態変更時だけincrementする。
- runtime中にComponentを生成しただけではSceneContainerが自動再injectしないため、今回のscopeではProvider集合はScene起動時固定とする。

SceneContainerは現在 `BindType.Interfaces` のため、`MidiParameterRegistry` 等のconcrete componentをDI解決しない。`MidiSystemController` は以下を `[SerializeField]` で明示参照し、Trip26の同じ `Managers/MIDI` GameObject上で設定する。

- `BpmManager _bpmManager`
- `MidiParameterRegistry _parameterRegistry`
- `MidiParameterRuntime _parameterRuntime`
- `MidiProfileStore _profileStore`
- `MinisMidiInput _midiInput`
- `RtMidiOutput _midiOutput`
- `ApcMiniMk2Surface _surface`
- `ApcMiniMk2LedRenderer _ledRenderer`
- `ApcMiniMk2Runtime _apcRuntime`

`OnValidate` と起動時validationでnull参照を列挙し、一つでも欠ければMIDI subsystem全体を開始せず `Debug.LogError` を一回出す。Parameterと通常VJ機能はそのまま動作する。

### Save transaction contract

PrefsGUIの `PrefsAny<T>.Set` はKVS cacheを即時変更するため、RosettaUI編集中には呼ばない。`MidiProfileStore` はruntime working copyとlast-saved snapshotを別々に保持する。

`MidiProfileStore` は `IMidiProfilePersistence` だけに依存する。productionでは `PrefsMidiProfilePersistence` を `MidiSystemController` が生成して `Initialize` へ渡し、testではin-memory fakeを渡す。testから実ユーザーPrefs keyと `Prefs.Save/DeleteAll` を呼ばない。

```csharp
public interface IMidiProfilePersistence
{
    MidiMappingProfileData LoadMapping(MidiMappingProfileData fallback);
    MidiRuntimeStateData LoadState(MidiRuntimeStateData fallback);
    MidiConnectionSettingsData LoadSettings(MidiConnectionSettingsData fallback);
    void Stage(MidiMappingProfileData mapping, MidiRuntimeStateData state,
        MidiConnectionSettingsData settings);
    void RollbackStage();
    void CommitStage();
}
```

```csharp
public interface IPrefsSaveParticipant
{
    void PrepareSave();
    void CommitSave();
    void AbortSave();
}
```

`MidiSystemController` は `IUiTarget` と `IPrefsSaveParticipant` を実装し、UI生成を2つのplain UI builderへ、save methodをProfile Storeへ委譲する。これによりSceneContainerのinterface bindingだけで既存 `UiBuilder` に登録される。`UiBuilder` は `IEnumerable<IPrefsSaveParticipant>` を追加注入し、Save buttonで次を順番に実行する。

1. 全participantの `PrepareSave()` を呼ぶ。Profile StoreはruntimeからDTOをdeep copyし、persistenceの `Stage` を呼ぶ。production adapterだけが3つの `PrefsAny<T>.Set` を行う。
2. 全prepare成功時だけ `Prefs.Save()` を一回呼ぶ。
3. 成功後に `CommitSave()` を呼び、prepared DTOをlast-saved snapshotにする。
4. Prepare中の例外では `AbortSave()` → `RollbackStage()` を呼び、KVS cacheをprepare前へ戻して `Debug.LogError` を一回出す。`Prefs.Save()` 自体のI/O例外はlogし、package側が途中まで永続化した可能性を隠さない。

`Revert` はglobal `Prefs.Load()` を呼ばない。他機能の未保存Prefsまで巻き戻さないため、Profile Storeのlast-saved snapshotをruntimeへdeep copyして適用する。起動時のlast-saved snapshotは `PrefsAny<T>.Get()` のdeep copy、保存recordがない場合はScene defaultのdeep copyである。

`Reset Mapping` はScene default Mapping、`Reset Values` は各Parameter defaultをruntimeへ適用するだけで、Prefs recordは次回Saveまで変更しない。

DTOのdeep copyは `JsonUtility.ToJson` → `JsonUtility.FromJson<T>` の一経路に統一する。runtimeのlist参照、Scene default、Prefs cache、last-saved snapshotを共有しない。DTO classとそのnested dataはUnity `JsonUtility` が扱えるpublic `[Serializable]` class、serialized field、引数なしconstructorだけで構成し、Dictionary、interface field、readonly field、propertyだけの保存を禁止する。

Profile Storeは `WorkingRevision` と `SavedRevision` を持つ。Mapping、保存対象Parameter値、Connection Settingsの変更でWorkingRevisionを進め、`CommitSave` 後だけSavedRevisionを一致させる。Binding UIのSave statusは両者の一致だけで判定する。`AbortSave` はPrepare前の3 recordを `PrefsAny.Set` でKVS cacheへ戻し、last-saved snapshotとSavedRevisionを変更しない。

Profile Storeは `MappingChanged`、`ConnectionSettingsChanged`、`DirtyChanged` eventをmain threadで発火する。`MidiSystemController` はMapping/Settings変更eventではpending flagと最新snapshotだけを記録し、そのcall stack内でrebuild/reconnectしない。`ApcMiniMk2Runtime.Update(-90)` 冒頭から `MidiSystemController.ApplyPendingChanges()` を呼び、Listen cancel → Controller/Renderer unsubscribe → Registry rebuild → Controller/Renderer resubscribe → full LED redrawの順にMappingを適用する。その後Settings snapshotをinput/outputへ渡し、必要なtransportだけreconnectする。Storeは保存対象Parameterの `Changed` を購読してdirty化し、Dispose時に全購読を解除する。

前frameのRosettaUI変更を次のraw inputより先に反映するため、`MidiParameterRuntime.Update(-110)` 冒頭でも同じ `ApplyPendingChanges()` を呼ぶ。input dispatch中のListen確定はその後にpendingとなり、同frameの `ApcMiniMk2Runtime(-90)` で反映される。pendingがなければno-opとし、一つの変更を二回rebuildしない。

Profile適用中は `_isApplyingSnapshot` guardでParameter `Changed` 由来のdirty incrementを抑制する。起動loadとRevertは適用後にWorking/Saved revisionを同値へそろえ、Resetは適用後にWorking revisionだけを進める。Changed event自体は抑制せず、consumer、Fader mode解除、LED更新には通知する。

### Typed bindings

```csharp
[Serializable]
public struct ApcButtonSlot
{
    public bool Assigned;
    [Range(0, 7)] public int Page;
    [Range(0, 7)] public int Row;
    [Range(0, 7)] public int Column;
}
```

```csharp
[Serializable]
public sealed class ToggleMidiBinding
{
    public string ParameterId;
    public ApcButtonSlot Button;
}

[Serializable]
public sealed class RadioMidiBinding
{
    public string ParameterId;
    public List<ApcButtonSlot> OptionButtons;
}

[Serializable]
public sealed class SequenceMidiBinding
{
    public string ParameterId;
    public List<ApcButtonSlot> StepButtons;
}

[Serializable]
public sealed class FaderMidiBinding
{
    public string ParameterId;
    public bool Assigned;
    public int FaderIndex;
    public FaderButtonFunction ButtonFunction;
    public BeatDivision RandomDivision;
}
```

Oneshot、Momentary、Stateは単一 `ApcButtonSlot` の専用classとする。slotはPage/Row/Columnで保存し、NoteはSurfaceが導出する。

Binding DTOの全listとslot listはnullを許容せず、deserialize直後に空listへ正規化する。未割当slotは `Assigned=false` とし、`Page/Row/Column=-1` のsentinel値へ依存しない。

Faderも `Assigned=false` を未割当とし、assigned時だけ `FaderIndex` 0..8を検証する。未割当時のFaderIndex値は参照しない。

`RadioMidiBinding.OptionButtons.Count` はParameterのOptions数、`SequenceMidiBinding.StepButtons.Count` はStepCountと必ず一致させる。Prefs load時に不足slotは未割当で追加し、余剰slotは未解決データとして保持してUIに警告表示し、入力には使用しない。

Binding Registryは各recordへ `Resolved / UnresolvedParameter / KindMismatch / DuplicateParameter / DuplicateControl / InvalidSlotCount` のresolution statusを付ける。破損Prefsで同一Parameterまたは同一Grid cellが重複した場合は、競合する全recordをinactiveにし、list先頭を暗黙採用しない。競合しないrecordは通常動作を継続する。statusと理由はBinding Editorへ表示し、同じ内容のConsole warningはRegistry rebuildごとに一回だけ出す。

通常Button Bindingが使用できる物理controlは8x8 Grid Padだけである。Scene 1-8はpage選択、Track 1-8はFader 1-8 mode、ShiftはMaster Fader modeへ予約し、Binding slotやButton Listenの候補にしない。Fader ListenはCC 48-56だけを候補にし、それ以外のCCを無視する。

### Raw MIDI contracts

```csharp
public interface IMidiInput
{
    bool IsConnected { get; }
    event Action<bool> ConnectionChanged;
    event Action<int, float> NoteOn;
    event Action<int> NoteOff;
    event Action<int, float> ControlChanged;
}

public interface IMidiOutput
{
    bool IsConnected { get; }
    event Action<bool> ConnectionChanged;
    void SendNoteOn(int note, int velocity, int channel = 0);
    void SendNoteOff(int note, int channel = 0);
    void SendControlChange(int control, int value, int channel = 0);
    void SendRaw(ReadOnlySpan<byte> message);
}
```

- `MinisMidiInput.Update(-100)` はcallbackで積んだpending listをdispatch listとswapし、そのframe分をevent発火する。処理中の追加は次frameへ残す。
- Input System/Minis callbackがmain thread外から来る可能性に備え、pending queueへの追加とswapは短いlockで保護し、lock内でeventを発火しない。
- callback内では `Debug.Log` を呼ばない。`LogInputEvents` 有効時もmessage dataをqueueし、`Update` のdispatch時にmain threadからlogする。
- Note On velocity 0はNote Offとして正規化する。
- Note/CC/channel範囲外はdropし、種類ごとに最初の一回だけwarningを出す。
- device filterはproduct nameのcase-insensitive部分一致、channelは0..15。空filterは最初の一致deviceを選ぶ。
- 複数候補時はInputSystem.devicesの先頭を使い、選択deviceが消えたときだけ再bindする。同一deviceへの重複購読を禁止する。
- bind成功時に `ConnectionChanged(true)`、選択device解除前に `ConnectionChanged(false)` をmain threadで一回発火する。StopInputとsettings再接続でも同じ経路を使い、Oneshot/physical Fader状態をresetできるようにする。

`RtMidiOutput` は添付実装と同じく `ConcurrentQueue<byte[]>` と `AutoResetEvent` を使う。最大512 message、reconnect最小0.5秒、worker poll最大50ms、OnDisable join最大100msとする。queue超過時は最古を破棄するが、LED Rendererのdesired stateを失わないため次回full redraw要求をmain threadへ返す。

send失敗、port切断、settings再接続では未送信queueをclearする。再接続後は古いmessageを再利用せず、`ConnectionChanged(true)` を受けたRendererがdesired stateからfull redrawを新規生成する。

workerはUnity APIとC# eventを直接呼ばない。volatile/lock保護したconnection snapshotとerror stringだけを更新し、`RtMidiOutput.Update` が前frameとの差分を検出して `ConnectionChanged` と `Debug.Log` をmain threadで発火する。port filter/reconnect設定変更はmain threadからreconnect request flagを立て、workerが現在handleを閉じて新設定で開き直す。

Settings差分の適用規則は、Input Product Filter/Input Channel変更でinput再bind、Output Port Filter変更でoutput再接続、Reconnect Interval変更は次回試行間隔だけ更新、Log Input Events/Clear LEDs On Disable変更は再接続なしとする。

Stop時の100ms joinでworkerが終了しなかった場合はbackground thread参照を保持し、同Componentの再enableで二本目を起動しない。`StartOutput` は既存workerが `IsAlive` の間falseを返してwarningを一回出す。workerが後で終了したことをmain thread Updateで確認後にだけ再start可能とする。

APC Pad LEDはpalette colorをvelocity、behaviourをMIDI channelとしてNote On送信する。今回の共通色はpaletteで表現できるためRGB SysExは実装しない。Track/Scene LEDはchannel 0、velocity `0=Off / 1=On / 2=Blink` を使う。

```csharp
public enum BeatDivision { Bar, Quarter, Eighth, Sixteenth }
```

4/4前提で `Bar=4 beats`、`Quarter=1`、`Eighth=0.5`、`Sixteenth=0.25`。Sequence DivisionはParameter、Fader Random DivisionはAPC固有Bindingが所有する。

division indexは `floor(beat / beatsPerStep)` で求める。Sequence active stepはそのindexのpositive modulo StepCount。Fader Randomは前回division indexと異なる時だけ更新し、frame dropで複数境界を跨いだ場合も到達した最終indexから一回だけ決定する。乱数は既存 `PcgHash.Pcg01(divisionIndex + 1, faderIndex + 1000)` を使用して再現可能にし、0.5未満を0、それ以外を1とする。

### Prefs records

```csharp
[Serializable]
public sealed class MidiMappingProfileData
{
    public int SchemaVersion;
    public string ProfileId; // trip26
    public List<ToggleMidiBinding> ToggleBindings;
    public List<RadioMidiBinding> RadioBindings;
    public List<OneshotMidiBinding> OneshotBindings;
    public List<MomentaryMidiBinding> MomentaryBindings;
    public List<StateMidiBinding> StateBindings;
    public List<SequenceMidiBinding> SequenceBindings;
    public List<FaderMidiBinding> FaderBindings;
}
```

```csharp
[Serializable]
public sealed class MidiRuntimeStateData
{
    public int SchemaVersion;
    public string ProfileId;
    public List<BoolParameterState> BoolValues;
    public List<IntParameterState> RadioValues;
    public List<IntParameterState> StateValues;
    public List<FloatParameterState> FloatValues;
    public List<SequenceParameterState> SequenceValues;
}

[Serializable]
public sealed class MidiConnectionSettingsData
{
    public int SchemaVersion;
    public string InputProductFilter = "APC mini mk2 Control";
    public int InputChannel;
    public string OutputPortFilter = "APC mini mk2";
    public float ReconnectIntervalSeconds = 2f;
    public bool LogInputEvents;
    public bool ClearLedsOnDisable = true;
}
```

state entryは全て `ParameterId` と値だけを持つ型別DTOにする。`SequenceParameterState` は `List<bool> Steps` と `BeatDivision Division` を持つ。固定StepCountは保存せずScene Parameter定義を使う。`Kind`を別fieldで重複保存せず、格納listの型をkindとして扱う。load時はParameter IDが該当interfaceを実装する場合だけ適用する。

Prefs key:

- `qoooo.midi.trip26.mapping`
- `qoooo.midi.trip26.state`
- `qoooo.midi.settings`

StateはParameter ID、kind、型別valueを保存する。Oneshot、Momentary、beat、page、Random一時値、Fader mode、Listen/Pickup、接続、LED queueは保存しない。

`CurrentSchemaVersion` は3 recordとも1とする。`ProfileId != "trip26"`、version 0以下、version 1より大きいrecordは適用せずScene defaultを使い、recordごとにwarningを一回出す。version 1より古いmigrationは現時点では存在しない。将来追加する場合はversionごとのpure functionとする。

production persistenceは3 recordを個別にtry/catchしてloadする。一つがnull、JSON不正、schema不一致でも他の正常recordは適用し、失敗recordだけfallbackへ戻す。Settingsはchannelを0..15、reconnect intervalを0.5秒以上へnormalizeし、null filterを空文字へ変換する。

Runtime StateのParameter欠損/kind不一致entryは起動時に適用しないがorphan stateとして保持し、次回Saveでも失わない。Parameterが再び同じID/kindで現れたら適用する。`Reset Values` はorphan stateもclearする。

### Fader mode and Pickup

- Fader 1-8はTrack 1-8、MasterはShiftでmode切替。
- FunctionはMappingへ保存し、現在modeは保存しない。
- Muteは `Normal <-> Mute`、Randomは `Normal <-> Random`。
- 起動時は全Fader Normal。
- 標準Functionは `Mute, Random, Mute, Random, Mute, Random, Mute, Random, Mute`。
- 未割当FaderのTrack/Shift操作は何もしない。Float BindingへFaderを初回割当した時点で、物理indexに対応する上記標準Functionと `Quarter` Random Divisionを設定する。
- UI/Prefs変更後はPickup待機へ入る。
- Pickup toleranceはnormalized値で0.02とする。前回/今回の物理値がtargetを跨ぐか、今回値がtargetの±0.02へ入ると成立し、その後追従する。
- 各Faderについて `hasReceivedPhysicalValue`、`previousPhysicalValue`、`pickupActive` を保持する。接続後最初の値は記録だけ行い、tolerance内の場合だけ成立させる。disconnectではhasReceivedをfalseに戻す。
- Randomはdivision境界ごとに0か1、Muteは0をParameterへ供給する。
- mode解除後はPickupへ戻してjumpを防ぐ。
- ControllerがParameterへ書く間だけsource guardを立てる。guard外で `Changed` を受けた場合はRosettaUI/コード等の外部変更と判定し、Mute/RandomをNormalへ戻してその値を採用し、Pickup待機へ入る。
- Mute/Random中にSaveした場合はその時点のParameter値を保存する。mode自体は保存しないため、次回起動はNormalかつ保存値から開始する。
- subsystem起動時のcurrent pageは0。input/output再接続とMapping rebuildではpageを維持するが、Mapping rebuild時は全Fader modeをNormal、Pickupを待機へ戻す。

### LED rules

| 対象 | 非アクティブ | アクティブ |
| --- | --- | --- |
| 未割当Pad | Off | - |
| Toggle | Dim Green | Bright Green |
| Radio | Dim Blue | Bright Blue |
| Oneshot | Dim Yellow | Bright Yellow while true |
| Momentary | Dim White | Bright White for at least one flush |
| State | Dim Cyan | 共通paletteをstate indexで循環 |
| Sequence | Off | Orange、active stepはWhite優先 |
| Listen | - | Blink |
| Scene page | Off | current page On（単色LED） |
| Track / Mute | Off | On |
| Track / Random | Off | Blink |

ShiftにはLEDがないためMaster modeはRosettaUI上だけで確認する。

Track/Scene Buttonはprotocol上 `Off / On / Blink` の単色LEDであり、PadのようなDim/Bright channel指定を使わない。Listen開始時、slotが既にGridへ割当済みならそのPadをBlink表示する。未割当slotは点滅させる物理Padがないため、RosettaUIの `Listening...` 表示だけを出す。

palette/behaviour定数は添付 `ApcMiniMk2.cs` とprotocolに合わせ、少なくとも `Black=0`、`White=3`、`Red=5`、`Yellow=13`、`Green=21`、`Blue=45`、`Pink=53`、`Cyan=90`、`Dim=Brightness50(channel 2)`、`Bright=Solid(channel 6)`、`Blink=Blink1Per4(channel 14)` を定義する。Stateは `[Cyan, Blue, Pink, Yellow, Green, Red, White]` をindex moduloで使う。

LED Rendererのflush intervalは1/15秒、1回最大32 messageとする。desired/current arraysを64 Pad、8 Track、8 Sceneについて保持し、異なる要素だけを出力queueへ積む。page変更、Mapping revision変更、output再接続ではcurrent arraysをinvalid化してfull redrawする。

## Implementation Steps

### Step 0: test assemblyを成立させる

`Assets/qoooo/Scripts/qoooo.Runtime.asmdef` は次の参照を持つ。

```json
{
  "name": "qoooo.Runtime",
  "references": [
    "UnitySimpleContainer",
    "RosettaUI",
    "PrefsGUI",
    "PrefsGUI.RosettaUI",
    "Minis",
    "RtMidi.Runtime",
    "Unity.InputSystem",
    "Unity.TextMeshPro",
    "UnityEngine.UI",
    "Klak.Syphon.Runtime"
  ],
  "autoReferenced": true
}
```

`Assets/qoooo/Tests/EditMode/qoooo.EditModeTests.asmdef` は次の設定にする。

```json
{
  "name": "qoooo.EditModeTests",
  "references": ["qoooo.Runtime"],
  "optionalUnityReferences": ["TestAssemblies"],
  "includePlatforms": ["Editor"],
  "autoReferenced": false
}
```

- asmdef追加直後にcompileし、既存Scene/PrefabのMonoBehaviour参照切れがないことを確認する。
- `AssemblyInfo.cs` に `[assembly: InternalsVisibleTo("qoooo.EditModeTests")]` を追加し、runtime helperをtest目的でpublic化しない。
- 既存 `ApcMiniMk2LayoutTests` と `BpmClockTests` がEditMode Test Runnerに発見されることを確認する。
- package assembly名がUnity上の実名と一致しない場合に推測で変更せず、対象packageのasmdef `name` を根拠に修正する。

完了条件: 既存2 test classが発見され全件passし、runtime compileがError 0になる。

### Step 1: Parameter core

対象: `Assets/qoooo/Scripts/Parameters/`。

- interface、型別Parameter、ID、default、validation、Changedを実装する。
- Radio options、State count、Sequence steps/divisionをParameterへ置く。
- Momentaryのpoll/event/countとframe resetを実装する。
- pure C# testを作る。

完了条件: MIDIなしで全Parameter型の状態遷移を検証できる。

### Step 2: ProviderとRegistry

- UnitySimpleContainerからProviderを列挙しID indexを構築する。
- null、空ID、重複IDを検証する。
- UI用にowner/display nameでgroup化したreadonly一覧を提供する。
- `MidiParameterRuntime` を追加し、System Controllerから `BpmManager` とRegistryを渡す。
- `Update(-110)` 冒頭で全Momentaryをbegin-frame resetし、その後全Sequenceへ `BpmManager.Beat` を渡す。

完了条件: Scene内のMIDI対応Parameterだけを列挙・解決できる。

### Step 3: 最初の既存機能をParameter化

- `Assets/qoooo/Scripts/Textures/TextureLayer.cs` の `_useChromaKey: bool` を `_useChromaKey: BoolParameter` へ置換する。
- `UseChromaKey` は `_useChromaKey.Value` を返す。
- `CreateParameterElement` のfield binderはgetterに `Value`、setterに `TrySetValue` を使う。
- `TextureLayer` を `IMidiParameterProvider` にし、`MidiParameterGroupName => $"Texture Layer / {gameObject.name}"`、`MidiParameters` は `_useChromaKey` 一件を返す。
- Trip26にある3 instanceへ以下のstable ID、display name、既定値をScene serializationで設定する。
  - `BackgroundLayer`: `texture.background.chroma-key` / `Use Chroma Key` / false
  - `TextLayer`: `texture.text.chroma-key` / `Use Chroma Key` / true
  - `ModelLayer`: `texture.model.chroma-key` / `Use Chroma Key` / true
- 標準Mappingでは3 Parameterを未割当とし、ユーザーがBinding EditorのAddとListenで割り当てる。
- MIDIなしでUI、保存、resetを検証する。

完了条件: 実機能でownershipと既存挙動互換を証明する。

### Step 4: raw MIDI I/O分離

- 現classのMinis device管理とqueue drainをraw inputへ移す。
- RtMidi worker、bounded queue、reconnect、thread-safe snapshotを実装する。
- workerからUnity APIを呼ばずmain threadでlogする。
- fake I/Oをtestで使えるinterfaceにする。

完了条件: raw Note/CCとoutput byte列をAPC非依存に検証できる。

### Step 5: APC Surfaceと型別Binding

- Grid/Fader/Track/Scene/Shiftの番号変換とsemantic eventをSurfaceへ実装する。
- 8 page、Fader mode、PickupはControllerへ実装する。
- `ApcMiniMk2Runtime.Update(-90)` からControllerへ現在beatを一度渡し、Fader Random division更新をinput event処理から分離する。
- union Bindingを型別listへ置換する。
- kind、slot数、control/Parameter一意性を検証する。
- 文字列状態storeを削除しParameterを直接操作する。
- Pickup、Mute、Fader Random divisionを実装する。

完了条件: fake inputで全Binding型とFader modeを操作できる。

### Step 6: Prefs Profile Store

- `MidiProfileStore` のserialized `_defaultMapping` と `_defaultSettings` をScene defaultとする。
- `MidiSystemController.Awake` で `PrefsMidiProfilePersistence` と3つの `PrefsAny<T>` を生成し、Scene defaultsをdeep copyしてからProfile Storeをloadする。
- EditMode testではin-memory `IMidiProfilePersistence` を使用し、実Prefsへ触れない。
- 未解決BindingのDTOを保持する。
- `IPrefsSaveParticipant` と `UiBuilder` のsave transactionを実装する。
- Save、Revert、Reset Mapping、Reset Valuesを上記transaction契約どおり実装する。
- ID/kind検証後に保存対象Parameterを復元する。

完了条件: round-trip、未知version、欠損Parameterで安全に起動する。

### Step 7: Listen Controller

- 単一session、Button/CC learn、threshold、slot移動、cancelを実装する。
- 確定入力を通常Binding処理から抑制する。
- Listen ControllerはSurfaceを直接購読せず、Controllerから渡された入力を `TryConsume` のbool戻り値で消費する。
- Listen確定したNote/CC番号は当該 `Time.frameCount` の抑制setへ入れ、同じdispatch batch内の後続CCや対応Note Offも通常Bindingへ渡さない。次frame冒頭でsetをclearする。
- 開始/確定/移動/cancelを `Debug.Log` へ一回出す。

完了条件: Toggle、Radio option、Sequence step、Faderを個別Listenできる。

### Step 8: LED Renderer

- 共通色、page、Track mode、Listen表示を状態から生成する。
- changeでdirty化し、送信間隔・1 flush上限を設ける。
- Momentaryを最低1 flush latchする。
- reconnect、page/Mapping変更でfull redrawする。

完了条件: fake outputが状態と一致し、未変更時に重複送信しない。

### Step 9: RosettaUI

- owner/nameでParameterを選びkind別editorを構築する。
- Radio option、Sequence stepごとのListen/clearを表示する。
- Fader FunctionがRandomの場合だけDivisionを表示する。
- 未解決Binding、Revert、Reset操作を表示する。
- 接続filter、channel、reconnect、log、clear設定を編集可能にする。
- 仮想APCとmonitorは追加しない。

`MidiBindingUi.CreateElement()` の構成を次に固定する。

```text
MIDI Mapping
  Save status: Saved / Modified
  [Revert] [Reset Mapping] [Reset Values]

  Add Binding
    Target: owner名 / parameter表示名 dropdown
    [Add]

  Toggle Bindings
  Radio Bindings
  Oneshot Bindings
  Momentary Bindings
  State Bindings
  Sequence Bindings
  Fader Bindings
  Unresolved Bindings
```

- 既にBinding済みのParameterはAdd dropdownから除外する。
- target選択はRosettaUI 2.0.1の `UI.Dropdown(LabelElement, Func<int>, Action<int>, IEnumerable<string>)` overloadを使い、sort済みParameter descriptor listのindexを保持する。文字列を保存値には使わない。
- 各entryはtarget表示、物理controlの `Describe()`、`Listen/Cancel`、`Clear`、`Delete` を持つ。
- `Clear` はslotだけ未割当にしBindingを残す。`Delete` はBinding record自体を消す。
- Radio/Sequenceはoption/step名ごとのrowを表示する。
- 可変構造変更時はMapping `Revision` をincrementし、`UI.DynamicElementOnStatusChanged(() => Revision, ...)` で該当部分を再構築する。
- 値変更だけではUI treeを再構築しない。RosettaUI binderがgetterを読む。
- Revert/Resetは確認dialogを新設せず即時実行する。これは単独利用かつSave前ならRevert可能という前提による。
- `MidiSettingsUi` はInput Product Filter、Channel(0..15)、Output Port Filter、Reconnect Interval(min 0.5)、Log Input Events、Clear LEDs On Disableを一つのfoldへ表示する。

完了条件: 不要設定を表示せず全編集が即時反映される。

### Step 10: Trip26統合と旧構造除去

- Sceneへ新componentsと標準設定を保存する。
- 旧Mappingが空であることを再確認しunion/state storeを削除する。
- 合意したVJ Parameterを段階的に登録する。
- Domain Reload有効/無効でPlayを反復する。

完了条件: UI、実機入力、Prefs、LED、再接続が通り、旧文字列getter利用がない。

## Edge Cases

| ケース | 期待動作 |
| --- | --- |
| MIDI未接続 | Parameter/UI/Prefsは通常動作し、output queueを増やさない。 |
| outputだけ未接続 | input継続。再接続後に全LED同期。 |
| ID重複 | 両方Binding対象外、owner付きerror一回。 |
| Parameter欠損 | 未解決保持、I/O無効、UI修復可能。 |
| Parameter kind変更 | 未解決扱い。自動変換しない。 |
| Radio option数変更 | 共通slot維持、余剰は修復対象。 |
| Sequence step数変更 | 共通pattern/slotだけ復元、残りdefault。 |
| 使用済みPadをListen | 旧slotから新slotへ移し値は変えない。 |
| Listen中のdisconnect | session維持、Cancelか再接続を待つ。 |
| Oneshot保持中のpage変更 | Note On時のParameterを記録し、元のParameterをNote Offで解除する。 |
| Oneshot保持中のMapping変更/切断 | 保持中Parameterを全てfalseへ戻してstuck状態を残さない。 |
| Fader noise | threshold/Pickup成立前は値を変えない。 |
| UI変更後のFader | Pickup成立まで論理値維持。 |
| Mute/Random解除 | Pickupへ戻りjumpしない。 |
| 同frame Momentary複数回 | poll=true、event/countは回数分。 |
| Reset後Saveしない | 次回は最後の保存状態へ戻る。 |
| 未知schema | Scene defaultで起動しwarning一回。 |
| output queue飽和 | 古い更新を統合/破棄しdesired stateから再送可能。 |
| worker停止遅延 | 短い上限付きjoin後にshutdown継続。 |
| Domain Reload無効 | static event/sessionを残さない。 |

## Verification

### Automated tests

- 全Parameter型のdefault、validation、Changed、reset。
- Momentaryのpoll、event、複数発火、frame reset。
- Provider収集、empty/duplicate ID。
- 型別Bindingのkind、slot数、control/Parameter一意性。
- Listen移動、threshold、確定入力抑制。
- Pickup crossing/tolerance、Mute/Random解除。
- Beat DivisionごとのSequence/Random境界。
- 3 recordのround-trip、Revert、Reset、unknown version、missing Parameter。
- APC座標変換、Type別LED、dirty dedup、full redraw。
- output message bytesとbounded queue policy。

既存EditMode testがTest Runnerに発見されない状態が確認済みである。実装開始時にtest assemblyを追加し、上記testを発見・実行可能にする。

### Compile

各C#変更後にUnity Consoleをclearし、`uloop compile` でError 0 / Warning 0を確認する。

### Play Mode / hardware

1. APC未接続でParameter UIを操作する。
2. 実機接続logを確認する。
3. 全Binding型をListenし、入力、Parameter、UI、LEDの一致を確認する。
4. 使用中Padを移動し、旧機能が動かないことを確認する。
5. UI変更後のFader Pickupを確認する。
6. Mute/Random、Track LED、divisionを確認する。Master modeはUIで確認する。
7. page切替とScene/Pad LED再描画を確認する。
8. Save・再起動後、Mappingと保存対象値だけが復元されることを確認する。
9. Fader mode、page、Momentary、beat、Pickupが復元されないことを確認する。
10. output切断・再接続でUnityが停止せずLEDが戻ることを確認する。
11. Domain Reload有効/無効でevent重複やstale instanceがないことを確認する。

## Risks

| リスク | 対策 |
| --- | --- |
| Parameter化が大規模化 | MIDI対応する実機能だけを一つずつ移行する。 |
| ID変更でPrefsが切れる | semantic ID、validation、未解決保持、schema version。 |
| PrefsGUIの複雑型serialization | version recordごとに `PrefsAny<T>` を使用し、実装の最初にround-trip testでUnity serialization対象を検証する。 |
| RosettaUI可変listの再構築 | Mapping revision変更時だけdynamic elementを再構築。 |
| Momentary LEDが短すぎる | Rendererで最低1 flush latch。 |
| RtMidi native call停止 | worker限定、bounded queue、短いjoin。 |
| 古いLED message | desired stateをsource of truthにしてfull redraw可能にする。 |
| 別portへ誤接続 | 独立filter、接続port表示、APC名default。 |
| Pickup成立困難 | crossingと許容幅を併用しUIへ待機表示。 |
| Scene defaultとPrefsの混同 | dirty/save状態とRevert/Resetの意味をUI表示。 |

rollbackはParameter ownerを旧primitive fieldへ戻し、新MIDI componentsをSceneから外して旧 `ApcMiniMk2MidiInput` を再有効化する。旧Mappingは空のため逆変換不要。

## Assumptions

- 主なSceneはTrip26一つ。
- activeなAPC input/outputは各一つ。
- Beat Divisionは4/4前提。
- MIDI対応Parameterのsemantic IDを開発時に一度指定できる。
- 入力変更はmain threadで順序付けられ、最後の変更を採用する。
- Type共通LED色で十分。
- Master modeはShiftで切り替え、実機LED表示できなくてよい。
- debugは `Debug.Log` を使い、永続ファイル診断は不要。

## Open Questions

なし。最初のvertical sliceはTrip26の3つの `TextureLayer._useChromaKey` とし、標準Mappingは未割当から開始する。その他のVJ fieldは本基盤完了後に機能単位でParameter化する。
