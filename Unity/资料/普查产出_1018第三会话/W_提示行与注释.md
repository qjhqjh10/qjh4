### ① HintForCode：改前 / 改后 · 判据 · 断言

**判据（`Loc` 的查询口）**：`Core/Loc.cs:961` **`public static bool HasEntry(string key)`**（`return Table.ContainsKey(key)`；`key == null` 时**出声 + 返回 false**，不抛）。写入口**没有**（`Table` 是私有 `static readonly Dictionary<string, Entry>`，只有 `ResetForTest`/`RestoreForTest` 那种「放回语言档」的口，**不能插条目**）⇒ 这一点直接决定了下面「有键态只能靠自检喂键」。

**改前**（`Battle/BattleDriver.cs`，旧 `:7590-7594`）：
```csharp
string key = RuleCodes.TermKey(rc);
return key != null ? Loc.T(key) : RuleCodes.Describe(rc);
```
**真缺陷（现核）**：`Terms` 那 4 条键 **一条都不在 `Loc` 表里**（`grep -n "NotEnoughMana\|NotYourTurn\|NoTargetAvailable\|NotEnoughRoom" Core/Loc.cs` ⇒ **0 命中**；表里那 9 条 `Battle/Tips/*` 是 `EnergyCost`/`Continue`/`HandFull`/`MeleeAttack`/`RangeAttack`/`HealthPoints`/`GoFirst`/`DragToTarget`/`UnitNotReady`）。而 `Loc.T` 缺键 ⇒ **返回键名本身**（`Loc.cs:916 return key`）⇒ **提示行会印 `Battle/Tips/NotEnoughRoom`**。触发面 = `ErrCost`(付不起) / `ErrNotTurn` / `ErrNoTargetAvailable` / `ErrNotEnoughRoom` **四档全中**，即「玩家被拒」时几乎每次都印键名。

**改后**（`BattleDriver.cs:7598-7622`，两段）：
```csharp
public static string HintForCode(int rc) { return HintForCodeWithKey(rc, RuleCodes.TermKey(rc)); }
public static string HintForCodeWithKey(int rc, string key)
{ return (key != null && Loc.HasEntry(key)) ? Loc.T(key) : RuleCodes.Describe(rc); }
```
- 缺键 **或** 键不在表里 ⇒ **回 `Describe(rc)` 的人话**（⛔ 不是键名、⛔ 不是空串）。⛔ 不用 `??`（`Loc.T` 从不返回 null）。
- 🔴 **为什么要开 `HintForCodeWithKey` 这个口（如实标，是为了能测）**：`Terms` 四条键今天一条都不在表里 ⇒ **走单参那条路，「有键态」今天构造不出来**；`Loc` 又没有「按测试插一条」的写入口 ⇒ 自检要钉「键在表里 ⇒ 印词条值、不是键名」**只能**由自检直接喂一个**确实在表里的键**（用 `BattleDriver.HandFullTerm` = `Battle/Tips/HandFull`，`Loc.cs:683` 确有其条）。产品代码一律走单参那条。

**断言**（宿主 `Editor/BattleScene.cs` §3b，`:3002-3093`，全部重写）：
| 组 | 钉什么 | 灭自证 / 🧨 改坏法 |
|---|---|---|
| ① 单参那条路 | 按 `Loc.HasEntry(键)` **自动选期望值**：缺键态 ⇒ `== Describe(ErrCost)`、非空 | 🧨 写回旧式 `key != null ? Loc.T(key) : …` ⇒ 实测变键名 ⇒ 红 |
| ① 灭自证（结构） | 结果**不含 `Battle/Tips/` 前缀**（键名必然含、人话必然不含 ⇒ 结构上不可能两边同时满足） | 只要回到「缺键就印键名」⇒ 红 |
| ① 灭自证（结构） | `Loc.MissingCount` **没涨**（缺键态 `HasEntry` 先挡掉；`Describe` 是纯本地表） | 改回 `Loc.T(key) ?? …` ⇒ 缺键那路会去查表记一笔 ⇒ 红 |
| ② 有键态（`HintForCodeWithKey` + `HandFullTerm`） | `!空 && != 键名 && !含前缀`；另有一条 `== Loc.T(键)`（**注释里如实标「这条是定义式的、近同义反复」**，主判据是前一条） | 🧨 实现写成 `key != null ? key : …`（直接印键名）⇒ 红 |
| ③ **全码普查** | 对 `Terms` 那 4 个码**逐条**跑单参 `HintForCode`，要求「非空 且 不含前缀」，失败时把实测值拼进失败文案 | 🧨 `HintForCode` 少一道 `Loc.HasEntry` ⇒ 四条全红 |
| ④ 无键那条（`ErrSlot`） | 仍 `== Describe`、非空（**兜底没被删**） | 🧨 `Loc.T(TermKey(rc)) ?? Describe(rc)` ⇒ 无键时 `Loc.T(null)` 给 `""` ⇒ 红 |

⚠️ **① 刻意不写死「表里没有这条键」的前提断言** —— `G5` 的下一步就是把值补进 `Loc`，那时写死会**假红**（行为其实是对的）。所以按「表里到底有没有」自动选期望值，两态都有话说。
🧨 **改坏法**全部写在断言文案里（本仓惯例）。

### ② 过期注释逐处：原句 / 新句（按内容定位）

| # | 文件 | 原句（摘） | 新句 / 处置 |
|---|---|---|---|
| 1 | `Battle/BattleDriver.cs` `HintForCode` 的 doc | 「**`TermKey` 今天只映射了两条**（`ErrCost` / `ErrNotTurn`）」 | 改「**映射了四条**」，并把四条键名列全 + 明写「这四条**一条都不在 `Loc` 表里**」。**同段还查出两处连带过时**，一并改：①「我们那 **16** 条笼统中文整句」⇒ **18** 条（`RuleCodes.Names` 19 条含 `OK`）；②「与同族 `HandFullText` 的写法**不同**…本条只判 `TermKey` 非 null（键在、值不在时印**键名**）」⇒ 现在两条写法**一致**（都 `HasEntry` 打头），差别只剩兜底那句的来源。 |
| 2 | `Editor/BattleScene.cs` §3b | 「`RuleCodes.ErrSlot` … 粗码 ⇒ `TermKey` 答 null（**原版那一档还盖着 `NotEnoughRoom`**）」 | 改「拆码之后它只剩『参数非法/越界』这一层意思，原版没有对应词条」+ 就地标 ✅ 订正痕迹。 |
| 3 | `Editor/BattleScene.cs` §⑨ | 「钮搬走了，`TutorialOverlay` 上那几个口（`ClickSkipAt` / `SkipWorldPos`）**只是转发**（零像素）」 | 改「**那四个口已删**（`TutorialOverlay.cs` 只剩『【已删】』留痕）」+ ✅ 订正痕迹，并注明**本批断言一个字都没依赖它们**（只是注释过时、无行为问题）。 |

### ④ 四个口（`SkipPanel` / `SkipVisible` / `SkipWorldPos` / `ClickSkipAt`）：现核 + 我两个文件里的过期处

**现核**：全仓 `grep` 这 4 个名字 ⇒ **活代码 0 处**，只剩注释/留痕。`TutorialOverlay.cs:116/131/214/221/346-347/519` 全是「【已删】」。

**我两个文件里改掉的（4 处）**：
1. `Battle/BattleDriver.cs`（`EnsureTutorialOverlay` 内，原 `:7780`）：「⚠️ 那**四个口本身还在 `TutorialOverlay.cs` 里**…要删干净**得另派一件**」⇒ ✅ 已删光（列出那 6 行留痕坐标）。同段「`BattleScene.cs` 的**两处**注释」这句话也顺手改准。
2. `Battle/BattleDriver.cs`（`_settingsPanel` 创建处，原 `:11405`）：「完整理由与**还剩哪四个口没删**」⇒ 已删光。
3. `Battle/BattleDriver.cs` `:2345` **（不是注释，是一句 `Debug.Log` 文案 —— 如实标，请裁）**：「**跳过钮已亮**」⇒ 改「跳过闸已备好（`SetSkip` 只归零计时起点，那颗钮的真身在设置面板里、显隐跟着面板走）」。**判据**：`TutorialOverlay.SetSkip` 现在**只** `_skipArmed`/`_shownFor`（`TutorialOverlay.cs:513-517`），钮的显隐归 `SettingsPanel`。**副作用核查**：全仓 `grep 跳过钮已亮` 只此一处，**没有任何断言 grep 日志**（不会弄红自检）。
4. `Battle/BattleDriver.cs`（`Begin` 的本局账清零，原 `:2541`）：「上一局亮的提示/高亮/光标/**跳过钮**必须显式收掉」⇒ 从这一列里**去掉「跳过钮」**、换成「标注」（`HideAll` 现在够不到钮，`TutorialOverlay` 里连 `_skipGo` 都没了；`HideAll` 保住的是 `_skipArmed = false` 那个计时起点）。

**别人文件里的（只列、⛔ 未改）**：
- `Battle/TutorialOverlay.cs:123-124`「`minTimeBeforeSkip` 那道闸**没动**…`SkipPanel` / `SkipVisible` / `SkipWorldPos` / `ClickSkipAt` **都还在**」—— 这半句**已过时**（那四个口已删；同一文件 `:131/214/221/519` 又写着【已删】⇒ **同文件内自相矛盾**）。建议下一批订正。
- `Battle/TutorialOverlay.cs:148-149`（`Editor/BattleScene.cs:14844` 的**行号指针**）—— `BattleScene.cs` 现在那一段在 `:15042` 附近，**行号已漂**（本仓不按行号定位，此处仅记录）。
- 其余（`:193` / `:345` / `:386-390`）都是「已删/等价物」的**正确**留痕，不用动。

### 类型检查 · 行尾 · 没查清

- **类型检查**：`TMPDIR=/tmp/wf_sc bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改完 `BattleDriver.cs` 跑一次、改完 `BattleScene.cs` 跑一次、收尾跑一次，三次全 0；**没有**报出「别人正在写的文件」的假错）。
- **行尾**：两个文件都是 **纯 CRLF**（`BattleDriver.cs` CRLF 13328 / LF 13328 · `BattleScene.cs` CRLF 18555 / LF 18555 ⇒ 两个数相等 = 一行 LF-only 都没有）。**⛔ 没用 `sed -i`**，全部走 Edit 工具。`git diff --numstat`：`BattleDriver.cs 407/83`、`BattleScene.cs 679/17` —— ⚠️ **这两个数里绝大部分不是本代理的改动**（开工前这两个文件在 `git status` 里本来就是 `M`，带着本会话前几轮的未提交改动）；本代理的 hunk 是 `@@ -7466 +7562,62 @@`（`HintForCode` doc + 新口）、`@@ -7616 +7774,11 @@`（`EnsureTutorialOverlay`）、`@@ :2345/:2541/:11404` 那几处小改 + `BattleScene.cs` §3b 与 §⑨。数字远小于行数 ⇒ **没翻行尾**。
- **没查清 / 请裁**：
  1. `BattleDriver.cs:2345` 那句是 **`Debug.Log` 文案、不是注释**（我按「那颗钮的过期说法」一并订正了）。若本批口径要**只动注释**，请把这一处回退（一行字符串）。
  2. `Loc.HasWarnedMissing` 在 §3b **不再被使用**（旧断言的「Loc 被问过」判据已废）。该 API 在别处仍有读者吗 —— **本代理未查**（`Core/Loc.cs` 不在白名单）。
  3. `HintForCodeWithKey` 是**为可测性新增的 public 口**（产品代码不调它）。若口径不允许加口，替代方案只有「`Loc` 开一个测试用写入口」（要动 `Core/Loc.cs`，不在本批白名单）⇒ **本代理选的是不动别人的文件**。
  4. **红绿未跑**：`BattleScene.Run` 是 Unity 批处理（本代理白名单禁跑）⇒ 这 8 条断言的**实际红绿由调度台在同步点跑**。静态复核过：四条映射键今天**全部** `HasEntry == false` ⇒ ③ 的四条都走 `Describe`（非空、不含前缀）；`HandFullTerm` 在表里且中英两列都非空 ⇒ ② 非空。
