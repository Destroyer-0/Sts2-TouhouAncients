---
name: deslop
description: >-
  Rewrites a document into plain Simplified Technical English or plain
  zh-Hans, then scores the draft with the bundled linter. Use only when the
  user attaches this skill or names deslop, and asks to deslop, rewrite, or
  clean a document, README, runbook, release note, or PR description.
  English follows the mechanical ASD-STE100 rules. Chinese follows the
  zh-Hans plain-language rules.
disable-model-invocation: true
---

# Deslop a document

Condensed from [asd-ste100](https://github.com/woosal1337/blog/tree/main/videos/ep01-the-cure-for-ai-slop) 2.0 by Ege Chelebi (MIT, see [LICENSE](LICENSE)). English rules are the mechanical subset of ASD-STE100 Issue 9. The zh-Hans rules are a local parallel. They are not part of the standard.

Unofficial. Not affiliated with ASD. ASD-STE100 is a registered EU trademark (No. 017966390). The full standard is free at https://asd-ste100.org. Do not paste it.

## When this applies

The user attached this skill or named it. Deslop the document they point at.

Leave code, identifiers, command syntax, and log output unchanged. Write the rewrite in the document's language. Do not translate. Talk to the user in the language they used.

Out of scope for a voice piece (essay, blog, video script, ad copy): say so, and stop, unless they still ask you to deslop it. Then remove only the mechanical slop below. Do not flatten the voice.

## Language and mode

- English prose. Follow Layer 1. Lint without `--zh`.
- zh-Hans prose. Follow [zh-hans.md](references/zh-hans.md) and the phrase list in [zh-hans-ban.md](references/zh-hans-ban.md). Lint with `--zh`. A Chinese document auto-detects when it has at least 40 Han characters and those characters outnumber English words two to one. Short Chinese still needs `--zh`.
- Both languages in one file. Clean each language in place. Lint with `--zh` when the prose is Chinese.

**Flavored** is the default. READMEs, reference docs, release notes, PR text, wiki pages.

**Strict** is for procedures, runbooks, safety text, and error messages. Add `--strict`. Strict English also uses [ste-recurring-errors.md](references/ste-recurring-errors.md).

## What to do

Default is **rewrite**. Keep every fact, number, condition, and scope qualifier. Change the smallest span that breaks a rule. If the text already complies, leave it and say so.

If the user says **review**, do not rewrite. Output a table `Rule | Original | Simplified`, one row per violation, then one line on anything you left alone.

1. Read the document.
2. Rewrite the prose.
3. Lint from this skill's directory. Fix the reported categories. Lint once more. Two passes, then stop.
4. Put the score with the text. Do not call the text clean without a lint run.

```
python scripts/ste-lint.py --fail-over 2.5 FILE
python scripts/ste-lint.py --strict --fail-over 1.5 FILE
python scripts/ste-lint.py --zh --fail-over 2.5 FILE
python scripts/ste-lint.py --zh --strict --fail-over 1.5 FILE
```

If `python` is missing, use `py -3`. Add `--json` for the category breakdown. `--shape` is the English reply-shape check. It stays out of the score, and the zh-Hans score ignores it.

The score is violations per 100 words (English) or per 100 Han characters (zh-Hans). Lower is cleaner. Flavored target: under 2.5. Strict target: under 1.5.

## Layer 1 — English words

Numbers in parentheses are ASD-STE100 Issue 9 rule numbers.

- One name for one thing (1.11). Do not rotate check / verify / validate / confirm. Pick one.
- Short word: start, use, help, make sure, do, give, show, before, after, about, get, also. The linter's flavored ban list includes utilize, leverage, facilitate, ensure, provide, additionally, furthermore, moreover, in order to, prior to.
- No marketing adjectives: seamless, robust, powerful, cutting-edge, effortless, world-class, next-generation, revolutionary, and the rest of the linter's marketing list.
- American spelling (1.14).
- Active voice. "The parser reads the file." A past participle used as an adjective is fine: "the valve is closed" (3.3).
- Simple tenses only (3.2). "We received the report." No present perfect. No stacked auxiliaries (3.4).
- A verb for an action (3.7). "Analyze the log." No "-ing" main verb where a simple tense works (3.5).
- No phrasal verbs (9.3): spin up, dive into, kick off, roll out.
- One instruction per sentence (5.2). Max 20 words for an instruction (5.1), 25 for a descriptive sentence (6.3). The linter flags anything over 20. Keep a longer sentence when splitting would drop a fact, and flag it.
- Condition before command, with a comma (5.4): "If the test fails, read the log."
- Do not drop words to compress (4.2). No contractions. Use an article where the sentence needs one (4.5). A label stays a label. Do not expand a label into a sentence only to add an article.
- Plain connectors: then, but, thus, as a result (4.4).
- A multi-word noun has at most three words (2.1). Unpack a longer cluster.
- Define an abbreviation at first use, then use the abbreviation.
- No semicolons (8.1). Two sentences. An em dash is a slop marker. Replace it with a period when you are deslopping.
- One topic per paragraph (6.5), six sentences maximum (6.6). Steps: a numbered list, one imperative action per item.

Strict adds the word set in [ste-recurring-errors.md](references/ste-recurring-errors.md). The linter's `--strict` flag counts however, since, may, should, shall, using, follow, and em dashes. Safety text: WARNING means injury, CAUTION means damage, NOTE means information and is never an instruction (7.1). Put it on the line before the step it protects.

## Layer 2 — task, issue, or PR description only

Skip this layer for a reference doc, README, or release note.

- First line: the next action, command, or path.
- Number a multi-step task. One action per step. Cap an action list at five items. A reference table has no cap.
- No preamble, no recap, no closer.
- Concrete estimates: minutes, hours, or days.
- An error states the cause, then the fix.
- Finish one issue. Offer the next one as its own question.

## Guards

- Never drop a fact, number, condition, or scope qualifier to hit a length cap.
- Preserve identifiers, part numbers, units, error strings, and safety wording.
- Change the smallest span that fixes a violation.
- This skill fixes the form. It cannot make a hollow paragraph true.

## Checklist when the linter cannot run

English: sentence length, semicolon, contraction, present perfect, passive with a known actor, nominalization, phrasal verb, noun cluster of four or more, dropped article, two names for one thing.

zh-Hans: the replace-table and the judgment list in [zh-hans.md](references/zh-hans.md). Sentence over 40 Han characters. Semicolon. Em dash. 进行 + a noun. Stacked modal.

## Examples

English rewrite:

```
In order to ensure a seamless experience, the parser has been utilized to facilitate comprehensive validation.
```

```
The parser checks every field. It uses one rule set.
```

zh-Hans rewrite:

```
在当今数字化时代，本文旨在赋能研发团队。我们对日志模块进行了全面的优化，显著提升了查询效率，打造了无缝的排查体验。综上所述，希望对您有所帮助。
```

```
我们优化了日志模块的查询路径。查询更快。
```

The second sentence keeps the speed claim and drops 显著, because the source gave no number.
