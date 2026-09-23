# Rendering

## Output Flow

```text
ModelCamera
  ↓
PosterCamera
  ↓
TextureCompositor
  ↓
Preview / Syphon
```

`TextureCompositor` は enabled layer を bottom-to-top で ping-pong 合成する。PosterCamera の描画完了後に合成を始めることが必須である。

## Sources and Effects

`ITextureContainer` は texture source の contract である。`ProcessedTextureSource` は raw source と source-specific effect Parameter を所有し、1 frame 内では処理済み texture を再利用する。

effect は次の固定順で処理する。stage は並べ替えず、同一 stage で複数 effect が enabled の場合は
定義順に適用する。

```text
UV → Sampling → Color → Alpha
```

## Poster

`PosterLibrary.Documents` が Poster document の source of truth である。`PosterCanvas` は active document を Canvas primitive として描画し、font catalog と palette を参照する。

Grid placement は grid point に element anchor を合わせ、Width / Height を grid unit として解釈する。Free placement は normalized coordinate、anchor は 9-point anchor を使う。element list の先頭は前面である。

## Shared Visual Resources

`PaletteProvider` は shared palette catalog と active palette を所有し、rendering consumer へ提供する。`PosterFontCatalog` は Poster text 用の font を stable Font ID で管理し、Poster 保存データはその ID を保持する。

Legacy Text subsystem は Trip26 の最終 output には入らない。現在の text output は Poster document の Text element で扱う。
