# zh-Hans phrase list

Two lists. The first is what to replace. The second is what to leave alone.

The replace rows were checked against `D:\Repos\MystiaExtensionFramework\docs`
(nine Chinese design docs, about 90,000 Han). The generic AI rows are the
ones that did not appear there, and still apply to other documents. Counts
below are from those docs, with code spans removed. `——` alone occurs 482
times. That is the main mechanical hit.

The linter flags every "Linter" row. Judgment rows are for you.

Approved replacements are the short form, the way the English recurring-error
list prints them.

## Replace these

### Frames that recur in the design docs

| Do not write | Write | Seen | Linter |
|---|---|---|---|
| 也就是说 | Delete the frame. The next clause is the sentence | 15 | yes |
| 换句话说 / 换言之 | Delete the frame. The next clause is the sentence | 1 | yes |
| 顺带一提 / 顺带发现 / 顺带一条 / 顺带纠正 / 顺带的收益 / 顺带要删 / 顺带一个 | 另外, or start with the fact | 30 of 顺带, these frames are the asides | yes |
| 顺便 | 另外, or start with the fact | 1 | yes |
| 目的是 | Delete it. Keep the purpose clause | 4 | yes |
| 用…的方式 / 以…的方式 | The verb. 不得逐个加载 dll, not 不得用逐个加载 dll 的方式 | 5 | yes |
| 之类 | Name the set, or delete it when the examples are already there | 8 | yes |
| 更重要的是 | Delete. Write the fact | 2 | yes |
| —— / — | A period. Two sentences | 482 | yes |
| ； / ; | A period. Two sentences. An option label may keep its semicolon | many | yes |

顺带 as a verb stays. 顺带作废, 顺带登记, 顺带刷新, 顺带断言, 顺带挡 are actions. The linter does not flag bare 顺带.

### Generic AI frames

These were absent from the design docs. Flag them anyway.

| Do not write | Write | Linter |
|---|---|---|
| 值得注意的是 / 需要指出的是 / 需要注意的是 | Delete. Write the fact. A real warning starts with 注意： | yes |
| 众所周知 / 不难发现 / 显而易见 | Delete | yes |
| 综上所述 / 总而言之 / 总的来说 | Delete | yes |
| 与此同时 | 同时, or a new sentence | yes |
| 除此之外 | 另外, or delete | yes |
| 不仅如此 | Delete. Write the next fact | yes |
| 本文将 / 本文旨在 / 本指南将 / 本文件旨在 / 在当今 | Delete the opener | yes |
| 随着…的发展 | Delete the background sentence | yes |
| 希望对您有所帮助 / 希望对你有所帮助 / 希望本文对你有所帮助 | Delete | yes |
| 如有疑问 / 欢迎交流指正 / 让我们拭目以待 | Delete. Name the action if the reader must do something | yes |
| 赋能 / 深度赋能 | The concrete action | yes |
| 助力 | 帮助 | yes |
| 抓手 | The concrete method | yes |
| 颗粒度 | The real unit. 粒度 as a technical noun stays | yes |
| 底层逻辑 | The cause | yes |
| 顶层设计 | The structure or the plan | yes |
| 组合拳 | The steps | yes |
| 全方位 / 一站式 / 无缝 / 极致 / 深度融合 | Delete, or name the actual scope | yes |
| 进行 + 分析/优化/检查/处理/探讨/研究/总结/梳理/评估/测试/配置/部署/验证/确认/说明/介绍/讨论 | The verb alone. 进行时 stays. It is a timestamp: 截至 P2 进行时 | yes |
| 可能可以 / 或许能够 / 应该可以 / 不妨可以 | 可以, or 必须, or delete | yes |
| 显著提升 / 显著提高 / 显著改善 / 显著增强, with no number | The number, or delete 显著 | yes |

## Your judgment

| Do not write | Write |
|---|---|
| 本质上 / 实际上, when they add no correction | Delete the word. Keep 实际上 when it corrects a false claim |
| 正是, when it only adds emphasis | 就是, or delete it. Keep 正是 when it points at a specific thing (它们正是这些输入) |
| 意味着, when it only restates | The two facts. Keep 意味着 when X really means Y |
| 是一个 / 是一种, in front of an empty noun | The noun |
| 打造 | Keep when something is built. Delete when it means "create an experience" |
| 闭环 | Keep for a closed loop or a closing check (闭环条件). Delete when it means "end to end" with no mechanism |
| 对齐 / 沉淀 / 链路 / 生态 / 矩阵 / 赛道 / 痛点 / 落地 | Keep the precise noun. In these docs, 对齐 is field alignment, and 落地 means the change is in the tree (落地即归档) |
| A chain of 的 longer than three | Unpack it |
| 首先 / 其次 / 最后 as throat-clearing | Delete the frame. Keep a real sequence as a numbered list |

## Calques

A calque puts an English design-doc metaphor into a Chinese word that means something else, or stacks English word order into one noun phrase. Replace it with the concrete thing. Do not keep the metaphor and add a gloss.

The rows below are the ones found in `D:\Repos\MystiaExtensionFramework\docs` beyond the generic AI list. The linter flags the compounds. It does not flag 面, 红, 绿, 族, or 壳 by themselves.

### Noted by hand

| Do not write | Write |
|---|---|
| 投机性 | 说不准将来会不会这样 |
| 账本 | 记录, or the event that gets added |
| 守卫 | 检查. 重入守卫 → 防止再次进入的检查 |
| 裸写 / 转发裸 / 裸循环 / 裸异常 | 直接调用那个方法 / 直接遍历 / 异常直接抛出 |
| 钉成 / 钉死 / 钉住 | 固定成这个数 / 锁在这个构建上 / 测试锁住 |
| 偏离 / 真偏离 | 和其余调用不一致 / 这处和计划不同 |
| 控制族 / 对话族 / 回调族 / 枚举家族 / 那一族 | 这组方法 / 这些回调 / 这个枚举 |
| 工厂 / 手拼 | 创建它的方法 / 各处自己拼字符串 |
| 铸 | 创建. 铸一个 → new 一个 |
| 唯一通路 / 通路 / 认领通路 / 失败通路 | 只有这一条调用 / 退出的步骤 |
| 形状镜像注入面 | 读接口和注入接口一一对应 |
| 它就会红 / 立刻红 / 红起来 / 全程绿 / 转绿 / 签出是红的 | 测试会失败 / 构建通过 / 那次提交构建失败 |
| 放宽 / 加宽 | 放宽一条必须成立的条件 → 取消那条条件. 加宽 → 新增一个成员 |
| 漂移 | 两处结果不一样. 行号漂移 → 行号对不上. 令牌漂移 → 两个计数器不一致 |
| 游戏算的,读它 | 这个数是游戏算的。SDK 只读它,不写公式 |
| 产出方 / 消费者 / 喂给 | 写这些文件的程序 / 读这些文件的程序 / 把文件交给谁 |
| 押着 | 停在这里等你 |
| 假构建树 | 测试用的构建目录 |
| 判据 | 用哪条标准判断 |
| 启动期致命 | 启动时就退出 |
| 便宜的就绪探测 | 不跑游戏也能做的检查 |

### More of the same kind

| Do not write | It stands in for | Write |
|---|---|---|
| 公共面 / 注入面 / 创作面 / 冻结面 / 内部面 / 对方的面上 | surface | 公开 API / 注入接口 / 创建对象的类型 / 发布后不能改的类型 / 桥内部的接口 / 对方的类型上 |
| 薄壳 | thin shell | 一行转发 |
| 托管壳 | managed shell | 生成出来的托管结构体 |
| 写穿 | write through | 写出界,盖住调用者的栈 |
| 跳板 | trampoline | 方法体只转到 `il2cpp_runtime_invoke` |
| 大声失败 / 响亮拒绝 / 失败即大声 | fail loud | 失败就退出并写日志 / 未知值就抛异常 |
| 单一致命汇聚点 / 致命端口 | single fatal sink | 只有一处负责退出 |
| 类型化拒绝 | typed refusal | 拒绝时写明原因,而不是直接退出 |
| 默认体 | default method body | 接口方法里的空实现 |
| 兜底 | backstop | 靠抛异常收场 |
| 咽喉 / 真咽喉 | choke point | 真正关掉面板的那个函数 |
| 空转 | idling | 还没有旧 mod,这些检查不起作用 |
| 归宿 / 指到它的家 | where the member lives | 这个方法改挂到哪个类型 |
| 口径 / 本机口径 / peer 味 | the wording / the smell | 文档里那句话 / 文档写成了联机 |
| 下沉 | sink to a lower layer | 挪到内部项目 |
| 可达域 / 有效域 / 常驻层 / 编译期可达 | reach, resident layer | 什么时候调才有效 / 整个进程都在的服务 / 拿不到类型就写不出调用 |
| 叶子 | leaf | 服务图末端的那个对象 |
| 懒建 | lazy init | 第一次用到才创建 |
| 白拖住 | keep a graph alive for nothing | 对象已经作废,引用还留着那棵树 |
| 心智模型 | mental model | 不用再记一套新规则 |
| 会撒谎 / 撒谎 | the object lies | 返回一个并不指向那场的对象 |
| 歪斜检测 | skew detection | 契约号对不上 |
| 不发明 / 回落到猜 / 说实话 | do not invent / fall back to guessing / tell the truth | 缺了就留空 / 缺了不要猜 / 这个数是不是真的 |
| 投标 / 投标窗口 | bid window | 生成时登记接管 / 只有生成那一次可以接管 |
| 后写覆盖先写 | last write wins | 后登记的盖过先登记的 |
| 不透支 | overdraw the semantics | 不要让同一件事有两种说法 |
| 测试缝 | test seam | 测试能换掉的那一层 |
| 假端口 / 假对象 | fake | 测试里的替身 |
| 升格为硬约束 | promote to a hard invariant | 改成必须遵守 |
| 作用域放错 | wrong scope | 这个方法放错了类型 |
| 最小宿主 | minimal host | 只为把接口实现出来的空宿主 |
| 可抄形态 | a shape you can copy | 可以直接照着写的做法 |
| 红/绿信号 | red/green signal | 失败或通过 |
| 注完 / 违 A-12 / 太宽 / 标量读 / 读就读到游戏了 | telegram | 写成完整的句子。注完 → 注入之后. 太宽 → 那句话覆盖得太广 |

## Keep these

The linter does not flag them. A rewrite leaves them.

| Word | Why it stays |
|---|---|
| 落地 | The change is in the tree. 落地即归档 |
| 对齐 | Field alignment |
| 闭环 | A closing check |
| 进行时 | 截至 P2 进行时 |
| 本文 | This document. Only 本文将 / 本文旨在 go |
| 提供 | The API supplies a member |
| 保证 | An invariant holds |
| 即可 | A short imperative. The rewritten survey kept it |
| 必须 / 不得 / 可以 / 需要 | Rule language |
| 如何验证 / 为什么 | The rule template: imperative, why, how to verify |
| 契约 | The ordinary word for a published API contract |
| 令牌 / 幂等 / 短路 / 回调 / 签名 / 异常 / 解引用 / 字面量 / 白名单 | Ordinary technical Chinese |
