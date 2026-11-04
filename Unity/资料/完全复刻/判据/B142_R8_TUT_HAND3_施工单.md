# B142_R8 · `B141-TUT-HAND3` 施工单（3 个解不出的起手 pid → 真卡 + 生成器最小改法）

> 只读代理产出。**未改任何源码 / 数据 / 活状态表，未跑 Unity、未动 git、未执行任何解包产物。**
> 关联：`活状态表.md:241`（`B141-TUT-HAND3`）· `:327`（`B142-TUT-S6` 数值面，同一件事）。
> 生成器 = `d:/4/Unity/工具/gen_tutorial_stages.py`（现读 931 行）· 产物 = `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_stages.json`（现读 652,073 B）。

---

## 〇、一句话结论

**三个 pid 都能在【工程内已导出】的 2026 build allcards 数据里查到，落卡 = `SAU34` / `TL27` / `TL30`，与账上写的一致，置信度 = 高（pid 直接命中 + `uniqueId` 与卡池 id 逐字相等 + 卡名双向往返 + 同卡同阵营出现在该关牌组里）。**
最小改法 = 在生成器的 pid→名 链上**补第二档**（2026 allcards `cards_raw.json`），且**只在原本 `ourId` 为空时才填** ⇒ 已解析的卡一个都不动；唯一必须同步改的断言 = `CardPresentation/Editor/BattleScene.cs:35697` 那条写死的 `wantHand.Count == 3`（改成 `== 4`）。

---

## 一、三个 pid 逐条落卡

| pid（`RawCardScript.m_PathID`） | 数据源 | 命中卡 | 证据 | 置信度 |
|---|---|---|---|---|
| `1696504206920290120` | ① `Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（工程内导出，见 §二）· ② `Unity/资料/普查产出_1018/allcards_1350/cards.tsv:777` | `SAU34`「Monolith」（m_Name = `34 Sautekh-Monolith` · cardName = `Monolith` · cardArmy = 40） | cards_raw.json 单行 JSON，该 pid 在 **byte 38,513,131**；同一对象的三格 `path_id=1696504206920290120` / `m_Name=34 Sautekh-Monolith` / `cardName=Monolith` / `uniqueId=SAU34`。`cards.tsv:777` = `SAU34  Monolith  40 … 34 Sautekh-Monolith`（uniqueId 列显式就是 SAU34） | **高** |
| `8270355771166178363` | 同上 | `TL27`「Exocrine」（m_Name = `29 Leviathan-Exocrine` · cardArmy = 60） | cards_raw.json **byte 63,645,298**；`path_id=8270355771166178363` / `cardName=Exocrine` / `uniqueId=TL27`。`cards.tsv:1281` = `TL27  Exocrine  60 … 29 Leviathan-Exocrine` | **高** |
| `8906429412044970690` | 同上 | `TL30`「Tyrannofex」（m_Name = `32 Leviathan-Tyrannofex` · cardArmy = 60） | cards_raw.json **byte 65,818,970**；`path_id=8906429412044970690` / `cardName=Tyrannofex` / `uniqueId=TL30`。`cards.tsv:1327` = `TL30  Tyrannofex  60 … 32 Leviathan-Tyrannofex` | **高** |

**独立旁证（不是同一份数据自证）**：

1. **卡池侧**：`RuleEngine/Resources/cards_engine.json`（v49 · `count=1142`）里 `SAU34=Monolith`（byte 2,734,722）· `TL27=Exocrine`（4,743,416）· `TL30=Tyrannofex`（4,801,980）—— 与上表的 `uniqueId` **逐字相等**。
2. **牌组侧**：`RuleEngine/Resources/tutorial_decks.json` 的 `stages[2].player.cardIds`（`NecronsTutorial`，30 张）**含 `SAU34`**；`stages[5].player.cardIds`（`LeviathanDeckTutorial`，30 张）**含 `TL27` 与 `TL30`**。三个 pid 所属阵营（Sautekh / Leviathan）与对应关卡的玩家牌组阵营完全吻合。
3. **起手位**：`tutorial_stages.json` 现读 `stages[2].playerStartingTroopsInHand[0].pid=1696504206920290120`、`stages[5].playerStartingTroopsInHand[1].pid=8270355771166178363`、`[2].pid=8906429412044970690` —— 与账上写的 S3[0] / S6[1] / S6[2] 一致（三条 `ourId` 全空、`matchTier = no-name`）。

⚠️ 卡名这一层有个**反向陷阱**（报告 `普查产出_1018/allcards_1350/报告.md:32,58,67` 明说）：**SO 的 `cardName` 有时是内部名、且池内"同名不同 id"有 325 条** ⇒ **名字不能当主键**。所以下面补丁走的是 **`uniqueId` 直连**（不是按名匹配）。

---

## 二、哪一份数据源可用（以及"没在哪"）

| 候选 | 有没有 pid→卡 | 现读结论 |
|---|---|---|
| **`Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`**（67,051,793 B · 1,351 张 `RawCardScript`） | ✅ **有** | **采用这一份**。它是归档包 `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data`（531,962 B，存在 ✅）的**本地只读导出**（该 json 自带的 `bundle` 字段就是这条路径）。`path_id` 是卡 SO 的 PathID、`uniqueId` 是原版卡 id。**同一 CAB（`CAB-fb8f01449289635703d83d52c3f18245`）** ⇒ 与教程 SO 的 externals 同一个 pid 空间。 |
| `Unity/_tmp_view/allcards_export/cards_raw.json`（70,870,327 B） | ✅ 也有 | 同一批数据的**临时工作区副本**（`_tmp_view` 整个被 `.gitignore:267` 的 `/_tmp_*/` 挡住、可随时删）。⛔ 别把它当正式源。 |
| `Unity/数据/游戏数据/card_ids.json` | ❌ **没有 pid** | 只有 `mapping`（1,351 条，键 = `uniqueId`、值 = 卡名）+ `poolTrueIds`；**不含 PathID**。 |
| `Unity/数据/游戏数据/card_id_truth_index.json` | ❌ 没有 pid | 键 = `note/source/counts/army/base/by_official_en/by_tail/by_name`，是「池 ↔ 真值 id」的 join 索引。 |
| `Unity/数据/游戏数据/cards_truth.json` / `cards_engine.json` | ❌ 没有 pid | 我们自己的卡池（效果树 / 运行时卡表），`id` 已是真值，但没有原版 PathID 列。 |
| `普查产出_1018/allcards_1350/{cards.tsv, pool_true_ids.tsv, missing118_resolved.tsv, new_ids_not_in_mapping.tsv}` | ❌ 没有 pid | 都是**分析产物**（有 `uniqueId`/`cardName`/`m_Name`，唯独没有 `path_id`）。`cards.tsv` 里 `m_Name` 的 `34 ` 前缀是**包内序号**、不是 PathID。 |
| `D:/2/关服前存档/…`（原始归档） | —— | 里面是 **bundle 二进制**，不是 json；要读它得跑 UnityPy + 解一个不可信包体 ⇒ **本单不碰**（工程内已有等价导出）。 |

**搜过的地方**（避免下一个人重搜）：`D:/4/Unity/数据/游戏数据/`（60+ 个 json 逐个开键）· `D:/4/Unity/资料/普查产出_1018/allcards_1350/`（15 个文件）· `D:/4/_tmp_view/`（2 份 cards_raw）· `D:/4` 全盘 `find -name "*cards_raw*"`（只有上面两份）· `D:/2/关服前存档/`（3 个子目录，只有 bundle 与 xapk）· 关键词 `path_id` / `m_PathID` / `PathID` / `uniqueId` / 三个 pid 字面量。

---

## 三、生成器**现有**回退链（逐档 + 行号）

链是**两段**：`pid → 卡名` 只有 **1 档**，`卡名 → 我们的 id` 有 **3 档**。缺的就是第一段的第二档。

### 3·1 第一段：pid → 卡名（`build_pid_name_map()` · `gen_tutorial_stages.py:225-252`）

| 档 | 做法 | 位置 | 备注 |
|---|---|---|---|
| **①**（**唯一**一档） | 从 SO 自己的 `actionType` **字符串**里抠括号内容（= `ScriptedAction.UpdateName()` 的逆） | `build_pid_name_map()` `:225-252`，抠串在 `parse_action_arg()` `:175-190` | 实测覆盖 76 个 pid；**只写「发起单位」** ⇒ 「只当目标」的 pid、`*StartingTroops*` 里的 pid 天然没名字 |

**⛔ 这里就是缺口**：`make_card_ref()` `:500-507` / `make_data_entry()` `:510-525` / `make_starting_list()` `:566-569` 全部只做 `pidname.get(pid, "")`（`:518` · `:568`），**取不到就写 `matchTier="no-name"` 走人**（`:506` · `:519`），**没有第二档名字源、也不会报错**（只有产物里的 `unresolvedNoName` 清单）。

### 3·2 第二段：卡名 → 我们的 id（`resolve()` · `:642-657`）

| 档 | 做法 | 位置 | tier 写法 |
|---|---|---|---|
| **②** exact | `cards_engine.json` 的 `name` 逐字相等、且池内唯一 | `build_card_resolver()` `:260-280`（命中在 `:272`） | `"exact"` |
| **③** norm | 归一化后相等（小写 / 去撇号 / 去括号内容 / 去尾部数字 / 去非字母数字 / >4 字去尾 `s`） | 同上（`:273`）+ `norm()` `:164-171` | `"norm"` |
| **④** 裁定映射 | `OCR_NAME_FIX`（2 条，调度台 2026-10-17 裁定） | `:296-299` · 用在 `:647-654` · 判据复核 `verify_ocr_fix()` `:336-341` | `"ocrName"` |
| **⑤** 候选线索 | `card_stats.json` 的 `ocrName` 列 —— **只写 `note`，⛔ 绝不当结果** | `build_candidate_lookup()` `:302-342` · 用在 `:655-656` | 不产 tier（仍 `no-hit`） |

> 每一步「宁可认不出」的纪律是对的（⛔ 别改成模糊匹配）。本单只补**第一段的第二档**，**不动第二段任何一档**。

---

## 四、最小改动补丁

**设计三条硬约束**（都为了"不动其它已解析的卡"）：
1. **只在 `ourId` 原本为空时**才用 allcards 那一档 ⇒ 现有 `exact=216` · `norm=44` · `ocrName=12` · 起始单位 `exact=22 / norm=2 / ocrName=3` **一个字节都不变**（现读 `tutorial_stages.json` 的 `matchTierHistogram`）。
2. **`uniqueId` 直连卡池**，不走卡名 ⇒ 不吃"SO `cardName` 是内部名 / 同名不同 id 325 条"那两个坑（§一 的警告）。
3. **`cards_raw.json` 在 `.gitignore:274` 里**（"原版资产衍生物不进库"）⇒ 新 clone 上没有这个文件；**这一档必须可缺省**：文件不在 ⇒ 打印警告 + 返回空 + 其余流程与今天完全一样（⛔ 别 `assert`，否则会把"能跑的机器"变成"跑不动的机器"）。

### 4·1 新增常量 + 新函数（插在 `:342` 之后、`# ==== ③ 读原版 bundle` 之前）

```python
# ============================================================ ②·B pid → 卡：2026 allcards 回退档（B142-TUT-HAND3）
# 判据：当前 build（2026）allcards 包里，PathID 就是教程 SO 里 RawCardScript 的 pid，
#       且 uniqueId 与我们卡池 cards_engine.id **逐字相等**（池 1,142 张里除 13 张自建 TRANSFORM_* 外全命中）。
# 来源 = 归档 bundle 的工程内只读导出（`cards_raw.json` 自带 bundle 路径字段指向那份 __data）。
# ⚠️ 该 json 在 `.gitignore:274` ⇒ 新 clone 上没有 ⇒ 本档**可缺省**（⛔ 别 assert）。
ALLCARDS_RAW = "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"


def load_allcards_pid_index(path=ALLCARDS_RAW):
    """pid(PathID) → (卡片 SO 的 cardName, uniqueId)。文件不在 ⇒ {} + 出声（不静默）。"""
    if not os.path.exists(path):
        print("! 2026 allcards 导出不在（%s）⇒ pid→卡 回退档不可用；"
              "只当目标的 pid 与那 3 张起手卡会继续留在 `unresolvedNoName`" % path)
        return {}
    raw = load(path)["cards"]
    out = {int(c["path_id"]): (c.get("cardName") or "", c.get("uniqueId") or "") for c in raw}
    print("2026 allcards pid 索引：%d 条（%s）" % (len(out), path))
    return out
```

### 4·2 改 `make_starting_list()`（`:566-569`，整函数替换）

```python
def make_starting_list(items, pidname, resolve, acpid=None, pool_ids=None):
    """`*StartingTroops{InHand}` 这类 `List<RawCardScript>` → 产物对象数组。

    🔴 B142-TUT-HAND3：SO 的 `actionType` 字符串**只写「发起单位」** ⇒ 起手列表里那些
       「只当目标/只被摆进起手」的 pid 天然没名字。加第二档：SO 字符串空了再查
       **2026 allcards**（pid = 同一 CAB 的 PathID；uniqueId = 我们卡池 id）。
       ⛔ **只在 `ourId` 原本为空时才填** ⇒ 已解析的卡一个都不动。
    """
    acpid = acpid or {}
    pool_ids = pool_ids or set()
    out = []
    for p in items:
        pid = p["m_PathID"]
        if not pid:
            out.append(dict(pid="", name="", ourId="", matchTier="null-ref", note=""))
            continue
        r = make_card_ref(p, pidname.get(pid, ""), resolve)
        if not r["ourId"] and pid in acpid:
            nm, uid = acpid[pid]
            if not r["name"]:
                r["name"] = nm
            r["ourId"] = uid if uid in pool_ids else ""
            r["matchTier"] = "allcardsId" if r["ourId"] else "allcards-no-pool"
            r["note"] = ("🔴 `name`/`ourId` 取自 **2026 build 的 allcards SO**"
                         "（`pathID=%s` ↔ `uniqueId=%s`），**不是**本 TutorialStage SO 的 "
                         "`actionType` 字符串；uniqueId 与 `cards_engine.json` 的 id 同源"
                         "（同一 CAB `CAB-fb8f01449289635703d83d52c3f18245`）" % (pid, uid))
        out.append(r)
    return out
```

### 4·3 主流程接线（`main()` 里两处）

- 在 `pool_by_id = …`（`:638`）之后加：

```python
    acpid = load_allcards_pid_index()
    allcards_pool_ids = set(pool_by_id)
```

- 把 `:734-737` 那四行的 `resolve` 后面各补一个实参：

```python
            playerStartingTroops=make_starting_list(s["playerStartingTroops"], pidname, resolve, acpid, allcards_pool_ids),
            enemyStartingTroops=make_starting_list(s["enemyStartingTroops"], pidname, resolve, acpid, allcards_pool_ids),
            playerStartingTroopsInHand=make_starting_list(s["playerStartingTroopsInHand"], pidname, resolve, acpid, allcards_pool_ids),
            enemyStartingTroopsInHand=make_starting_list(s["enemyStartingTroopsInHand"], pidname, resolve, acpid, allcards_pool_ids),
```

### 4·4 顺手要改的**过期文字**（铁律 5，就地改）

`knownIssues[1]`（`:821-825`）现读写着「它们是 `scriptedActionData` 里引用的卡 SO，住在 `allcards_assets_all.bundle` … **那个包在 catalog 里有、本地 84 个 bundle 里没有** ⇒ 解不出名字」。**这半句已经不成立**（2026 build 的 allcards 导出就在本地，本次正是靠它解的）。改成：

> 「🔴 有 %d 个 pid 拿不到卡名：它们只出现在 `data[].target`（脚本只给「发起单位」写名字）⇒ **天然没名字**。它们的 pid 在 2026 allcards 导出（`普查产出_1018/allcards_1350/cards_raw.json`）里**查得到**，但本脚本只把那一档用在 `*StartingTroops*`（目标位不需要卡名匹配）。」

同族还有两处（可同批改，也可另记）：`:51-53` 文件头的 `OCR_NAME_FIX` 说明、`:289` 注释、以及产物 `ocrNameFixNote`（`:886-891`）都写着「原版卡 SO 包 `allcards_assets_all.bundle` **本地没有** ⇒ 钉不死」—— **2026-10-18 起已经有了**。顺带一个**新的旁证**：allcards 里 `pathID=7386907376838221809` 的 `cardName` 就是 **`Gauss Warrior`**（→ `SAU4`），与 `OCR_NAME_FIX["Gauss Warrior (tutorial)"] = SAU4` **同卡同证**（但注意：这是 2026 build 的名字，⛔ 别据此把 `(tutorial)` 那张的数值当成同一个版本）。

### 4·5 重生成

```bash
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_tutorial_stages.py --dry   # 先看读数
"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_tutorial_stages.py         # 再写盘
```
（🔴 **必须用带 UnityPy 的解释器**：牌组名/音效名要开原版 bundle。普通 python 会直接报错，见 `:4-5`、`:346-354`。）
预期读数变化：

- `未解出pid` 那行打印值 **8 → 5**，`unresolvedNoName` 也是 **8 行 → 5 行**（三条起手 pid 消失；那 5 个仍留 —— 见 §八）；
- `matchTierHistogram` 多出 **`起始单位:allcardsId=3`**，`起始单位:no-name` 由 **3 → 0**；
- S3 玩家起手 **3 → 4**、S6 玩家起手 **1 → 3**（= `B142-TUT-S6` 的数值面同时消掉）。

### 4·6 判据澄清：本档**不是**「按名字猜」

`uniqueId → cards_engine.id` 是**逐字相等**（1,142 张池卡里除 13 张自建 `TRANSFORM_*` 外全部命中，本次实测），
所以它是「原版 SO 自己带的 id」，与 `card_stats.json` 的 `ocrName` 那条**裁定映射**（`:296-299`，按 PnP 卡图 OCR 出来的名字对上的）**性质完全不同** ——
前者可当原版判据，后者才是"我们这边的裁定"。

---

## 五、双向核验（不是单向推断）

| 方向 | 做法 | 结果 |
|---|---|---|
| **pid → 卡** | 用三个 pid 在 `cards_raw.json` 的 `cards[]` 里按 `path_id` 精确查 | `1696504206920290120 → SAU34 Monolith` · `8270355771166178363 → TL27 Exocrine` · `8906429412044970690 → TL30 Tyrannofex` |
| **卡 → pid** | 反向：先取 `uniqueId`，在 `cards[]` 里找该 `uniqueId` 的 `path_id` | 三条**互逆一致**（1,351 张里 `uniqueId` 0 重复，报告 `报告.md:37`）；再用 `cards_engine.json` 的 `id → name` 复核一遍（`SAU34=Monolith` 等），两库同解 |
| **业务自洽** | 这三张是否出现在「该关该玩家」的牌组里 | S3 玩家 `NecronsTutorial` 含 `SAU34`；S6 玩家 `LeviathanDeckTutorial` 含 `TL27`/`TL30` ⇒ 阵营与关卡对得上 |

⇒ **结论：账上写的 `SAU34` / `TL27` / `TL30` 成立，无改名。**

---

## 六、附带影响：必须同步改/复跑的地方

1. 🔴 **`CardPresentation/Editor/BattleScene.cs:35697`** —— 现读 `Check(wantHand.Count == 3, "（前提）第 3 关我方起手表 4 条里有 **3** 条能认出我们的卡 id" …)`。修好之后 `wantHand.Count` 会变 **4** ⇒ **这条会红**。改法：`== 3` → `== 4`，并把提示文案与 `:35683-35684` 那段注释（"第 1 条 `ourId` 是空的 ⇒ 我们只插 3 张"）就地订正为"4 条全认得出 ⇒ 插 4 张（`B142-TUT-HAND3` 已结）"。
   - 紧随其后的 `Check(TutHandIds(cT3.Players[0]) == string.Join("/", wantHand))`（`:35699`）**自动跟着变**，不用改。
2. ⚠️ `BattleScene.cs` 里教程那一段其它断言（`:35705` 起 `GetChosenCard` 一族、`:35776` 起手牌按名取）**逻辑上仍成立**（它们不写死张数），但 S3 我方手牌由 3 张变 4 张 ⇒ **必须实跑一次 `BattleScene.Run` 复核**（按铁律 12：动了 `RuleEngine/Resources/*.json` + 一个自检宿主，跑这一条即可）。
3. ⚠️ 生成器 `--check` 模式（`tutorial_audio_check()`）**不重新生成**，与本次改动无关；跑不跑都行。
4. ⛔ **`data[]` 那 5 个 pid 本补丁不动**（见 §八）⇒ `TutorialScript.ScriptedCardMatches`（`:2073-2080`，先比 `name`、再退回 `ourId`）的匹配行为**完全不变**。

---

## 七、`tutorial_stages.json` 是生成物，不是手改件

- 文件自己的 `note` 第一句就是「由 `工具/gen_tutorial_stages.py` 生成，**别手改**」；生成器 `:10-11` 也写着「产出（覆盖写、可重跑、幂等）」。
- 写盘点 = `:98 OUT` / `:908-913`（`--dry` 只打印不写；不带参数 = **覆盖写**）。
- 全仓 `grep` 过：`tutorial_stages.json` 的**写方只有这一个脚本**，其余全是读方（`BattleDriver.cs` · `TutorialOverlay.cs` · `工具/ruleprobe/TutorialProbe.cs` · `Editor/BattleScene.cs` · `Editor/B108_A8_TutorialArrow_Assert.cs` 等）。
- ⇒ **⛔ 别手改这份 json**（下次重生成会被覆盖）；要改就改生成器再重跑。

---

## 八、顺手发现（不进本单范围，供调度台分流）

1. **另外 5 个 `unresolvedNoName` pid 也能解**（同一份 allcards，判据同上）：

| pid | allcards 结果 | 池内是否直接命中 | 备注 |
|---|---|---|---|
| `-124800208381561086` | `GOF2` Makari the Grot | ✅ 池内 `GOF2` | 出现 9 处（S2 的 RadioMessage / SmallTip） |
| `4406439244507976715` | `ASH7` Veteran Guardian | ✅ | 2 处（S5 AI 5 的 SmallTip / PlayCard） |
| `7386907376838221809` | `SAU4` Gauss Warrior | ✅ | 1 处（S3 AI 2 的 PlayCard）—— 与 `OCR_NAME_FIX` 同卡，可当旁证 |
| `8095124868697240450` | uniqueId `141c5ed661a9e42bd9a793f0d2a1c682` Unhallowed Gifts | ✅ 池里就是这个 id | 1 处（S4 Player 2 的 DrawCard） |
| `8223774895360888806` | uniqueId `8d04a976efd628e43ad37d67e57140a0` Aggressor Sergeant (tutorial) | ⚠️ **名字带 `(tutorial)`**，按 `norm` 会落到 `UM19` | 1 处（S1 AI 7 的 Attack）—— **tutorial 变体的数值可能与池里那张不同，⛔ 别按 norm 硬塞** |

   ⚠️ 把这 5 个也一并填进全局 `pidname` 的话，会**顺带改变** `TutorialScript.ScriptedCardMatches` 的匹配严格度（原先 `name`/`ourId` 都空 ⇒ `HasScriptedKey=false` ⇒ 原版口径"退回 `list[0]`"；填了之后会按名/按 id 过滤）⇒ **那是另一件事，必须单独裁定 + 单独复跑教程断言**。本单**故意不做**。
2. **`cards_raw.json` 是 67 MB 且被 `.gitignore:274` 挡住** ⇒ 换机器/新 clone 上生成器会退回今天的行为。**根治建议**（另立账）：由 `cards_raw.json` 派出一张**小表进库**（`path_id ⇄ uniqueId ⇄ cardName`，1,351 行 ≈ 100 KB），生成器优先读小表、读不到再读大 json。这样 73 MB 的衍生物仍不进仓库、而 pid 映射可复现。
3. **报告 `普查产出_1018/allcards_1350/报告.md:58`** 已明写「名字不能当主键（同名不同 id 325 条）」—— 与本次选择"走 `uniqueId` 不按名"同源，别再走回头路。

---

## 九、没查清 / 风险

- ❓ **2026 build 的 allcards 与教程 SO（2025 的 `assets_full`）是不是同一份 SO 版本**：pid 空间一致（同一 CAB 哈希 `CAB-fb8f01449289635703d83d52c3f18245`），且账里 `B141-STARTHAND` 那轮已用 76 个带名 pid 反查过「名字不符 0」。**但"同一 PID"只保证"同一张卡的 SO"，不保证教程当时用的数值版本与 2026 版一致**。对起手这件事没影响（`SetupInitialHand` 只按 id 从**我们的卡池**取 `CardDef`，不读 allcards 的数值）；对将来若想从 allcards 取数值才有影响。
- ❓ `cards_raw.json` 的 `path_id` 是否可能在两份 CAB 里指向不同对象（铁律里记过"同 pid 在两份 CAB 里可以是不同对象"）：本单的判据是 **pid 命中后 `uniqueId` 还要在卡池里逐字存在 + 卡名与关卡阵营自洽**，三重条件同时满足才落卡 ⇒ 已排掉这种可能。
- ⚠️ 本单**没有实跑生成器**（只读代理，且跑它要 UnityPy + 写盘）。补丁是照着现读的 931 行代码写的，**行号以本文件生成时刻（2026-11-04）为准**，落地前请 `grep` 复核函数名（⛔ 别只按行号定位）。

---

## 十、证据清单（可复查坐标）

| 用途 | 位置 |
|---|---|
| 三个 pid 的正本 | `Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（单行，byte 38,513,131 / 63,645,298 / 65,818,970） |
| 同上（易读副本） | `Unity/资料/普查产出_1018/allcards_1350/cards.tsv:777` / `:1281` / `:1327` |
| 池内卡名 | `RuleEngine/Resources/cards_engine.json`（`id=SAU34/TL27/TL30`，byte 2,734,722 / 4,743,416 / 4,801,980） |
| 关卡牌组 | `RuleEngine/Resources/tutorial_decks.json` → `stages[2].player.cardIds`（含 SAU34）· `stages[5].player.cardIds`（含 TL27/TL30） |
| 未解出清单（现状） | `tutorial_stages.json` → `unresolvedNoName`（8 行，含这 3 条）· `matchTierHistogram`（`起始单位:no-name=3`） |
| 生成器链路 | `Unity/工具/gen_tutorial_stages.py:225-252`（pid→名，唯一一档）· `:260-280`（名字→id exact/norm）· `:296-299`+`:642-657`（裁定映射）· `:302-342`（候选线索）· `:500-507`/`:510-525`/`:566-569`（三个消费点）· `:734-737`（起手四表）· `:821-825`（knownIssues） |
| 运行期消费 | `RuleEngine/Core/TutorialScript.cs:1598-1626`（`SetupInitialHand`，按 `ourId` 精确查池、认不出就出声跳过）· `:1669-1689`（`PoolLookup` / `Resolve`）· `:2073-2080`（`ScriptedCardMatches`） |
| 会变红的断言 | `CardPresentation/Editor/BattleScene.cs:35697`（`wantHand.Count == 3`）· 同段 `:35683-35703` |
| 归档包真身 | `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data`（531,962 B） |
