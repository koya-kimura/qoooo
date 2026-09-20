# Codex Handoff

> Current as of 2026-09-20. This is a point-in-time handoff, not a permanent architecture specification.

## Project

- Unity VJ system: `qoooo`
- Unity: `6000.3.12f1`, URP `17.3.0`
- Main Scene: `Assets/qoooo/Scenes/Trip26.unity`
- Output: Game View preview and Klak Syphon
- Physical controller: Akai APC mini mk2
- Runtime editing: RosettaUI for visual Parameters, MIDI mapping, Listen, and profile operations
- Working tree before this documentation task: clean

The architecture entry point currently present in the repository is `Docs/Architecture.md`. The requested path `Docs/Architecture/Architecture.md` does not exist as of this handoff. The detailed source-of-truth documents under `Docs/Architecture/` are `ParametersAndMidi.md`, `RenderingAndText.md`, `Runtime.md`, `Development.md`, and `PlanStatus.md`.

## Current architecture summary

### Main texture pipeline

```text
MaterialTextureSource / CameraController / other ITextureContainer source
↓
TextureLayer
↓ optional
TextureEffect
↓
TextureCompositor (Bottom-to-Top, two RenderTextures ping-ponged)
↓
PreviewController / SyphonController
```

`TextureLayer` references a `MonoBehaviour` implementing `ITextureContainer`. The Trip26 compositor currently contains four layers in this order:

```text
BackgroundLayer
ModelLayer
TextLayer
PosterCanvasSource
```

`PreviewController` assigns the compositor texture to a UI `RawImage`. `SyphonController` assigns the same texture to `SyphonServer.SourceTexture`.

### Poster pipeline

```text
Model Camera
↓
Model RenderTexture (shared by all Poster RawImages)
↓
PosterCanvas primitives and selected layout
↓
ScreenSpaceCamera Canvas + Poster Camera
↓
PosterCanvasSource RenderTexture
↓
Poster TextureLayer
↓
TextureCompositor
↓
Preview / Syphon
```

`PosterCanvasSource` owns only the Poster Camera target RenderTexture and follows `IResolutionContainer`. It does not create another Model Camera or Model RenderTexture.

## Completed systems

### Parameter and MIDI

The active architecture is owner-owned typed Parameters. Application components expose them through `IMidiParameterContainer`; nested texture sources use `IMidiParameterSource`. `MidiParameterRegistry` validates and indexes Parameters but does not own their values.

Implemented Parameter kinds:

- `BoolParameter` / Toggle
- `RadioParameter`
- `StateParameter`
- `FloatParameter`
- `SequenceParameter`
- `OneshotParameter`
- `MomentaryParameter`

The active APC path includes:

- Minis raw MIDI input with queued main-thread dispatch
- 8 APC pages selected by Scene Launch buttons
- Grid Pad bindings for Toggle, Radio, State, Oneshot, Momentary, and Sequence
- Fader bindings with pickup
- Track/Shift button Mute and Random fader modes
- Pad and Fader Listen, including moving an already-used control
- RtMidi output worker
- APC LED rendering with type colors, page state, fader-mode state, and reconnect redraw
- RosettaUI Parameter and binding editing
- Scene default mapping plus Prefs-backed mapping and runtime state
- Save, Revert, Reset Mapping, and Reset Values

`ApcMiniMk2ParameterController` currently combines mapping validation, page state, Listen, pickup, and fader modes. Do not replace it with the historical string-key controller. Legacy MIDI classes remain in source but are not placed in Trip26; see `Docs/Architecture/PlanStatus.md`.

### Background

Parameter:

```text
ID: background.pattern
Type: RadioParameter
Owner: MaterialTextureSource, forwarded by TextureLayer
```

Current options, in index order:

1. Noise Texture
2. Diagonal Stripe
3. Vertical Stripe
4. Horizontal Stripe
5. Wave Stripe
6. Checkerboard
7. Polka Dot
8. Sunburst
9. Grid Line
10. Psychedelic Ring

The procedural source also exposes Main Color, Sub Color, Tiling, Offset, Speed, and Opacity through RosettaUI. It sends the shared `IBpmSource.Beat` to shader `_Beat`.

### Model

Current Model-owned Parameters:

```text
model.scale           FloatParameter
model.motion          RadioParameter: Still / Spin / Pulse
model.reset-transform OneshotParameter
model.active          BoolParameter, default true
```

`model.visibility-sequence` has been removed. Model visibility is not BPM-driven in the beta. `ModelController` collects `Renderer[]` from its parent `ModelManager` hierarchy with `GetComponentsInChildren<Renderer>(true)` and applies `model.active` through `Renderer.enabled`; it does not call `SetActive()`.

Trip26's serialized `ModelManager._models` list contains 17 models:

1. ArmchairSlopeReplica001
2. Candles001_CandleMelted02_Geo
3. FoodCinnamonRolls001
4. FoodGrapes001_Collection01_Geo
5. BreadBaguetteFrenchSlice001
6. Headphones001
7. JugCeramicGlazed001
8. Microwave002_Geo
9. PlantAgave002
10. PlantBanjoFig001
11. PlantOrchid001
12. PlantZZ001
13. SinkElevated002
14. SofaReversiReplica001
15. ToiletModern001
16. UtensilsJar001
17. WallLanternExterior001_Geo

`ModelManager.Start()` currently activates list element 0. This is not yet a Parameterized Model Source selector.

### Poster Composition

`PosterCanvas` is split across:

- `Assets/qoooo/Scripts/Rendering/Sources/PosterCanvas.cs`
- `Assets/qoooo/Scripts/Rendering/Sources/PosterCanvas.Layouts.cs`

Current public authoring surface:

- `Width`, `Height`, `SizeUnit`
- `Image` with Stretch or Cover
- source-pixel Crop
- Image rotation
- `SolidRect` and rotation
- `Line`
- `Text` and rotation
- `TextWidth`

Coordinates use a top-left origin with X right and Y down. Positive authoring rotation is clockwise.

Current Parameters:

```text
poster.layout              Radio: Editorial / Split / Modular / Custom
poster.model-layout        Radio: Hero / Split / Repeat
poster.typography-layout   Radio: Poster Copy / Headline / Labels
Owner: PosterCanvas
```

`poster.model-layout` controls only Model RawImage placement. `poster.typography-layout` controls Text and its supporting SolidRect/Line/Band graphics. `poster.layout` is a compatibility Safe Preset: Editorial applies Hero + Poster Copy, Split applies Split + Headline, and Modular applies Repeat + Labels. Editing either independent axis sets it to Custom. The existing APC mapping still assigns only Editorial / Split / Modular to Page 0, Row 0, Columns 0-2; Custom and the independent axes are unassigned.

### Texture Effects

The currently implemented layer effect features are:

- Invert
- Mosaic with Mosaic Size
- Tiling with Tile Number and Offset

They are combined through `EffectParams` and one `TextureEffect` material pass. ModelLayer and TextLayer have a `TextureEffect` component assigned; BackgroundLayer and PosterCanvasSource do not. All effect flags are currently off in the Scene. An enabled effect allocates one same-resolution temporary RenderTexture owned by that `TextureEffect`.

There is no separate effect catalogue, routing graph, or final-effect Parameter yet.

## Current Visual Parameter Map

The beta Visual System under consideration is:

```text
SOURCE
- Model Source
- Model Active
- Model Motion
- Background Pattern

CAMERA
- Camera Pose

COMPOSITION
- Model Texture Layout
- Typography Layout
- Main Text

GLOBAL
- Palette

EFFECT
- Layer Texture Effects
- Final Effect concept

MACRO
- Visual / Safe Preset
```

This map is design direction, not a claim that every axis is implemented.

### Important decisions

#### Model Active

Use the beta Parameter:

```text
model.active
BoolParameter / Toggle
```

Do not restore a visibility Sequence now. Reconsider rhythmic visibility only after practical VJ testing.

#### Model Texture Layout

This means arranging the single Model Camera RenderTexture multiple times on Poster Canvas, for example Hero, Split, or Repeat. It is separate from selecting a Model source and separate from positioning objects in the 3D Scene.

#### Typography Layout

Treat the following as one intentionally coupled layout axis:

- Text placement
- Text size and alignment
- Text motion
- supporting Rect, Line, Band, and Arrow graphics

Do not create separate Text and support-graphics Parameters. Future TextureEffect routing should still leave room to render Text glyphs and support graphics as separate render groups.

#### Accent

Accent Layout and Accent Effect are currently out of scope. Reconsider only if VJ testing demonstrates a need.

#### Main Text

Only the principal display text should be MIDI-switchable. Date, BPM, MIDI state, and Parameter information are runtime information rendered by Typography, not separate MIDI text choices.

#### Palette

Palette is planned as shared visual state across layers. Initial examples:

```text
Palette A
Main = Blue
Sub = Green
```

A provider injected into consumers is under consideration, but is not implemented or approved architecture yet. Model palette use means image processing after the Model RenderTexture, such as flat color, colored shadow, or posterized color. It does not mean directly mutating arbitrary 3D Materials.

#### Background

`background.pattern` is the existing Background Shader Pattern axis. Do not introduce a separate Background Effect concept.

#### Final Effect

Keep the concept for color adjustment or global processing after final composition. Its Parameter shape and implementation are not decided.

#### Visual Preset

A Visual Preset is a Macro / Safe Preset, not another multiplicative visual axis. It should apply values to several existing Parameters once; individual Parameters remain editable afterward.

## Beta target counts

```text
Model Source         : existing all models
Model Active         : 2
Model Motion         : 3 initially / 5-8 later
Camera Pose          : 3 initially / about 5 later
Background Pattern   : existing patterns
Model Texture Layout : 3 initially / 5-8 later
Typography Layout    : 3 initially / about 10 later
Main Text            : 3 initially / about 10 later
Palette              : 3 initially / 5-8 later
Texture Effect       : existing + about 3 initially / about 8 later
Visual Preset        : 3-5 initially
```

For unfinished axes, carry about three choices through the complete system first. Expand only axes that prove useful during actual VJ operation.

## APC concept

The final APC surface should operate all major live Parameters:

```text
Discrete Parameter
→ Grid Pad

Continuous FloatParameter
→ Fader

Visual Preset
→ Grid Pad
```

The final APC cheat sheet will be maintained in Google Sheets. Do not rush a complete mapping redesign before the beta Parameter set is stable. New Parameters should be registry-visible and bindable first; Scene pad allocation should be a separate explicit task.

## Latest completed task

Model Texture Layout and Typography Layout are independent Parameter axes. The former coupled
`poster.layout` remains as a local compatibility macro; no generic composition graph, manager,
or preset framework was added.

## Do not do

- Accent system
- generic composition graph
- node editor
- ScriptableObject preset framework
- large architecture refactor
- MIDI architecture refactor
- Parameter architecture refactor
- Prefs migration
- legacy cleanup
- unrelated cleanup
