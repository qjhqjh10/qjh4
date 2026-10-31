# B112_R3 · `targetedSpellAnim` 原版真值提取 + 施工规格（`B-105` 的数据那一半）

> 只读查判据代理 `B112_R3` · 2026-11-01 · 第一百一十一会话第 2 波。
> ⛔ 没跑 Unity、没动 git、没改任何 `.cs` / `.py` / 资产 / 账 / 正本 / 活状态表 / `工具/**`；**`d:/2` 一个字节没改**（只读）。
> 解包/探针脚本全部落在 `d:/4/_tmp_view/b112_r3/`，读的时候一律 `-I`；**没有执行包里解出来的任何东西**。
> 产出 = 本文件 + `判据/B112_R3_targetedSpellAnim.tsv`（1,351 行逐卡表）。

---

## 〇、一句话结论（先看这条）

🔴 **`RawCardScript.targetedSpellAnim(+0x130)` 在当前 build 的 1,351 张卡 SO 里【100% 为 null】**
（`{m_FileID:0, m_PathID:0}`，**1,351 / 1,351**；池内 **1,129 张同样 0 张有值**）。
拿**另一个 build 的老快照**（2022 时代 1,195 张）交叉验证：也是 **0 张有值**。

⇒ 原版 `BattleManager.StartTargetedSpellAnim` 的唯一用法是
`if (该字段 != null) Instantiate(prefab, pos, rot)`（`BattleManager__StartTargetedSpellAnim.c:31-35`）
⇒ **这条链在原版里始终走空**：`BattleManager+0x370`（那个实例）**从来没有被赋过非空值**，
`StopTargetedSpellAnim` 永远 `Destroy(null)`，`CancelActionStates.c:61` 里那个内联 `Destroy` 同样永远 `Destroy(null)`。

**⇒ `B-105` 的「数据缺口」不是「我们没提取」，而是「原版这份数据本身就是空的」。**
按 **铁律 11 例外①**（原版本身就没有 / 判据是空的）——**建这一对 `Start`/`Stop` 在我们的实现里同样是零可见行为**。
（施工规格仍然给全，见 §四；但请调度台按这条先决定「建 / 不建」。）

---

## 一、数据来源与可信度（为什么这条「0 张」不是假阴性）

### 1·1 那份 `__data` 是什么（定位 + 身份）

| 项 | 现读 |
|---|---|
| 路径（本任务用的那份） | `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data` |
| 同一份的**第二个副本** | `D:/2/关服前存档/安卓版本warpforge，包含远端资料/Everguild_Warpforge_Unity_Cache/Everguild_Warpforge/54bdb7b1…/ea8bca4f…/__data` |
| 两份是否相同 | **md5 完全一致** `178db0098ad88a4b3888cfbdd4a6e486`（大小都 531,962 B）⇒ 不必怀疑「读的是另一份」 |
| 头 | `UnityFS` / `5.x.x` / `6000.2.6f2`（内层 SerializedFile `version = 6000.2.6f2`） |
| 内层 CAB 名 | `CAB-fb8f01449289635703d83d52c3f18245` |
| `m_AssetBundleName` | `54bdb7b1114a2de41519cac560a4bce4.bundle` |
| 内容直方图 | `MonoBehaviour 1359` + `AssetBundle 1`（**0 个 GameObject / Prefab / Texture / ScriptableObject 独立资产**） |
| 1,351 的判据 | `m_Script.m_PathID == 8864902669260471755` 的对象 = **1,351 个 `RawCardScript`**（另 7 个 `AlternateArtCard` + 1 个 `AddressablesBuildNumber`）；与 `资料/普查产出_1018/allcards_1350/报告.md` §二 一致 |
| 是不是「当前 build 期望的那份」 | ✅ **是**：`D:/2/10.13游戏/Warpforge_Data/StreamingAssets/aa/catalog.bin`（PC build 的 addressables 目录）里逐字出现 `allcards_assets_all_ea8bca4f5c6909d3124fd4975dc96173.bundle` ⇒ 我们这份的**内容哈希就是 PC build catalog 点名的那一份** |

### 1·2 读法与交叉验证（四道）

1. **主通道**：`cards_raw.json`（`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`，67 MB，1,351 张 × `full_raw` 116 字段）
   —— 它本来就是 `gen_cards_engine.py:862` 的 `TRUTH_CARDS_SRC`，是本仓**既定的卡数据真值源**（不是我新开的通道）。
2. **控制列**（证明「不是整张卡都读成默认值」）：
   `whileInPlayAnim == 1` 有 **41 / 1,351** · `goSecondCardInHand` 非空 **64** · `relatedCard1` 非空 **212** ·
   `cardImageReference` 非空 **1,328** · `customRangedAttackParticles` 非空 **540** · `initiative` 取值 `{20:1349, 30:2}`。
   这些都与本仓已知的原版机制吻合（`goSecondCardInHand` = 「后手牌」、`whileInPlayAnim` 41 张 = 生成器 `:4233-4235` 已记的值）。
   ⇒ 同一个 `full_raw` 里别的字段有正常分布，**`targetedSpellAnim` 读成 0 不是因为解析器整体失败**。
3. **字段邻接对齐**（`dump.cs:23286-23293` 的原生偏移序）：
   `initialSpiritStoneModifier(+0x128)` → `initiative(+0x12C)` → **`targetedSpellAnim(+0x130)`** → `nonTargetedSpellAnim(+0x138)` → `secretIdentity(+0x140)` → `startingCardInHand(+0x148)` → `goSecondCardInHand(+0x150)`。
   前后两个邻居（`initiative` 20/30、`goSecondCardInHand` 64）都读出可解释值 ⇒ **这 12 字节没有错位**。
4. **独立老快照**：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1,195 张，**另一个 build / 另一路导出**）：
   `targetedSpellAnim` 非空 **0**（1,192 张显式 `{0,0}`、另 3 张根本没有该键）；`nonTargetedSpellAnim` 同样 0。

### 1·3 🔴 结构性判据：这份 bundle **物理上装不下一个 prefab**

- 全包只有 `MonoBehaviour ×1359` + `AssetBundle ×1` ⇒ **一个 `GameObject` 都没有**。
- 内层 SerializedFile 只有 **1 个 external**（`archive:/CAB-17b1b5f865c595045b5fe42b7f8c8967/…`，`AssetBundle.m_Dependencies = ['cab-17b1b5f8…']`）。
- 一个非空的 `PPtr<GameObject>` 只可能：① `m_FileID = 0` 指向本文件（**本文件没有 GameObject，指了也解析成 null**）；② `m_FileID = 1` 指向那个 external。
- **实测 1,351 张的 `m_FileID` 全部 = 0、`m_PathID` 全部 = 0**；同包里**有值**的 PPtr（`relatedCard1` 212 / `goSecondCardInHand` 64 / `startingCardInHand` 3）**也全是 `m_FileID = 0`** 且都指向同包的 `RawCardScript`。

⇒ 「全 null」不是读法问题，**是数据本身就这么序列化的**。

---

## 二、数据提取结果（表）

### 2·1 总表

| 口径 | 张数 | `targetedSpellAnim` 非空 |
|---|---|---|
| 当前 build 全部卡 SO | **1,351** | **0** |
| 其中池内（`cards_engine.json`，现读 `count = 1,129`；简报写的 1,127/1,128 是上一轮读数，⛔ 别写死） | **1,129** | **0** |
| 其中 `cardType == 10`（Hero / 督军 —— **唯一真正会被 `Start` 读的那一族**） | **66** | **0** |
| 2022 时代老快照（交叉验证） | 1,195 | 0 |

**按 `cardType` 分布**（SO 侧，`CardTypeOptions`：`Minion=0 · Hero=10 · Tactic=20 · Whispers=40`）：

| cardType | 张数 | 非空 |
|---|---|---|
| 0 Minion | 605 | 0 |
| 10 Hero | 66 | **0** ← 唯一会被 `Start` 读的一族 |
| 20 Tactic | 680 | 0 |
| 40 Whispers | **0（本池没有）** | — |

### 2·2 会走到 `Start` 的那一面（行为面，给调度台判断用）

原版 `CardScript__MoveIntoFieldFromHand.c:44-84`（亲读）：只有 **`cardType ∈ {0x14 Tactic, 0x28 Whispers}` 且 `targeted`** 才进那一组三件事；Minion（`cardType == 0`）支 `StopReflected` 后直接 `return`（`:45-49`）。
三件事本身在 **`:72-83`**：`:74` `GetHeroPos` → `:75` `StartSpellFrom` → **`:77` `StartTargetedSpellAnim`** → `:80` `DissolveWholeCard(VarsGlobal+0xB0 = 0.2)`。
- 池内 `cardType == 20` 的卡 **487** 张，其中 `targeted == true` **196** 张（`cards_engine.json` 现读；SO 侧 Tactic 是 680，两个口径不要混用）。
- 池内 `targeted == true` 全部 **246** 张。
- ⇒ **即使这 196 张每次都触发 `Start`，它们取的都是「当回合行动方的督军卡」的那个字段，而那 66 张督军卡 0 张有值。**

### 2·3 指向的 prefab 名单

**空**（去重后 **0 个**）：没有任何卡引用任何 prefab，因此**没有 guid / pathID 可给**。

### 2·4 本地有没有这些 prefab —— **没有「这些」**（三层查证）

| 查法 | 结果 |
|---|---|
| ① **有没有被引用**（本源） | **0 张卡**引用 ⇒ 名单为空 ⇒ **没有「目标 prefab」可谈**（这不是「没找到」，是「不存在引用」） |
| ② **按名字搜解包资源** | `d:/2/新解包资源/assets_full/`（**24.7 万文件 / 91 个 bundle**）：`*TargetSpell*` / `*SpellTarget*` / `*TargetedSpell*` **0 命中** |
| ③ **按 addressables 条目搜** | 本机 PC build catalog（`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.json`，1.7 MB）：`targetedspell` / `targetspell` / `targeted` 全 **0 命中** ⇒ 连一个叫这名字的 addressable 条目都没有 |

🔴 **结论口径**：**不需要「另外导」**（与 `B111_E3B` §⑦ 的猜测相反）—— 不是本地缺资产，是**原版数据里根本没有这个引用**。
⚠️ 也⛔ **不要**拿「`cardanimsgeneral_assets_all.bundle` 里有 571 个 AnimFX MonoBehaviour」这类东西去凑数：
那些是 `AnimFX` 组件对象，**没有任何一件被 `targetedSpellAnim` 指到**，用它们当「替代品」= 自己发明原版没有的行为（铁律 18·0 明令禁止）。

### 2·5 逐卡明细

`判据/B112_R3_targetedSpellAnim.tsv`（1,351 行 + 表头，制表符分隔）：

| 列 | 含义 |
|---|---|
| `uniqueId` / `cardName` / `cardType` | 卡本身 |
| `inPool` | 该 id 是否在我们 `cards_engine.json` 里（`Y` / `N`） |
| `engine_targeted` | 我们那边 `targeted`（`Y`/`N`，非池内为 `-`） |
| `tsa_fileID` / `tsa_pathID` | `RawCardScript.targetedSpellAnim` 原值（`+0x130`） |
| `ntsa_fileID` / `ntsa_pathID` | `nonTargetedSpellAnim` 原值（`+0x138`） |
| `whileInPlayAnim_control` | **控制列**（41 张为 `1`）—— 读它就能自证「不是整列默认」 |

---

## 三、🔴 一处必须订正的判据：**取的是【督军卡】的字段，不是被出的那张卡**

`B111_E3B` 报告 §4.4 第 3 步写的是「`Instantiate(<该卡的 targetedSpellAnim prefab>, Get2DPosOfCard(...))`」。
**这句不准确**。亲读 `BattleManager__StartTargetedSpellAnim.c`（全文 80 行）：

```
:21  cVar3 = IsPlayerTurn(bm);
:22  if (!cVar3) lVar4 = *(bm + 0xC8);        // PlayerManager enemyManager
:26  else      lVar4 = *(bm + 0xC0);          // PlayerManager playerManager
:28  if (lVar4 != 0 && *(lVar4 + 0x28) != 0)  // PlayerManager.heroCard  (dump.cs:40509)
:29    lVar4 = *(*(lVar4 + 0x28) + 0x28);     // EntityScript.rawCardData (dump.cs:21968)
:30  uVar6 = *(lVar4 + 0x130);                // RawCardScript.targetedSpellAnim (dump.cs:23288)
:31-35 cVar3 = Object.op_Inequality(uVar6, null); if (!cVar3) return;   // ← 全 null ⇒ 每张卡都在这儿返回
:52  Get2DPosOfCard(..., *(lVar4 + 0x28) /* = PlayerManager.heroCard */, useCenter: 1, 0);
:65  Quaternion.Internal_FromEulerRad(euler, ...)                        // euler × 0.0174533f（已用 read_literal 核实 = deg→rad）
:74  Instantiate(uVar6, position, rotation);
:75-76 *(bm + 0x370) = 实例;  ((+0x370 的写法寄存器包装)
```

**⇒ 正确语义**：`StartTargetedSpellAnim` 取的是**当前行动方（`IsPlayerTurn` 决定）的督军卡**的 `targetedSpellAnim`，
把它实例化在**督军卡**的 2D 位置上；**跟「正在打出的那张战术卡」无关**。

**`BattleManager+0x370` 的全库读写点（亲数，**3 处**；`Start` 里 `:76` 是同一处写的守卫封装，不计）**：
`StartTargetedSpellAnim.c:75`（唯一写）· `StopTargetedSpellAnim.c:11`（读→Destroy）· `CancelActionStates.c:61`（读→内联 Destroy）。

---

## 四、施工规格（可粘贴；⛔ 但先看 §〇 的判定）

> ⚠️ 前提：**本任务的结论是「数据 100% 为空」**。下面是「若调度台决定仍要做结构等价」时的完整规格。
> 按铁律 18·0，**不许**因为「反正是空的，随便编个 prefab 试试」——原版数据为空，我们也必须为空。

### 4·1 文件与行数（`BattleDriver.cs`）

- 路径：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`
- **不是 `partial`**：`:22` `public class BattleDriver : MonoBehaviour, INetBattleHost, INetChatHost, INetFoePresentationHost`
  ⇒ 「新建 partial 文件绕开文件所有权」**这条路不存在**，必须由**唯一写手整文件写**。
- 现读行数：**40,229 行**（`b.count(b'\n') = 40229`；`CRLF = 40229`；**裸 CR = 0** ⇒ 纯 CRLF，改的时候别翻行尾）。
- 🔴 **行数漂移**：加一个字段 + 两个方法 ≈ **+15~25 行**；这是**非 partial 的热点文件**，`账/*.tsv` 里大量按 `文件:行号` 钉着的行会整体漂 ⇒
  落地后**必须**由调度台在「没有其他 `.cs` 写手」时跑一次 `check_ledger_points --all-A --fix`，并按 CLAUDE.md「新成员追加到类尾」办。

### 4·2 数据侧（`cards_engine.json` / `gen_cards_engine.py`）—— 🔴 **本代理建议：不加这一列**

- 现读：`cards_engine.json` 里 `grep -c targetedSpellAnim` = **0**（该列不存在）；`count = 1129`。
- 若**非要加**（只为结构等价）：`gen_cards_engine.py` 有**两处**卡字典装配口，**必须同刻都加**（⛔ 别只加一处）：
  - 主循环：`:5197` 那一行 `entry["whileInPlayAnim"] = while_in_play_anim_of(...)` 的**下一行**；
  - 备用路：`:5836` 同一句的**下一行**（`with alternateArts` 那条装配）。
  取值 = `trow "targetedSpellAnim"` 的 `m_PathID`（**现读 1,351 张全是 0/null**）⇒ 落出来的列**全 null**。
- 🔴 **不建议加的三条理由**：① 全 null，任何消费者都拿不到东西；② 该字段是 **`PPtr<GameObject>`（`m_FileID`/`m_PathID`）**，**不是** `AssetReference`（`m_AssetGUID`）⇒ 本仓既有的那套「卡字段 GUID → 资产」索引（`Battle/BattleSfx.cs` 头注：28 个字段里 16 个解得出）**对这条通道用不上**；③ 加了以后每张卡多一个恒 null 的键，读起来像「我们支持这个」= 静默误导。
- **只交补丁、别改生成器**（简报要求）：上面两处行号就是补丁锚点；**本代理没有写任何补丁文件**（数据为空，补丁无内容可交）。

### 4·3 `Start` 的落点（我们这侧）

**落点 = `BattleDriver.cs:8058` `TryBeginPlayTargetPick(...)`，紧贴 `:8177` `if (!unitCard) DissolveWholeCardInField(card);`**
（原版这三件事 `StartSpellFrom` + `StartTargetedSpellAnim` + `DissolveWholeCard` 是同一段 `MoveIntoFieldFromHand.c:72-83`；
我们 `:8172-8176` 那条注释已经把判据抄过了 —— ⚠️ 它写的行号 `:60-75` 比真值偏早，见 §六·5）。

形状（照原版逐句）：

```text
// 原版 `BattleManager__StartTargetedSpellAnim.c:28-75`：
//   取 **当回合行动方督军卡**（PlayerManager.heroCard → EntityScript.rawCardData → RawCardScript+0x130）
//   的 targetedSpellAnim；空 ⇒ 直接 return（:31-35 的 op_Inequality 闸）。
//   本仓实测该字段 1,351 张全 null（判据 → 判据/B112_R3_targetedSpellAnim数据.md §二）⇒ 今天恒返回。
void StartTargetedSpellAnim()
{
    var hero = ViewAt(_me, RuleEngine.BoardSpec.WarlordSlot);   // 我们的督军卡视图（原版 PlayerManager.heroCard）
    // ⚠️ `TargetedSpellAnimOf` **现在不存在** —— 这是个**要新建的数据口**，而且它背后没有任何数据可给
    //    （卡表里没有这一列，原版 SO 里也是全 null）⇒ 它只会恒返回 null。
    var prefab = TargetedSpellAnimOf(hero != null ? hero.Data : null);
    if (prefab == null) return;                                  // 原版那道闸，⛔ 不许改成「兜底随便挑一个」
    _targetedSpellAnim = Instantiate(prefab, Get2DPosOfCard(hero, true), Quaternion.Euler(/* 见 §五·1 未查清 */));
}
```

⚠️ **`ViewAt(seat, BoardSpec.WarlordSlot)` 是我从 `BattleDriver.cs:3697/3738` 读到的「督军那一格视图」用法**；
我们**没有**一个叫 `PlayerManager.heroCard` 的等价字段 ⇒ 这是**行为等价映射**，不是逐句搬运（照铁律 18 要在注释里写明）。

### 4·4 `Stop` 的三个调用点（原版 4 处，其中 3 处走方法、1 处内联）

| # | 原版位置（亲读） | 原版时机 | 我们这侧的落点 | 把握 |
|---|---|---|---|---|
| ① | `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1144`（状态机 `case 0xc`，第一句 `+0x10 = -1` = 终态） | **出牌结算完成的那一拍**，紧接着进 `NeedsToChooseFromPool` 分支 | `ResolvePlayTargetByUnit`（`:8283`）成功落地后的尾部 / 或在 `PlayAsks`（`:11031`）之前 | 中（我们这条协程是拆开的，没有同名状态机） |
| ② | `BattleManager__Update.c:794`（**`case 4 = playingTargetedSpell`**，626 行起） | 等目标期间「自动动作入队 + `CardWaitingToBePlayed`」那一支 | `DrivePlayerTurn`（`:14929`）里等目标态那一支 | 低-中（**行为等价近似**） |
| ③ | `BattleManager__Update.c:825`（同一 `case 4`） | 「超时/放弃」支：`OutFromPlayingAreaAnim` → `StopTracking` → **`StopTargetedSpellAnim`** → `StopShowingPotentialTargets` → `UIstate = 2` | `CancelTargetChoosingByInvalidClick`（`:14895`，**同族里唯一带 `OutFromPlayingAreaAnim` 等价缺省的那条取消路**） | 中 |
| ④ | `BattleManager__CancelActionStates.c:61`（**不走 `Stop` 方法，直接 `Destroy(+0x370)`**） | 玩家主动取消正在出的牌（`+0x1a0` 攻击类型选择器那一支） | `CancelActionStates()`（`:36742`）→ `ResolvePlayTargetByUnit(null, -1, silent: true)`（`:36746`） | 高（那条 doc `:36725` 已经把这一句抄进去了） |

⚠️ **如实标**：我们这侧**没有 `UIstate 4` 这个显式状态机**（`BattleDriver` 用的是输入状态字典达，见 `:36730` 那句 doc）
⇒ ②③ 的映射是**行为等价层面**的近似，不是逐句对应。这一点写进代码注释，别写成「原版就是这样」。

### 4·5 `Stop` 的形状

```csharp
// 原版 `StopTargetedSpellAnim.c` 全文 17 行（去掉 class-init 守卫只剩 `:11` 读 `+0x370` + `:15` Destroy）：**不清字段**。
void StopTargetedSpellAnim()
{
    if (_targetedSpellAnim == null) return;          // Destroy(null) 本来也安全，这一句只为可读
    Destroy(_targetedSpellAnim);
    // 🔶 自己设计（非原版）：原版**不清** `+0x370`（销毁后仍留着野指针），我们清成 null，
    //   免得下一局/下一次 Stop 对已销毁对象再 Destroy 一次 · 判据 = 上面那两句 · 调度台 2026-11-01 由本规格提出，待裁
    _targetedSpellAnim = null;
}
```

（若调度台不想引 `🔶 自己设计`，就照原版**不清字段** —— 那一行为与 Unity 的 `Destroy(null-safe)` 语义等价，没有差别。）

### 4·6 断言怎么写（给宿主写手）

数据恒 null ⇒ **唯一能断的是「空跑不炸 + 不凭空造 prefab」**，别去断「播出来了」：
- ① `StartTargetedSpellAnim()` 在 196 张 targeted Tactic 的夹具上调用 **不抛**、`_targetedSpellAnim == null`；
- ② `StopTargetedSpellAnim()` 在没有实例时调用 **不抛**；
- ③ 🧨 **判别式**：夹具里若有人把 `prefab == null` 那道闸删成「兜底建一个空 GameObject」⇒ ② 之后 `_targetedSpellAnim != null` ⇒ 本条红。

---

## 五、还差什么 / 没查清（如实列，⛔ 一个都不许拿猜测填）

1. **朝向那个全局量的出处没解出来**：`StartTargetedSpellAnim.c:56-65` 的 euler 来自一个静态块 `DAT_1842da2a8 + 0xb8`
   （一个 `Vector3`，**三个分量都乘** `0.0174533f` 再喂 `Quaternion.Internal_FromEulerRad`；
   `read_literal.py` 现读 `0x1834b2dc0` = `0.0174533`，**已确认是 deg→rad**）。
   位置那边的 z 则是显式 `= 0`（`:66-70` 的 `local_30 = 0.0`）。
   但**哪个类/哪个字段**（`VarsDevice`？`ScenarioVariables`？相机？）**没解出来**（`dump.cs` 里没有该地址的静态字段行）。
   —— ⚠️ **它永远执行不到**（prefab 恒 null，`:31-35` 先返回）⇒ 现在不影响任何行为；要补只能在「字段真的有值」之后再查。
2. **②③ 两处 `Update` 调用点的映射是近似**：我们那边没有 `UIstate` 显式状态机（见 §4.4 末尾）。
3. **`StartSpellFrom`（三件事里的第 1 件）我们只有注释、没有同名实现** ——
   本代理只是 grep 到 `BattleDriver.cs:8174 / 16178 / 21433 / 34506 / 34509 / 35448 / 38102` 与 `TargetReticle.cs:639` 的**注释**，
   **没有逐句核它在我们这侧是否已由别的名字等价实现**（准星 + `PrepareMovement` + 点亮潜在目标那一套看着像，但那是**未验的推测**）。
   这属于 `B-105` 的另一半，**建议单独记一条账**（⚠️ 与 `B-105` 本身不同：它可能已经有等价物，只是名字不同）。
4. **`nonTargetedSpellAnim(+0x138)`**：本代理**没有**做「全库有没有读者」的完备证明（`D:/2/Warpforge_code` 那份是**空方法体桩**，字段只在声明处出现说明不了任何事；C 伪码里 `0x138` 又会撞别的类）。
   ⇒ **写成「未核」**，⛔ 不写「死字段」。反正 1,351 张也全是 null。

---

## 六、顺手发现（调度台分流用）

1. 🟡 **两份 `__data` 是同一份**（md5 一致）—— 以后「换一份再读一遍」这种复核**没有意义**，别浪费一轮。
2. 🟡 **`B111_E3B` 的 §4.4 第 3 步有语义错误**（「该卡」→ 实为「督军卡」）。若那份报告的施工规格被别的代理抄走，请以本文 §三 为准。
3. 🟢 **`allcards` 包物理上装不下 prefab**（只有 MonoBehaviour + 1 个 external CAB）—— 这条对**任何**「卡 SO 里的 prefab 引用」类字段都成立，
   不只是 `targetedSpellAnim`：**`RawCardScript` 上凡是 `PPtr<GameObject>` 的字段，在当前 build 里必然要么 null、要么指向那个唯一 external。**
4. 🟢 **PC build catalog（2026-02）点名 `allcards_assets_all_ea8bca4f…`** —— 说明这份**就是当前 build 期望的版本**，
   以后再有「是不是旧包」的疑问可以直接引这一条。
5. 🟡 **我们代码里那条注释的行号偏早**：`BattleDriver.cs:8174` 写「`CardScript__MoveIntoFieldFromHand.c:60-75`」，
   亲读实际是 **`:72-83`**（`:60` 那句是 `cardType != 0x28` 的早退、`:75` 是 `StartSpellFrom`）。
   `:35448` 那条注释同样引了这段。**建议该文件的写手顺手把行号改准**（本代理只读，没改）。
