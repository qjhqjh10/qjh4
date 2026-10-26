# B9_W19FX —— 两个 β 缺陷修复报告（`BT4` 残骸音效阵营判错 · `RW1` 每日奖励窗缺图）

> 会话 2026-10-27（第八十四会话收尾）· 执行代理 **W19FX** · 独占文件：
> `CardPresentation/Battle/BattleDriver.cs` · `CardPresentation/Shell/DailyRewardPopup.cs` · `CardPresentation/Shell/ForgeTab.cs`

---

## ① 结论

| 编号 | 定性 | 改了什么 | 结果 |
|---|---|---|---|
| **`BT4`** | **β（真实现缺陷）** | `BattleDriver.PlayRemnantSfx` 的「这一格现在立着残骸」分支：阵营判据从 `UnitState.Has(Waystone)`（**运行时**关键词）改成 `CardFeel.IsRemnantFamily(now.Card, out …)`（**卡的原数据**） | ✅ 灵族残骸恢复播 `Aeldari To Waystone Death`；死灵仍 `CardShatter` |
| **`BT4` 同族第二处** | **β（同一缺陷类，同一文件）** | `BattleDriver.RemnantPrefabOf` 的关键词兜底路：两个关键词位同样改成按**卡的原数据**算 | ✅ 表没载进来时的兜底不再恒判「不是残骸族」 |
| **`RW1`** | **β（真实现缺陷）** | `DailyRewardPopup.ArtArmyArrow` 常量：`"40K_ArmyTrack_chosen faction"` → `"40K_ArmyTrack_chosen_faction"` | ✅ `MissingArt` 归 0（那条红应转绿） |
| `RW1` 同族 `ForgeTab.cs:756` | ❌ **不成立** | —— | `ForgeTab.cs:756` **本来就是下划线**（`HEAD` 也是），未改（见 §③·3） |

---

## ② 判据

### 2·1 `BT4` —— 为什么 `now.Has(Waystone)` 恒假

1. **残骸翻面时关键词被整本摘光**：`RuleEngine/Core/RuleCore.cs:9835`
   ```csharp
   rem.ClearAllKeywords(); ps.Board[slot] = rem;
   ```
   原版判据同源：`CardScript__TransformIntoRemnant.c:54-75`（倒序 `RemoveTraitSilently`）⇒ 残骸**一条 trait 都不剩**。
2. **`UnitState.Has` 在残骸档只读另一本书**：`RuleEngine/Core/UnitState.cs:968-972`
   ```csharp
   if (IsRemnant && !IsLeavingRemnant) return _remnantTraits.Contains(keyword);
   return _keywords.ContainsKey(keyword);
   ```
   即：残骸身上只认 `buffType == 90 (addTraitRemnant)` 后挂的（`UnitState.cs:945-967` 那段 doc 写明了这条闸）。
   ⇒ `now.Has(KeywordTable.Waystone)` 在**真残骸上恒 false** ⇒ `RemnantSfx.Play(..., aeldari:false, ...)`
   ⇒ **所有残骸都播死灵那条**（`Moment.ToRemnant` 死灵侧 = `CardShatter`）。这正是「`E10TB` 的残骸闸把它暴露出来」的机理。
3. **按卡的原数据判**才是对的：`UnitState.Card` = `Instance.Card`（**翻面沿用同一个实例**，`RuleCore.cs:9818` `new UnitState(u.Instance, false)`）
   ⇒ `now.Card` 仍是**原来那张卡的 `CardDef`**，它的 `_keywords` 没被清过（`CardDef.Has` 是纯字典查表，`RuleEngine/Core/CardDef.cs:6139`）。
4. **判据只此一处**：`CardPresentation/Core/CardFeel.cs:2595-2599`
   ```csharp
   public static bool IsRemnantFamily(RuleEngine.CardDef card, out bool waystone)
   {
       waystone = card != null && card.Has(RuleEngine.KeywordTable.Waystone);
       return waystone || (card != null && card.Has(RuleEngine.KeywordTable.Remnant));
   }
   ```
   同方法另一支（`now == null`，按 `CardDatabase` 查回来的卡）**本来就用它**（`BattleDriver.cs:21347-21350`），
   `CardFeel.PickDeathFx` 也用它 ⇒ 本次把残骸那一支改成同一口径，⛔ 不再两份。

**改后代码**（`CardPresentation/Battle/BattleDriver.cs:21333-21338`）：
```csharp
if (now != null && now.IsRemnant)
{
    bool aeldari; CardFeel.IsRemnantFamily(now.Card, out aeldari);
    RemnantSfx.Play(RemnantSfx.Moment.ToRemnant, aeldari, BoardWorld(e.Player, e.Slot));
    return;
}
```
（**零行数变化**：仍是 2 行 → 2 行，下游 460+ 条账落点不受影响。）

### 2·2 `RW1` —— 为什么取不到图

* `Shell/DailyRewardPopup.cs:128` 原来：`public const string ArtArmyArrow = "40K_ArmyTrack_chosen faction";`
* 取图链路：`Art(name)`（`DailyRewardPopup.cs:790-796`）→ `CardArt.MenuUi(name)` → 取不到就 `MissingArt.Add(name)`
  ⇒ `RewardsScene.cs:8300` 的 `Check(dr.MissingArt.Count, 0, "每日奖励窗没有取不到的图")` 红（实得 1）。
* 落盘真名 = **下划线**：`Assets/CardPresentation/Resources/Art/ui_menu/40K_ArmyTrack_chosen_faction.png`
  （导入器 `name.replace(' ', '_')`；原版切片名确实是带空格的 `40K_ArmyTrack_chosen faction`，
  见 `素材/Warpforge原版/UI图集/图集/0_mainmenu/Sprite/40K_ArmyTrack_chosen faction.json`）。
* 同族对照（本来就是下划线，可当判据）：`Shell/ExpansionPassEventWindow.cs:346`（注释直接写着「切片名里的空格换成下划线」）· `Shell/LeaderboardWindow.cs:190`。

---

## ③ 同族扫描结果

### 3·1 「按 `Has` 判阵营/残骸族」在 `BattleDriver.cs` 里有没有第二处 —— **有，1 处，已一并修**

| 位置 | 写法 | 判定 |
|---|---|---|
| `BattleDriver.cs:21335`（原） | `now.Has(KeywordTable.Waystone)` | 🔴 **β**，已修（活 1） |
| `BattleDriver.cs:26375`（原，`RemnantPrefabOf` 的关键词兜底路） | `u.Has(KeywordTable.Waystone), u.Has(KeywordTable.Remnant)` | 🔴 **同一缺陷类** —— 只有「表没载进来」才走到，但真残骸上两位**都恒 false** ⇒ 兜底永远返回 null（静默不建残骸体）。**已改为按 `u.Card`**（同文件、零行数变化） |

**其余 `.Has(KeywordTable.*)` 都核过、⛔ 不是同类**（判的是「当前状态」或**卡模板**，不是「阵营/残骸族」）：
`BattleDriver.cs:21111 / 21163`（`card.Has(Ambush)`，卡模板）· `:23798`（`u.Has(Flying) && !u.Has(Vanguard)`，运行时状态）·
`:27379`（`card.Has(Jam)`，卡模板）· `:27678-27697 / :27728`（关键词条展示，运行时状态）。

**跨仓 `.Has(Waystone|Remnant)` 另外两处（都没动、语义正确）**：
* `RuleEngine/Core/RuleCore.cs:9816` —— 判「要不要翻面成残骸」，调用点在 `ClearAllKeywords()`（`:9835`）**之前**、且判的是**原卡** `u` ⇒ 正确。
* `RuleEngine/Editor/RuleEngineTest_S23_Resolve.cs:102` —— 引擎侧断言，非本代理白名单。

### 3·2 空格名全仓扫描

* `grep -rn '"[^"]*40K_ArmyTrack_chosen faction[^"]*"' --include=*.cs Assets` ⇒ **0 命中**（代码字面量已清零）。
* 任意位置（含注释）剩 **2 处**，都是**引用原版切片名**（不是取图）：
  `Shell/DailyRewardPopup.cs:106`（本次已就地补一句「落盘/取图用下划线版」）· `Shell/LeaderboardWindow.cs:168`。

### 3·3 `ForgeTab.cs:756` —— **D1DG 的「同族 ①」在现状不成立**

* 现状：`_win.Rect(hbGo, "40K_ArmyTrack_chosen_faction", …)` —— **已经是下划线**。
* 复核：`git show HEAD:…/Shell/ForgeTab.cs | grep ArmyTrack_chosen` ⇒ `756: …"40K_ArmyTrack_chosen_faction"`（HEAD 就是下划线），
  且 `git diff --numstat` 对该文件**无改动**。⇒ 无需改，也未改（可能是 `D1DG` 把它与 `DailyRewardPopup` 的常量记混了）。

---

## ④ typecheck + 指纹

| 检查 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_W19FX bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 对局指纹 | `bash 工具/ruleprobe.sh play check` | **rc=0**，结算指纹 `1b40f42ab010e623e908fcd4a236205f`（= 基线，**未变**） |
| 解析指纹 | `bash 工具/ruleprobe.sh check` | **rc=0**，输出指纹 `c9a64798…`（未变） |

两处改动都在**表现层**（音效挑 cue / 菜单取图），不碰引擎 ⇒ 指纹不变是预期；**零行数变化**：
`BattleDriver.cs` 34421 行（改前=改后）· `DailyRewardPopup.cs` 804 行（改前=改后）· 行尾未翻（前者 CRLF 恒 34421、后者 LF 恒 804）。

账补丁：`账/_parts/B9_W19FX.tsv`（11 列 · 2 行 · `check_patch_conflicts.py` 报「无跨补丁重复键」）。

---

## ⑤ 顺手发现（**未改**，交调度台分流）

1. 🔴 **夹具与生产不对齐（白名单外，`W20AS` 的文件）**：`CardPresentation/Editor/BattleScene.cs:47640` 的
   `ProbeRemnantView` 造合成残骸时**只**设了 `IsRemnant = true`，**没有照生产调 `ClearAllKeywords()`**
   （生产在 `RuleCore.cs:9835` 调）。后果：夹具里那张卡仍带着卡面关键词（`_keywords` 非空），与真残骸的运行时状态不一致。
   **建议**：在上面那行 `};` 之后补一句 `ctx.Players[seat].Board[slot].ClearAllKeywords();`。
   ⚠️ 本次修复后该断言**两种写法都会绿**（因为判据改成读 `now.Card` 了），所以这行不是必须，但它能让夹具「照生产先行清关键词」——
   正是 `D1DG` 提的那一句。
2. `DailyRewardPopup.cs:106` / `LeaderboardWindow.cs:168` 的注释保留原版切片名（带空格）—— **不是缺陷**，是原版资产名，供以后对照；只是别照抄进取图代码。
3. `RemnantSfx.cs` 的六条 cue 表（`Moment × aeldari`）本身**没错**，错的一直是「喂给它的 `aeldari` 位」；以后再有残骸音效问题，先查这个位。
