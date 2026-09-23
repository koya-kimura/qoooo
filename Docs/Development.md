# Development

## Environment

- Unity `6000.3.12f1`
- Main Scene: `Assets/qoooo/Scenes/Trip26.unity`
- Render pipeline: URP

## Commands

```sh
uloop compile --timeout-seconds 180
git diff --check
git status --short
```

## Handoff

Codex は実装、compile、diff check、変更内容の handoff までを担当する。人間は Unity 上で Play、visual、MIDI を確認してから commit する。

## Change Boundaries

- Parameter を追加する場合、値を使う component が owner になり、stable ID を持つ。
- rendering source は texture と RenderTexture lifecycle の owner を明確にする。
- Scene、asset、shader を移動する場合は対応する `.meta` を維持する。
- 現行 architecture を変える変更では、[Architecture.md](Architecture.md) と該当する詳細文書を同じ変更で更新する。
