# B116_W23 · `ENG-07` `massive` 的敌方卡水平镜像

日期：2026-11-02 · 执行代理 `B116_W23` · 判据来源：`B116_R7`（2026-11-02 现读，本次未重查，只补了「是哪个对象」那一层）

## 一、判据（一手，本次复核到的两句）

`d:/2/tools/decomp_full/CardScript__CardSetup.c:198-210`（ILSpy `CardScript.cs:6536`）：

```c
cVar8 = RawCardScript__ShowDefaultTrait(*(longlong *)(param_1 + 0x28), 0x24e, 0);
if ((cVar8 != '\0') && (param_3 == '\0')) {            // param_3 == isPlayer
  lVar13 = UnityEngine_Component__get_transform(*(longlong *)(param_1 + 0x20), 0);   // cardImage
  local_48 = CONCAT44(0x3f800000, DAT_1834b2bc8);      // (-1, 1, 1)
  UnityEngine_Transform__set_localScale(lVar13, &local_48, 0);
}
```

- `_DAT_1834b2bc8` = **`-1`**（float）⇒ 三元组 `(-1, 1, 1)` = **水平镜像，不是放大**。
- `param_1 + 0x20` = **`EntityScript.cardImage`**（`dump.cs:21966`，`UnityEngine.UI.Image`）。
  在 `d:/2/tools/assetripper_out/ExportedProject/Assets/CardPrefab.prefab` 里它指向
  `CardPrefab/2DCard/Front/CardImage`（GO `&1121005239445321`）⇒ **我们的等价层 = `CardView._art`**。
- ⚠️ 同值 `0x24e` 另有 `AbilityTrigger.Combo = 590` —— **另一个枚举**，`CheckForCombo` 那 4 处与本条无关（未混）。
- ⚠️ 条件读的是 **`ShowDefaultTrait`（卡模板自带的 trait）**、不是运行时 trait ⇒ 实现用 `CardDef.Keywords`，
  **不用** `UnitState.Has(...)`（那本字典会被 jam 摘掉，而原版不还原）。

## 二、三处落点（改前 / 改后）

| # | 文件:行号 | 改前 → 改后 |
|---|---|---|
| 1 | `Core/CardView.cs:7022`（`ApplyArtTransform`） | `= Vector3.one * _artLocalScale` → `= new Vector3(_artLocalScale * _artMirrorX, _artLocalScale, _artLocalScale)` |
| 1b | `Core/CardView.cs:15813-15829`（**类尾新增**） | `SetArtMirrorX(bool)` / `ArtMirrorX` / `ArtLayerScaleX` / `float _artMirrorX = 1f` |
| 2 | `Battle/BattleDriver.cs:26906`（`SyncBoard`，`SetAmbush` 之后**同一物理行**） | `… NoteAmbushObserved(owner, s, u);` → 追加 `ApplyMassiveMirror(v, u.Card, mine);` |
| 2b | `Battle/BattleDriver.cs:40224-40240`（**`BattleDriver` 类尾**，`NoticeOathGateBlocked` 之后） | `const string MassiveKeyword` + `static bool ApplyMassiveMirror(CardView, CardDef, bool mine)` |
| 3 | `RuleEngine/Core/CardDef.cs:8686`（`Prefixes`，**同一行**） | `… { "precognition", … },` → 追加 `new[] { "massive", "massive" },` |

**行数变化**：`CardDef.cs` **0** · `BattleScene.cs` **0**（同行追加）· `CardView.cs` `15794→15831`（+37，全在类尾）·
`BattleDriver.cs` `40288→40324`（+36，全在 `BattleDriver` 类尾）。**行尾**：HEAD 与工作树逐字节同口径
（`CardView`/`CardDef` 纯 LF、`BattleDriver`/`BattleScene` 纯 CRLF），**无翻行尾**。

### 🔴 踩到的一个坑（已就地写进 `BattleDriver.cs:40219-40221`）
本文件有**两个顶层类**：`BattleDriver`（结束于 `:40205`）+ 文件后半 `namespace` 里的 `ChoicePanelPool`。
第一版把新成员追加到**文件最末** ⇒ 落进 `ChoicePanelPool` ⇒ `typecheck` 报 `CS0103 ApplyMassiveMirror 不存在`。已挪进 `BattleDriver` 类尾。
⇒ **纪律**：「追加到类尾」之前先确认「文件最末那对 `}` 关的是不是这个类」。

### 明确**没有**做的事
⛔ `massive` **没进** `Implemented`（`trait_collection.json` 无 SO，进了会多显示一枚原版没有的徽标）·
⛔ 没镜像 `_body3D`（原版只写 `cardImage`，而 `Card 3D` 自带 `localRotation (0,180,0)`，见 `BuildBody3D:7927` ⇒ 再镜像会凭空多一个差异）·
⛔ 没改 `IsPlayer` 语义、没改任何别的层（卡框 / 数值 / 徽标 / `_artFront`）。

## 三、断言（新建 `Editor/B116W23_Assert.cs`，**29 条**，已接线）

接线：`Editor/BattleScene.cs:46896`（方法尾聚合行，`=== 合计 ===` **之前**、`//` **之前**，同一物理行 ⇒ 零行数变化）。

- **A 段（7 条）** `Normalize("Massive"/"massive"/"MASSIVE") == "massive"`（期望值是**字面量**）· `massive` **不在** `Implemented` ·
  带 `Massive` 的 `CardDef.Keywords` 里**真的有** · `KeywordTable.Dropped` 里没有 `"Massive"` · 对照卡真的没有。
- **B 段（9 条 + 7 条硬前提）** 我方 `+1` / 敌方 `-1` **并排断**（`mv.ArtLayerScaleX != fv.ArtLayerScaleX`）；读的是
  `_art.transform.localScale.x` 的**真值**（不回读字段）；另断 `ArtLocalScale` 仍 `+1`（只翻 x 一维）。
- **C 段（6 条）灭自证** 敌方·**不带** `massive` ⇒ 两侧都 `+1`；同一张敌方视图改喂不带 massive 的卡 ⇒ **回到 `+1`**
  （🧨 只改字段不调 `ApplyArtTransform` ⇒ 红）；再喂回 ⇒ 又 `-1`（可逆）。
- 硬前提一律写成独立的 `Tr(...)`（⛔ 没有 `if (x != null) Check(...)` 那种静默空转绿）。

## 四、各闸读数

| 闸 | 读数 |
|---|---|
| `bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `check_ledger_points.py` | A 5831 · 通过 5831 · **漂移 0 · 失败 0**（告警 864 未变） |
| `check_aggregate_lines.py Editor/BattleScene.cs --any-assert-call` | **PASS**（`:46896` 56 个调用全在 `//` 之前，首个 `//` 在字符 5334） |
| `bash 工具/ruleprobe.sh play check` | **rc=0** · 哨兵 ✓「基线内容与钉住的一致（`a8a2a218…`）」**指纹未变** |

## 五、顺手发现（本次不改，交调度台分流）

1. 🔴 **`_art` 在场上形态是隐藏的**（`CardView.ApplyFace:2374` `Show(_art, !board)`）——正常路径看得见的是 3D 体 `_body3D`
   ⇒ 本次镜像在**场上**画面里看不见。**但这与原版一致**：原版 `cardImage` 也在 `2DCard` 里，而原版 inPlay 类状态把整张 `2DCard` 关掉
   （既有判据 `CardView.cs:4383-4385`）。镜像真正可见的时机 = 「手牌→场上那条 clip 里 2D 卡面淡出前半段」及任何 2D 卡面视图。
   **建议**：⛔ 不要为了「看得见」去镜像 `_body3D`（原版没这么做）。
2. ⚠️ **池内 0 张卡带 `massive`**（`cards_engine.json` 1127 / `cards_truth.json` / `cards_raw.json` 现扫各 **0** 命中）⇒ 本条**零行为变化**，
   只把「将来一有卡就静默失效」提前堵上（与 `ward`/`Requiem` 同一类）。真机验收需造一张带 `massive` 的卡。
3. ⚠️ 已登记但**未进** `Implemented` ⇒ 带 `massive` 的卡面会打 `massive*`（「本版引擎不结算」）。这是如实报（红线），
   但 `massive` 的「机制」其实只是表现层镜像、没有结算侧行为 —— 若要**不**打星，需要一个「表现层关键词」白名单，**那是另一件活**（本次未做、未立账）。
