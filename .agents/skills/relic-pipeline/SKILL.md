---
name: relic-pipeline
description: "Full relic localization pipeline: plain Chinese relic specs → formatted zhs JSON → eng translation → jpn copy. Orchestrates format-relic-text, write-relic-loc, and translate-relic-loc agents in serial, pausing for user confirmation between each stage. Use when: user provides raw Chinese relic designs and wants them fully integrated into all three localization files."
argument-hint: "Paste raw Chinese relic specs (Name\tDescription per line)"
user-invocable: true
disable-model-invocation: false
---

# Relic Localization Pipeline

Full end-to-end pipeline that takes raw Chinese relic design text and produces complete zhs/eng/jpn localization entries. Each stage pauses for user review before proceeding.

## When to Use

- User pastes plain relic specs like `名称\t描述`
- User says "add a new relic", "create relic localization", "pipeline this relic"
- User wants the full zhs→eng→jpn chain

## Pipeline Stages

```
User input (plain text)
    │
    ▼
┌──────────────────────────────────────────────────────┐
│ Stage 1: format-relic-text                            │
│ Plain text → annotated JSON (TODO keys + {Value})     │
├──────────────────────────────────────────────────────┤
│ Output: JSON block shown to user                      │
│ Action: Ask user to review and confirm                │
└──────────────────────────────────────────────────────┘
    │ User confirms
    ▼
┌──────────────────────────────────────────────────────┐
│ Stage 2: write-relic-loc                              │
│ Resolve TODO keys + {Value} → write to zhs/relics.json│
├──────────────────────────────────────────────────────┤
│ Output: proposed key/var names + insertion position   │
│ Action: Ask user to confirm before writing            │
└──────────────────────────────────────────────────────┘
    │ User confirms → written to zhs
    ▼
┌──────────────────────────────────────────────────────┐
│ Stage 3: translate-relic-loc                          │
│ Find new zhs entries → translate → write to eng       │
├──────────────────────────────────────────────────────┤
│ Output: translated JSON block + insertion position    │
│ Action: Ask user to confirm before writing            │
└──────────────────────────────────────────────────────┘
    │ User confirms → written to eng
    ▼
┌──────────────────────────────────────────────────────┐
│ Stage 4: Copy eng → jpn                               │
│ Copy new entries from eng/relics.json to jpn/relics.json│
├──────────────────────────────────────────────────────┤
│ No translation needed — content is identical to eng   │
│ Action: Run PowerShell copy, then validate            │
└──────────────────────────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────────────────────────┐
│ Stage 5: Validation                                   │
│ Run validate-relic-loc.ps1                            │
├──────────────────────────────────────────────────────┤
│ Checks: JSON syntax, duplicates, missing keys,        │
│ sub-keys, variable consistency                        │
└──────────────────────────────────────────────────────┘
```

## Stage 1: Format Raw Text

1. Collect the raw Chinese relic text from the user (tab-separated `Name\tDescription` or free text).
2. Invoke the `format-relic-text` agent via `runSubagent`:

```
runSubagent(
  agentName: "format-relic-text",
  description: "Format relic text",
  prompt: "Format the following plain Chinese relic specs into STS2 annotated JSON:
[PASTE RAW TEXT HERE]
Return only the complete JSON blocks."
)
```

3. Show the output JSON to the user.
4. **Ask the user**: "Does the formatted JSON look correct? Any changes needed before writing?"
5. Only proceed to Stage 2 after user confirms.

## Stage 2: Write to zhs/relics.json

1. Invoke the `write-relic-loc` agent via `runSubagent`:

```
runSubagent(
  agentName: "write-relic-loc",
  description: "Write relic to zhs",
  prompt: "Write the following formatted relic JSON into TouhouAncients/localization/zhs/relics.json:
[PASTE STAGE 1 OUTPUT HERE]
Present the resolved keys, variable names, and insertion position. Wait for my confirmation before writing."
)
```

2. The agent will present proposed changes and ask for confirmation.
3. After the agent writes and validates, proceed to Stage 3.

## Stage 3: Translate to eng/relics.json

1. Invoke the `translate-relic-loc` agent via `runSubagent`:

```
runSubagent(
  agentName: "translate-relic-loc",
  description: "Translate relic to eng",
  prompt: "Find newly added relic entries in zhs/relics.json that are missing from eng/relics.json, translate them to English, and write them into eng/relics.json. Present translations for confirmation before writing."
)
```

2. The agent will present translations and ask for confirmation.
3. After the agent writes and validates, proceed to Stage 4.

## Stage 4: Copy eng → jpn

After eng/relics.json is updated, copy the newly added entries to `TouhouAncients/localization/jpn/relics.json`.

**This is a content copy, NOT a translation.** The jpn file uses English text (same as eng).

Procedure:
1. Find the lines that were added to eng/relics.json (the new entries from Stage 3).
2. Use PowerShell to append them to jpn/relics.json at the corresponding position:

```powershell
# Find the insertion point in jpn/relics.json (mirror eng structure)
# Use the same replace_string_in_file approach as write-relic-loc did for eng
```

3. Ensure proper JSON formatting — the jpn file must remain valid JSON with no duplicate keys.
4. After writing, run validation:

```powershell
. ".agents\skills\touhou-ancients-mod\scripts\validate-relic-loc.ps1"
```

But note: the validation script checks zhs vs eng. For jpn, also verify:
```powershell
# Quick check that jpn is valid JSON
try { $null = Get-Content "TouhouAncients\localization\jpn\relics.json" -Raw | ConvertFrom-Json; Write-Host "[PASS] jpn/relics.json - valid JSON" } catch { Write-Host "[FAIL] jpn/relics.json" }
```

## Stage 5: Final Validation

Run the full validation script:

```powershell
. ".agents\skills\touhou-ancients-mod\scripts\validate-relic-loc.ps1"
```

Also check jpn:
- Valid JSON syntax
- Same base keys as eng
- No duplicate keys

---

## Summary of the Full Flow

| Stage | Agent | Output File | Confirms? |
|-------|-------|-------------|-----------|
| 1 | `format-relic-text` | (output only, not written) | Yes |
| 2 | `write-relic-loc` | `zhs/relics.json` | Yes |
| 3 | `translate-relic-loc` | `eng/relics.json` | Yes |
| 4 | (manual copy) | `jpn/relics.json` | Auto |
| 5 | (validation script) | (checks all 3 files) | Auto |

## Constraints

- DO NOT skip any user confirmation stage
- DO NOT copy eng→jpn until eng is fully written and confirmed
- DO NOT proceed if validation fails — fix issues first
- If any stage fails, stop and report the error before continuing
