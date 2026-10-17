---
name: decompile-mod
description: 反查第三方 mod 的 DLL 实现（例如排查与其他 mod 的兼容性）。只要需要解包/反编译某个第三方 mod 的 DLL，就必须套用本 skill。严禁用于解包杀戮尖塔本体（sts2.dll），本体一律查 .agents/local.md 的 sts2-src。
---

# Decompile Mod — 第三方 Mod 反查

用来读**其他 mod** 的 C# 实现。用户让你查某个 mod 与本 mod 的兼容性、或对比它的实现时使用。

## 硬性规则（不可协商）

1. **DLL 路径只能由用户给出**。不得自己猜路径、不得遍历游戏目录 / 创意工坊目录找 DLL。
2. **解包本体是禁止的**：`sts2.dll`（含 `data_sts2_*\` 下任何程序集）永远不解包。要查本体，走 `.agents/local.md` 的 `sts2-src` 或 `docs/项目参考.md`。
3. **同一条命令最多执行 2 次**。第 2 次仍失败 → **停止**，报告「命令原文 + 两次的退出码 + 日志尾 50 行」，然后等用户指示。**禁止换参数、换工具、换输出目录再试**。
4. **日志只读最后 50 行**。脚本已代为读取并回显，直接使用脚本输出；禁止 `Get-Content` 全量、禁止反复读同一日志。
5. **只准写临时目录**。允许写入的只有 `%TEMP%\ta-decompile\`；游戏目录、mods 目录、解包脚本一律只读，不得修改或删除。
6. **单步超时 120 秒**，已写死在脚本里。不得自行去掉、调大或改用别的超时方式。
7. **不得修改** `.agents/skills/decompile-mod/scripts/decompile-mod.ps1`。需要未覆盖的场景时，停下来问用户。

## 唯一命令

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".agents/skills/decompile-mod/scripts/decompile-mod.ps1" -Dll "<用户给出的 DLL 绝对路径>" -OutName "<短名>"
```

- `-OutName`：字母/数字/`.`/`_`/`-`，如 `BaseLib`、`TouhouFolk`。一个 mod 固定用一个名字。
- 不要给脚本追加任何其它参数（没有 `-Reference`、没有 `-Type`、没有 `-Clean`）。

## 输出位置

| 内容 | 路径 |
|---|---|
| 反编译源码 | `%TEMP%\ta-decompile\<OutName>\src\`（按命名空间分目录，一型一文件：`src\<命名空间>\<类名>.cs`） |
| 完整日志 | `%TEMP%\ta-decompile\<OutName>\ilspycmd.log` |

脚本最后会打印 `exit=<code> files=<n> outdir=<路径>`，以及日志尾 50 行。

## 退出码

| 码 | 含义 | 下一步 |
|---|---|---|
| 0 | 成功 | 直接去 `src\` 检索 |
| 2 | `-OutName` 非法 | 改正名字（这不是解包失败，可以改） |
| 3 | DLL 不存在 | 报告用户，别自己找 |
| 4 | 命中的是本体程序集 | 停止，改走 `sts2-src` |
| 5 | 找不到 ilspycmd | 报告用户，别自行安装 |
| 6 | 输出目录已存在且非空 | 报告用户，由用户决定复用还是换名 |
| 7 | 没产出 `.cs` | 按「失败」处理 |
| 124 | 120s 超时 | 按「失败」处理 |

## 成功之后怎么查

- 先按文件名定位：`Get-ChildItem "$env:TEMP\ta-decompile\<OutName>\src" -Recurse -Filter "*<关键词>*.cs"`。
- 再 `grep` / `view` 具体文件，只读需要的那几段，不要整目录通读。
- 引用结论时写清来源：`<mod名>@<DLL路径>` + 文件名。

## 失败时怎么报告

```text
解包失败（第 N/2 次）
命令：<原样命令>
退出码：<code>
日志尾 50 行：
<脚本回显的内容>
```
报告完即停，等用户决定下一步。

## 收尾

- 临时目录默认**不要清理**（用户可能要继续看）；需要清理先问。
- 结论写进 `docs/项目参考.md` 时，注明是哪个 mod 的哪个版本。
