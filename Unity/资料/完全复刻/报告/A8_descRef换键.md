# A8 · 卡面正文取键改「原版三元」（`cardDescriptionTextReferenceId`）

- 轮次：`B87-A8` · 2026-10-27 · 执行代理 `A8`
- 施工规格：`资料/完全复刻/调度台日志.md` **§12·94**（回执 `W64DESC` + 裁定）
- 判据（🥇 逐句）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2334-2349`（`GetLocalizedCardDesc()`）
  · 旁证：`d:/2/tools/decomp_full/RawCardScript__GetLocalizedCardDesc.c`（同义，函数名即自己 ⇒ 不是错位产物）
- 独占文件：`RuleEngine/Core/CardDef.cs` · `RuleEngine/Data/CardDatabase.cs` · `CardPresentation/Battle/BattleDriver.cs`
- ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改正本 / 账主表

---

## ① 三处各改了什么（行号为改后现值）

### 1·1 `RuleEngine/Core/CardDef.cs`（+32 行，追加在类尾 `AttachTargetingTextReference` 之后）

| 落点 | 内容 |
|---|---|
| **`CardDef.cs:6775`** | 新属性 `public string CardDescriptionTextReferenceId { get; private set; }` |
| **`CardDef.cs:6779`** | 新挂载口 `public void AttachCardDescriptionTextReference(string refId)` |

- **⛔ 没动那个 40 参构造函数**（加一格会让它那 40 处账落点整体漂移）；照同一文件里 `AttachTargetingTextReference`（`:6743/:6747`）的既有做法：**属性 + 构造后挂载**。
- 三态口径与 `TargetingTextReferenceId` 同：`null` = 表里没这一列 / 夹具直建（判不了）；`""` = 表里**明确写了空**（⇒ 原版回退 `uniqueId` 那一支）。⛔ 不把 `null` 归一成空串。
- 注释里写死了判据出处、数据来源（`cards_engine.json` 的 v35 列）、消费点、以及**原版第三条支路我们没做**（见 §④·1）。

### 1·2 `RuleEngine/Data/CardDatabase.cs`（+16 行）

| 落点 | 内容 |
|---|---|
| **`CardDatabase.cs:74`** | `CardDto` 加列 `public string cardDescriptionTextReferenceId;`（紧跟 A4 那行 `targetingTextReferenceId`，同样的「一条式」写法） |
| **`CardDatabase.cs:1704-1706`** | `Parse` 里构造后挂载：`def.AttachCardDescriptionTextReference(c.cardDescriptionTextReferenceId);`（紧跟 A4 的 `AttachTargetingTextReference` 之后） |
| **`CardDatabase.cs:1430-1440`** | `dto.version < 35` 出声（照 v33/v34 那两条守卫的写法与口径）：「旧表 ⇒ 该列 `null` = **判不了** ⇒ 取键退回 `Card_Description/` + 自己 id（= 改前行为）」，⛔ 不把旧表读成「原版说这 15 张都没有 ref」 |

### 1·3 `CardPresentation/Battle/BattleDriver.cs`

| 落点 | 内容 |
|---|---|
| **`BattleDriver.cs:36119`**（**类尾**新增，后面没有任何成员） | `public static string OfficialDescKey(string id, string descRef)` ⟶ `"Card_Description/" + (string.IsNullOrEmpty(descRef) ? id : descRef)` |
| **`:28140`**（中文档）· **`:28142`**（中文档但中文字体缺失 ⇒ 英文列）· **`:28144`**（英文档） | `Loc.OfficialCurrent/OfficialEnglish("Card_Description/" + c.Id)` ⟶ `…(OfficialDescKey(c.Id, c.CardDescriptionTextReferenceId))` |
| **`:28152`**（其余 10 种语言） | `Loc.Lang13Current("Card_Description/" + c.Id)` ⟶ `Loc.Lang13Current(OfficialDescKey(c.Id, c.CardDescriptionTextReferenceId))`（外层 `string.IsNullOrEmpty(c.Id) ? null :` 原样保留） |

- **4 处消费点全改到了**（= §12·94 说的「消费端 4 处」；其中「其余 10 种语言」分支正是它点名的**最易漏的第 4 处**）。
- ⛔ **没有**「ref 缺键 ⇒ 退回自己 id」那一支（§12·94 明令）：缺键就是缺键，`Loc.Lang13Current` 自己会出声；⛔ 不在取键处悄悄换另一条键。
- **零行数变化**：四处都是**就地改写已有行**（⛔ 没插行）⇒ `BattleDriver.cs` 那 642 条账引用（最大在 `:36001`）**零漂移**；唯一新增是**类尾**那 14 行。
- **CRLF 未翻**：改后 `crlf = lf = 36199`、裸 CR = 0（二进制点数）。

---

## ② 5 张卡的键对照表（`cards_engine.json` v35 + `Resources/official_loc_13lang.json` 现读）

改前：**一律** `Card_Description/<自己 id>`；改后：`descRef` 非空 ⇒ `Card_Description/<descRef>`。

| 卡 | `descRef` | 旧键 | 新键 | 旧 EN / 新 EN（截断） | 两档都不同？ |
|---|---|---|---|---|---|
| `ASH48` Cosmic Serpent | `ASH48_new` | `Card_Description/ASH48` | `Card_Description/ASH48_new` | `Trigger the abilities requiring Spirit Stones of all your troops` / `…all your troops.<br>((Spirit_x,2)) ((Repeat_this_effect))` | EN ✓ ZH ✓（EN 64→106 字符，ZH 15→57） |
| `SAU58` Implacable Conqueror | `SAU58_new` | `…/SAU58` | `…/SAU58_new` | `Lower the cost of all troops in your deck by 2` / `…by 2. ((Draw_a_troop))` | EN ✓ ZH ✓（46→64 / 16→34） |
| `SAU15` Deathmark | `SAU15_new` | `…/SAU15` | `…/SAU15_new` | `[[remnant]]. [[sniper]]` / `[[remnant]]. [[sniper]]. [[rally]]: If you control no other troops, gain [[stealth]]` | EN ✓ ZH ✓（23→84 / 22→58） |
| `TAU65` War Shaper | `TAU65_new` | `…/TAU65` | `…/TAU65_new` | `[[talent]]: $$b3735ef3…$$` / `[[strike]]: ((Heal_1)). [[talent]]: $$b3735ef3…$$` | EN ✓ ZH ✓（48→72 / 47→72） |
| `SOR25` Imagifier | `SOR25_new` | `…/SOR25` | `…/SOR25_new` | `[[pray]]: Give [[flank]] to another friendly troop` / `[[pray]]: Give ((Melee_attack_icon_x,+1)), ((Ranged_attack_icon_x,+1)) and [[flank]] to a friendly t…` | EN ✓ ZH ✓（50→104 / 28→84） |
| `AM72` Vitus Gryf（对照） | **空** | `…/AM72` | **`…/AM72`（不变）** | 88 / — | 不适用（`descRef` 空 ⇒ 走自己 id 那一支） |

🔴 **这 5 张的 `_new` 正文，逐条正好就是白名单注释里写着「官方缺」的那半句**：
`Repeat this effect`（ASH48）· `Draw a troop`（SAU58）· `Rally: … gain Stealth`（SAU15）·
`Strike: Heals 1`（TAU65）· `+1,+1`（SOR25）。⇒ 根因确认（白名单是创可贴、`descRef` 才是根因，与 §12·94 的回执一致）。

### 2·1 记号可达性（`_new` 正文会印到卡面上，所以逐条查了）

这 5 张 `_new` 正文里的 6 个 `((…))` 记号，全部能在 `Resources/official_names.json` 的
`Modular_Description/*` 里取到词条（⇒ `CardIcons.RewriteMarks` 会渲染成图标/词，**不会把 `((…))` 原样印到卡面**）：

| 记号 | 词条 | EN | ZH |
|---|---|---|---|
| `Repeat_this_effect` | `Modular_Description/Repeat_this_effect` | Repeat this effect | 重复此效果 |
| `Draw_a_troop` | `…/Draw_a_troop` | Draw a troop | 从己方卡组中抽一张军队卡牌 |
| `Heal_1` | `…/Heal_1` | Heals 1 | 恢复 1 点生命值 |
| `Spirit_x,2` | `…/Spirit_x`（`{0}`=2） | `<link=spiritStone><sprite name=Atlas_SpiritStone_{0}></link>` | 同左 |
| `Melee_attack_icon_x,+1` | `…/Melee_attack_icon_x`（`{0}`=+1） | `<nobr>{0}<link=melee><sprite name=Atlas_trait_icon_Melee></nobr></link>` | 同左 |
| `Ranged_attack_icon_x,+1` | `…/Ranged_attack_icon_x`（`{0}`=+1） | `<nobr>{0}<link=ranged><sprite name=Atlas_trait_icon_Ranged></nobr></link>` | 同左 |

`[[…]]` 那 8 个 token（`flank`/`pray`/`rally`/`remnant`/`sniper`/`stealth`/`strike`/`talent`）在
`official_names.json` 的 `Card_Trait/*` 里 **8/8 命中**。`SpiritStone_1..5` 在我们的
`Resources/Fonts/Warpforge Trait TextSprites.asset` 里在（`Atlas_SpiritStone_*` 的全名↔短名两套由
`Editor/IconSetup.cs:291` 一带注册）。

---

## ③ 验收读数

### 3·1 `typecheck`（`TMPDIR=/tmp/wf_a8 bash d:/4/Unity/工具/typecheck.sh`）

| 次数 | 读数 | 说明 |
|---|---|---|
| 第 1 次（三处改完） | 运行时 **3** 错 / 编辑器 **28** 错 | 3 条全在**别人的文件**：`Shell/ChatTab.cs:156,176` + `Shell/MainMenuRuntime.cs:3700` —— `SocialData.ChatMessage` 没有 `Channel` 成员（社交线代理正在飞）。编辑器那 28 条是**级联**（`INetChatHost` 等定义在运行时程序集，运行时编不过 ⇒ 编辑器引用拿不到） |
| 第 2 次（几分钟后） | 运行时 **6** 错 | 多出的 3 条在 `BattleDriver.cs:15142/15269/15340`：`IsActiveAbilityEntry` / `SkillNote` / `ActiveAbilitySideOr` 未定义 ⇒ **见 §④·2**（另一个代理正在写同一个文件，那次读到了它的半成品） |
| 第 3 次（等对方写完） | 运行时 **3** 错 / 编辑器 **28** 错 | **我的三个文件 0 错**；剩下 3 条仍是上面 `SocialData.ChatMessage.Channel` 那 3 条 |
| 第 4 次（只改了一行 `///` 注释的角括号写法之后复跑） | 运行时 **3** 错 / 编辑器 **28** 错 | 同上，**我的三个文件 0 错** |

⇒ **`CardDef.cs` / `CardDatabase.cs` / `BattleDriver.cs` 三个文件 0 错**（三次读数里没有一条落在这三个文件上，
除了第 2 次那 3 条**不是我写的内容**、且第 3 次已消失）。

### 3·2 数据自证（`ASH48`，python 直读两个数据源）

```
cards_engine.json  (version 35, 1129 张)
  ASH48.cardDescriptionTextReferenceId = "ASH48_new"      ← 非空
  全池该列：1129/1129 张有键，**15 张非空**（全是 "<自己 id>_new" 形状，0 例含 "(("）

Resources/official_loc_13lang.json  (10862 键)
  Card_Description/ASH48      en 64 字符  zh 15 字符   ← 改前取这条
  Card_Description/ASH48_new  en 106 字符 zh 57 字符   ← 改后取这条（含 ((Spirit_x,2)) ((Repeat_this_effect))）
```

⇒ 键存在、两档内容真的不同（不是「同一条键换个名字」）。

### 3·3 落点漂移（供同步点 `--fix` 参考）

| 文件 | 我的净位移 | 受影响的账引用 |
|---|---|---|
| `CardDef.cs` | **+32 行**（插在旧 `:6749` 的类尾） | 旧 `>6749` 的引用全部 +32；现读唯一一条是 `Core/CardDef.cs:7864-7867`（`战斗逻辑驱动.tsv:985`）⇒ 该条**改前就已经漂了**（真锚点改后 = `:8512`） |
| `CardDatabase.cs` | **+1**（`:74` 的 DTO 列）· **+12**（`:1430-1441` 的 v35 闸）· **+3**（`:1704-1706` 的挂载） | `:604`→`605` · `:1040-1070`→`1041-1071` · `:1610`→`1611` · `:1690`→**`1703`**（正好是真锚点 `AttachTargetingTextReference`） · `:2303`→`2319`（该条改前已漂：真锚点 `MapBoardGate` 现读 `:2403`） |
| `BattleDriver.cs` | **0**（四处就地改写）· 类尾 **+14** | **零漂移**（类尾在最大引用 `:36001` 之后） |

### 3·4 既有断言（`Editor/BattleScene.cs`）—— **不需要改**

`§12·94` 说「会打红 3 处既有断言、必须同批改」。**现读它们已经改成 `descRef` 口径了**
（另一路代理做的，`BattleScene.cs:25324/25325` 与 `:25364/25365` 用 `A1945DescKeyOf`，`:25225` 同）：

- ⑤ 英文档全池（`:25334-25338`）：`want = Rewrite(OfficialEnglish(A1945DescKeyOf(id)))` 与
  `BattleDriver.FaceTextFull(c).body` 比 —— **两侧现在用同一条键规则**（编辑器侧读卡表原文、刻意不走生产实现，灭自证）⇒ 不会因为这次换键变红。
- ⑥ 中文档全池（`:25372-25374`）：同上。
- `:25106`（Howling Banshee 的 `descZh` 记号渲染）：走的是 `CardIcons.Rewrite(..., DescZh)`，与取键无关。

⚠️ 我**没跑 Unity** ⇒ 这句是**静态推理**，不是实跑结论。

---

## ④ 顺手发现（只报不改）

1. 🔴 **原版 `GetLocalizedCardDesc()` 是【三条支路】，我只实现了两条**（判据 `RawCardScript.cs:2338-2346`）：
   `ref` 非空且**含 `"(("`** ⇒ **正文就是 `ref` 本身**（不走本地化键，`baseString = cardDescriptionTextReferenceId; goto IL_00a7;`）。
   - **现池 0 例**（15 张 ref 全形如 `<id>_new`、`Contains("((")` 全假）；⛔ 而且 `§12·94` 的施工规格只写了「`descRef` 非空 ⇒ 键 = …／否则 卡 id」两条。
   - ⇒ 按规格**没做**，在 `OfficialDescKey` 的 doc 与 `CardDef` 的注释里都写死了「我们没实现 / 0 例」。**要不要补、何时补，请调度台裁**（补它要决定「ref 里的记号要不要过 `RewriteMarks`、要不要 `ModifyLocalization`」——原版这两步的顺序是 `goto IL_00a7` **跳过** `ModifyLocalization` 之外的取词条，但**最终仍过 `ModifyLocalization`**）。
2. 🔴 **`BattleDriver.cs` 有人并发在写**（与简报「独占」不符，仅报事实）：我两次 `typecheck` 之间，文件里**凭空出现**了 `IsActiveAbilityEntry` / `ActiveAbilitySideOr` / `SkillNote` 三个新方法（现读 `:36148/:36162/:36193`，**追加在我的 `OfficialDescKey`（`:36119`）之后**）以及它们在 `:15142/:15269/:15340` 的调用点。第 2 次 `typecheck` 正好撞上「调用点已写、定义还没落」的中间态（3 条 `CS0103`），第 3 次已自愈。
   - ⇒ 我的改动**没被覆盖**（`OfficialDescKey` 仍在 `:36119`）、**行尾没被翻**（`crlf = lf = 36199`、裸 CR 0）。
   - ⚠️ 但**两边都往类尾追加** = 下一次谁再追加都可能与对方撞同一区；建议调度台确认该文件的**实际唯一写手**。
3. 📌 **`Loc.cs` 的 `OfficialCurrent` doc 句子已过期**（不是缺陷、也不在我白名单）：
   `CardPresentation/Core/Loc.cs` 里 `OfficialCurrent` 的 `<para>` 还写着
   「② **卡面正文** —— `BattleDriver.FaceTextFull` 用 `OfficialCurrent("Card_Description/&lt;卡 id&gt;")` 取官方 en/zh」
   ⇒ 现在是 `OfficialCurrent(OfficialDescKey(id, descRef))`。同理 `OfficialEnglish` 那段也写着同一句。
   **改法**：把那一句 `<卡 id>` 换成 `<descRef 或卡 id>` 即可（**零行数变化**）。
4. 📌 **`Editor/BattleScene.cs` 的 `A1945DescKeyOf`（`:49110-49115`）是自检侧的第二份取键规则**：
   注释（`:49073-49074`）自己写着「生产侧同一口径的唯一一份应当是 `BattleDriver.OfficialDescKey`，它落地之后把下面两个函数改成转发」。
   **现在它落地了**（`:36119`）⇒ 可以改成转发（`A1945DescKeyOf(id) => BattleDriver.OfficialDescKey(id, A1945DescRefOf(id))`；`A1945DescRefOf` 仍读卡表原文 = 保持灭自证）。
   **我没动**（`Editor/BattleScene.cs` 不在我的白名单、另有代理改过它）。
5. 📌 **`GetLocalizedCardDescWithLineBreak()`（原版 `:2460`）只是 `GetLocalizedCardDesc()` + `AppendLine`** ⇒ 天然吃到这次的修，不需要第二处改动。它的消费点（`SpoilOfWarUI.SetRawCardData`）我们没做（账里是 D，`未分类_其他.tsv:598`），与本件无关。
6. 📌 **官方 zh 列本身写着英文碎片**（`TAU65_new` 的 zh = `[[strike]]: ((Heal_1)). [[talent]]: $$…$$`）——
   这是**官方数据就长这样**，按 `§12·104` 那条裁定（照抄各自语言列、⛔ 不「修正」）**原样显示**。⛔ 别当缺陷去改。
