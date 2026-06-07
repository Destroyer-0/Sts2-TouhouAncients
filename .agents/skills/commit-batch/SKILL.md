---
name: commit-batch
description: 分批提交 Git 暂存区内容。将大型暂存区分批提交为多个独立、语义清晰的本地提交，遵循项目规范提供中文提交信息。
---

# Commit Batch — 分批提交工作流

当暂存区有大量文件需要分批提交时使用此工作流。

## 前置条件

- 所有文件已 `git add` 到暂存区
- 当前分支无冲突

## 工作流

### 1. 检查暂存区内容

```powershell
# 查看暂存区概览
git diff --cached --stat

# 查看暂存区文件列表
git diff --cached --name-only

# 查看具体文件改动
git diff --cached -- scripts/relics/SomeRelic.cs
```

### 2. 按行数分类文件

```powershell
# 按改动行数区分：仅池子修改（2行）vs 有额外修改（>2行）
git diff --cached --stat -- scripts/relics/ | ForEach-Object {
    $parts = $_ -split '\s+\|'
    $file = $parts[0].Trim()
    $changes = $parts[1].Trim()
    if ($changes -match '^(\d+)') {
        $count = [int]$matches[1]
        if ($count -gt 2) {
            Write-Host "$count`t$file - 有额外修改"
        } else {
            Write-Host "$count`t$file - 仅池子修改"
        }
    }
}
```

### 3. 制定分批方案

按以下优先级分组：

| 批次类型 | 说明 |
|---------|------|
| **本地化** | `localization/zhs/*.json` 单独一批 |
| **图标资源** | `images/icon/*`, `images/potion/*`, `images/cards/*` 等资源文件 |
| **新增脚本** | 新增的 `.cs` 文件（卡牌、遗物等），按功能分组 |
| **修改的卡牌** | 修改的卡牌脚本，写出每张卡的详细更新内容 |
| **修改的先古之民** | Ancient 脚本修改（如按钮透明度、遗物池更新） |
| **核心脚本** | Entry/Config/Patch/基类 + 版本号 + 场景/杂项资源 |
| **仅改池子的遗物** | 所有只改 `[Pool]` 属性的遗物合为一批 |
| **有额外修改的遗物** | 每个遗物单独一批，写出详细更新内容 |

### 4. 提交信息格式

```powershell
# 本地化
git commit -m "i18n: 更新本地化文本"

# 图标
git commit -m "feat: 为附魔/能力/药水系统补充图标"

# 新增
git commit -m "feat: 新增卡牌「名称」及遗物「名称」「名称」"

# 修改卡牌（详细内容）
git commit -m "refactor: 修改卡牌「名称」「名称」
- 卡牌A：修复了XXX
- 卡牌B：重做——改为XXX"

# 先古之民
git commit -m "refactor: 优化先古之民的按钮透明度并更新遗物池"

# 核心脚本（写出每个文件的改动）
git commit -m "chore: 核心脚本更新
配置：修复XXX
补丁：重构XXX"

# 仅改池子的遗物
git commit -m "refactor: 将所有遗物池子从 Shared 改为 Event"

# 单个遗物修改
git commit -m "refactor: 遗物名称——具体修改内容"
```

### 5. 常用文件路径

#### 图标资源
```
images/icon/enchantment/       # 附魔图标
images/icon/power/             # 能力图标（含 BigIcon/ 子目录）
images/icon/relics/            # 遗物图标（含 IconLarge/ 子目录）
images/potion/                 # 药水图标
images/cards/                  # 卡牌肖像
images/icon/MapNode/           # 地图节点图标
```

#### 脚本目录
```
scripts/relics/{角色名}/       # 遗物
scripts/cards/                 # 卡牌
scripts/ancients/              # 先古之民
scripts/Enchantment/           # 附魔
scripts/powers/                # 能力
scripts/potions/               # 药水
scripts/Patches/               # Harmony 补丁
scripts/CmdUtils/              # 工具命令
```

### 6. 完整提交示例

```powershell
# 1. 本地化
git commit -m "i18n: 更新本地化文本" -- TouhouAncients/localization/zhs/*.json

# 2. 图标
git commit -m "feat: 为附魔/能力/药水系统补充图标" -- images/icon/enchantment/ images/icon/power/ images/potion/

# 3. 仅改池子的遗物
git commit -m "refactor: 将所有遗物池子从 Shared 改为 Event" -- scripts/relics/Kaguya/ scripts/relics/Marisa/ ...

# 4. 单个遗物行为修改
git commit -m "refactor: 封魔针——现在奥斯提的攻击也会享受封魔针的效果" -- scripts/relics/Reimu/SealingNeedle.cs
```
