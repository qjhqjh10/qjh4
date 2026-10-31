# B107_D9 · CollectionScene 唯一 1 条红的诊断（只读）

> 只读诊断：未跑 Unity、未动 git、未改任何源码/账/正本。日期 2026-10-31。

## 一、结论

- **定性 = α（断言自身写错）**，**不是产品缺陷**，也不是本批回归（γ）。
- `B107_W13_Assert.cs:126-127` 期望 **`""`**，实现实际返回 **`null`**；日志里两者都印成 `[]`（`MenuCheck.Check` 的格式 `期望 [{}]，实得 [{}]` 对 null/空串无差别）⇒ 看起来「相等却判红」。
- 同一条断言的夹具**也不是它文案说的那种情况**：执行到这一行时 `SocialData.CurrentSeason == 8`（前一句 `:122` 设的），而 `new SocialData.Member()` 的 `Season == 0` ⇒ 走的是**不同赛季**支（与 `:123` 那条重复），根本没读到 `m.RankedRating`。所以「同赛季但没填值」这句前提**不成立**。

## 二、证据（文件:行 + 日志行号）

| # | 证据 | 位置 |
|---|---|---|
| 1 | 失败行：`✗ （前提）同赛季但没填值 ⇒ 仍是空串（既有夹具可见态不变） —— 期望 []，实得 []`；栈顶 `B107W13_Assert:C<string>` → `RunSeason` `:125`→`:70` | `d:/4/_tmp_view/collection.log:97512`（前一行 = 断言代码在 `Editor/B107_W13_Assert.cs:126-127`；同段 4 条中另 3 条 ✓ 在 `:97463` / `:97479` / `:97496`） |
| 2 | 断言原文：`C(AllianceMemberRow.RankedRatingText(new SocialData.Member()), "", "（前提）同赛季但没填值 …")` | `Unity/MyGame/Assets/CardPresentation/Editor/B107_W13_Assert.cs:126-127` |
| 3 | 实现：`if (m == null) return null; return m.Season == SocialData.CurrentSeason ? m.RankedRating : null;` —— **`null` ⇔ 评级 0**（doc 明写），同赛季支**原样返回** `m.RankedRating` | `Shell/AllianceMemberTab.cs:2630-2634` |
| 4 | `Member.RankedRating` 是**无初值字段**（`public string DraftRating, RankedRating;`，全类无构造函数）⇒ 默认 **`null`**，不是 `""` | `Shell/SocialData.cs:105`（`Member` 三段 partial 声明在 `:98/:596/:640`，均无 ctor） |
| 5 | `Member.Season` 默认 **0**（无初值）；执行到 `:126` 时 `CurrentSeason` 已被 `:122` 置 **8** ⇒ 0 ≠ 8 ⇒ 走 else 支 ⇒ `null` | `Shell/SocialData.cs:645` · `Editor/B107_W13_Assert.cs:119/122/131` |
| 6 | 比较口径：`EqualityComparer<string>.Default.Equals(null, "")` = **false** ⇒ 红（不是浮点/大小写/不可见字符问题） | `Editor/MenuCheck.cs:86-95` |
| 7 | 可见态等价：`Rating()` 用 `bool zero = string.IsNullOrEmpty(value);` 判「值 == 0 ⇒ 整块关闭」，且建字用 `value ?? ""` ⇒ **null 与 `""` 画面完全同形** ⇒ 断言文案里「既有夹具可见态不变」这层语义**其实没被破坏** | `Shell/AllianceMemberTab.cs:2032-2055`（`v.Text(..., value ?? "", …)` 在 `:2038`） |
| 8 | 宿主接线：仅 `CollectionScene` 调 `B107W13_Assert.RunAll(chat)`；`CollectionScene` 内**没有**任何 `SocialData.Members` 夹具 ⇒ 本段是唯一受影响点 | `Editor/CollectionScene.cs:11689` |
| 9 | 全仓 `SocialData.CurrentSeason` 写入点只有 W13 这三处，且 `finally` 还原 ⇒ 不污染别的断言 | grep 现读：`Editor/B107_W13_Assert.cs:116/119/122/131` |
| 10 | 总数：`=== 结束：2848/2849 通过，**1 条失败** ❌ ===`，与任务描述一致 | `d:/4/_tmp_view/collection.log:124185` |

## 三、最小修法（⛔ 不动生产代码，只改这条断言）

把夹具**真的**摆成「同赛季 + 未填值」，期望值按实现合同写成 **`null`**：

```csharp
// Editor/B107_W13_Assert.cs:126-127 替换为：
C<string>(AllianceMemberRow.RankedRatingText(new SocialData.Member { Season = SocialData.CurrentSeason }), null,
  "（前提）同赛季但没填值 ⇒ 仍为 null（= 评级 0 ⇒ `Rating()` 把 null/空串同判 0、整块关闭 ⇒ 既有夹具可见态不变）");
```

**为什么是 `null` 而不是 `string.IsNullOrEmpty`**（任务里点名不许放宽）：
- 用 `IsNullOrEmpty` 会让这条与 `:123`「不同赛季 ⇒ null/空」**完全重合**，丢掉「**同赛季** + 未填」这个分支的检出能力 —— 把实现改成「同赛季也恒返 null」（例如误写 `return null;`）时，`:120` 那条「同赛季显示 32」仍会红，但本条不再是独立的第二道闸。
- 期望 `null` + 同赛季夹具同时钉住两件事：① 同赛季支**原样回读** `m.RankedRating`（不编造值）；② 未填 = null ⇒ 零 ⇒ 整块关闭，可见态与加闸前一致。
- **反过来改实现**（`return m.RankedRating ?? "";`）虽也能让这条绿，但会与实现 doc「返回 null ⇔ 评级 0」和 `:125`「空成员 ⇒ null」自相矛盾，且 `Rating()` 本来就吃 null ⇒ 不改生产侧。

**只做单 token 改动的备选**：仅把 `""` 改成 `null`（不改夹具）也能绿，但文案里的「同赛季」仍是假的（实际走的是不同赛季支） ⇒ 至少应把文案改成「不同赛季」或按上面把夹具补齐。推荐上面那一版。

## 四、与 W30（N130）是否交叉

**无交叉。**
- W30 = `Editor/B107_W20_Assert.cs`，N130 = 「切语言后四窗文本跟着变 + `RewardsWindow` 整窗重建」；该文件对 `Member` / `Alliance` / `Social` **0 命中**，既不读 `CurrentSeason` 也不读 `RankedRatingText`；宿主也不同（`RewardsScene` vs `CollectionScene`）。
- 唯一同族（都是「默认值相等」的隐式前提）：`Editor/MainMenuScene.cs:13097-13106` 的成员夹具写 `RankedRating = "32"` 但**没写 `Season`** ⇒ 今天显示得出来只因 `Season(0) == CurrentSeason(0)`。⚠️ 若将来有人在 MainMenu 宿主里把 `CurrentSeason` 置非 0，这两格评级会**静默变空**（不是今天的问题，记一笔即可）。

## 五、还差什么

1. **未实测**（红线：不跑 Unity）—— 以上按代码静态推：改后 `EqualityComparer<string>.Default.Equals(null, null)` ⇒ `true` ⇒ 该条转绿、合计变 2849/2849。**实跑结论待宿主跑一次 `CollectionScene.Run` 确认。**
2. 原版闸吃的是 **`member.Rating`（int）**，我们两格是**显示串**（`DraftRating` / `RankedRating`）—— `B107_W13_Assert.cs:13` 与实现 doc 已自述这一口径差异；本轮不涉及，**未核**它会不会在「显示串 = `"0"`」这类输入上与 `AllianceRatingDisplay.Initialize(value, disableIfZero:true)` 的 int 语义分叉（`Rating()` 的 `int.TryParse == 0` 会当 0 处理，属既有实现）。
3. 没查 `Draft Rating` 那格有没有同类「null vs 空串」的期望（本轮红只有这一条，其余 4 条同段 ✓）。
