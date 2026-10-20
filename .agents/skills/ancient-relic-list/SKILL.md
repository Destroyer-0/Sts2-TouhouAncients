---
name: ancient-relic-list
description: 把先古之民的遗物导出成 docs/THA-Relic.md 那种 markdown 清单（Tab 缩进分「行 / 遗物 / 效果」三层）。用于快速查看某一位或全部先古之民的遗物与衍生牌、关键词，或重新生成 docs/THA-Relic.md。
---

# Ancient Relic List — 先古之民遗物清单

把某位（或全部）先古之民的选项池，按池顺序导出成固定格式的 markdown：遗物文案取 `zhs/relics.json`，数值取遗物 `.cs` 的 `CanonicalVars`，衍生牌/关键词由代码引用与文案色标推导。

## 唯一命令

```powershell
python ".agents/skills/ancient-relic-list/scripts/relic_list.py" [先古之民] [--out 路径] [--list] [--lang zhs|eng]
```

- `先古之民`：中文名片段（`魔理沙`）、类名（`KirisameMarisa`）、文件名（`KirisameMarisaAncient`）都行；**留空 = 全部**。
- `--out 路径`：写文件（相对路径按仓库根解析）；缺省打印到 stdout。
- `--list`：列出所有先古之民（文件名 + 中文名 + 称号）。
- `--lang`：本地化语言，默认 `zhs`；`eng` 用 `TouhouAncients/localization/eng` + 本体 `localization/eng`。
- 输出中文靠 stdout，终端显示成乱码是控制台编码问题，写文件不受影响。

常见用法：

```powershell
# 看一位
python ".agents/skills/ancient-relic-list/scripts/relic_list.py" 魔理沙
# 重新生成整份文档
python ".agents/skills/ancient-relic-list/scripts/relic_list.py" --out "docs/THA-Relic.md"
```

## 输出格式

```markdown
# 雾雨 魔理沙

> 普通的高塔魔法使

- 第一行
	- 迷你八卦炉
		- 回合结束时，消耗1~3张手牌。若消耗的牌不小于1张，下个回合开始时获得1能量。
	- 恋色手电筒
		- 拾起时，将1张极限火花加入你的牌组。
		- 极限火花：攻击。耗能X。对所有敌人造成7点伤害X+1次。如果X的最终数值为4或以上，则将X的数值翻倍（翻三倍）并获得1能量。
```

- 层级 = **Tab + `-`**，不是字面 `--` / `---`（`--- 文字` 不是合法列表项，渲染时会被并进上一行）。
- `# 名称`、`> 称号` 后各留一个空行，否则称号那行会把整个列表当自己的续行吞掉。
- 单池角色（猯藏 / 阿求 / 帝 / 正邪 / 贝子）平铺不标行；多池角色标 `- 第一行`…；**同一池产出 N 个选项时**占 N 行，写成 `- 第一行/第二行`（如辉夜）。

## 推导规则

| 内容 | 来源 |
|---|---|
| 遗物文案 | `zhs/relics.json`，`{占位符}` 用该遗物 `CanonicalVars` 的实际数值替换，富文本标签去掉 |
| 选项池 / 行 | `scripts/ancients/*.cs` 的 `TARelicOptionPools`；已被注释掉的 `TARelicOption<X>()` 不算 |
| 衍生牌 | 遗物 `.cs` 里的泛型引用（`FromCard<X>` 等）+ 文案里带色标的名称 |
| 牌的类型/费用 | 卡牌 `.cs` 的 `energyCost` / `CardType`，缺了就读 `: base(...)`；`HasEnergyCostX` → 耗能X |
| 关键词 | 卡牌 `CanonicalKeywords`，中文名优先取 `.agents/local.md` 的 `sts2-src` 里原版 `card_keywords.json`，mod 关键词取本 mod 的 |
| 升级差异 | `OnUpgrade` 的 `UpgradeValueBy` → `基础（升级）`；`AddKeyword`/`RemoveKeyword` → `（保留+）`/`（消耗-）` |
| 休息处选项 | `zhs/rest_site_ui.json` 的 `OPTION_*.name`，要求遗物文案同时出现该名字与「休息处」 |
| 药水 | 本 mod `zhs/potions.json` 里被遗物文案提到的（原版药水不列） |
| 侵蚀（苦痛） | **只看代码**：`HoverTipFactory.FromAffliction<T>` / `ModelDb.Affliction<T>()` / `CardCmd.Afflict<T>(card, n)`（mod 类名 → `zhs/afflictions.json`，原版类名 → `sts2-src` 的 `afflictions.json`）。文案里的同名字不算数（饕餮尤魔的「吞噬遗物」是动词）；`{Amount}` 只认代码里的那个实参（例：哔哩电池流电 6、伪灵异珠沉重 1） |
| 原版卡牌 / 附魔 | 代码 hover 里出现的原版牌与附魔也列进衍生条目（正文取本体 `cards.json` / `enchantments.json`，类型费用关键词取本体源码；网页端**不给卡图**）。原版能力/药水不列，避免噪音 |
| 卡牌自身 hover | 衍生卡自己的 hover 提示也算（例：仆从牌 → 原版「小刀」「晕眩」，心之瞳 → 原版附魔「本能」） |
| 关键词说明 | 上面那张关键词表（`CanonicalKeywords` + 运行时 `AddKeyword` 全文扫描）+ 代码 `FromKeyword(...)` 悬停提示，除 消耗/虚无/保留 外都会进网页衍生条目 |
| 升级差异 | `OnUpgrade` 的 `UpgradeValueBy` / `Add|RemoveKeyword` / `EnergyCost.UpgradeBy`；网页给每个卡牌条目输出 `htmlUp` / `metaUp`（升级减费时费用标绿）/ `keywordsUp`，前端每张牌各自的「显示升级后」开关切换 |
| 语言 | `set_language("zhs"|"eng")` 切换 mod 与本体本地化，并重建关键词/苦痛/标题索引、覆盖表、类型名与行标签；`ENERGY_WORD` / `COST_WORD` / `REST_SITE_WORD` 控制文案里的中英措辞 |

- **原版卡牌 / 关键词不做衍生条目**（晕眩、孢子心灵、华彩之类直接跳过），只有本 mod 的牌、附魔、能力、药水才列。
  例外：文案里明确提到的**原版诅咒/卡牌**（如「贪婪」，本体只有标题无正文，正文写在 `BASE_CARD_LINES`）要列出来。
- `relic_refs(src, raw, owner)`：代码泛型名有歧义时（例：mod 附魔 `Greed` vs 原版诅咒牌 `Greed`）用 `REF_OVERRIDE[(遗物类名, 泛型名)]` 显式指定，不要靠猜。
- 通用对话：`ancients.json` 的 `talk.ANY.N-M.ancient`（按 N 分段、段内按 M 排序，`.speaker` 决定差分头像）只在网页端用，markdown 不输出。
- 多语言：`set_language()` 重载两套本地化；网页由前端按「`?lang=` → `localStorage` → 浏览器语言」决定加载 `data.js` 还是 `data.en.js`。
- 衍生牌格式：`名称：类型。耗能N。关键词。效果`，效果里的升级差异写在 `（XX）` 里。
- 附魔 / 能力 / 药水 / 关键词 / 休息处选项格式：`名称：效果`。
- 仅用于 UI 提示的动态块（`{InCombat:…}`）会删掉；附魔层数这类运行时数值写作 `X`。

## 值算不出来时

`relic_list.py` 顶部有 6 张覆盖表（`RELIC_OVERRIDE`、`CARD_OVERRIDE`、`DERIVED_OVERRIDE`、`REST_OVERRIDE`、`KEYWORD_LINES` / `EXTRA_LINES`、`REF_OVERRIDE`），只在**静态解析不出**（值来自 `SavedProperty`、运行时属性、动态拼字符串）时使用：

- `DERIVED_OVERRIDE[类名] = "整行"`，`None` 表示这条不要。
- `CARD_OVERRIDE[类名] = {"title": …, "body": …, "bodyUp": …}`：正文可带真实换行（网页分行显示），类型/费用/关键词仍从 `.cs` 读，不要写进正文。
- 覆盖表按语言分开：`_RELIC_OVERRIDE` / `_CARD_OVERRIDE` / `_DERIVED_OVERRIDE` / `_REST_OVERRIDE` / `_EXTRA_LINES` / `_KEYWORD_LINES` 都是 `{"zhs": {...}, "eng": {...}}`，只影响网页显示，**不改 `localization/**`**。
- 新增覆盖时写清「为什么算不出来」，别把能用 `CanonicalVars` 推出来的条目也塞进来。

## 硬性规则

1. **不要手改 `docs/THA-Relic.md`**：改脚本事后重跑命令覆盖生成，手改会被下次重跑冲掉。
2. **文案的增删改要先问用户**：脚本只做占位符替换与去标签，不得改写游戏文案本身（`relics.json` / `cards.json` 等一律只读）。
3. 只读仓库文件；不改 `scripts/`、不动 `.agents/local.md`。
4. 生成后建议自查：`Compare-Object` 与旧文件对比、或 `python -c "import markdown"` 渲染确认没有 `<hr>`、列表能正常嵌套。
5. 新增了一批遗物/卡牌后重跑即可；若出现 `{占位符}` 残留，按上一节补覆盖表而不是手改输出。
