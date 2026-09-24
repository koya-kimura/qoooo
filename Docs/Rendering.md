# Rendering

## Output Flow

```text
ModelCamera
  ↓
PosterCamera
  ↓
TextureCompositor
  ↓
Output ProcessedTextureSource
  ↓
Preview / Syphon
```

`TextureCompositor` は enabled layer を bottom-to-top で ping-pong 合成する。PosterCamera の描画完了後に合成を始め、直後に Output source を更新することが必須である。

## Sources and Effects

`ITextureContainer` は texture source の contract である。`ProcessedTextureSource` は raw source と source-specific effect Parameter を所有し、1 frame 内では処理済み texture を再利用する。最終 Output も同じ処理を使い、Parameter ID は `effect.output.*` とする。

effect は次の固定順で処理する。stage は並べ替えず、同一 stage で複数 effect が enabled の場合は
定義順に適用する。

```text
UV → Sampling → Color → Alpha
```

最終 Output はAPC Fader用に、Tiling、Mosaic、Block Offset、Chromatic Offset、Bloom、Invert、
Posterize、Grain、Echoの9つの独立したintensity Parameterを持つ。値域は`0..1`で、`0`ではeffect passを
実行しない。値を上げるとeffect固有の量と原画からのmixが増えるため、複数Faderを同時に上げて
effectを重ねられる。

## Poster

`PosterLibrary.Documents` が Poster document の source of truth である。`PosterCanvas` は active document を Canvas primitive として描画し、font catalog と palette を参照する。

`PosterLibrary.MainTexts` は空でない可変長listとしてmain text本文を所有する。`poster.main-text` のoption indexは現在のlist positionと一致し、追加、削除、並べ替え後もMIDI mappingはposition basedで扱う。

Poster Text の `Character Scramble` animation は、各文字に決定的なbeat offsetを与え、`Interval Beats`ごとの`Active Beats`区間で`Steps`回、元文字列内の文字へ切り替える。`Seed`が同じなら同じbeatで同じ表示になり、文字列長は変えない。

Poster Text の `Character Spacing` はfont size基準のem値で保存する。`0`はfont既定、正値は文字間を広げ、負値は詰める。描画時にfont内部単位へ変換するため出力解像度には依存しない。

Poster Text のLiteralとMain Textは複数行入力に対応する。TextArea内のEnterによる改行に加え、文字列中の `\n` も描画時に改行へ変換する。Character Scrambleは空白と改行の位置を変更せず、Typewriterは改行を含むTMP文字列の順序で表示する。

Grid placement は grid point に element anchor を合わせ、Width / Height を grid unit として解釈する。Free placement は normalized coordinate、anchor は 9-point anchor を使う。element list の先頭は前面である。

## Shared Visual Resources

`PaletteProvider` は shared palette catalog と active palette を所有し、`Main` / `Sub` / `Base` / `Accent` の4 roleを同じ名前でrendering consumerへ提供する。`Main` は主役色、`Sub` は補助色、`Base` は背景と本文の基準色、`Accent` は限定的な強調色として扱う。Posterのcolor参照も同じrole名を使い、保存値はこの順のslot positionとして扱う。

`ModelController` は名前付きanimation preset listと `model.animation-preset` Radioを所有する。各presetはanimation entry listを持ち、選択中のlistだけがmodel transformへ適用される。preset option indexはlist positionと一致し、MIDI mappingはposition basedで扱う。preset内容はQoooo Saveで保存する。

`PosterFontCatalog` は Poster text 用の font を stable Font ID で管理し、Poster 保存データはその ID を保持する。

Legacy Text subsystem は Trip26 の最終 output には入らない。現在の text output は Poster document の Text element で扱う。
