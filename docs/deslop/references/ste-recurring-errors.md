# The 39 recurring errors

ASD-STE100 Issue 9 lists the words writers reach for on autopilot, with the
approved replacement for each. Approved replacements are UPPERCASE, the way
the dictionary prints them. Some replacements change the part of speech
(`check` as a verb becomes CHECK the noun: "do a check"). Those need a
different sentence, not a word-for-word swap.

Use this list in **strict** mode. Flavored mode leaves it alone, except the
ten software entries at the bottom, which the linter already flags.

| # | Do not write | Write |
|---|---|---|
| 1 | acceptable (adj) | PERMITTED |
| 2 | alternate (adj) | ALTERNATIVE |
| 3 | any (adj) | none, or a different sentence construction |
| 4 | avoid (v) | PREVENT |
| 5 | both (adj) | THE TWO |
| 6 | check (v) | CHECK (n) — "do a check" |
| 7 | cover (v) | COVER (n) |
| 8 | complete (adj) | COMPLETED |
| 9 | damage (v) | DAMAGE (n) |
| 10 | ensure (v) | MAKE SURE |
| 11 | fit (v) | INSTALL |
| 12 | follow (v) | OBEY |
| 13 | further (adj) | MORE |
| 14 | further (adv) | MORE |
| 15 | have to (v) | an action verb in the imperative form |
| 16 | however (adv) | BUT |
| 17 | insert (v) | PUT |
| 18 | main (adj) | PRIMARY |
| 19 | may (v) | CAN |
| 20 | need (v) | NECESSARY (adj) |
| 21 | now (adv) | AT THIS TIME |
| 22 | old (adj) | REMAINING, USED, EXPIRED |
| 23 | over (prep) | ABOVE, ON, ALONG |
| 24 | people (n) | PERSON, PERSONNEL |
| 25 | perform (v) | DO |
| 26 | portion (n) | PART |
| 27 | press (v) | PUSH |
| 28 | reach (v) | GET |
| 29 | repeat (v) | DO … AGAIN |
| 30 | required (v) | NECESSARY (adj) |
| 31 | rotate (v) | TURN |
| 32 | secure (v) | ATTACH, SAFETY |
| 33 | shall (v) | MUST |
| 34 | should (v) | MUST |
| 35 | since (conj) | BECAUSE |
| 36 | test (v) | TEST (n) |
| 37 | therefore (adv) | THUS, AS A RESULT |
| 38 | under (prep) | BELOW, IN, LESS THAN |
| 39 | using (v) | USE, WITH |

## The ten that show up in software docs

The linter flags these in flavored mode too (`ensure`, `perform`, and the
banned list in `scripts/ste-lint.py`). The others wait for `--strict`.

- **ensure → make sure**
- **however → but**. **therefore → thus / as a result**
- **since → because**. "since" can mean time or cause.
- **may → can**. **should / shall → must**. Optional is "can". Required is "must".
- **perform → do**. "perform an analysis" becomes "analyze".
- **using → use / with**. "Run the command with the CLI."
- **check / test as verbs**. Strict STE makes them nouns ("do a check"). In flavored software prose, keep one verb and do not rotate check / verify / validate / confirm.

The full standard is free at https://asd-ste100.org. This list is a short excerpt. It is unofficial and not affiliated with ASD.
