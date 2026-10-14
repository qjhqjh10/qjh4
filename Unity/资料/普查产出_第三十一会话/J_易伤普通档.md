# J · 引擎伤害管线两笔（`A1335①` 第三档 = `N7` ＋ 同批 `N8`）

> 会话：第三十一会话（2026-10-14）· 写手 `J`
> 判据第一权威一律 = `d:/2/tools/decomp_full/` 的反编译方法体，**逐行现读过**（不是转述账文）。
> 范围 = 施工简报里那两笔；⛔ 没跑 Unity 批处理 · 没动 git · 没改正本。
> 唯一施工图 = 写手 `D` 的 `资料/普查产出_第三十一会话/E_引擎批A.md`（`A1335①` 那一节）。

---

## 一笔一结

### `N7` · `A1335①` 第三档（普通档的 `:200` + `:210-211` 闸）—— ✅ 落地

- **结论**：原版易伤那一发（**另一条独立的** `DealMultiDamage`，`:252-254`）前面有一道**三道合一的闸**：
  **这一发进管线的量 > 0**（`:200`）∧ **主伤害落完之后血还 > 0**（`:210`）∧ **不是残骸**（`:211`）。
  改前我们在**普通档**（既无空投舱、也无堡垒）把「主伤害 + 易伤那一份」**先加起来再判血**
  ⇒ **主伤害本来就能打死它时会多算那一份**（影响 `ApplyDamage` 的**返回值** = 事件 `Amount` /
  践踏溢出量；血本身由 `CleanupDeaths` 兜住，不会多掉）。
  ⇒ 现在闸落在 `RuleCore.DamageAfterReduction` 的最后两行，**结算侧与预览侧各照自己的原版**。

- **判据（现读 `d:/2/tools/decomp_full/CardScript._ReceiveDamage_d__381__MoveNext.c`，行号即文件行）**：

  | 行 | 内容 | 我们落点 |
  |---|---|---|
  | `:56-60` | 挡下三连 `0xf0`(dodge) / `0x136`(invulnerable) / `0x50`(shield) + `0xe6`(dropPod) 优先 | 早已有 |
  | `:64` | `if (*(char *)(lVar2 + 0x65) == '\0')` = **不是残骸**才有减甲那一段（`+0x65` = `EntityScript__get_isRemnant.c`） | ⚠️ **本批【没做】**，见「顺手发现 ①」 |
  | `:75` | `if (iVar8 < 1) goto LAB_1806144ca;`（`iVar8` = `+0xb8` 那个护甲值）⇒ 护甲 < 1 跳过减甲 | `u.Armor > 0` |
  | **`:78`** | `if (*(int *)(lVar3 + 0x18) < 1) goto LAB_1806144ca;`（`lVar3` = `DamageInfo`；`+0x18` = `damageValue`）⇒ **这一下 < 1 时也跳过减甲** | 🆕 `N8` 加的 `&& actual > 0` |
  | `:93-94` | `System_Math__Max(1, iVar8 - iVar9)` → **原地写回** `DamageInfo+0x18` | `Math.Max(1, actual - u.Armor)` |
  | `:99-124` | `LAB_1806144ca`：读 `+0x68`（生命）→ `health - damageValue` 写回 | 共同尾巴 |
  | `:130` | `bVar17 = -1 < iVar8`（`iVar8` = `RemoveBastionDamage` 的返回）⇒ **`bVar17` 为真 = 堡垒整份吃下（没打穿）** | 堡垒那一段（本批未动） |
  | `:145-153` | `iVar8 < 1` ⇒ `health += iVar8`（负 = 溢出进血） | 同上 |
  | `:188-191` | 第二发的触发闸：`易伤 > 0` ∧ `!dodge` ∧ `!shield` ∧ `!invulnerable` ∧ **`!bVar17`** | 本函数上面三条 `return 0` 天然同形 |
  | `:197-198` | `IsVulnerableDamage(damageType)` = `(uint)(type − 7) < 2` ⇒ **易伤伤害不再生一份** | `alreadyVulnerableHit`（`A1563` 有） |
  | **`:200`** | `if (0 < *(int *)(*(longlong *)(param_1 + 0x28) + 0x18))` ⇒ **进伤 > 0** | 🆕 本批 `actual <= 0`，见下「为什么必须带」 |
  | **`:210`** | `iVar8 = op_Implicit(&local_58)`，`local_58` 来自 `:201-203` 的 `*(undefined8 *)(lVar2 + 0x68)` = **主伤害写完之后的生命** | 🆕 本批 `u.Health - actual <= 0` |
  | **`:211`** | `if ((0 < iVar8) && (*(char *)(lVar2 + 0x65) == '\0'))` ⇒ **血还 > 0 ∧ 不是残骸** | 🆕 本批 `u.IsRemnant` |
  | `:226-227` | 造第二发的 `DamageInfo(量 = CurrentVulnerable(card), 类型 = (type==4)+7)` | — |
  | `:252-254` | `BattleManager__DealMultiDamage(…, priority 2, …)` ⇒ 它自己再走一遍整条管线 | 折进同一次调用（如实标着的近似） |

  **「为什么必须带上 `:200`」**（这是本批自己逼出来的一格）：`N8` 把「`dmg = 0` 时护甲不再把它抬成 1」
  补上之后，若不判 `:200`，`dmg = 0` 的伤害会**只剩易伤那一份**（= 凭空多打一份）。
  ⇒ 两笔必须同批落地，⛔ 不许只做一笔。

- **改了哪些文件哪些行**：
  - `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`
    - `DamageAfterReduction` 的 **body**：护甲那一行加 `&& actual > 0`；结尾两行从 `actual += VulnerableDamagePart(u); return actual;`
      改成 `if (actual <= 0 || u.IsRemnant || u.Health - actual <= 0) return actual; return actual + VulnerableDamagePart(u);`
      （上面各配逐行判据 + 四格判别式）。
    - 同函数 `<summary>`：「五道」→「六道」+ 两处 `(闸 = …)` 的现读判据。
    - `VulnerableDamagePart` 的 doc：③ 从「⏭ 仍开着」→「✅ 已做（`N7`）」+「预览侧**不加**这道闸，
      ⛔ 别去对齐」；同笔 `N8` 的指针。
    - `WouldKillByEntries` 的 doc 里「（含 `A1335①` 那一档**还没做**的现状）」→「（三档 2026-10-14 已全部落地）」。
    - `ApplyDamage` 的头部注释：`其中两条落地` → `三档全部落地`；「仍然没做的那一档」整段 → 「三档各自落点」；
      顺手订正一句**过期**的话（原文写「`DamageAfterReduction` 把**易伤 + 护甲折成了一个数**」——
      那句在 `A1563` 之后就过期了，结论不变、理由要换）。
  - `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs`：`Bastion` 常量 doc 里「仍开着的那一档是**普通档**…」→「那一档也补上了」+ 闸的三半 + 「预览侧不加」。
  - `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`：`TestA1563VulnerableDamageSplit` 末尾**新增 ⑧/⑨ 两组**（见下），
    另改两处**指到那一节 doc 的过期指针**（`：「判别式全文与「还没做的三档」」×2` → 现读口径）；
    ⑤「预览合计 == 真打一局」那段加了**边界说明**（见「没查清 / 如实标着」）。
  - `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs`：`:637` 那条**被钉着的**断言
    （`Check(RuleCore.DamageAfterReduction(t, 3), 5, …)`）**期望值不变**（本格落在闸的**过**这一侧：
    生命 5 − 3 = 2 > 0），只在消息尾巴追加「本格在哪一侧 + 判别式在哪两组」。

- **断言怎么改的（每组都配判别式，⛔ 没有一格是「两种口径同值」）**：

  | 格 | 夹具 | 期望 | 🧨 判别式 |
  |---|---|---|---|
  | ⑧(a) 致死档 | 3 血 / 护甲 0 / 易伤 2 / 打 3 | `DamageAfterReduction = 3`；`ApplyDamage` 返回 **3**、生命 3→**0**、`Hit{Amount=3}` ×1 | 去掉那道闸（改回「先加起来再判血」）⇒ 实得 **5** |
  | ⑧(b) 反向格 | 6 血 / 易伤 2 / 打 2 | 闸**放行** ⇒ **4**；生命 6→2；`Hit{4}` ×1 | 把易伤那一行整条删掉 / 闸判反 ⇒ 实得 **2** |
  | ⑧(c) 残骸半格 | 9 血 / 易伤 2 / 打 3 / `IsRemnant = true` | **3** | 去掉 `u.IsRemnant` ⇒ 实得 **5** |
  | ⑧(d) `:200` 半格 | 9 血 / 易伤 2 / 打 **0** | **0**（前提：`VulnerableDamagePart == 2`） | 去掉 `actual <= 0` ⇒ 实得 **2** |
  | ⑨ `N8` 主格 | 5 血 / **护甲 3** / 打 **0** | `DamageAfterReduction = 0`；`DamageAfterReductionOne(…,0,0) = 0`；`ApplyDamage` 返回 **0**、生命不动、`Hit{0}` ×1 | 去掉 `&& actual > 0` ⇒ 实得 **1** |
  | ⑨ 反向格 | 同上 / 打 5 | **2** = `max(1, 5 − 3)` | 把减甲整段删掉 ⇒ 实得 **5** |

  🔴 **「灭自证」怎么做的**：⑧(a)/(b) **方向相反**（一个「不该加」、一个「该加」）、⑨ 两格同理
  （一个「不该减」、一个「该减」）⇒ 任何**单一**错实现都不可能同时满足 —— 挡得住「把两边一起改回旧写法」。
  ⑧(a) 还配了**事件层**那一格（`Hit{Amount=3}`）：只断返回值会被「返回值对了、事件没跟上」蒙过去。
  ⛔ **没有动**写手 `D` 那批新写的断言（`RuleEngineTest.cs:12153-12196` Codex ④ 翻面 · `Prayed` 那几格 · `#20-bis`）。

- **⚠️ 没查清 / 如实标着**：
  1. **`N8` 那一档的可达性没查清** —— 全仓没有一处写死 0 的伤害点，`dmg = 0` 只可能由
     「攻击力被减到 0 / 某条算式算出 0」这类路径出现。公式照原版补（铁律 11），可达性留给下一轮。
  2. **「预览合计 == 真打一局的实际掉血」这条不变式现在【有边界】** —— 它只在
     「没有任何一发一击致死」时成立：结算侧有这道闸，而**原版预览**（`…WithDamageValues.c:183-205`）
     **本来就没有**（每条各加一次、整段不看血）⇒ **是原版自己两条口径**，不是我们分叉。
     `RuleEngineTest` 的 `A1563` ⑤ 取的是**非致死档**（9 血挨 6 点）⇒ 它仍然绿；
     ⑥/⑦ 只断预览 ⇒ 不受影响。已在 ⑤ 的注释里写明边界。
  3. **「池已碎 + 易伤 + 无堡垒」三重组合**（写手 `D` 标着「没跑对局验过」的那一格）本批**没验**。

---

### `N8` · `RuleCore.DamageAfterReduction` 缺原版 `:78` 的 `damageValue < 1` 守卫 —— ✅ 落地

- **结论**：原版 `dmg < 1` 时**不扣护甲**（跳过去走的是「`health -= damageValue`」那条共同尾巴，
  `dmg = 0` 时生命**一点不动**）。我们改前 `if (u.Armor > 0) actual = Math.Max(1, actual - u.Armor);`
  会把 **`dmg = 0` 抬成 1** ⇒ 已按原版补上 `&& actual > 0`。
- **判据**：同上一节表格的 **`:78`**（主判据）+ `:75`/`:99-124`（结构证据：两条 `goto LAB_1806144ca` 都是
  「跳到共同尾巴」）。**现读确认过**（不是转述账文）。
- **⚠️ 代理 `D` 说的「必须连带预览侧一起改才同式」—— 现核结果是：预览侧【本来就有】这道守卫**：
  `RuleCore.DamageAfterArmourOne` 那句 `if (u.Armor > 0 && actual > 0) actual = Math.Max(1, actual - u.Armor);`
  就是原版预览 `…WithDamageValues.c:138` 的 `(0 < iVar8) && (0 < iVar7)`（`iVar8` = 护甲、`iVar7` = 这一条的量）
  ⇒ **本笔只需改结算侧一处**，改完两边同式（⑨ 里配了两条一致性断言：`DamageAfterReductionOne(t,0,0) == 0`、`(t,5,0) == 2`）。
- **改了哪些行**：`RuleCore.cs` 的 `DamageAfterReduction` 那一行（与 `N7` 同一处）+ 那一段注释。
- **判别式**：见上表 ⑨（主格 + 反向格，方向相反）。

---

## 验收 / 验证（本批跑过的）

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_dmg bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 错 · 编辑器 0 错**（末次；中途两次也都是 0） |
| 行尾 | `python -I` 数 CRLF/LF | `RuleCore.cs`/`CardDef.cs`/`RuleEngineTest_S14.cs` = **纯 LF（未翻）**；`RuleEngineTest.cs` = **CRLF 29060/29060（未翻）**。⛔ 全程用 Edit 工具，没用 `sed -i` |
| 快内环·解析层 | `bash d:/4/Unity/工具/ruleprobe.sh check` | 🔴 **rc=1**（明细见下，**不是本笔造成的**） |
| 快内环·结算层 | `bash d:/4/Unity/工具/ruleprobe.sh play check` | 🔴 **rc=1**（**2 张是本笔**，语义正是 `N7` 预期；另 3 张是别人的在飞改动） |
| 行尾/类型检查之外的 Unity 自检 | —— | ⛔ **没跑**（按简报，自检由主对话在同步点跑） |

### `ruleprobe.sh check` 的红（⛔ 我没重钉指纹）

```
=== 哨兵合计：失败 1 条 ===
  ✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）
  ✗ [输出指纹] 当次现算的解析输出 ≠ 被确认过的那一版（A1382）
=== 全池 1120 张 · 解析差异 1 行 · 改名 0 行 · 新增 0 · 消失 0 ===
⚠️ 解析差异（前 1 张）：GSC29|Metamorph Iconbearer
```

**归因 = 不是本笔**：① 它是**解析层**的差异（本笔一个字都没动解析器）；
② 我第一次跑时 `ruleprobe` **根本编不过** —— `Assets/RuleEngine/Core/EffectText.cs(5075,25) error CS0103: 名称"ActPhraseAt"不存在`
（简报点名的「另一个写手正在改 `EffectText.cs`」，我按纪律**没去碰**）；③ `git diff --numstat` 现读
`Core/EffectText.cs 133/13`（别人的活），而我的三个文件是 `RuleCore.cs` / `CardDef.cs` / `RuleEngineTest*.cs`。

### `ruleprobe.sh play check` 的红（结算层 —— 本笔**看得见**）

```
  结算指纹：当次现算 = bab2fb7fcdddcae06add1a6290fdb370 · 基线文件 = 76ced8a686a9c316624c37415a91d58c（不同）
  --- ①-a 逐卡差异（主档 · 打出去那一拍）---  3 张卡变了
       BL34   faceOps 0 → 1
       GSC29  handCostDelta -1 → -3（手牌总价 7→6 变 7→4）
       TAU2   faceOps 0 → 1
  --- ①-b 逐卡差异（事件电池）---  5 张卡变了
       BL66   steps kill(dmg)=1000 → 999 · net 6 → 5 · netDmg 1 → 0
       BL71   steps kill(dmg)=1003 → 999 · net 8 → 5 · netDmg 10 → 0 · raw 25 → 26
       GOF18  raw 25 → 26 · net 0 → 3 · ✋ 手牌 P0 +「GOF_Sawbonez」×1
       GSC29  netFull 手牌总价 6→8/8→9 变 4→6/6→7
       TL_Psychophage  raw 25 → 26 · net 9 → 12 · ✋ 手牌 P0 +「TL81」×1
```

**归因（逐条）**：

- **`BL66` / `BL71` = 本笔的、而且是【预期**的 `N7` 语义**：那两条的 `kill(dmg)` 步就是
  `RuleCore.ApplyDamage(ctx, victim, 999, "夹具事件电池")`（`工具/ruleprobe/PlayProbe.cs:845`）
  —— 拿 **999 点**打一个带 `{vulnerable1}` / `{vulnerable2}` 的夹具 ⇒ **一击致死**
  ⇒ 原版 `:210-211` 那道闸**不放行**那一份 ⇒ 返回值从 `999 + 易伤` 落回 **999**。
  实测差：`BL66 1000 → 999`（差 **1** = 它身上那一份 `vulnerable1`）、`BL71 1003 → 999`（差 **4**）——
  ⚠️ `BL71` 那 4 我没逐张核卡面（只从 `netFull` 看它给 P1 的两张各挂了 `{vulnerable2}`，
  `KwValue` 是**累加**的 ⇒ 4 与「同一张挂两条 `Vulnerable 2`」自洽，但这只是**推断**，
  ⛔ 没查清的部分如实标着）。这两条的形状（`kill(dmg)` 掉、`netDmg → 0`）**只可能来自伤害公式**，
  而本批是今天唯一动过 `DamageAfterReduction` 的改动。
  ⇒ **基线里存的是【改前那个错的读数】**，按铁律 11/5 应当**重钉**（🔴 由主对话统一做，我不动）。
- **`GSC29` / `GOF18` / `TL_Psychophage`（①-b）＋ `BL34` / `TAU2` / `GSC29`（①-a）= 别人的**：
  变的全是 `raw 25 → 26`（**op 条数**）、`faceOps 0 → 1`、手牌总价、`+「TL81」/「GOF_Sawbonez」`（**新解出来的生成卡**）
  —— 那是**解析层**多认出了一句卡面正文，与伤害公式无关；`GSC29` 正是上面那条解析差异的同一张卡。
  ⇒ 与 `check` 的红**同源**（`EffectText.cs` 那个在飞改动）。
  🔴 **后半程拿到了更硬的归因**（主对话收窄文件边界时现核）：`Core/EffectResolver.cs` 上那 19 行
  **是另一个写手的 `A1989`**（`DoChooseCard` 的 `case "copies"` 支：把 `ctx.LastCreated` 改成指向**刚造出来的复制品**），
  它自己的注释里写着「📏 实测（`ruleprobe.sh play GSC29`，2026-10-14）：改前两张复制品仍按 2 费算
  （手牌总价 7 → 6）」—— 与 `play check` 里 `GSC29 handCostDelta -1 → -3` / 手牌总价那几格**逐字对上**。
  ⇒ **`GSC29` 那三格（`check` 的解析差异 ＋ `play check` ①-a/①-b）全是 `A1989`**，⛔ 与 `N7`/`N8` 无关。
  ⛔ **我一个字都没动 `EffectResolver.cs`**（`git diff` 现读：那些行全是 `A1989` 的 `copies` 支）。
  **本批【不需要】改它** —— 预览侧那一半（`DamageAfterArmourOne` / `WouldKillByEntries`）都在
  `RuleCore.cs` 里、且**本来就有守卫**；`EffectResolver` 只通过 `RuleCore.WouldKill` 间接读公式，行为不变。

---

## 顺手发现（照报，⛔ 没顺手改）

1. 🔴 **新账候选：残骸（`isRemnant`）不该吃护甲** —— 原版 `_ReceiveDamage…:64`
   `if (*(char *)(lVar2 + 0x65) == '\0')` **把整个减甲段（`:65-98`）包在「不是残骸」里面**；
   残骸走 `:99` 那个 `else`（`LAB_1806144ca`）⇒ 直接 `health -= damageValue`（**按原值扣，不减甲**）。
   我们 `DamageAfterReduction` 对残骸**照样减护甲**（`u.IsRemnant` 只在 `N7` 这道闸上用到）。
   ⚠️ **预览侧没有这一档**（`…WithDamageValues.c:138` 只判 `armour > 0 && dmg > 0`）
   ⇒ 两边**各照自己的原版**，⛔ 别为了「同式」往预览里补。
   判据我**现读确认过**（`:64` / `:99` / `:111-124` 三处互相印证）；**可达性没查清**（残骸在我们引擎里是真单位，
   但「哪些卡会把单位变成残骸、之后还会不会被伤害」我没查）⇒ 交给主对话立账。
2. **空投舱「池已碎」那一支的 `actual` 是【陈旧值】**（写手 `D` 本批留下的洞，**今天不可达**：
   全池 0 张卡带 `dropPod`）：`RuleCore.cs:5603` 按**主伤害**算好 `actual`；`:5810` 把 `hit = podVuln`
  （走给堡垒那一段看的），但**没同步 `actual`**；`:5831` 堡垒那一段只在 `u.Bastion >= 1` 时才会重算 `actual`
   ⇒ 若那个单位**没有堡垒**，`:5927` 的 `u.Health -= actual` 扣的是**主伤害那个数**，
   而原版扣的是**第二发自己的量** `max(1, 易伤 − 护甲)`。⛔ 我没改（超出本批范围）。
3. `RuleEngineTest.cs:22512` 那句注释「回归：**实际结算用的那一份没动**（合计版与 `WouldKill` 还是老口径）」
   —— **半句过期**（合计版在 `A1563` 就不再是「老口径」，本笔又改了它；那条断言本身仍绿）。
   ⛔ 那段是 `E1` 那批的文字，我没动，只报上来。
4. `RuleEngineTest_S14.cs:637` 那条被钉着的断言**期望值不用改**（本格落在闸的**过**这一侧），
   已就地补上「在哪一侧 + 判别式在哪」；⛔ 简报里写的行号是 `:633`，**现读真身是 `:637`**
   （`633` 是 `t.AddKeyword(KeywordTable.Bastion, 3);` —— 简报那一行是 `D` 改文件**之前**的读数）。

---

## 给主对话的三个动作项

1. **重钉 `ruleprobe` 结算基线**（`play check` 的 `BL66` / `BL71` 两行是**旧读数**）—— 由你统一做，我没动。
2. **`check` 那条解析红**归因给 `EffectText.cs` 的在飞改动（那张卡 `GSC29`）—— 别记到本笔账上。
3. **立账「残骸不吃护甲」**（本文件「顺手发现 1」，判据现成、改动一行）。
