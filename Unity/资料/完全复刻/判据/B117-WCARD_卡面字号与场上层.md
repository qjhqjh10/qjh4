# B117-WCARD · 卡面字号校正 / 场上 2D 阴影淡出 / 墓地不可命中（执行代理 `B117-WCARD` · 2026-11-02）

> 独占文件：`Unity/MyGame/Assets/CardPresentation/Core/CardView.cs` · 新建 `Assets/CardPresentation/Editor/B117_WCARD_Assert.cs` · 本文件。
> 红线：未碰 `BattleDriver.cs` / `Editor/*Scene.cs` / `Editor/B117_F14_Assert.cs` / 账主表 / `活状态表`；未动 git；未跑 Unity 批处理。

---

## 零、结论（三句话）

1. **① `UI-21` 字号**：F14 的**方向对、幅度不对**。按实跑读数（卡名 6.13% / 效果 4.94%）反推，
   把 **autosize 上界**（不是名义 em）按 `目标 ÷ 实测` 校正：卡名 **×1.1193**、效果文字 **×0.8333**。
   预计落 **6.86% / 4.11%**（判据带 `[6.40,7.30]` / `[3.70,4.50]`）。
2. **② `B117-53`**：`_shadowLayer` 现在按原版语义补上 **0.2 s `_ShadowColor.a` 淡出**（`CardHighlight.OutlineAnimTime`
   = `DAT_1834b2bb0` = `HIGHLIGHT_COLOR_CHANGE_TIME`），**⛔ 不是 `SetActive(false)`**；场上出生那一档在**建层处**直接落 0（两个入口各设一次）。
3. **③ `B117-54`**：**判据已过期 —— 这条【已经做完了】**。`SetHitEnabled` 早就有调用方（`BattleDriver.cs:25623` / `:35469`，
   恢复口 `:35499` + `CardFeel.cs:3630/3679/3682`）。本次只**就地订正**文档里那句「只有 helper、没有调用方」，并补断言钉住 CardView 侧契约。

---

## 一、① `UI-21` 字号校正（卡名 / 效果文字 × 三档）

### 1·1 为什么只改 autosize 上界就够了

`CardView.Fill` 的次序是「先 `FitToWidth`（只收宽）→ 再 `FitToBox`（跑原版 autosize 二分）」，
而 `FitToBox` 的**快路**（`Core/CardView.cs:5949`）第一句就是 `t.fontSize = bandMax;` ⇒
**装得下时渲染字号恒等于 `bandMax`，与名义 em 无关**。
F14 当初踩的坑（「只改名义字号一点也不变小」）正是这一条；反过来，**只改 `bandMax` 就足以改渲染尺寸**。

### 1·2 反推（判据 = F14 那条断言自己的实跑读数）

| 档 | 现 `bandMax` | 实跑 | 判据目标 | 倍率 | **新 `bandMax`** | **预计** |
|---|---|---|---|---|---|---|
| 卡名（单位/标准，`NameUnitBandMax`） | 1.425 | 6.13% | 6.86% | ×1.1193 | **1.595** | **6.86%** |
| 卡名（开包族，`NameUnitBandMaxForUse`） | 1.500 | — | 同比例 | ×1.1193 | **1.679**（= 1.595 × 20/19） | — |
| 卡名（战术档 `NameTacticBandMax`） | 1.800 | — | 同比例 | ×1.1193 | **2.015**（= 24 × 0.1 × 0.8395） | — |
| 卡名（`Big` 两档 `NameUnitBigBandMax` / `NameTacticBigBandMax`） | 1.800 | — | 同比例 | ×1.1193 | **2.015** | — |
| 效果（单位/标准，`DescUnitBandMax`） | 1.872 | 4.94% | 4.12% | ×0.8333 | **1.560** | **4.11%** |
| 效果（开包族，`DescUnitBandMaxForUse`） | 1.482 | — | 同比例 | ×0.8333 | **1.235**（= 1.560 × 19/24） | — |
| 效果（战术档 `DescTacticBandMax`） | 1.482 | — | 同比例 | ×0.8333 | **1.235** | — |
| 效果（`Big` 两档 `DescUnitBigBandMax` / `DescTacticBigBandMax`） | 2.028 | — | 同比例 | ×0.8333 | **1.690** | — |

推算依据（F14 同一条实测量法，`B117_F14_Assert.CjkMetrics`）：
`advance / bandMax = 0.1283 / 1.425 = 0.0900`（卡名）· `inkH / bandMax = 0.1644 / 1.872 = 0.0878`（效果）。
⇒ 目标 advance `0.0686 × 2.0927 = 0.14356` ⇒ `bandMax = 1.5945` ⇒ **1.595**；
目标 inkH `0.0412 × 3.3313 = 0.13725` ⇒ `bandMax = 1.5628` ⇒ 取 0.005 网格 **1.560**。

### 1·3 名义 em 为什么【不动】

`TitleEmOfCard`（0.04275）/ `KeywordFontSize`（0.0707）/ `TacticKeywordFontSize`（0.19）**保持原值**，理由两条：
① 上面那条快路 ⇒ 只要 `maxHeight > 0`（这四层都传了框高），**名义 em 对渲染尺寸零影响**；
② `Editor/CardFaceProbe.CheckLayers`（`W62/T2` 那四条）把名义值**钉成 14.25 / 23.55**，
   而 `CardFaceProbe.cs` **不在本代理白名单**（改它 = 越界 + 撞车）。
⇒ 本批只改「有效的那一半」，并在常量注释里写明「渲染尺寸 = `bandMax`」。

---

## 二、② `B117-53` 场上 2D 阴影淡出（不是关节点）

- **判据**（`B117-GD` §一 新查 + `decomp_full/Card2DController__ToggleCardShadow.c`）：
  原版 inPlay 系把整棵 `2DCard` 关掉，其中 `Card Highlight And Shadow` 走的是
  `ToggleCardShadow(false)` = 把 `_ShadowColor.a` 在 **0.2 s** 内淡到 0（`DAT_1834b2bb0`），**不是 `SetActive(false)`**。
- **实现**（都在 `CardView`，零行数变化的就地改 + 类尾新成员）：
  - 新增 `_shadowMul`（1 = 满、0 = 关）+ `SetCardShadow(bool on, float time)`（**`Core/CardView.cs:15916`，类尾**）：
    `DOTween.To(() => _shadowMul, v => { _shadowMul = v; ApplyShadowColor(); }, to, time)` 经 `CardTween.Use(..., Ease.Linear, this)`。
  - `ApplyShadowColor()` 的写入式改成 `ShadowAlpha * _alpha * _shadowMul`（**唯一写点** `:6407`，`ApplyTint` 那条路自动跟着走）。
  - **换形态入口**（`SetFace`，**`:2383`** 同一物理行）追加 `SetCardShadow(!board, CardHighlight.OutlineAnimTime)`。
  - **出生入口**（`Build` 建层处 **`:4396`** 同一物理行）按 `_faceMode == CardFace.Board ? 0f : 1f` 直接落值
    —— 铁律 10 第 5 条：两个入口各显式设一次。
- ⚠️ 该层**保持 active**（它与描边同材质同节点）；`MinionManager.HideCardShadow`（落点指示影）与本条无关。

---

## 三、③ `B117-54` 墓地 / 移出游戏不可命中 —— **已做完，判据过期**

| 判据 | 落地 | 位置 |
|---|---|---|
| `CardScript.SetColliderNotInHand(state)`：`inCemetery(6)` / `removedFromGame(11)` ⇒ `CardCollider2D.Toggle(false)` | 阵亡 → 进墓地那一档 | `Battle/BattleDriver.cs:25623`（`W9K01`，只做 6 那一半；`removedFromGame` 我们引擎全链未建） |
| 同上（战术卡 `DissolveWholeCard` ①） | 出对半溶卡时关 | `Battle/BattleDriver.cs:35469` |
| 恢复口 | 撤销溶卡 / 淡入到点 | `Battle/BattleDriver.cs:35499` · `Core/CardFeel.cs:3630 / 3679 / 3682` |
| `Toggle(false)` 连第二面（uGUI `UI Collider`）一起关 | `SetHitEnabled` 内 | `Core/CardView.cs:9571` |

**本次改动**：只把 `SetHitEnabled` 的 doc 里那句「🔴 2026-10-25（第 3 轮）：只有 helper、还没有调用方」**就地订正**
（铁律 5：错的描述要改掉本身），并补三条断言钉住 CardView 侧契约（两条命中判据 + 真 collider + 恢复）。
⛔ **没有**再往 `CardView` 加一个 `SetCardStateOption(int)` 之类的「状态 → 命中」封装 —— 那会是一条**零调用方**的死 API
（我们的 `CardView` 根本没有 `CardStateOptions` 那 18 档状态机，状态活在 `BattleDriver` 的表现层）；
真要收口，应该由调度台在 `BattleDriver` 那一侧把 6/11 两个调用点合并成一处，而不是在 CardView 造一个新入口。

---

## 四、断言（`Editor/B117_WCARD_Assert.cs`，静态类 + 无参 `Run()`）

**38 个 `Check` 调用点**（`Band()` 那处在运行期执行 8 次 ⇒ 实跑约 **45 条**）。三组：

| 组 | 条数 | 断什么 | 灭自证 |
|---|---|---|---|
| ① `FontRun` | ~20 | 真卡 `Uriel Ventris` 的**渲染读数**（卡名单字 advance / 卡宽 ∈ F14 那条带 · 效果行墨迹高 / 卡高 ∈ 同带）· `title.fontSize == 1.595` / `keywords.fontSize == 1.560` · **三档×两类八个数**（`BandMaxForTest`）· 开包族两档（1.679 / 1.235） | `fontSize` 那两条：只改名义 em 或把 band 改回旧值 ⇒ 先红；比值条同时越带 |
| ② `ShadowRun` | ~13 | 出生就在场上 ⇒ `_shadowMul == 0` · 手牌→场上 `Advance(0.1)` 时 **0 < `_shadowMul` < 1** · 终态 mul≈0 且材质 `_ShadowColor.a`≈0 · **那层节点仍 active** · 回手 `Advance(0.25)` ⇒ mul≈1、α≈0.604 | 中途读数 + 「节点仍 active」两条 ⇒ 改成瞬写 0 / `SetActive(false)` 必红 |
| ③ `HitRun` | 10 | `SetHitEnabled(false)` ⇒ `HitEnabled` / **真 `BoxCollider2D.enabled`** / `Contains` / `HitByCollider` / 第二面**五者一起关**；`SetHitEnabled(true)` ⇒ 恢复 | 真 collider 那条 ⇒ 「只在 `Contains` 里加假判据」必红 |

**接线（⚠️ 调度台做，本代理不许碰宿主）**：把 `B117_WCARD_Assert.Run();` 追加到
`Editor/CollectionScene.cs` 那条聚合行的**行尾 `//` 注释之前**（零行数变化）。
判绿红：`grep "B117WCARD"` 的**合计行**；逐条按**行首** `B117WCARD ✓` / `B117WCARD ✗` 数（⛔ 别 `grep -c ✗`）。

---

## 五、验证与已知的连带项

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wcard bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `CardView.cs` 行数守恒 | **15,893 → 15,967**：原有行号**一个没动**（锚点现核：`Show(_frame…)` 2383 · `_shadowLayer = AddLayer` 4393 · `ApplyShadowColor()` def 6402 · `TitleEmOfCard` 6025 · `DescEmOfCard` 6082 · `SetHitEnabled` 9567 · `RimParent` 15891 全部原位），**+74 行全是类尾新成员** |
| 行尾 | `CardView.cs` / 新断言文件都是 **LF、无裸 CR**（`b.count(b'\r\n') = 0`） |

🔴 **两处【别人名下】的断言钉着旧值，必须由调度台改**（本代理白名单外，改了就是越界）：

| 文件:行 | 旧 | 新 |
|---|---|---|
| `Editor/B106_W9_Assert.cs:80` | `1.425f` | `1.595f` |
| `Editor/B106_W9_Assert.cs:83` | `1.500f` | `1.679f` |
| `Editor/B106_W9_Assert.cs:87` | `1.872f` | `1.560f` |
| `Editor/B106_W9_Assert.cs:88` | `1.482f` | `1.235f` |
| `Editor/B108_A4_Aud02Card_Assert.cs:104` | `1.425f` | `1.595f` |
| `Editor/B108_A4_Aud02Card_Assert.cs:107` | `1.500f` | `1.679f` |
| `Editor/B108_A4_Aud02Card_Assert.cs:112` | `1.872f` | `1.560f` |
| `Editor/B108_A4_Aud02Card_Assert.cs:114` | `1.482f` | `1.235f` |

（⚠️ 这两份**当前没有任何宿主调用**（`grep B106_W9_Assert` / `B108_A4_Aud02Card_Assert` 在 `Editor/` 里 0 命中）⇒ 不进 `_run_8_checks.sh` 的 24 条；但**下次有人接线就会红**。）
🟡 `Editor/CardFaceProbe.cs:875-877` 的**散文**还写着「×0.75 / ×0.78 → 1.872」，而它检查的是**名义** `TitleFontSize` / `KeywordFontSize`（本次未动）⇒ **断言不红、文字过期**，接线时顺手改。

🟡 **待主对话跑一次确认（不是本代理能验的）**：
1. `CollectionScene.Run` ⇒ `B117F14` 那两条应转绿（预计 6.86% / 4.11%）。**若卡名落在 6.4% 边缘**：`FitsBox` 在 1.595 上如果判「装不下」会往下二分，届时把 `NameUnitBandMax` 回调到 1.59 再看一次（不要动带子）。
2. `CardBaseDemo.Run` 的 `AssertFaceTextFit`：它有一条 `lines >= needLines` 与「fit 真的缩过字号（fs < 名义）」。
   字号**变小**会让「每行装更多字」⇒ 理论上可能把 `needLines 2` 的卡压成 1 行。本轮按字数粗算不会
   （`Chaplain Letharius` 39 / `Roboute Guilliman` 62 / `Marneus Calgar` 47 字符，缩 17% 后仍 ≥ 3 行）；
   但**没跑过就是没验**，若红按 `B117_F14`/`F5` 的先例处理（那是「几行」这条 δ 判据本身的口径问题，⛔ 别改字号去凑）。
   ⚠️ 该宿主**不在** `_run_8_checks.sh` 的清单里（只手动单发）。

---

## 六、出处索引

- 字号档位：`Core/CardView.cs:5557-5578`（八个数）· `:15563-15568`（开包族两档）· `:6025` / `:6082`（名义 em，未动）
- 渲染尺寸 = `bandMax`：`Core/CardView.cs:5949`（`FitToBox` 快路）
- 阴影：`Core/CardView.cs:2383`（换形态入口）· `:4396`（出生入口）· `:6407`（唯一写点）· 类尾 `SetCardShadow`
- 命中：`Core/CardView.cs:9567` · `Battle/BattleDriver.cs:25623 / 35469 / 35499` · `Core/CardFeel.cs:3630 / 3679 / 3682`
- 判据源：`decomp_full/Card2DController__ToggleCardShadow.c` · `CardScript__RemoveFromGameCard.c:22-26` ·
  `CardCollider2D__SetColliderNotInHand.c:11-24` · `CardCollider2D__Toggle.c:6-9` · `资料/完全复刻/判据/B117-GD_动画与卡组组装.md`（B1 / B4）
- 断言：`Editor/B117_WCARD_Assert.cs`（新建）· 判据本体 `Editor/B117_F14_Assert.cs`（未动）
