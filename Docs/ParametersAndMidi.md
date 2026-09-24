# Parameters and MIDI

## Parameter Ownership

各機能の操作値は、その値を使う component が型付き Parameter として所有する。UI、Keyboard、MIDI は同じ instance を操作し、consumer は入力元を参照しない。

`MidiParameterRegistry` は provider から Parameter を収集し、validation と ID index を提供する。値を所有せず、重複 ID は registry から除外する。

Parameter ID は MIDI binding と保存データの stable identity である。ID の rename や option の互換性を変える場合は、既存 mapping と state を明示的に扱う。

## Provider Boundary

- `IMidiParameterContainer` は owner が Parameter を公開する contract である。
- `IMidiParameterSource` は texture source のような nested owner に使う。
- `TextureLayer` は nested source の Parameter を container として中継する。

## MIDI Boundary

`MinisMidiInput` は raw MIDI event を main thread へ渡す。`ApcMiniMk2ParameterController` は APC 固有の page、typed binding、Listen、pickup、fader mode を所有し、Parameter owner を直接操作する。

Fader は pickup / soft takeover を使い、assignment または値域を切り替えた直後の jump を防ぐ。Float binding は明示的な output range を持てる。range が未指定の場合は owner Parameter の native range を使い、range の参照だけで保存 mapping を変更しない。

`MidiProfileStore` は Scene の mapping を default とし、保存済み mapping と保存対象 Parameter state を復元する。Save は `IPrefsSaveParticipant` の transaction を通じて明示的に実行される。

## UI Boundary

Live UI は owner Parameter を直接編集する。`MIDI Bindings` は APC assignment、`MIDI Mapping` は profile と controller state を扱う。MIDI UI に Parameter value editor を重複して持たせない。

`Keyboard Bindings` も同じ registry Parameter を操作する。Bool はtoggle、Floatは設定値、Radioはoption index、Stateはnext、Sequenceはstep toggle、OneshotとMomentaryはtriggerとして保存mappingからdispatchする。TextField入力中はbindingを抑止する。
