# B97_US · 报告（第九十七会话 · 执行代理 `B97_US`）

> 一件活：账 `战斗逻辑驱动.tsv:521`（`BattleManager.ResolveJamUnit(BattleAction)`）的 **② 那两跳** ——
> `UnitState.RemoveAllAppliedEffectsForJam` 里缺的「`permanentBuff(+0x74)` 早退」与「清修饰后夹血」。
> 判据输入 = `资料/完全复刻/判据/B97_可推A七条_审查.md` §一·5（该节列了 3 条「还差什么」：① 夹血、② permanent 早退、③ 把 ③ 的覆盖等价论证抄进账的差异列）。
> 白名单内改动：`RuleEngine/Core/UnitState.cs` · 新建 `CardPresentation/Editor/B97_US_Assert.cs` · `账/_parts/B97_US.tsv` · 本报告。
> ⛔ 未碰 `Editor/RuleEngineTest.cs` · 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git · Unity 批处理。

---

## 〇、一句话结论

两跳都补上了，**零行数变化**（`UnitState.cs` 前后都是 **3,684** 行、无裸 CR）；`typecheck` **运行时 0 / 编辑器 0**；
账补丁 `_parts/B97_US.tsv` 把 `:521` 的 `状态` 由 **B → A**，`--dry` 合并演练 = **更新 1 行 · 新增 0 · 无变化 0**。

---

## 一、改了什么（3 处代码，全在 `RuleEngine/Core/UnitState.cs`）

### ① 新增数据槽 `StatMod.Permanent`（原版 `CardEffect.permanentBuff +0x74`）

- **落点**：`UnitState.cs:1853`（**追加在同一行**，⛔ 不新增行）—— `public string Source; public string Trait; public int BuffType; public bool Permanent;`
  同行的 doc（`:1852`）就地扩写，写明判据与「生产点尚未接线」。
- **判据**：`dump.cs:119100` —— `public bool permanentBuff; // 0x74`，上面一行 `[Tooltip("Can't be jammed")]`（现读 `d:/2/tools/il2cpp_out/dump.cs:119099-119100`）。
- **为什么必须新增这个位**：审查 §三·4 明说「`StatMod` 今天有没有 permanent 这个位要先现读」——**现读：没有**（`StatMod` 原字段 = `Attr` / `Value` / `Extrinsic` / `IsSet` / `Source` / `Trait` / `BuffType`）。
  `Permanent` 原本只存在于 `EffectRecord`（`UnitState.cs:2721`，那是**逐条摘的入参**，不落表）⇒ 挂在 `_statMods` 里的那批**没有任何地方记过它**，不建这一格就无从「按 permanent 跳过」。

### ② `AddStatMod` 收一个尾部可选形参 `permanent`（缺省 `false`）

- **落点**：`UnitState.cs:1867`（签名尾部 `…, string trait = null, bool permanent = false`）＋ `:1882`（对象初始化器追加 `Permanent = permanent,`）。**零行数变化**。
- 既有 **19 处**调用点（`grep -rn "AddStatMod("` 共命中 **20** 处，其中 1 处是声明行本身）**一处都不用改** —— 尾部可选形参，位置实参语义不变。
- ⚠️ **生产点尚未接线**：`EffectResolver.cs:12140/12143/12154/14862/14953-14955`、`Aura.cs:830`、`RuleCore.cs:4626-4629` 都还没传它 ⇒ 今天 `Permanent` 恒 `false`。
  这与 `B97_W1` 刚在本文件建的 `GrantRecord.Caster` 是同一处置（休眠，铁律 11 照建）。

### ③ ④ 那一段：整表清 → **倒序逐条摘 + 跳过 permanent + 夹血**

`UnitState.cs:2202-2204`（3 行换 3 行，**零行数变化**）：

```csharp
// 改前
n += _statMods.Count;
_statMods.Clear();
// 改后
for (int i = _statMods.Count - 1; i >= 0; i--) { if (_statMods[i].Permanent) continue; _statMods.RemoveAt(i); n++; }
ClampHealthToMax();
```

方法头注的逐本账清单（`UnitState.cs:2175`）也就地改写成「逐条倒序摘，`Permanent` 那条不清 …… 摘完把当前生命夹到新上限」。

---

## 二、证据（原版 `.c` 逐行现读）

### 跳 A · `permanentBuff` 早退

| 判据 | 现读行 |
| --- | --- |
| 被 jam 那一支是**逐条**摘，不是整表清 | `d:/2/tools/decomp_full/CardScript__ReactToUnitJammed.c:59-69` —— `:59` 取 `activeEffects(+0x108)` 长度、`:61` 倒序递减、`:65` 取第 `iVar6` 项、**`:67 CardScript__RemoveEffect(param_1, uVar9, 1)`**（第三实参 `1` = `fromJam`） |
| 每条都先过 permanent 闸 | `d:/2/tools/decomp_full/CardScript__RemoveEffect.c:43-45` —— `if (*(char *)(lVar12 + 0x74) != '\0') { return; }`（**在** `activeEffects.Remove` 之前，`:46-48` 才是 Remove） |
| 字段名 / 偏移 | `dump.cs:119099-119100` —— `[Tooltip("Can't be jammed")]` / `public bool permanentBuff; // 0x74` |

⇒ 我们原来是 `_statMods.Clear()` 一把全清 ⇒ **带 permanentBuff 的那条会被一起摘掉**（原版留着）。现在按条跳过，`n` 也只报真摘掉的。

### 跳 B · 清完修饰夹血

| 判据 | 现读行 |
| --- | --- |
| 摘 `addStats` 且**带血量改动**时把当前生命夹到新上限 | `CardScript__RemoveEffect.c:149-175` —— `:149` `if ((iVar7 == 1) && (*(int *)(lVar12 + 0x60) != 0))`（`buffType==1 ∧ healthChange(+0x60)!=0`）；`:150-153` 取 `currentHealth(+0x68)` 那 5 个 `ObscuredInt` 字；`:160` `iVar7 = currentMaxHealthProcessed`；`:161-163` `if (iVar8 < iVar7) iVar7 = iVar8;` ⇒ **`min(currentHealth, 新上限)`**；`:170-174` 写回 `+0x68` |

⇒ 我们整批摘完不夹 ⇒ 生命会**高于**新上限。落点用既有零件 `ClampHealthToMax()`（`UnitState.cs:1911-1915`，语义「只夹、不减」，同文件 `:1903-1910` 的 doc 已把这条 `.c` 判据写在案）。
位置在 ④ 末尾（表已摘完）⇒ 看到的是**摘完的** `MaxHealth`，正是原版那一刻的 `currentMaxHealthProcessed`。

### 反向闸（防「保护做过头」）

`CardScript__ResetToValuesInHand.c:26-33`：对 `activeEffects(+0x108)` 是**整表 `List.Clear()`**（`+0x18 = 0` + `System_Array__Clear`），**全文不看 `permanentBuff`**
⇒ 原版里 permanent 只保护「jam 那条逐条摘」的路，**不**保护整表重置。我们的 `ResetToValuesInHand`（`UnitState.cs:2622` 的 `_statMods.Clear()`）**照旧整表清**，本次**没改**（并写了断言钉住）。

---

## 三、typecheck

```text
TMPDIR=/tmp/wf_us bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（独立 `TMPDIR`，按铁律 13·3 避免与别的代理互相覆盖输出。）

**零行数漂移的另一道机检**：`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A`
⇒ 全表所有指向 `UnitState.cs` 的落点**仍是 `OK`**（本次改动零行数变化，故不可能引入新漂移）。
⚠️ 该次跑出的 `漂移 37 / 失败 8` 是**工作树里别人在飞的活**（全部落在 `SimpleAI.cs` / `RuleCore.cs` 等**不是本代理**改的文件上），⛔ 不是本笔引入。

---

## 四、账补丁 `账/_parts/B97_US.tsv`

- 格式：**11 列 + 首行表头**；`方法` 列 = 完整签名 `ResolveJamUnit(BattleAction battleAction)`（与主表 `:521` 逐字相同，⛔ 没改签名写法）；LF 行尾（主表也是 LF）；**逐列往返比对通过**（读→写→再读，11 列全等）。
- 定位键 = `(原版类, 方法)` = `("BattleManager", "ResolveJamUnit(BattleAction battleAction)")` —— 生成时按这个键在 `战斗逻辑驱动.tsv` 里找到**恰好 1 行**（物理行 521），⛔ 没有按硬编码行号取。
- 改动的列：

  | 列 | 改动 |
  | --- | --- |
  | `状态` | **B → A** |
  | `判据出处` | 追加 `· CardScript__RemoveEffect.c:43-45/149-175`（主表里用「 · 」接在原值后） |
  | `判据要点` | 追加「逐条 `RemoveEffect(e, fromJam:true)`（`:59-69`）→ 每条过 permanent 闸（`:43-45`）→ 摘 addStats+health 时夹血（`:149-175`）」 |
  | `差异` | 追加一整段（见下「判回 A 的依据」；含**只写我核过的**那几条 + 仍如实留的残余） |
  | `轮次` | 追加 `｜B97_US` |
  | `备注` | 追加「两跳已补 ⇒ 升 A；断言 = `CheckB97USJam()`；typecheck 0/0；未覆盖 = `_buffs`/`_grants` 无 permanent 位」 |
  | 其余 5 列 | **逐字照抄主表现读内容**（域 / 原版类 / 方法 / 我们的落点 / 等级） |

### 判回 A 的依据（= 写进 `差异` 列的那段，摘要）

1. **①（顺序）** 已由 `B95_WRE` 按原版改回（`CheckIfDead` 在广播之前）。
2. **②（本次）** `permanentBuff` 早退 + 清修饰后夹血，两跳都落到 `RemoveAllAppliedEffectsForJam` ④（`UnitState.cs:2202-2204`）；判据逐行见本报告 §二。
3. **③（覆盖等价）** 照审查原话记进账：`Auras.RefreshInPlayNumericAndKeywords` 是 `UpdateWhileInPlay(false)` 的**覆盖等价位**（既有两条同口径先例 = `ActionPump.cs:143` / `EffectResolver.cs:17027`；`Aura.cs:975-993` 的 doc 也把原版 20/240 两档写成这个口）；且 `FireTriggerAt` **没有**「这张卡算不在场」那个形参（原版靠 `param_4 = null` 表达）、也不去重 ⇒ 照字面改真调用会**叠 buff** ⇒ **现状是对的**。
4. **休眠如实记**：池内 `jam` 生产者 **0 张**；且全表唯一一条 `permanentBuff != 0` 是 `Living Icon` 那张附魔模板 `0474b16360827b6408fb02b6820e7e63` 的 **`buffType 2 changeCost`**（`CardDef.cs:9013-9014` 记的真值）⇒ 它本来也**不进 `_statMods`**。
   ⇒ 端到端**零可观测变化**（铁律 11：休眠 ≠ 不做，所以照建并配断言）。

### 合并指令（调度台执行）

```bash
python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py                 # 已跑：✅ 没有跨补丁重复键（3 份补丁）
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B97_US   # 已跑：会写 战斗逻辑驱动.tsv / 更新 1 · 新增 0 · 无变化 0
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B97_US
```

---

## 五、待接线那一行（⛔ 本代理没动宿主）

- **方法名**：`CheckB97USJam()`（在 `CardPresentation/Editor/B97_US_Assert.cs`，`public static partial class RuleEngineTest`；⛔ 不带 `Test`/`Report` 前缀、**不用** `Step(...)`，理由同 `B96_CR` / `B97_W1`）。
- **建议插在**：`RuleEngine/Editor/RuleEngineTest.cs` 的 **`{ CheckB97W1Grants(); }` 那一行（现读 = 第 1294 行）之后**，即：

  ```csharp
          // 🆕 2026-10-29（调度台代接线）：`B97_US` —— 账 521 `ResolveJamUnit` ② 的 `permanentBuff` 早退 ＋ 清修饰后夹血。
          { CheckB97USJam(); }
  ```

- **断言内容（3 段）**：
  1. `B97USJamClampsHealthToNewMax` —— 普通 `addStats +4 血`，生命 8 / 上限 9 ⇒ jam 后上限 5、**生命 5**（删掉末尾 `ClampHealthToMax()` ⇒ 实得 8）。
  2. `B97USJamKeepsPermanentStatMod` —— `AddStatMod("health", 4, permanent: true)` + 普通 `attack +3` ⇒ 摘完 **上限仍 9 / 近战回 2 / 计数 1 / 表里恰 1 条且是 permanent 那条**（退回 `_statMods.Clear()` ⇒ 上限 5；把 permanent 也数进 `n` ⇒ 实得 2）。
  3. `B97USResetToValuesInHandStillClearsPermanent` —— 反向闸：另一条清账路 `ResetToValuesInHand` **照旧整表清**（原版 `ResetToValuesInHand.c:26-33` 不看 permanent）；把保护做成全局 ⇒ 红。

**已核「不会顶掉既有断言」**：现读三处既有 `ApplyJam()` 用例的夹具都是「血量 = 上限、无 `_statMods`」⇒ 夹血是 no-op：
`RuleEngineTest_EngineB94B2.cs:126`（3 血，`CheckTrue(!died)`）、`RuleEngineTest_EngineB95WRS.cs:49`（3 血）、`RuleEngineTest_EngineB94B14.cs:97-121`（5 血、两条 `Instance` 账，走 `RuleCore.ResolveOps`）。

---

## 六、没查清 / 如实留的残余

1. **`_buffs`（`TempBuff`）与 `_grants`（`GrantRecord`）两本账没有 permanent 位** —— 原版是**逐条过整张 `activeEffects`**（`:59-69`），permanent 闸对**任何** `CardEffect` 都生效；我们这次只把这一格建在 `_statMods` 上（审查 §一·5 的修法②明确写的就是「`_statMods` 里标了 permanent 的那批不清」）。
   ⇒ 若将来有 permanent 的**限时增益**或 **grant 型**效果，那两本账仍会被 jam 清掉。**今天无此类数据**（全表 `permanentBuff != 0` 只有 1 条，且是 `buffType 2 changeCost`）。**没查清**：`TempBuff`/`GrantRecord` 要不要也收这一列，得看 `EffectResolver.ApplyOneGain` 那条路怎么把 `CardEffect` 投进我们这两本账 —— 那属别的写手的文件，本笔不擅自造第二件数据槽。
2. **生产点未接线**（见 §一·②）：`EffectResolver` / `Aura` / `RuleCore` 的 `AddStatMod` 都还没传 `permanent`。要真接，需要一个「这条 spec 是不是 `permanentBuff`」的判据入口（`CardDef.PermanentBuffAt(ability, logic)` / `PermanentBuffSpec` 已有，`CardDef.cs:9015-9080`），但**当前所有 `AddStatMod` 调用点都拿不到 `ability/logic` 下标** ⇒ 接线本身是另一笔活（会碰三个非白名单文件）。
3. **`healthChange != 0` 那个闸我们没有**：原版只在 `buffType==1 ∧ healthChange!=0` 时夹血；我们整批摘完**无条件**夹一次。净行为等价（没有血修饰被摘 ⇒ 上限没变 ⇒ `ClampHealthToMax` 是 no-op），但这是**形状差**，如实记在这里。
4. **没有跑 Unity 自检**（铁律 13·3：子代理不跑批处理）⇒ `CheckB97USJam()` 的三段断言**尚未实际执行过**，等调度台接线后由主对话在同步点跑。
