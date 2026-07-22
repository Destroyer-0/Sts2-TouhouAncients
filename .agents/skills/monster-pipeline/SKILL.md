---
name: monster-pipeline
description: "Full monster creation pipeline: Chinese monster design spec → parsed design doc → plan + code files + localization JSON. Orchestrates parse-monster-design and write-monster-files agents in serial, pausing for user confirmation at each stage. Use when: user provides a Chinese monster design spec and wants it fully implemented (C# class, encounter class, localization JSON)."
argument-hint: "Paste monster design spec (name, HP, buffs, skills, logic, encounter info)"
user-invocable: true
disable-model-invocation: false
---

# Monster Creation Pipeline

Full end-to-end pipeline that takes a Chinese monster design spec and produces complete C# class files + localization JSON entries. Each stage pauses for user review before proceeding.

## When to Use

- User pastes a monster design spec in the standard format
- User says "create a monster", "implement this monster design", "pipeline this monster"
- User wants the full parse→plan→code chain for a new monster

## Input Format

The user should provide a monster design in this format:

```
{怪物名称}
{HP}（{高进阶HP}）生命
默认buff：{Buff名称} {层数}（可选）
技能名称：
{技能A}：{描述}。
{技能B}：{描述}。
技能逻辑：{循环/随机/条件}
遭遇信息：（可选）
幕：{第几幕}
类型：{普通/精英/Boss}
怪物数量：{单怪物/多怪物}
```

Example:

```
测试怪物
10（12）生命
默认buff：领地意识1
技能名称：
撞击：造成3（4）点伤害。
吼叫：获得1力量。
技能逻辑：撞击->吼叫（循环）
遭遇信息：
幕：第1幕
类型：普通
怪物数量：单怪物
```

## Pipeline Stages

```
User input (design spec)
    │
    ▼
┌──────────────────────────────────────────────────────────┐
│ Stage 1: parse-monster-design                              │
│ Design spec → structured analysis (intent maps, state     │
│ machine, localization preview, TODO list)                  │
├──────────────────────────────────────────────────────────┤
│ Output: structured markdown with mapping tables + TODOs    │
│ Action: Ask user to review parsed design, answer TODOs     │
└──────────────────────────────────────────────────────────┘
    │ User confirms + answers TODOs
    ▼
┌──────────────────────────────────────────────────────────┐
│ Stage 2: write-monster-files                               │
│ Parsed design → plan (all files to create) → user review   │
│ → create C# classes + append localization JSON             │
├──────────────────────────────────────────────────────────┤
│ Output: Plan showing files to create, then actual files    │
│ Action: Ask user to confirm plan before writing            │
└──────────────────────────────────────────────────────────┘
    │ User confirms plan
    ▼
┌──────────────────────────────────────────────────────────┐
│ Stage 3: Files written                                     │
│ - scripts/monsters/{Name}.cs                               │
│ - scripts/encounters/{Name}Encounter.cs                    │
│ - [if custom power] scripts/powers/{Power}.cs              │
│ - TouhouAncients/localization/zhs/monsters.json (append)   │
│ - TouhouAncients/localization/zhs/encounters.json (append) │
│ - [if custom power] zhs/powers.json (append)               │
├──────────────────────────────────────────────────────────┤
│ Action: Summarize what was created, list any manual steps  │
│ (icon creation, tscn scene, Entry.cs registration)         │
└──────────────────────────────────────────────────────────┘
```

## Stage 1: Parse Design

1. Collect the monster design spec from the user.
2. Invoke the `parse-monster-design` agent via `runSubagent`:

```
runSubagent(
  agentName: "parse-monster-design",
  description: "Parse monster design",
  prompt: "解析以下怪物策划案：

[PASTE RAW DESIGN SPEC HERE]"
)
```

3. Present the parsed output to the user.
4. **Ask the user to confirm**:
   - Class name and localization keys
   - HP values (especially Max if not specified)
   - Any unrecognized buff types
   - Missing info from the TODO list
5. Collect user's answers to all TODO items. Do NOT proceed until the user confirms.

## Stage 2: Generate Code Plan

1. Feed the **confirmed parsed design + user's TODO answers** to the `write-monster-files` agent:

```
runSubagent(
  agentName: "write-monster-files",
  description: "Plan monster code",
  prompt: "根据以下【已确认的】解析结果生成计划（不要写文件）：

[PASTE CONFIRMED PARSED DESIGN + TODO ANSWERS HERE]"
)
```

2. The agent will output a plan listing all files to create.
3. **Ask the user to confirm the plan** before any files are written.
4. If there are custom powers, confirm the power name, type, and stack type.

## Stage 3: Write Files

1. After user confirms the plan, invoke `write-monster-files` again with instruction to write:

```
runSubagent(
  agentName: "write-monster-files",
  description: "Write monster files",
  prompt: "确认计划，生成所有文件。以下是已确认的计划：

[PASTE CONFIRMED PLAN HERE]

请创建所有 .cs 文件并追加所有 JSON 条目。"
)
```

2. After files are written, summarize:
   - Files created (with paths)
   - Files modified (with paths)
   - Manual steps remaining:
     - [ ] Create icon at `res://images/icon/power/{Power}.png` (if custom power)
     - [ ] Create tscn scene for monster (in Godot editor)
     - [ ] Create tscn scene for encounter (if multi-monster)
     - [ ] Register in `Entry.cs` (if any class has `[SavedProperty]`)

## Constraints

- Always use BaseLib
- Default ModPrefix: `TOUHOUANCIENTS`
- NEVER write files without user confirmation at each stage
- If the design has missing encounter info, Stage 1 TODO will catch it — do not proceed without answers
