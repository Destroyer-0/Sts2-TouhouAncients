# TouhouAncients Mod — TODO List

- [ ] 测试「光辉宝塔」「领主防晒霜 & 吃不完的糖」在正式版的适配问题
- [ ] 尝试添加 mod 宣传图标功能
- [ ] 尝试把因幡帝的「幸运」戏法的卡面过渡改成渐变
- [ ] 制作「冈格尼尔」特效
- [ ] 制作「封魔阵」特效

---

## 挑战 BGM 崩溃修复（方案 B：继续走 BaseLib 接口）

**根因**：BaseLib 3.4.7 `ModAudio.PlaySound` 处理 `SoundType.Music` 时 `foreach (_activeMusic)`，循环体内 `music.Stop()` + `RemoveChildSafely`；而该节点的 `TreeExited` 回调会 `_activeMusic.Remove(player)` → **边遍历边改集合** → `InvalidOperationException`。上游 master 至今未修，issues 里搜不到相关报告。
触发日志链：`ModAudio.PlaySound` ← `AutoModAudio.PlayMusic` ← `EncounterBgm.Start` ← `EncounterBgm.OnCombatSetUp`。
触发场景：Acts of Hallownest 的 act 常驻 BGM（`HallownestMusic`）占着 BaseLib 的单轨 Music 通道，我们的挑战战 BGM 去抢占时踩雷。

**关键坑**：不能写成"停 + 摘树"式清场。`CombatManager.cs:405`（`StartCombatInternal` 里）会在我们播放之后调 `UpdateTrack()` → 触发对方的 `SyncTrack` → 对方发现 `IsPlaying == false` 就**重播** → 撞上列表里我们的播放器 → 对方抛异常，并顺手把我们的播放器停掉摘走（我们这场就哑了）。所以必须**出列 + 暂停，但不摘树**。

### 待办

- [ ] 改 `scripts/EncounterBgm.cs`（唯一文件，不新建/删除其它文件）
  1. 保留 `AutoModAudio Audio` / `_current` / `_active`；新增"被我们暂停的播放器"记录列表
  2. `Start(file, fade)` 顺序改为：`StopCurrentPlayer()` → `SilenceOtherMusic()` → `MuteVanillaMusic()` → `try { _current = Audio.PlayMusic(file); }` / `catch (InvalidOperationException) { 记日志 + RestoreVanillaMusic() + RestoreOtherMusic(); return; }` → `FadeIn` → `TryShow()`（签名不变，辉夜侧 `HouraisanKaguyaMonster` 不动）
  3. 新增 `SilenceOtherMusic()`：反射取 `ModAudio._activeMusic`（`NonPublic | Static`），对**副本**枚举：`list.Remove(p)` 出列（让 BaseLib 的 foreach 看到空列表）+ `p.StreamPaused = true`（**不摘树、不动 Stream** → 保住对方的 `IsPlaying == true` → 对方不会重播）。整段 `try/catch`；反射不可用或异常 → 记一次 warning，降级为**不播自定义 BGM + `RestoreVanillaMusic()`**
  4. 新增 `RestoreOtherMusic()`：`IsInstanceValid` 且已暂停者 `StreamPaused = false`，清空记录；在 `Stop()` 里调用
  5. （待定）`OnCombatSetUp` 的 `AutoStartBgm == false` 分支是否也调 `SilenceOtherMusic()` —— 否则辉夜开场那段时间会响着别人的 act 曲子
  6. 音量**不需要**新 patch：我们的播放器仍在 `_activeMusic` 里，BaseLib 自己的 `SettingsSave.VolumeBgm` postfix 照旧更新它
- [ ] 更新 `docs/项目参考.md` §7.2：记录根因、"出列 + 暂停"而非"停 + 摘树"的理由、`CombatManager.cs` 1028（`UpdateTrack`）/ 1031（`CombatEnded`）的顺序

### 已知代价（选 B 就要接受）

- 我们的播放器仍在 BaseLib 共享列表里 → 别人若用"停 + 摘树"方式抢占，会把我们的 BGM 静音（不崩）
- 对方那个播放器永久占用 BaseLib 音乐池 1 个名额（上限 2）→ 若同时有第三个 mod 用 BaseLib 播音乐，第三个会拿到 `null` 并触发 BaseLib "Too many sounds" 警告
- 反射依赖私有字段名 `_activeMusic`，改名即降级为"不播 + 保留原版音乐"（不会再崩）

### 可选替代（更彻底，暂未采纳）

方案 A：自建 Godot `AudioStreamPlayer`（永不进 `_activeMusic`）+ 同样"暂停"对方播放器 + patch 游戏 `SettingsSave.VolumeBgm` setter 跟音量。代价是多一个 patch 文件，但不再与 BaseLib 共享列表耦合（对方用任何方式抢占都影响不到我们）。

### 验证

- `dotnet build` 通过
- 在 Acts of Hallownest 的 City of Tears 里触发一场带 BGM 的挑战战：日志无 `InvalidOperationException`、能听到我们的曲子、战斗结束曲名消失且原版音乐恢复
- 辉夜战：开场安静 →「五道难题」后 2 秒淡入 + 曲名出现

### 顺手的事

- [ ] 用下面这段发到 Acts of Hallownest 创意工坊页面评论/Discussion（该 mod 无公开仓库，作者 GitHub 为 VilktorK 但无 repo）：

  ```text
  Hello, this looks like an issue that BaseLib may be partly responsible for. I noticed that your BGM
  spans an entire act, which can cause a hang when a combat — reached through an event, or an encounter
  from another mod — plays its BGM through the BaseLib pipeline, because BaseLib's AutoModAudio can only
  play one music track at a time.
  ```

- [ ] （可选）确认创意工坊页面是否可开 Discussion；BaseLib 上游正解：`foreach (var music in _activeMusic.ToList())`，或先解绑 `TreeExited` 再移除

