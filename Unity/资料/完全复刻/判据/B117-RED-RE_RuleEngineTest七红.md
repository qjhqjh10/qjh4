# B117-RED-RE · `RuleEngineTest.Run` 七条红 —— 断言/夹具侧修正

> 执行代理 `B117-WRED-RE` · 2026-11-02 · 基线 = `_tmp_view/ruleengine.log`（12:06 · `13998/14005 通过 · 7 条失败`）
> ⛔ 本件**只动断言与夹具**，引擎（`RuleCore` / `EffectResolver` / `GameplayVariables`）**一个字节没改**。
> ⚠️ **实跑由调度台统一做** —— 本件不宣称「应该会绿」，只给「改了什么 + 新期望值怎么推出来」。

---

## 一、七条红 → 三处改动

| # | 红（原文） | 落点 | 根因 | 改法 |
|---|---|---|---|---|
| 1 | `同方场上带 oathDouble 的牌数 = 1` 期望 1 实得 0 | `RuleEngineTest.cs:18049` | 夹具 `cDouble` 只有**正文 bool**，没挂 `cardTraits`；`OathExtraReplays` 读 `HasCurrentTrait("oathdouble")`（原版 `BattleManager__IsThereDoubleOathEffect.c:33` 读 trait 表） | 给 `cDouble` 补 `AttachCardTraits` |
| 2 | `★ 正文跑了 2 遍（护甲 0 → 2）` 期望 2 实得 1 | `RuleEngineTest.cs:18052` | 同 #1（重放次数 = `OathExtraReplays`，为 0 ⇒ 只跑 1 遍） | 同 #1，**没有单独改这一行** |
| 3 | `（前提）经典局的督军卡面血 = 25` 期望 25 实得 30 | `RuleEngineTest.cs:4949` | 夹具`督军`从来不是那张 25 血的手牌卡，而是 `Deck()` 垫的 `Hero("FixtureWarlord", 2, 30)` | 前提改成 30 |
| 4 | `（前提）遭遇局的督军卡面血 = 25` 期望 25 实得 30 | `RuleEngineTest.cs:4950` | 同 #3 | 前提改成 30 |
| 5 | `经典：督军生命 = 卡面 25 + 10 = 35` 期望 35 实得 40 | `RuleEngineTest.cs:4951` | 同 #3（`B117_F6` 已把经典改回 `+10`，卡面 30 ⇒ 40） | 期望 = `卡面血 + cv.warlordLifeChange`（= 40） |
| 6 | `遭遇：督军生命 = 卡面 25 + 0 = 25` 期望 25 实得 30 | `RuleEngineTest.cs:4952` | 同 #3 | 期望 = `卡面血 + sv.warlordLifeChange`（= 30） |
| 7 | `前提：那个文本解析出来的 bool CardDef.OathDouble 为假` 期望 True 实得 False | `RuleEngineTest_B116W19.cs:492-494` | **断言本身写错了**：`UM100` 的 desc 就是 `Friendly Oath abilities apply an additional time.` ⇒ `MatchStaticBattleRule` 命中「誓约加倍」⇒ bool **为真**。原文案「全池 0 张正文写誓约加倍 ⇒ 旧判据恒 false」是**假命题** | 断言反转为 `CheckTrue(cassius.OathDouble, …)`，并**改写结论文字**（见 §三） |

---

## 二、逐处改动（`文件:行号` + 旧值 → 新值）

### A. `RuleEngineTest.cs:17969`（行数守恒：**同一物理行追加**，⛔ 该行行尾没有 `//` 注释，不存在「代码落进注释」风险）

```csharp
// 旧
            "common", "Test", 1, 2, 2, 0, noKws, subtype: "Infantry");
// 新
            "common", "Test", 1, 2, 2, 0, noKws, subtype: "Infantry"); cDouble.AttachCardTraits(new[] { "oathDouble" });
```

写法**照 `CardPresentation/Editor/B97_ER2_Assert.cs:162` 的既有先例**（`a.AttachCardTraits(new[] { "oathDouble" });`），不自创。
归一化链：`AttachCardTraits` → `TraitKeyOf` → `KeywordTable.Normalize("oathDouble")` = `"oathdouble"`
（`KeywordTable.Prefixes` 那三条 oath 变体排在 `"oath"` **之前**，`B117_F8` 已根治前缀撞车）。

`RuleEngineTest.cs:17971` 的断言文案同步补一句「同刻挂了 `cardTraits`」，**`CheckTrue(cDouble.OathDouble, …)` 这条断言本身保留**（它验的是正文解析，仍成立）。

**为什么 #2 跟着绿**：`EffectResolver.cs:9388/9392` 的 `for (int i = 0; i <= OathExtraReplays(ctx, owner); i++)`，`replays = 1` ⇒ 跑 2 遍 ⇒ 护甲 `0 +1 +1 = 2`；付费只在 `i == 0`（`i > 0` 时 `SuppressCostDepth++`）⇒ 能量仍是 `5 → 4`。

### B. `RuleEngineTest.cs:4949-4952`（行数守恒：4 行 → 4 行）

| 行 | 旧 | 新 |
|---|---|---|
| 4949 | `Check(ck…Card.Health, 25, "…卡面血 = 25")` | `Check(ck…Card.Health, 30, "…卡面血 = 30（= `Deck()` 垫的 `Hero(\"FixtureWarlord\", 2, 30)`…）")` |
| 4950 | `Check(sk…Card.Health, 25, …)` | `Check(sk…Card.Health, 30, …)` |
| 4951 | `Check(ck…MaxHealth, 25 + 10, "…35")` | `Check(ck…MaxHealth, ck…Card.Health + cv.warlordLifeChange, "…卡面 30 + 10 = 40…")` |
| 4952 | `Check(sk…MaxHealth, 25, "…25")` | `Check(sk…MaxHealth, sk…Card.Health + sv.warlordLifeChange, "…卡面 30 + 0 = 30…")` |

**新期望值怎么推出来（不写死数字，全部可从代码推）**：
1. 夹具督军 = `Deck()` 的 `Hero("FixtureWarlord", 2, 30)`（`RuleEngineTest.cs:2635`）；`BuildPlayer` 只把卡组里**第一张** `type == "hero"` 当督军（`RuleCore.cs:628`）⇒ **卡面血 = 30**。
2. `cv.warlordLifeChange` = **10**、`sv.warlordLifeChange` = **0** —— 相邻的 `RuleEngineTest.cs:4895/4896` 已经单独断过。
3. `BuildPlayer` 只做 `MaxHealthBase += ctx.Vars.warlordLifeChange`（`RuleCore.cs:648-654`）⇒ 经典 `30+10 = 40`、遭遇 `30+0 = 30`。
4. `:4953` 那条「两档相差 10」**没动**，仍成立（`40 − 30 = 10`）。

> ⚠️ **`umC` / `umS` 不是督军**：`BattleVars(new[]{ umC }, …)` 走 `Deck()`，`Hero("FixtureWarlord",2,30)` 已在列表**第一位** ⇒ `umC` 只是牌库里的一张卡。原注释「必须用真卡的 25 血」是**错的**，已在 `:4943` 就地订正（铁律 5），保留更正痕迹。
> ⛔ **没有改 `GameplayVariables`** —— `B117_F6` 的 10/0 是已裁定照原版的值。

---

## 三、第 7 条：改写后的注释文案（`RuleEngineTest_B116W19.cs:492-494`）

```csharp
                CheckTrue(cassius.OathDouble,
                          "前提：`UM100` 的正文**会**解析出 `CardDef.OathDouble = true`（desc = `Friendly Oath abilities apply an additional time.` ⇒ `MatchStaticBattleRule` 的「誓约加倍」）"
                        + "—— 旧实现**不是**「恒 false」：真正的缺口是它读的是【被激活的那张 oath 卡】而不是【来源卡】（旧 `RuleCore.OathExtraReplays` 读 `u.Card.OathDouble`，`u` 是被激活的 `UM101`，不是 `UM100`）");
```

事实依据：`CardDef.MatchStaticBattleRule` 的 `tb == "friendly oath abilities apply an additional time"` → 规则名「誓约加倍」→ `AddStaticBattleRule` 置 `OathDouble = true`（`CardDef.cs:4374` / `:4401`）；
基线日志也反证了这一点 —— 该行报的是「**期望 True，实得 False**」，即 `!cassius.OathDouble` 为假 ⇒ `OathDouble == true`（`_tmp_view/ruleengine.log:1019611`）。

---

## 四、自证

- `TMPDIR=/tmp/wf_wred bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  （第一次跑出现 5 条 `CS0103`，全在 `SettingsPanel.cs` / `CardChoicePanel.cs` —— **不是我碰的文件**，是并发写手；隔 90 秒复跑即 0/0。）
- `git diff --numstat`：`RuleEngineTest.cs` **8/8** · `RuleEngineTest_B116W19.cs` **3/3**；**行数 39,763 → 39,763 · 681 → 681（完全守恒）**，行尾未被翻（不是整篇重写）。

---

## 五、我判断仍可能红 / 需要复核的点（如实写）

1. **#1 依赖 `AttachCardTraits` 在 `Place` 之前生效**：`Place` 用 `ctx.NewInstance(card)` 把**同一个 `CardDef` 引用**交给 `UnitState`，`HasCurrentTrait` 读的就是它 ⇒ 顺序没问题；但**我没有实跑**，以调度台的实跑为准。
2. **#2 前提是「只有 `cDouble` 一张带 `oathdouble`」**：⑥ 那个 `ctx` 的棋盘上只有 `oath1`（牌面 `cardTraits` 为 null）与 `cDouble` ⇒ 计数 1。若 `Place` 之外有别的来源被同时摆上去，期望值会变 —— 现读没有。
3. **#3–#6 的 `MaxHealth` 表达式是「按现值推」而不是写死 40/30**：若将来有人把夹具督军从 30 改成别的血，这几条会**跟着变绿**（弱化但不会假绿 —— `:4949/:4950` 的两条前提会先红）。
4. **第 7 条只改了断言与文案**，`③-b`（`OathExtraReplays == 1`）等下游断言原本就是绿的，不受影响。
