# B89_E10d · 等级看得到增量 + 增量落盘 + 生产调用点接线

> 执行代理 `B89-E10d` · 2026-10-28 · 判据 = 上一波（E10c）的 `AddCollectorXp` 逐句 ＋ `Shell/SaveCenter.cs`（中央存档口）。
> 账补丁 = `账/_parts/B89_E10d.tsv`（**1 行** · 11 列 · LF · 在 `AddCollectorXp` 上**赢过** `B89_E10c.tsv`，B→A）。

---

## §1 三件都做完了

| # | 事 | 落点 |
|---:|---|---|
| ① | **等级看得到增量层** | `Shell/CardTierConfig.cs:205 PlayerLevel`（**见 §2 —— 只加在这一处**） |
| ② | **增量落盘 + 读档恢复** | `Shell/PlayerProfileState.cs:192 CollectorXpGranted` · `:200 CollectorXpSaveKey` · `:207 CollectorXpSave` · `:243 ReloadCollectorXpFromSave` |
| ③ | **生产调用点** | `Battle/BattleDriver.cs:32198`（结算那一段，紧跟 `RecordBattleLog()` / `Achievements()`） |

---

## §2 🔴 一处**必须说明的偏差**：`CardTierConfig` 只加了一处，不是两处

你给的是「`:203` / `:331` **各加一句** `+ PlayerProfileState.CollectorXpGranted`」。
**两处都加会双计** —— `PlayerLevel` 的实现就是 `GetLevelForXP(GetTotalPlayerXPFromInventory())`：

```
PlayerLevel = GetLevelForXP( GetTotalPlayerXPFromInventory() + 增量 )     // 若再加在外层
GetTotalPlayerXPFromInventory() = 库存求和 + 增量                          // 且这里也加 ⇒ 增量 ×2
```

⇒ 我按「**单一加处**」落：

- **`CardTierConfig.cs:205`（`PlayerLevel`）**：`GetLevelForXP(GetTotalPlayerXPFromInventory() + PlayerProfileState.CollectorXpGranted)`
  —— 这是**唯一**把增量算进等级的地方。
- **`CardTierConfig.cs:330`（`GetTotalPlayerXPFromInventory` 的 doc）**：**保持原版口径（只算库存那份）**，
  并在那一行**就地**写明「增量在 `PlayerLevel` 那一处加；⛔ 别在这里也加，会双计」。
  ⚠️ 这一句是**就地改写原有那一行**（不是新行）⇒ 行数不变。

如果你要的就是「两处都加」，那 `PlayerLevel` 那一处必须去掉 —— 请回一句，我 30 秒改回来。
（`PlayerProfileState.TotalCollectorXp` 仍是「库存那份 + 增量」，与 `PlayerLevel` 同一个口径，不重复加。）

---

## §3 落盘的形状（照 `SaveCenter` 的既有口径）

- **走中央存档，不自建文件**：`SaveCenter.Get<T>/Set<T>/Flush`，根目录 `persistentDataPath/WarpforgeSave/`
  （与 `WarpforgeDaily` / `WarpforgeDraft` 并列），文件名 `_ours_CollectorXp.json`。
- **键** = `SaveKeys.OursPrefix + "CollectorXp"` = `"_ours_CollectorXp"` —— 用 `_ours_` 前缀是因为
  **原版没有这条键**（`PlayerDataKey` 51 个里没有「奖励发的收藏经验」这一格），⛔ 不硬套 `CollectorLevel(43)`。
- **payload** = `CollectorXpSave { public int grantedXp; }`。
  ⚠️ 按你的提醒写了：**判「这一格在不在」只能看 `SaveCenter.Get` 是不是 `null`（= 整个 key 缺）**，
  ⛔ **不能用 `!= null` 判字段** —— `JsonUtility` 会把缺的引用类型字段物化成整棵子树（记忆 `jsonutility-null-subtree`）。
  本 DTO 只有一个 `int`，缺字段 ⇒ 就是 `0`（可接受，已在类的 doc 里写明）。
- **懒加载**：第一次读 `CollectorXpGranted` 时读一次盘；取不到（第一次跑 / 档坏 / 未来版本）⇒ 保持 0，
  并**由 `SaveCenter` 自己出声**（它的 `Get` 四种 null 都出声）。
- 🔴 **批处理不读玩家真档**：`EnsureCollectorXpLoaded()` 里有一道 `if (!SaveCenter.StoreOn) return;`
  —— 否则 `PlayerLevel` 每次都会去读玩家的真档，**自检的等级断言会随玩家存档漂**（同代码不同机器结论不同）。
  自检要验往返时设 `SaveCenter.OverrideRoot = <临时目录>`（`StoreOn` 就变真，那道闸自动放开）。
- **写**：`AddCollectorXp` 改完立刻 `SaveCenter.Set(...)`（原子写）；写不进去**只说一次**（批处理会连续命中）。

---

## §4 验证

- **秒级类型检查**：`TMPDIR=/tmp/wf_B89E10d bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
  ⚠️ 中途一次被 `RuleEngine/Editor/EnchantmentTemplateCheck.cs` 的 `CS0103 SectionE_EnchantmentTemplates` 顶红
  （**R1c/E6c 的文件**，不是我的），等它编干净后复跑即 **0/0**。
- ⛔ 本波**没有跑 Unity**（红线）⇒ 新增断言**一条都没执行过**。
- `CheckCollectorXp()` 从 13 条扩到 **17 条**，新增 4 条全部对着这三件事：
  1. 🔴 `CardTierConfig.PlayerLevel == GetLevelForXP(库存那份 + 42)`（**等级看得到增量**）；
  2. `SaveCenter.PathOf(CollectorXpSaveKey)` 那个文件**真的在**（落了盘）；
  3. 🔴 `ReloadCollectorXpFromSave()` 之后**仍是 42**（**真往返**：先清内存读口、再从盘读回来 —— 不是内存残值）；
  4. （前提）加 42 之后增量 == 42。
  夹具：`SaveCenter.OverrideRoot` 指 `Application.temporaryCachePath/wf_pdmath_save`，`finally` 里删文件+删目录+还原并 `ResetForTest`。

---

## §5 行数核验（`BattleDriver.cs` 被账大量按行号引用）

- **`CardTierConfig.cs`**：`HEAD 473 行 / 工作树 473 行`，`git diff --numstat = 3 3`（**三处就地改写，零漂移**）。
- **`BattleDriver.cs`**：我的改动是**一行换一行**（`AwardAchievements();` 后面同一行追加
  `PlayerProfileState.GrantMatchXp(...)`）⇒ **零新增行、零删除行、零漂移**。
  ⚠️ `git diff --numstat` 对这个文件**不可用**（工作树里本来就有别的代理的大量未提交改动，实测 `294/26`），
  所以判据用「构造上是一行换一行」＋ `sed -n '32198p'` 现读：该行**同时**是
  `AwardAchievements();` 与 `PlayerProfileState.GrantMatchXp(...)`，前一行 `RecordBattleLog();` 仍在 `:32194`，
  后一行注释仍在 `:32199` ⇒ **`:32198` 及其之后没有任何账落点移位**（没有新增行可移）。

---

## §6 顺手发现（⛔ 没改）

1. 🔴 **`PlayerLevel` 现在会触发一次磁盘读**（进程内第一次访问时，`SaveCenter.Get`）。它被顶栏每帧刷新链路读到 ——
   只有**第一次**真的读盘（`_collectorXpLoaded` 置位后不再读），量级没问题；但**别在别处再引入第二份懒加载**。
2. **`SaveCenter` 的 `missing` 出声是一进程一次**（`SpeakOnce("missing|key")`）：新号第一次跑会看到一条
   「缺 key '_ours_CollectorXp'」的提示 —— 那是**预期的**（原版同款：`No player data found for key X`），⛔ 别当缺陷删。
3. **`GrantMatchXp` 的调用点在「结算那一段」里**，而 `AwardAchievements` / `RecFinish` 也在同一段。
   原版的顺序是「先记战报 → 再发奖励」，所以放在 `AwardAchievements()` 之后、`RecFinish` 之前是**对的**
   （经验值不影响录像文件名）。若将来 `RecordBattleLog` 被移到别处，**这句要跟着走**。
4. **回放局不会发 XP**：`Ctx.MatchType` 是 `Replay(160)` ⇒ `GetXPFromMatch` 落 `default` ⇒ 返 0 ⇒ `GrantMatchXp` 直接返回、
   两条 toast 都不弹（**靠的是同一个分支，不是另加一道闸**）—— 这是照原版形状，⛔ 别加 `if (replay) return;`。

---

## §7 交接

- **账补丁** `账/_parts/B89_E10d.tsv`：1 行 —— **340 `AddCollectorXp` 建议 `A`**（四句全落：增量口 / 升级判定 / 两条 toast / 落盘 / 生产调用点）。
  它**取代** `B89_E10c.tsv` 的 340 行（落点行号 188/192/211 → 192/251/270），`check_patch_conflicts.py` 确认 E10d 赢。
- **自检入口不用改**：断言仍在 `PlayerDataMathCheck.Run` 里（本波 +4 条）。
- **本波碰过的四个文件**：`Shell/PlayerProfileState.cs`（我的）· `Shell/CardTierConfig.cs` · `Editor/PlayerDataMathCheck.cs`（我的）·
  `Battle/BattleDriver.cs`（本轮归我）。⛔ 其余一个字没动。
- **仍然开着的（与原版有差、但都有裁定/都记录在案）**：
  升级 toast 的正文口径（账 510 既有 A 裁定，原版印等级数字、我们印「收藏者升级！」）；
  XP 的**值**仍是本地模拟（🔶，原版来自服务器回包）。
