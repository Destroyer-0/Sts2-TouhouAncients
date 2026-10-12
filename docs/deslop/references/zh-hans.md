# zh-Hans plain language

Use this when the document's prose is Simplified Chinese. Do not translate.
Keep every fact, number, condition, name, and identifier.

These rules are a parallel to the English mechanical rules. They are not
ASD-STE100.

Before a rewrite, read [zh-hans-ban.md](zh-hans-ban.md). That file is the
phrase list. The linter (`python scripts/ste-lint.py --zh`) flags the rows
marked there, plus a sentence over 40 Han characters, a semicolon, an em
dash, and a paragraph over six sentences.

## Sentences

- One instruction per sentence. Cap every sentence at 40 Han characters.
  A longer sentence that holds a fact stays. Flag it. Do not cut the fact.
  A catalog line, an option label, and a list of names may exceed the cap.
  Leave the line.
- Active voice. Write 解析器读取文件. Avoid 文件被解析器读取 when the actor is known.
- One name for one thing. Do not rotate 检查 / 校验 / 验证 / 确认 for the same action.
- Use the verb. 分析日志, not 进行分析. 优化查询, not 进行优化.
- No stacked modals. 可以 or 必须, not 可能可以, 或许能够, 应该可以, 不妨可以.
- No semicolon. Write two sentences. Replace —— and — with a period.
  A semicolon inside an option label can stay.
- One topic per paragraph. Six sentences maximum. A paragraph of short facts
  can stay when splitting it would only add headings.
- Steps are a numbered list. One action per item. Imperative. Condition first: 如果测试失败，打开日志。
- A list label stays a label. Do not stretch it into a sentence.
- No decorative emoji in prose.

## Strict mode

Procedures, runbooks, safety text, and error messages. The linter adds these with `--zh --strict`.

| Do not write | Write |
|---|---|
| 应该 / 应当 | 必须 for an obligation. 可以 for a permission |
| 或许 / 尽量 / 最好 | 必须, 可以, or a condition |

Safety labels, when the document uses them:

- 警告 — risk of injury
- 注意 — risk of damage
- 说明 — information only, never an instruction

Put the label on the line before the step it protects. Start with the command or the condition, then the risk.
