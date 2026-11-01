# B114_WB2 · 附魔模板 `ca` 桥（`CardDatabasePartial_B110E5.cs`）

> 会话：第一百一十四会话 · 第 1 波 · 写手 `W-B`（第二张工单，接我上一份报告 §五·1 那条缺口）｜日期：2026-11-01
> 白名单：`Assets/RuleEngine/Data/CardDatabasePartial_B110E5.cs` + 本报告。其它一切没碰。
> ⛔ 未跑 Unity / `play check` / git。

---

## 一、结论

| # | 结论 |
|---|---|
| 1 | `MapEnchantmentTriggerAbilityIndices` 的 **`lg >= 0` 那支**已补 `Ca = (a.logic[j] != null) ? a.logic[j].ca : -1` |
| 2 | **`lg == -1` 那支保持 `Ca = -1`**（判据见 §四：那条 ability **没有 `AbilityLogic` 实例** ⇒ 原版根本没有 `chosenAbility` 可读；且**现读该支命中 0 行**） |
| 3 | **N = 135 行**（85 张模板里 `Ca != -1` 的行数，补丁前恒 **0**）；覆盖 **83 张**模板（另 2 张 `abilities: []`，本来就没有行） |
| 4 | 文件 **净 0 行**（`git diff --numstat` = `2 / 2`，两处都是**同物理行原地改**，且都落在行首 `//` 之前 —— 这两行本来没有行尾注释） |
| 5 | `TMPDIR=/tmp/wf_wb2 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；账闸只读复跑 **0 漂移 / 0 失败**（本文件在账里只被引用一处：`CardDatabasePartial_B110E5.cs:51`，在我编辑的 `:48`/`:80` 之外，未受影响） |

---

## 二、改动清单

| 文件 | 增/删 | 净 | 改了什么 |
|---|---|---|---|
| `Assets/RuleEngine/Data/CardDatabasePartial_B110E5.cs` | 2 / 2 | **0** | ① `:80`（`lg >= 0` 支的 `new CardDef.TriggerAbilityIndexSpec {…}`）同物理行加 `Ca = (a.logic[j] != null) ? a.logic[j].ca : -1` + 一句 `//` 说明；② `:48`（方法 summary 的「`trigger < 0` 的行出声+丢弃」那一行）就地追加 `Ca` 的口径与「`lg == -1` 为什么是 -1」 |
| `资料/完全复刻/报告/B114_WB2_附魔模板ca桥.md` | 新建 | — | 本报告 |

`git diff -U0`（逐字）：
```
@@ -48 +48 @@          // summary 那一行：… 丢弃**。</para>  →  同 + 「🆕 v47（B114W-B2）：lg>=0 的行带 Ca …；lg==-1 … Ca=-1（未知）」
@@ -80 +80 @@          // { Ab = i, Lg = j, Trigger = a.trigger, TriggerValue = a.triggerValue });
                       → { … TriggerValue = a.triggerValue, Ca = (a.logic[j] != null) ? a.logic[j].ca : -1 });  + // 说明
```

---

## 三、读数：`Ca` 已知行数 **0 → 135**

**口径**：一行 = 模板的一条 `abilities[i].logic[j]`；`Ca != -1` 即「已知」。

| 读数 | 值 |
|---|---|
| 模板总数 | **85**（`enchantment_templates.json` v1） |
| 有 `abilities[]` 的模板 | **83**（另 2 张是 `[]` = 明确没有：`Hive Fleet Leviathan` / `Saviour Protocols` —— 与文件头 §二 的既有记录一致） |
| 总行数（含 `lg == -1`） | **135** |
| **`Ca` 已知行（≥0）= N** | **135** |
| `Ca` 未知行（-1） | **0** |
| 其中 `lg == -1`（无 logic 行） | **0** ← 我**没动**的那一支今天命中 0 行 |
| 至少一行已知的模板 | **83** / 85 |
| 取值种类 | **19 种**；top：`10 doDamage 30` · `30 buffOthers 28` · `40 buffSelf 16` · `0 drawCard 12` · `60 buffMana 10` · `50 buffHand 8` · `340 removeThisEnchantment 8` · `20 doHeal 4` · `130 stun 4` · `13 doDamageFast 2` |

**为什么「补丁前恒 0」是事实、不是估计**：补丁前那两处 `new CardDef.TriggerAbilityIndexSpec {…}` **没有给 `Ca` 赋值**
（`git show HEAD:` 版本可核），而 `Ca` 的字段初始化就是 `-1`（`CardDef.cs:9229` `public int Ca = -1;`）
⇒ 85 张模板的 **135 行全部落在「未知」档**。补丁后同一批行全部变为真值。

**独立复核（不是自证）**：把 `enchantment_templates.json` 的 `abilities[].logic[].ca` 与
`cards_truth.json`（真值卡 SO）同模板的 `abilities[].logic[].chosenAbility` **逐模板逐行比**：
**85 张 / 135 行，0 处不符**（脚本 `d:/4/_tmp_view/wb2/enchant_ca.py`，`-I` 跑）。
⇒ 135 这个数是**真值侧的**，不是生成器自己绕一圈的产物。

---

## 四、`lg == -1` 那支为什么不补（判据，非偏好）

- 原版 `chosenAbility` 是 **`AbilityLogic` 的字段**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityLogic.cs:9`
  `public AbilityEffect chosenAbility;`）。`lg == -1` 的含义由本文件 `:70-76` 定死 = **这条 `CardAbility` 的
  `abilityLogic` 为空 / 为 null** ⇒ **根本不存在任何 `AbilityLogic` 实例** ⇒ 没有 `chosenAbility` 这个值。
- 原版那条路径对空 logic 就是一个空循环（本文件 `:72` 的注释已记），所以「`Ca = -1`（未知）」与行为自洽：
  这一行**唯一**的用途是让 `TriggerAbilityRowsOf(trigger)` 有它（原版只按 trigger 过滤、不看 logic 条数），
  而它落到执行侧本来就「没有动作可排」。
- 现读该支在池内模板上命中 **0 行** ⇒ 无论补不补都零可观测；补了反而会**造出原版没有的值**（铁律 18·0：判不出来就 -1，⛔ 别猜 0）。

---

## 五、验收对照

| # | 要求 | 结果 |
|---|---|---|
| 1 | 两处都补上、**净 0 行**、⛔ 不插行 | ✅ `2 / 2`；两处都是同物理行原地改。`:80` 那行**原本没有行尾 `//`**（不存在「代码落进注释」那种坑）；`:48` 是 `///` 文档行，新文字追加在既有的 `</para>` **之前** |
| 2 | 补一条断言或读数，证明 0 → N，N 写进报告 | ✅ **读数**（§三）：N = **135** 行 / 83 张模板；独立真值复核 0 不符。说明见下 |
| 3 | `TMPDIR=/tmp/wf_wb2 typecheck` → 0 / 0 | ✅ 运行时 0 / 编辑器 0 |
| 4 | ⛔ 不跑 Unity / `play check` / git | ✅ 一条没跑 |

**关于「断言」为什么只给读数**（如实说明，等你裁）：
本波白名单只有**这一个数据文件**，而真正的断言宿主是 `Assets/CardPresentation/Editor/B110_E5_Assert.cs`（不在白名单、且本波禁跑 Unity）。
我**没有**在这个数据文件里塞一个「静态计数器」——那会① 是零调用方的死代码（项目明令不许），② 把「哪些行存在」这条规则再实现一遍（两处写同一条规则 = 迟早不一致）。
现成的、不需要新代码的断言口就在 `TriggerAbilityIndices` 上，**可粘贴片段**（谁拿到 `B110_E5_Assert.cs` 就贴进 C 组）：

```csharp
// 🆕 v47（B114W-B2）：附魔模板的 Ca 桥 —— 读数 135/135 行已知（补丁前恒 0）
var __tpls = CardDatabase.LoadEnchantmentTemplates();
int __rows = 0, __known = 0;
foreach (var __t in __tpls)
{
    var __idx = __t.TriggerAbilityIndices;
    if (__idx == null) continue;
    foreach (var __r in __idx) { __rows++; if (__r.Ca >= 0) __known++; }
}
Check(__rows, 135, "★ Ca 行数 = 135（85 张模板的 abilities×logic 行）");
Check(__known, 135, "★★ Ca 已知行 = 135（🧨 把 :80 的 `Ca = …` 删掉 ⇒ 恒 0 ⇒ 红；把 `lg == -1` 支也硬塞值 ⇒ 行数/已知数会变）");
```

---

## 六、没查清 / 顺带

1. **运行期读数本波验不了**（同上一次工单的理由：`JsonUtility` 只能在 Unity 里跑）。间接证据：typecheck 0/0（证明 `EnchantmentTemplateLogicDto.ca` 这个字段真的存在且可读）+ 数据侧 135/135 + 真值侧 0 不符。
2. 本文件 `:14` 那句「形状照卡表主路 `MapTriggerAbilityIndices`（`Data/CardDatabase.cs:4785-4810`）」的行号**已经落后**（本波 v47 之后那段在 `:4782-4840` 一带）。
   我**没有**改它 —— 它不在本次验收项内，且改了会动到同一行里的其它说明；要修的话是一行原地改，留给调度台一并处理。
3. `enchantment_templates.json` 侧**零改动**（`ca` 那一列早就在，`gen_cards_engine.py:5053`），本轮只补 C# 的搬运 —— 所以本波**没有重跑生成器**，卡表也一个字节没动。
