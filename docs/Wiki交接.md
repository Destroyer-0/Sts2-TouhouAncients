# Wiki 图鉴交接说明

> 任务：把先古之民与遗物信息做成可直接分享的网页 wiki。
> 状态：**网页、exe 工具、数据生成均已完成并逐项实测；剩余只有 GitHub Pages 配置与推送（用户自己操作）。**
> 更新于 2026-10-19 晚。数据现状：18 位先古 / 192 件遗物 / 120 条衍生说明。

---

## 1. 目标与需求（用户原话归纳）

- 首页：先古之民的**头像 + 名称 + 称号 + 出现层数**，按「1 层 → 2 层 → 3 层 → 2/3 层」排序。
- 点开某位先古：**MapNode 节点图 + 名称后的头像 + 全部遗物**（按「第一行 / 第二行…」分组）；层数/遗物数下方随机显示一条该先古的**通用对话**（多句分行；女苑、紫苑按说话者显示差分头像）。
- 遗物行：**图标（缺图用 `default`）+ 名称 + 说明 + 小字 flavor**；说明保留游戏富文本颜色。
- 有衍生的遗物可点击：显示**卡面 + 类型/费用 + 关键词 + 正文**；升级数值、升级增删关键词、升级减费（标绿）由**每张卡各自的「显示升级后」开关**控制。附魔 / 能力 / 药水 / 苦痛 / 关键词 / 原版卡牌与附魔都作为衍生条目列出（原版条目不给图）。
- 工具：**exe**（不要 bat），带「更新Wiki」，树形列出「先古 → 遗物名」并用勾选框控制是否包含；另有「打开预览」「导出可分享版」。
- 工具、脚本、站点都放 **`Tools/`**；图片**不复制**，网页直接读仓库 `images/`。
- 页脚：原作 + 模组作者/画师；标题下方挂「前往创意工坊」链接；搜索忽略空格。
- 发布：GitHub Pages，**网址分享**（`github.io`，不买域名）。

---

## 2. 当前状态

### 已完成并实测

| 产物 | 说明 |
|---|---|
| `.agents/skills/ancient-relic-list/scripts/relic_list.py` | 共享解析：先古池、遗物/卡牌/附魔/能力/药水/苦痛/关键词/原版卡牌与附魔、先古对话。 |
| `.agents/skills/ancient-relic-list/SKILL.md` | skill 说明：命令、推导来源、覆盖表用法。 |
| `Tools/build_wiki.py` | 生成 `Tools/wiki/data.js`（中文）与 `data.en.js`（英文）；`--list` 给 exe 用，`--select` 按勾选生成，`--lang` 只生成一种语言。 |
| `Tools/wiki/{index.html,style.css,app.js}` | 页面外壳 / 深色主题 / 渲染逻辑（hash 路由、筛选、弹层、升级开关）。 |
| `Tools/wiki/data.js` / `data.en.js` | **生成物**：中文 18 位 / 192 件 / 120 条衍生；英文 18 位 / 192 件 / 120 条。 |
| `Tools/WikiTool/` | WinForms exe 源码；已编译并通过 UI 自动化实测「勾选 → 更新Wiki → 打开预览 → 导出可分享版」。 |
| `.nojekyll`（仓库根） | 让 Pages 跳过 Jekyll。 |
| `TouhouAncients.csproj` | 已加 `<Compile Remove="Tools/**" />`，mod 编译不受 WinForms 源码干扰（实测 451 个 Compile 项里 0 个来自 Tools）。 |
| `.gitignore` | 已忽略 `Tools/wiki/dist/`、`dist.zip`、`__pycache__/`、`*.pyc`（`bin/`、`obj/` 原有规则覆盖 exe 产物）。 |

### 待办（都在用户手上）

1. **GitHub Pages 设置**：Settings → Pages → Deploy from a branch → 分支（交接时写的是 `Beta`）/ `/ (root)`。
2. **提交与推送**：`git add` / `commit` / `push` 由用户执行。

### 已废弃

- `docs/THA-Relic.md`：用户明确不再需要，已删除。`relic_list.py` 的 markdown 输出保留可用，但**不再维护、不做回归比对**。

---

## 3. 文件清单与职责

```
Tools/
  build_wiki.py                    生成数据（--list 给 exe 用，--select 按勾选生成）
  WikiTool/                        exe 源码（net8.0-windows，无第三方包）
    WikiTool.csproj
    Program.cs
    MainForm.cs
  wiki/
    index.html                    页面外壳（提交）
    style.css                     样式（提交）
    app.js                        渲染逻辑 + 语言切换（提交）
    data.js                       中文生成物（提交，勿手改）
    data.en.js                    英文生成物（提交，勿手改）
    dist/                         导出可分享版时才生成（已 gitignore）
.agents/skills/ancient-relic-list/
  SKILL.md                        用法与规则
  scripts/relic_list.py           共享解析逻辑（wiki 主用；markdown 输出保留但不再维护）
.nojekyll                        让 Pages 跳过 Jekyll
```

### 数据流

```
scripts/ancients/*Ancient.cs ─┐                    ┌─ CanonicalVars / UpgradeBy / Keyword
scripts/relics/**/*.cs        ├─► relic_list.py ───┤  HoverTipFactory.From(Affliction|Card|Enchantment|Keyword)
locals: TouhouAncients/localization/zhs/*.json      └─ talk.ANY 对话
images/**/*.png ──────────────┤
sts2-src（.agents/local.md）──┘  （原版关键词/苦痛/卡牌/附魔的中文名与文案）
        │
        ├─► build_wiki.py（renders HTML、保留颜色、拼衍生条目）─► Tools/wiki/data.js
        └─► 前端直接读 data.js；图片走 ../../images/... 相对路径
```

**关键点：`data.js` 里不存图片数据，只存 `../../images/...` 相对路径**，所以本地双击与 Pages 根目录发布都成立，不复制任何素材。

---

## 4. 关键决策与原因

| 决策 | 原因 |
|---|---|
| 零复制图片（`../../images/`） | 本地 `Tools/wiki/index.html` 与线上 `…/Sts2-TouhouAncients/Tools/wiki/` 的相对路径解析结果相同；git 里不多存 23 MB。 |
| Pages 用「Deploy from a branch → `/ (root)`」+ `.nojekyll` | 根目录发布才能让 `/images/` 与 `/Tools/wiki/` 同时可访问；`.nojekyll` 让 Pages 不跑 Jekyll（否则会跳过 `_`/`.` 开头的目录、把 `{{ }}` 当模板、并每次全仓库重建）。 |
| 不建 GitHub Actions 工作流 | 多一层 CI 且**内容会变**：runner 上没有 `.agents/local.md` → 拿不到 `sts2-src`，原版关键词会退化成英文名、苦痛/原版卡牌附魔条目消失。手动 exe 触发能让「未发版内容不上线」。 |
| 发布靠手动 commit + push `data.js` | 更新 wiki = 点 exe 生成 `data.js`；何时提交由用户决定。 |
| 站点放 `Tools/wiki/` | 用户指定。URL 形如 `https://destroyer-0.github.io/Sts2-TouhouAncients/Tools/wiki/`。 |
| 富文本颜色映射到游戏实际色值 | 取自 `E:\Sts2New\src\Core\Helpers\StsColors.cs`：gold `#efc851`、blue `#87ceeb`、red `#ff5555`、green `#7fff00`、orange `#ffa518`、pink `#ff78a0`、purple `#ee82ee`、aqua `#2aebbe`。 |
| 苦痛（侵蚀）**只看代码** | `HoverTipFactory.FromAffliction<T>` / `ModelDb.Affliction<T>()` / `CardCmd.Afflict<T>(card, n)`；文案里的同名字会骗人（饕餮尤魔的「吞噬遗物」是动词）。`{Amount}` 只取代码实参（流电 6、沉重 1、抵押物 2 回合）。 |
| 原版卡牌 / 原版附魔也列为衍生条目 | 用户要求；正文取本体 `cards.json` / `enchantments.json`，类型费用关键词取本体源码，**网页端不给卡图**。原版能力/药水不列，避免噪音。 |
| 衍生卡自身的 hover 也算 | 例：仆从牌 → 原版「小刀」「晕眩」；心之瞳 → 原版附魔「本能」。 |
| 升级差异用开关而不是括号 | 每张卡各自的 `htmlUp` / `metaUp` / `keywordsUp`；升级减费用 `<span style="color:#7fff00">` 标绿；消耗/虚无/保留 不单独列说明。 |
| 通用对话「每次打开随机」 | 用户选择；构建是确定性的（两次生成字节一致，仅 `generatedAt` 不同），随机发生在浏览器。 |
| 构建确定性 | 纯正则 + JSON 解析，无 AI、无联网、无随机；改输入才会改结果。 |
| 双语 | 一次生成两份数据（`data.js` / `data.en.js`）；前端按「`?lang=` → `localStorage` → 浏览器语言」选语言，点顶栏按钮切换并记住。文案源是 `localization/zhs` 与 `localization/eng`，英文缺失/拼串的少量条目在手写覆盖表里按语言各写一份（**不改 localization**）。 |

---

## 5. exe 工具（已实现）

**技术选型**：C# WinForms，`net8.0-windows`，无第三方包。
放置：`Tools/WikiTool/`；编译产物 `Tools/WikiTool/bin/Release/net8.0-windows/WikiTool.exe`；
单文件发布（框架依赖，需 .NET 8 桌面运行时）：`dotnet publish -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o bin\publish` → `Tools/WikiTool/bin/publish/WikiTool.exe`（约 180 KB）。

### 界面

```
┌───────────────────────────────────────────────────────────┐
│ [筛选______]  [全选] [全不选] [反选]                      │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ ☑ 雾雨 魔理沙  — 普通的高塔魔法使                      │ │
│ │    ☑ 迷你八卦炉                                       │ │
│ │    ☑ 恋色手电筒                                       │ │
│ │    ☐ 彗星加速器                                       │ │
│ │ ☐ 博丽 灵梦  — 高塔的不可思议巫女                      │ │
│ └───────────────────────────────────────────────────────┘ │
│ [更新Wiki]  [打开预览]  [导出可分享版]                     │
│ 已勾选 12 位 / 180 件 · 上次更新 2026-10-19 18:38         │
└───────────────────────────────────────────────────────────┘
```

### 行为（实现细节）

1. 启动时执行 `py -3 Tools/build_wiki.py --list`（`py -3` 不可用则退回 `python`），读回 JSON 构建 TreeView（先古为父节点带 `[层数]`，遗物为子节点，均带勾选框，默认全勾）；状态栏显示「已勾选 N 位 / M 件 · 上次更新 …」。
2. **筛选框**：按名字过滤树，勾选状态保存在集合里，清空筛选后不丢。
3. **更新Wiki**：把勾选写成 `select.json`（**写在 exe 同目录**，因为受限环境里 `%TEMP%` 可能不可写）→ 跑 `--select` → 成功后刷新状态栏（**成功不弹框**）；失败才弹 MessageBox 显示 stderr。
   - 选择语义：某先古下有任一遗物被勾就入选；全不勾则该先古整块不出现。
4. **打开预览**：`Process.Start` 打开 `Tools/wiki/index.html`。
5. **导出可分享版**：复制 html/css/js 到 `wiki/dist/`，按 `data.js` 里出现的 `../../images/...` 逐张复制到 `dist/images/...`，并把 `data.js` 里**所有** `../../images/` 整体替换为 `images/`（只改 `assetPrefix` 会漏掉每条记录里的内嵌路径），最后打 `dist.zip`。若 `dist/` 已存在会先确认覆盖。
6. 仓库根定位：从 exe 所在目录逐级向上找含 `Tools/build_wiki.py` 的目录（兼容 `Tools/WikiTool/` 与 `bin/Release/...`），不写死绝对路径；启动时把当前目录固定为 exe 目录，避免输入法等注入进程往用户启动目录丢日志。

### 已完成的相关改动

- `TouhouAncients.csproj`：`<ItemGroup><Compile Remove="Tools/**" /></ItemGroup>`（否则 Godot 的 `**/*.cs` 会把 WinForms 源码编进 mod）。
- `.gitignore`：`Tools/wiki/dist/`、`Tools/wiki/dist.zip`、`__pycache__/`、`*.pyc`（`bin/`、`obj/` 原有规则已覆盖 exe 产物）。

---

## 6. 命令速查

```powershell
# wiki 数据（全部 / 列表 JSON / 按勾选 / 只出英文）
python "Tools/build_wiki.py"
python "Tools/build_wiki.py" --list
python "Tools/build_wiki.py" --select select.json
python "Tools/build_wiki.py" --lang eng

# 本地预览：直接双击 Tools/wiki/index.html

# exe 编译 / 单文件发布
cd Tools/WikiTool
dotnet build -c Release
dotnet publish -c Release -r win-x64 -p:SelfContained=false -p:PublishSingleFile=true -o bin\publish

# mod 编译（验证 Tools/** 没被编进去）
dotnet build "TouhouAncients.csproj" -t:Compile

# markdown 清单（保留可用，但已不再维护）
python ".agents/skills/ancient-relic-list/scripts/relic_list.py" 魔理沙
python ".agents/skills/ancient-relic-list/scripts/relic_list.py" --list
```

---

## 7. 坑与注意事项

1. **PowerShell 输出中文乱码**是控制台编码问题，不代表文件有问题。验证时写文件后用 `Get-Content -Encoding UTF8` 比较，别信 stdout。
2. **本地化 JSON 带 UTF-8 BOM**，一律用 `encoding="utf-8-sig"` 读。
3. **`TARelicOptionPools` 的 `(池, N)` 语义**：同一个池产出 N 个选项 = 占 N 行。辉夜的 5 件遗物池 `count = 2`，标签是 `第一行/第二行`；其余多池角色是 `第一行` `第二行` …。单池角色（猯藏 / 阿求 / 帝 / 正邪 / 贝子）**平铺不标行**。网页多池才显示行标签（与旧 markdown 同口径）。
4. **注释掉的选项不算**：解析前会剥掉 `//` 与 `/* */`。已排除 `NohMask`、`OneEyedKarakasa`、`HakureiCharm`；`HakureiAmulet` 在池里，**必须保留**。
5. **`.agents/local.md` 的 `sts2-src` 是内容来源之一**（原版关键词中文名、苦痛、原版卡牌/附魔文案）。它被 gitignore，**换机器要改这个路径**；文件缺失或路径失效时脚本只打一行 stderr 警告并**静默退化**（原版关键词变英文名、苦痛/原版卡牌附魔条目消失），退出码仍是 0。换到另一版本的反编译源码（如 `E:\STS20.107`）也会让原版文案变化。
6. **苦痛只认代码**：`HoverTipFactory.FromAffliction<T>` / `ModelDb.Affliction<T>()` / `CardCmd.Afflict<T>(card, n)`；**不要**再用文案里的名字匹配（饕餮尤魔的「吞噬遗物」是动词，会造成假条目）。
7. **苦痛 `{Amount}` 只取代码实参**：`FromAffliction<Galvanized>(GalvanizedDamage)` → 6、`CardCmd.Afflict<Weighted>(card, DynamicVars.Energy.IntValue)` → 1。绝不能回落到遗物自己的同名 `Amount`（伪灵异珠 `Amount=2` 就是坑）。
8. **嵌套颜色标签必须用栈解析**：旧哨兵写法会把 `[red]第[blue]{X}[/blue]回合[/red]` 渲染成外层丢色、漏出字面 `red`。
9. **`CARD_OVERRIDE` 是结构化对象**：`{title, body, bodyUp}`。运行时拼串（`{LeafText}` 等）算不出数值时用它；类型/费用/关键词仍从 `.cs` 读，别写进正文。
10. **覆盖表在 `relic_list.py` 顶部**：`RELIC_OVERRIDE`、`CARD_OVERRIDE`、`DERIVED_OVERRIDE`（值为 `None` = 不输出）、`REST_OVERRIDE`、`KEYWORD_LINES`、`EXTRA_LINES`、`REF_OVERRIDE`（泛型名歧义，如 mod 附魔 `Greed` vs 原版诅咒牌 `Greed`）。只在**静态解析不出**时使用。
11. **派生数据不要手改**：改脚本后重跑 `python Tools/build_wiki.py`（或点 exe「更新Wiki」）；`data.js` 是生成物。
12. **构建是确定性的**：同一份输入连续跑两次，`data.js` 只有 `generatedAt` 不同；没有 AI、没有联网、没有随机（对话随机在浏览器里）。
13. **缺图遗物**（纯狐那批 + `KonshiiNoKusuri` 等）前端已 `onerror` 回落到 `icon/relics/default.png`。
14. **仓库是 PUBLIC**：根目录发布后 `scripts/`、`localization/` 也会被 Pages 公开（本来就是公开仓库；将来若转私有，此发布路线失效）。
15. **不做**：不改任何游戏文案（`localization/**` 只读）、不复制素材进仓库、不建 Actions 工作流；git 操作由用户自己执行。
16. **英文文案的已知上游问题**（在 `localization/eng` 里，不归 wiki 管）：`FUTATSUIWA_MAMIZOU_ANCIENT.epithet` 值多了 `TOUHOUANCIENTS-` 前缀；`DISILLUSION_TRIDENT.flavor` 是双重编码的乱码。修好 localization 后重跑即自动生效。
17. **访问量统计用 GoatCounter**（站点代码 `sts2-tha-wiki`，看板 `https://sts2-tha-wiki.goatcounter.com`，仅所有者登录可见）。`index.html` 里的内联脚本会跳过 `file:` / `localhost` / `127.*`，所以本地双击与本地预览**不计数**；`http://<内网IP>` 会计数。GitHub 的 Insights → Traffic 只统计**仓库页面**，不含 Pages 站点，别拿它当网站访问量。导出可分享版的 `dist/` 会带着这段脚本。

---

## 8. 验收清单
- [x] `python Tools/build_wiki.py` 输出「18 位先古之民 / 192 件遗物 / 120 条衍生说明」。
- [x] 双击 `Tools/wiki/index.html`：首页 18 张先古卡，头像不破图，层数标签与排序正确（1 层 2 位 / 2 层 7 位 / 3 层 6 位 / 2·3 层 3 位）。
- [x] 点开任一先古：节点图 + 名称后头像 + 遗物按行分组，图标正常；下方随机一条通用对话（女苑/紫苑带差分头像）。
- [x] 点击有「衍生 N」徽标的遗物：弹层出现卡面 + 类型/费用 + 关键词 + 正文，且**没有**「XXX · 衍生内容」小字。
- [x] 卡牌的「显示升级后」开关**逐卡独立**：数值变化、关键词增删正确，升级减费标绿。
- [x] 苦痛条目取自代码：流电 6、沉重 1、抵押物 2 回合、吞噬；文案里的「吞噬遗物」不再误判。
- [x] 原版卡牌/附魔条目无图但含类型费用与关键词（灼伤、蛇咬、虚空、小刀、晕眩、贪婪、华彩、稳定、本能）。
- [x] 筛选框输入「极限火花」只剩魔理沙；输入「雾雨魔理沙」（无空格）也能命中。
- [x] exe：取消勾选某遗物 → 更新Wiki → 页面中该遗物消失；「导出可分享版」产出的 dist 页面图片正常。
- [x] 中英双语：顶栏按钮切换，`?lang=` / localStorage / 浏览器语言优先级正确；标题、占位符、按钮、页脚、行标签、费用标签都跟着换。
- [x] 访问统计：`https://destroyer-0.github.io/...` 会加载 GoatCounter；`file://`、`localhost`、`127.*` 不加载（6 个用例已验证）。
- [x] `git status` 里没有 `Tools/WikiTool/bin`、`obj`、`wiki/dist`、`__pycache__`。

---

## 9. 发布步骤（用户自己执行）

1. 仓库 **Settings → Pages → Source: Deploy from a branch → 你的分支（交接时是 `Beta`）/ Folder: `/ (root)`**。
2. **首次推送**要把 `.nojekyll` 一并带上（否则 Pages 走 Jekyll），需要的文件：
   `.nojekyll`、`.gitignore`、`TouhouAncients.csproj`、`Tools/build_wiki.py`、`Tools/wiki/*`、`Tools/WikiTool/*.cs*`、`.agents/skills/ancient-relic-list/**`。
   `Tools/WikiTool/bin|obj`、`Tools/wiki/dist`、`__pycache__` 已被忽略，不会进仓库（exe 只在本机）。
3. 等待 Pages 构建，访问：
   `https://destroyer-0.github.io/Sts2-TouhouAncients/Tools/wiki/`
4. 之后每次更新：改代码/文案/图标 → 点 exe「更新Wiki」→ `git add Tools/wiki/data.js` → commit → push。
   **只 push 别的文件不会改变线上 wiki 内容**。
5. 不打算用 GitHub Actions 发布：内容保持在提交里的 `data.js`，任何机器都能复现（前提是 `.agents/local.md` 指向**同一版本**的 `sts2-src`）。
