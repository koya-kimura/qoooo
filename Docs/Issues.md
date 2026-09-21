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

| Status | Meaning |
| --- | --- |
| `inbox` | Playtest中の気づき。まだ仕様整理前 |
| `ready` | 実装内容・完了条件が明確 |
| `in_progress` | 実装中 |
| `verify` | 実装済み。人間のUnity確認待ち |
| `done` | 人間による確認完了 |
| `hold` | 今回は実装しない |

---

## Priority

| Priority | Meaning |
| --- | --- |
| `P0` | 本番までに必須 |
| `P1` | かなり欲しい |
| `P2` | 余裕があれば |

---

## Type

| Type | Meaning |
| --- | --- |
| `BUG` | 壊れる、表示されない、意図しない挙動 |
| `CONTROL` | MIDI、UI、操作性 |
| `VISUAL` | 見た目、構図、表現 |
| `WANT` | 新しく欲しい機能・表現 |

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
ここから下へIssueを追加する。

Issue IDは原則、

ISSUE-001
ISSUE-002
ISSUE-003

のように連番にする。

Playtest中は完璧に書く必要はない。
Observedだけ書いてもよい。
-->

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
