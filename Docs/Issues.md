# qoooo Issues

VJのPlaytest中に見つかった問題、操作上の不満、
Visual改善案、新しく欲しくなった表現を管理する。

このファイルは、

```text
Playtest
↓
Issue記録
↓
ChatGPTと整理
↓
ready
↓
Codex実装
↓
人間がUnityで確認
↓
done
```

の橋渡しに使用する。

## Important Rules

- `inbox`のIssueを勝手に実装しない。
- Codexは、ユーザーから明示的に指定された`ISSUE-xxx`だけを実装する。
- Issueに書かれた症状から、無断で大規模architecture変更を行わない。
- unrelated refactor / cleanupは禁止。
- 複数Issueを一度に実装しない。原則`1 Issue = 1 focused change`。
- 実装方法より、まずObserved / Expectedを優先する。
- Issue本文に解決方法が未記入の場合、勝手に大きな仕組みを追加しない。
- 実装後はStatusを`verify`へ変更する。
- `done`への変更は、人間がUnity上で確認した後に行う。
- Codexはcommitしない。

---

## Status

| Status        | Meaning                            |
| ------------- | ---------------------------------- |
| `inbox`       | Playtest中の気づき。まだ仕様整理前 |
| `ready`       | 実装内容・完了条件が明確           |
| `in_progress` | 実装中                             |
| `verify`      | 実装済み。人間のUnity確認待ち      |
| `done`        | 人間による確認完了                 |
| `hold`        | 今回は実装しない                   |

---

## Priority

| Priority | Meaning        |
| -------- | -------------- |
| `P0`     | 本番までに必須 |
| `P1`     | かなり欲しい   |
| `P2`     | 余裕があれば   |

---

## Type

| Type      | Meaning                              |
| --------- | ------------------------------------ |
| `BUG`     | 壊れる、表示されない、意図しない挙動 |
| `CONTROL` | MIDI、UI、操作性                     |
| `VISUAL`  | 見た目、構図、表現                   |
| `WANT`    | 新しく欲しい機能・表現               |

---

# Issue Template

ユーザーが新しいIssueを書くときは、
以下をコピーして`Active Issues`へ追加する。

```md
## ISSUE-001: 短いタイトル

**Status:** inbox  
**Priority:** P1  
**Type:** VISUAL

### Observed

実際に触っていて起きたこと・感じた不満を書く。

### Expected

どうなってほしいかを書く。

### Context

必要なものだけ書く。

- Scene:
- Parameter:
- Model / Layout:
- 再現条件:
- Screenshot / Reference:

### Acceptance Criteria

仕様がまだ決まっていなければ空でもよい。

- [ ]

### Implementation Notes

`inbox`段階では空でよい。

ChatGPTとの相談などで実装方針が決まった場合のみ記入する。

### Verification

実装前は空にする。

Codexが実装した場合は、
実施したcompile / Play Mode確認などの事実だけを書く。
```

---

# Active Issues

<!--
Status:
- inbox: Playtest中の気づき。まだ仕様整理前
- ready: 実装内容・完了条件が明確
- in_progress: 実装中
- verify: 実装済み。人間のUnity確認待ち
- done: 人間による確認完了
- hold: 今回は実装しない

Priority:
- P0: 本番までに必須
- P1: かなり欲しい
- P2: 余裕があれば

Type:
- BUG: 壊れる / 表示されない / 意図しない挙動
- CONTROL: MIDI / UI / 操作性
- VISUAL: 見た目 / 構図 / 表現
- WANT: 新しく欲しい機能・表現
-->

## ISSUE-001: Grid DesignによるPoster Layoutの再設計

**Status:** verify  
**Priority:** P0  
**Type:** VISUAL

### Observed

現在のPoster / Typography / Model配置には、
画面全体を統一するGrid / Alignment / Spacingの規律が弱い。

要素ごとに個別の座標を与えているため、

- 中途半端な位置に置かれる
- TextとModelのedgeが揃わない
- 余白やspacingが統一されない
- 意図のない回転や散らしが入りやすい
- 「設計されたグラフィック」より「要素を置いた画面」に見える

という問題がある。

参考にしているPoster Designでは、
自由に配置されているように見えても、

- center axis
- left / right edge
- column
- baseline
- image edge
- text block

などが明確に揃っている。

### Expected

PosterCanvas上の主要Visualを、
共通Grid Designに従って配置する。

目標は自由配置機能を増やすことではなく、

**良い制約を増やし、少ないルールで強い画面を作れるようにすること。**

細部のデザイン判断は実装時に調整してよいが、

```text
Grid
Alignment
Text Role
Rotation
Presetごとの構造
```

は以下の仕様を守る。

---

### Design Rules

- 12-column grid
- Safe Marginあり
- Column間にGutterあり
- 縦方向はBaseline Grid
- Text / Model / SolidRect / Lineを同じGrid基準で配置する
- 関連するTextは同じalignmentを共有する
- spacingは固定されたSpacing Scaleから選ぶ
- random positionは禁止
- 意味のないarbitrary positionを避ける
- 回転角度は`0 / 90 / 180 / 270°`のみ
- 基本は`0°`
- 90° / 270°はSide Label / Meta Text用途として使用可能
- 任意角度の斜め回転は今回使用しない
- 崩す前に、まず正しく揃える

原則:

```text
なぜその位置にあるか説明できない配置は作らない。
```

---

### Grid Tokens

既存PosterCanvasのpixel-like authoring APIを維持する。

```text
Width
Height
MinSize = min(Width, Height)
```

基本値:

```text
margin   = MinSize * 0.06
gutter   = MinSize * 0.02
baseline = MinSize * 0.04
```

横方向:

```text
| margin | col 1 | gutter | col 2 | ... | col 12 | margin |
```

縦方向:

```text
y = margin + baseline * n
```

---

### Spacing Scale

```text
XS = MinSize * 0.01
S  = MinSize * 0.02
M  = MinSize * 0.04
L  = MinSize * 0.08
XL = MinSize * 0.16
```

任意の細かいratioを大量に作らず、
原則このScaleから選ぶ。

Design document上のcolumn番号は読みやすさのため`1..12`とする。

実装内部で`0..11`を使用してもよいが、
Grid helper内で対応関係を明確にし、
Layout code内で1-based / 0-basedを混在させない。

---

### Text Content

Trip26で使用する基本文字列:

```text
HAPILABO
DJ RØKI

2026
09.26
SATURDAY
22:30

NAEBA
NIIGATA / JAPAN

UNITY
MIDI
LIVE

VISUALS BY QOOOO
AFTER PARTY

128 BPM
BEAT 01 / 04
```

日付情報は必要に応じて、

```text
Year
Month
Day
Weekday
Time
```

へ分解してLayout素材として使用できるようにする。

#### Content Scope

このIssueの目的はGrid / Layoutの再設計であり、
新しいText Parameter / CMS / Runtime Data Bindingの実装ではない。

既存の`poster.main-text`は維持し、
Hero Textのsourceとして引き続き使用する。

```text
poster.main-text
- QOOOO
- TRIP26
- LIVE VISUAL
```

以下はTrip26用のSupporting Textとして扱う。

```text
HAPILABO
DJ RØKI
2026
09.26
SATURDAY
22:30
NAEBA
NIIGATA / JAPAN
UNITY
MIDI
LIVE
VISUALS BY QOOOO
AFTER PARTY
```

`128 BPM` / `BEAT 01 / 04`もTypography素材として使用してよいが、
このIssueでは実BPM / Beat値とのRuntime接続は行わない。

Rhythmとの接続はISSUE-002で扱う。

---

### Text Roles

#### Hero

主役となる最大サイズの文字。

```text
HAPILABO
DJ RØKI
```

#### Secondary

Heroを補助する第2見出し。

Heroに使っていない方を使用可能。

#### Meta

小さな情報。

```text
2026
09.26
SATURDAY
22:30
NAEBA
NIIGATA / JAPAN
UNITY
VISUALS BY QOOOO
```

#### Badge / System

小さなラベル。

```text
LIVE
MIDI
UNITY
AFTER PARTY
```

MIDIの実接続状態などdebug情報はVJ出力へ表示しない。

#### Rhythm

```text
128 BPM
BEAT 01 / 04
```

将来的にRhythm Systemと接続する。

---

### Text Alignment Rules

Textは原則として、

```text
Left
Center
Right
```

のいずれかへ明確に揃える。

関連するTextではalignmentを共有する。

例:

```text
Hero.left == Secondary.left == Meta.left
```

または、

```text
Hero.centerX == Secondary.centerX == Model.centerX
```

---

# Layout Specification

Layout図中の`HAPILABO` / `DJ RØKI`は構成例であり、
既存`poster.main-text`を置き換える指定ではない。

Hero slotには`poster.main-text`を使用し、
Event / Artist textはSecondary / Meta / Supporting Typographyとして配置する。

既存のComposition軸は維持する。

```text
Editorial
→ Hero + Poster Copy

Split
→ Split + Headline

Modular
→ Repeat + Labels
```

`poster.model-layout`と`poster.typography-layout`を
再統合するarchitectureへ戻さない。

既存3 Layoutの中身をGrid Designで改善する。

---

## Editorial

### Character

王道 / 強い / 安全。

中央軸を強く使い、
ModelとTypographyを一つの塊として見せる。

参考Posterの、

```text
大きい中央Typography
+
大きい写真 / Object
+
四隅の小さいMeta情報
```

という構造を使う。

### Structure

```text
┌──────────────────────────────┐
│ 09.26                 UNITY  │
│ SATURDAY                2026 │
│                              │
│          HAPILABO            │
│          DJ RØKI             │
│       ┌────────────┐         │
│       │            │         │
│       │   MODEL    │         │
│       │            │         │
│       └────────────┘         │
│                              │
│ NAEBA               128 BPM  │
│ NIIGATA / JAPAN        LIVE  │
└──────────────────────────────┘
```

### Rules

Hero / Secondary:

```text
center aligned
cols 2–11程度
```

Model:

```text
center aligned
cols 3–10程度
大きく使用
rotation 0°
```

TextとModelは意図的にoverlapしてよい。

Top Left Meta:

```text
09.26
SATURDAY
```

Top Right Meta:

```text
UNITY
2026
```

Bottom Left:

```text
NAEBA
NIIGATA / JAPAN
```

Bottom Right:

```text
128 BPM
LIVE
```

中央軸は崩さない。

---

## Split

### Character

情報デザイン / シャープ / 明確な左右分割。

TextとModelを明確なColumn境界で分ける。

### Structure

```text
┌──────────────────────────────┐
│ HAPILABO      │        UNITY │
│ DJ RØKI       │          ↑   │
│               │          │   │
│ 09.26         │ ┌──────────┐ │
│ SATURDAY      │ │          │ │
│               │ │  MODEL   │ │
│               │ │          │ │
│               │ │          │ │
│               │ └──────────┘ │
│                              │
│ NAEBA              128 BPM   │
└──────────────────────────────┘
```

### Grid

目安:

```text
Text  = col 1–5
Gap   = col 6
Model = col 7–12
```

Hero / Secondary:

```text
Left aligned
同じleft edge
```

Meta:

```text
Heroと同じleft edge
```

Model:

```text
右側を縦に大きく使用
rotation 0°
```

UNITY:

```text
right safe margin
rotation 90°または270°
```

このLayoutではTextを左右へランダムに散らさない。

---

## Modular

### Character

Graphic / Repeat / 遊び。

Modelを複数回配置するが、
ランダム配置にはしない。

明確なGrid Cellへ配置する。

### Structure

```text
┌──────────────────────────────┐
│ HAPILABO             09.26   │
│ DJ RØKI               2026   │
│                              │
│   [model]   [ BIG MODEL ]    │
│                              │
│              [ BIG MODEL ]   │
│                        UNITY │
│   [model]                ↑   │
│                          │   │
│ VISUALS BY QOOOO    128 BPM │
└──────────────────────────────┘
```

### Model

既存Repeat Model Layoutを利用する。

例:

```text
Large:
cols 5–10

Small 1:
cols 1–3

Small 2:
cols 10–12

Small 3:
cols 2–4
```

すべて明確なGrid位置を持つ。

rotationは基本`0°`。

### Typography

Top Left:

```text
HAPILABO
DJ RØKI
```

Top Right:

```text
09.26
2026
```

Side Meta:

```text
UNITY
rotation 90°または270°
```

Bottom Left:

```text
VISUALS BY QOOOO
```

Bottom Right:

```text
128 BPM
```

---

### Support Graphics

以下もGrid基準で配置する。

```text
SolidRect
Line
Band
Border
Label Box
```

Support Graphicsは単独で適当な位置へ置かず、

```text
Text edge
Model edge
Column boundary
Baseline
```

のいずれかと関係を持たせる。

Band / Border / Label Boxのための新しいprimitive APIは原則追加しない。

既存の、

```text
SolidRect
Line
Text
```

を組み合わせて表現する。

---

### Model Placement

Model TextureもGridへ載せる。

既存の、

```text
Cover
Contain
Stretch
```

は維持する。

Model Sourceが変わっても
Layout構造が破綻しにくいRectを優先する。

---

### Implementation Scope

汎用Layout Engineは作らない。

既存PosterCanvasへ、

```text
Grid
Column
Span
Baseline
Spacing
```

を計算する薄いauthoring helperを追加する。

GridはRendering architectureではなく、
Layout codeを書くためのUtility。

追加しないもの:

```text
expression parser
constraint solver
automatic collision detection
automatic layout generation
normalized-coordinate authoring API
arbitrary rotation system
```

大規模なText CMS / Data Binding Systemも作らない。

Trip26で必要な固定文字列を扱える最小構造でよい。

PosterCanvasに既存rotation機能がある場合はそれを再利用する。
新しいrotation architectureは作らない。

Grid Layout側が渡すrotation値を、

```text
0 / 90 / 180 / 270
```

へ制限する。

---

### Acceptance Criteria

- [ ] PosterCanvas用の薄いGrid helperが存在する
- [ ] 12-column位置を取得できる
- [ ] column start / spanからRectを作れる
- [ ] baseline位置を取得できる
- [ ] Safe Margin / Gutterを共通利用できる
- [ ] Spacing ScaleをLayoutから使用できる
- [ ] Text / Model / SolidRect / Lineを同じGrid基準で配置できる
- [ ] Text rotationとして0 / 90 / 180 / 270°を扱える
- [ ] 任意角度rotationを新Layoutでは使用しない
- [ ] Hero / Secondary / Metaのalignmentが明確
- [ ] ModelとTextのedgeまたはcenterに明確な関係がある
- [ ] EditorialがGrid Designへ再設計されている
- [ ] SplitがGrid Designへ再設計されている
- [ ] ModularがGrid Designへ再設計されている
- [ ] `poster.layout`と`PosterLibrary.Documents`のindexが一致する
- [ ] Model Source変更後もLayoutが破綻しない
- [ ] 既存PosterCanvasのpixel-like authoring APIを維持
- [ ] Compile error 0
- [ ] Play Mode error 0

### Verification

- Compile: errors 0 / warnings 0
- Play Mode: errors 0 / warnings 0
- Editorial / Split / ModularのGrid配置を確認
- `poster.layout` option数とDocument数が一致し、追加・削除後も切替できることを確認
- Model Source 0〜16でPoster Image配置が維持されることを確認
- Visual refinement後、Hero / Supporting TypographyのPalette panel支持とLarge / Small hierarchyを再確認
- Legacy Text runtimeを停止し、Posterのみが最終出力されることを確認
- `Poster Composer`でDocumentの追加・選択・rename・複製・削除と即時描画を確認
- v1固定4-field PrefsがDocumentsへmigrationされることを確認
- Qoooo Save後のPlay Mode再起動で全Documentの編集内容がv2から復元されることを確認

---

## ISSUE-002: Rhythm / Motionの設計が不足している

**Status:** inbox  
**Priority:** P0  
**Type:** VISUAL

### Observed

現在のVisualは時間変化していても、
音楽のリズムに乗っている感覚が弱い。

単にBeatで全部を動かしたいわけではなく、
動きには複数種類あると考えている。

### Expected

少なくとも以下のようなMotionの種類を整理したい。

- 一定周期で動き続けるもの
- 一方向 / 往復などの連続運動
- Beatごとに状態が変わるもの
- ボタンを押した瞬間に反応するもの
- `lerpNoise`などで滑らかに揺れるもの

どのParameter / Visual要素をどの時間構造で動かすかを整理してから実装したい。

---

## ISSUE-003: Tap Tempoがない

**Status:** inbox  
**Priority:** P0  
**Type:** CONTROL

### Observed

ライブ中に現在流れている曲へBPMを合わせるための
Tap Tempo操作がない。

### Expected

演奏中に数回Tapして、
現在のBPMを素早く合わせられるようにしたい。

Rhythm系Visualの基準として使用したい。

---

## ISSUE-004: 本番操作にPresetが不足している

**Status:** inbox  
**Priority:** P0  
**Type:** CONTROL

### Observed

見た目を大きく変えるために
複数Parameterを個別に操作する必要がある。

ライブ中に複数ボタンを連続して押さないと
次のVisualへ移れない。

また、高頻度で使う操作が複数Pageへ分散している。

### Expected

1操作で安全なVisual状態へ遷移できるPresetが欲しい。

P1はすべてのParameterを詰め込むPageではなく、

- Preset
- よく使うOverride
- 緊急操作

を中心としたPerformance Pageにしたい。

詳細調整はP2以降に残してよい。

---

## ISSUE-005: LayerごとのNone / Off状態が不足している

**Status:** inbox  
**Priority:** P1  
**Type:** VISUAL

### Observed

足し算のVisualは作れるが、
要素を減らす操作が十分ではない。

例えば、

- BackgroundをPatternなしのSolid Colorへする
- Overlay Textを出さない
- Modelを出さない

などを統一された方法で行いたい。

### Expected

各主要Layerについて
「何も表示しない / 最小状態にする」選択肢を明確にしたい。

---

## ISSUE-006: Layer Effectによる絵変わりが弱い

**Status:** inbox  
**Priority:** P1  
**Type:** VISUAL

### Observed

現在も一部TextureEffectは存在するが、
ライブ中にVisualの印象を大きく変える手段としては弱い。

### Expected

Model / TextなどのLayerについて、
効果が明確な少数のEffectをライブ操作できるようにしたい。

Effect数を増やすこと自体ではなく、
「押した瞬間に絵が変わったと感じるか」を基準にしたい。

---

## ISSUE-007: Camera Motionが弱い

**Status:** inbox  
**Priority:** P1  
**Type:** VISUAL

### Observed

CinemachineによるCamera Pose切替はあるが、
基本的には静止した構図同士の遷移になっている。

カメラそのものがVisualのMotionとして機能していない。

### Expected

Cinemachineを活かして、
継続的またはRhythmに関連したCamera Motionも検討したい。

ただし画面酔いするような常時激しい動きにはしたくない。

---

## ISSUE-008: RosettaUIを操作用途ごとに整理したい

**Status:** inbox  
**Priority:** P2  
**Type:** CONTROL

### Observed

MIDI UIが縦に長くなっている。

現在、

- MIDI Assign
- Visual Parameterの値変更

がUI上で混ざって見える部分がある。

また、MIDIへ割り当てない細かいParameterも
UIからは触りたい。

### Expected

役割を分けたい。

MIDI UI:
MIDI Assignmentを行う場所。

Visual / Texture / Parameter UI:
実際のParameter値を変更する場所。

MIDIではプリセット的な選択肢だけを操作し、
UIではより細かい値まで編集できる構造にしたい。

---

## ISSUE-009: Vertex deformationによる絵変わりが欲しい

**Status:** hold  
**Priority:** P2  
**Type:** WANT

### Observed

Model自体の形状変化がないため、
Visualの変化量が不足しているように感じる。

### Expected

頂点変形など、
Modelそのものの形状が変わる表現も候補として検討したい。

現時点ではGrid / Rhythm / Presetより優先しない。

---

## ISSUE-010: Skybox / 風景的Backgroundを検討したい

**Status:** hold  
**Priority:** P2  
**Type:** WANT

### Observed

現在のPattern Backgroundだけでは
画面の空間感や風景的な変化が不足している。

### Expected

Skyboxなど、
現在のPatternとは異なる種類のBackground表現を検討したい。

現時点ではGrid / Rhythm / Presetより優先しない。

---

# Done

人間による確認まで完了したIssueをここへ移す。

Issue本文は削除せず、
実装・検証履歴として残す。

---

# Codex Workflow

ユーザーから例えば、

```text
ISSUE-004だけ実装してください
```

と指定された場合のみ、そのIssueを対象にする。

実装開始時:

```text
ready → in_progress
```

実装・検証完了時:

```text
in_progress → verify
```

に更新する。

`verify → done`は原則としてユーザー確認後に行う。

IssueのAcceptance Criteriaを満たすために
追加の設計判断が必要で、Issue本文から判断できない場合は、
推測して実装を進めずユーザーへ確認する。

---

# Playtest Philosophy

Issueは「最初に思いついた解決方法」ではなく、
まず「実際に何が不満だったか」を残す。

例えば、

```text
Bad:
Posterize shaderを追加する

Better:
Modelが写真的すぎてPoster Layoutの中で浮いて見える
```

のように、問題と解決策を分離する。

解決策は`ready`へ整理するときに決める。

---

# Verification

作成後に確認してください。

- `Docs/Issues.md`だけが新規変更になっている
- Markdownとして読みやすい
- Issue Templateがcopy可能
- Active Issuesには架空のIssueを追加しない
- 既存コード・Scene・Architectureを変更していない
