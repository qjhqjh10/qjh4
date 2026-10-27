# AUD1 · 任务线核销审查（2026-10-27 · 第八十五会话）

> 执行代理 = **AUD1**（审查型，只改 `资料/任务线/*/任务总表.md`）。
> ⛔ 未跑 Unity · 未动 git · 未改正本（`CLAUDE.md` / `项目任务.md`）· 未改账 `*.tsv` · 未碰 `判据/`。
> **判据来源（按优先级）**：① `资料/交接_第八十五会话.md` §三（本会话成果）② `资料/完全复刻/调度台日志.md` **§十一（§11·1～§11·11）** ③ `资料/完全复刻/待接线与排队项.md`（挂起项，**不是**「没做」）④ **代码现读**（`grep` / `sed` 逐条核）⑤ 各 `报告/B9_*.md`。
> 🔴 **凡「交接说做了」但代码现读对不上的，一律按代码判、留 §一 并写明缺口。**

---

## 〇、一句话结论

**14 条线的 §一 全部过了一遍**：**搬走 2 行**（§一 → §二，原文留痕）· **补进 7 行**（本会话新立、原各线 0 命中）· **就地订正 6 处**（状态/口径/数字 + 3 处「判据推翻」+ 1 处两文件互相打架）· **收窄 1 条**（不整条核销）· **判不清 4 组**（§三）。
**表格形状与行尾全部复验通过**（14 份：**0 处列数不符** · 全 **LF** · **裸 CR 0** · 全部以换行收尾）。
§一 表体行合计 **136 → 141**（移走 2、补进 7）。

---

## 一、逐线：§一 原有几条 → 移走 → 补进 → 现在几条

> 计数口径 = **§一 里的表格数据行**（散文/块注记**不计**；它们本轮有 6 处就地订正，见 §二·B）。

| 线 | 旧 §一 | 移走 | 补进 | 新 §一 | 说明 |
|---|---:|---:|---:|---:|---|
| 引擎线 | 30 | **1** | **3** | 32 | 移走「`ResolveTargets` 不挡 stealth/camo/ward」；补 `AiScoring.ActiveAbilityManaCost` / 「先试后拒」活 ctx 副作用 / `ErrUnimplemented` 逐处定性 |
| 界面线 | 16 | **2** | 0 | 14 | 移走 R1 开场动画链、`ShopData._shopItems`；另 **4 处就地订正**（R3 / Premium Campaign / X4 的 0.75·0.76 / U2·V1） |
| 战斗线 | 35 | 0 | **1** | 36 | 补 `GetCardEffectsToShow` 项源不统一 |
| 卡牌线 | 5 | 0 | **1** | 6 | 补 `NestedFadeGroup` 结构缺 18 条 |
| 特效线 | 11 | 0 | **1** | 12 | 补 Coffee.UIEffect 三个 dll（用户已批准搬） |
| 联机线 | 4 | 0 | **1** | 5 | 补 `NetProtocol` 对 `ctx.Events` 的说明是错的（**纯文档**，不需恢复整条线） |
| 自检断言线 | 6 | 0 | **1** | 7 | 补 `RewardsScene.cs:2546` 的 `StartsWith` 断言没鉴别力 |
| 音效线 | 1 | 0 | 0 | 1 | **就地订正 1 处**（`CardFaceAssertE5` 早就在 `_run_8_checks.sh` 里了） |
| 远端挂起线 | 1 | 0 | 0 | 1 | **就地订正 1 处**（§三「战场线暂停」与战场线自己的顶部互相打架） |
| 卡牌ID逆推线 | 2 | 0 | 0 | 2 | 逐条核过，无一条被本会话解决（`F1` 的 S1 总表已建、S2/S3 仍欠） |
| 工具文档线 | 10 | 0 | 0 | 10 | 逐条核过，无一条被本会话解决 |
| 战场线 | 10 | 0 | 0 | 10 | A1AU（上一轮）已按 `J10AR` 核销过；本会话无战场改动 |
| 活动线 | 4 | 0 | 0 | 4 | `L01–L03` 仍 `⏸ 等真机`、`L04` 仍 `✗`；LiveOps 框架 ②③⑥ 仍开 |
| 新手引导线 | 1 | 0 | 0 | 1 | 用户暂停；`E01` 仍欠 7 阶段主链 |
| **合计** | **136** | **2** | **7** | **141** | |

---

## 二、搬走 / 就地订正 / 收窄（逐条带证据）

### A · 搬走（§一 → §二，2 条）

| 线 | 原条目 | 判定 | 证据 |
|---|---|---|---|
| **引擎线** | **`ResolveTargets` 这一层不挡 `stealth` / `camouflage` / `ward`**（原 `✗ 要做`，理由「结算侧比选目标侧宽」） | **判据推翻：原版本来就只在合法性层挡** ⇒ 我们三条路已对齐、**不许改** | `报告/B9_C1T7_引擎三条判据.md` §③，原文「**正确，⛔ 不许改**」：`AbilityLogic.GetTargets`（= 我们 `ResolveTargets` 的对应物）里 `HasCurrentTrait` **调用数 = 0**。**错因** = 把「选目标挡」误推成「结算也该挡」。⚠️ 与引擎线 §一 `B9·IsValidTarget 的 camouflage 闸我们比原版严`（**仍开**）**不是同一件** |
| **界面线** | **R1·开场动画链（Boot → Intro UI → 播 intro.webm）**（原 `✗ 未做`，理由「素材在、链没有」） | **已完成**（阶段二就落地了，账过期） | `Shell/ShellRuntime.cs:1031` `Resources.Load<VideoClip>("Art/videos/intro")`（**不带扩展名**）+ 找不到时**出声**（`:1035`）· `Editor/ShellScene.cs:4718` 有断言面。**错因** = 证据是 `grep "intro.webm"` = 0，而代码用不带扩展名的 `Resources.Load` ⇒ 那条 grep 命中 0 是**必然的**。⚠️ **另 4 个视频**（`Main Menu Multiplayer` / `Main Menu Practice` / `Gacha Crate Opening` / `Menu_Chronomancer_Animation`）本地**没有**，那半**未随之关闭**（已在搬入行里写明） |
| **界面线** | **`ShopData._shopItems` 是字典 ⇒ 丢了顺序**（原 `✗ 未做`） | **判据推翻：假账，判 `D`** | `调度台日志` §11·5「= **假账**（字典只作身份缓存、货架渲染走有序数组）」· §11·6「裁定：商店两条 T7-1『假账』**C→D**（原版排列由服务端 store 决定、本地 0 份）」。**错因** = 只看了 `Dictionary` 这个类型名、**没看消费者** |

> ⚠️ §二 那两行故意**不合并**（A1AU 时的调度台裁定口径：`T09` 与 `↑详情` 两行都留、不合并）；本轮同样逐行留痕。

### B · 就地订正（6 处，都留在原节、原文保留）

| 线 | 位置 | 订正内容 |
|---|---|---|
| **界面线** | §一 `R3·三张 offer 底图` | `✗ 未做` → **`◐ 部分做`**：✅ 脚本 `工具/import_offer_backgrounds.py` + `Shell/OfferArt.cs` 的 GUID→图名表 + `OfferContainer.BuildForeground` 已接（`BaseOfferPopup.cs:799` 传 `C.FgArt`）；⏳ **仍欠 = `background` 那格没画**：`Content.BgArt` **全仓零消费**（`grep BgArt` 只命中 `CardView` 一个同名 `Color32`），`Artwork/background` 仍画常量 `40k_shop_popup_info_bg`（`BaseOfferPopup.cs:186/791`） |
| **界面线** | §一 `Premium Campaign 15 个 SO` | **15 → 14**（`13 阵营 PremiumItem` + `1 个 Premium Campaign Store`，另一个脚本）。🔴 原「13 阵营 + Store + UM」**把 UM 数了两遍**（`调度台日志` §11·5 现读订正）；另补「原版 Store `items[]` 最后一条 `targetId` 写 `premium_campaign_ec` 却引用 **SW** ⇒ **原版自己的 bug，照抄不修**」（同 §11·5） |
| **界面线** | §一 2026-10-25 X4 那条散文 | ✅ `Generic Popup Background` 的 `m_PixelsPerUnitMultiplier = 0.75` **已修**（`SettingsWindow.cs:1712-1715`，端帽 `169 ÷ 0.75 = 225.33`）；🔵 同句 `Mask Tabs buttons` 的 `0.76` **判据推翻、不用接** —— 那颗 `Image`（`40k_popup`）所在节点带 `Mask` 且 **`m_ShowMaskGraphic = 0`** ⇒ 原版**自己不画它**（`SettingsWindow.cs:1805-1812` 逐字段已记） |
| **界面线** | §一 `U2·V1 头衔空态` 的【代码项】 | ✅ **已落**：`ProfileData.EnsureLocalDefaultTitleOnce()`（`ProfileData.cs:1260-1273`）**装载期一次性**兜底、**只兜内存不走 setter**、**读口保持纯读**（第一版写在 getter 里会吃红 `BattleScene` 两条断言）⇒ 该行**只剩真机观感**（已 `⏸`） |
| **音效线** | §二 `SoundCueAssertT` 那行的尾巴 | 🔴 **该尾巴已闭**：`工具/_run_8_checks.sh:53` **已含 `CardFaceAssertE5.Run`**（全脚本 **14 条** `run`）⇒ `SoundCueAssertT` 10+2 条 + D 组**例行批跑跑得到**。⚠️ 原句「已记 → `自检断言线/任务总表.md` §一」**指空**（那行从未建过）—— 因为它已不需要，⛔ 别再补建 |
| **远端挂起线** | §三 表格的 **战场线** 行 | 该行一直停在 **10-23「再次暂停」**，与 `战场线/任务总表.md` 顶部的 **10-25 新口径**（「战场线可以复刻，只是暂停的是验收」）**互相打架** ⇒ 按现口径订正并划掉 |

### C · 收窄（1 条，**不整条核销**）

| 线 | 条目 | 收窄后 |
|---|---|---|
| **引擎线** | **`Aura.AddAuraKeyword` 不写 `_remnantTraits`（两本表打架）** | 候选①「残骸整条不施加」**已落地**（`Aura.cs:883` `if (t.IsRemnant) return;`，注释明写「⛔ 别改成往 `_remnantTraits` 补写」），**覆盖了 `Aura.cs:900` 那个调用点**；但 `AddAuraKeyword` **还有第二个调用点** `EffectResolver.cs:10981`（`fromAura` 嵌入关键词那条路），那一处**过不过残骸闸未核** ⇒ **本条仍开**，缺口收窄到这一处 |

---

## 三、判不清（逐条写「差什么」）

1. **引擎线 `Aura.Activation` 第二条路（`EffectResolver.cs:10981`）是否过残骸闸**
   差什么：要读 `GrantEmbeddedAura` 的**调用方**（谁在什么时候对哪个单位调），确认目标能不能是残骸；`EffectResolver.cs:10933-10985` 这段本身没有 `IsRemnant` 判断，但闸可能在调用点。**没查清 ⇒ 不核销。**
2. **界面线 R3 的 `background` 那一格到底该不该画**
   差什么：原版该 prefab `background` Image 的**出厂 `m_Sprite`**（若出厂就非空 ⇒ 我们今天画的常量可能就是对的，tier 图只走 `foreground`），以及 4 份 Premium Item Offer SO 的 `BackgroundImage=1` 与 `ForegroundImage=2` 两格**各自指向哪张 tier 图**。我确认的是「`Content.BgArt` 零消费」这个**事实**，判不了「是不是缺陷」。
3. **界面线 `Premium Campaign` 除 UM 之外的 13 份阵营 SO**
   差什么：`Shell/CampaignData.cs` 只落了 `premium_campaign_um`（`:10`）。要判「其余 13 份是否需要各自的**节点表**、还是共用一张」需要读那 13 份 SO 的 `items[]`/节点结构 —— 本轮只核了「只有 UM 一套」这个事实，**没核规模**。
4. **`RuleCodes.ErrUnimplemented` 的全仓混用面**
   差什么：本轮只做了**定点抽查**（`BattleDriver.cs:16182` 教程闸 / `NetApply.cs:128/167` / `EffectResolver.cs:1957` / `RuleCore.cs:11725` 已改）。**逐处定性本身就是那条新账的活**，不在核销审查里做完。
5. **`ctx.Events` 与 `Fingerprint` 的现状**
   差什么：我采信了 `E5ENG` 的现核（`报告/B9_E5ENG_引擎三条真偏离.md` `:672-679`：`PlaySignals` 抽的是 `Ctx.Signals`；`ctx.Events` 只在 `EventCapacity = 2000` 时从头部裁、从不清空），**没有独立复跑** `BattleContext.cs:620/1858`。文档该改成什么，我在新账行里写了，但「`Fingerprint` 该不该含 `Events.Count`」是**设计裁定**、不是核销问题。
6. **覆盖面边界（必须说清）**
   本轮**没有**逐条回源核 §一 的全部 136 行（那等于重做一遍完全复刻）。做法 = 以**交接 §三 + `调度台日志` §十一**（简报指定的判据源）为筛子先找出「本会话可能动过」的条目，**只对这些回代码现核**；其余按「无本会话完成证据 ⇒ 留 §一」处理。⇒ **「留在 §一」不等于「我核过它确实没做」**，只等于「本会话没有把它做掉的证据」。

---

## 四、我**没有**权限改、但该改的（列给调度台）

| 文件 | 该改什么 | 为什么 |
|---|---|---|
| `资料/交接_第八十五会话.md` **§四 第 1 条** | 「Coffee.UIEffect 第三方 dll 要不要搬进工程 —— 等用户拍板」⇒ **用户 2026-10-27 已批准搬**，改成执行项（判据 `判据/J-12_Coffee前置判据.md`） | 简报明确「用户已批准搬」；交接仍写「等拍板」会误导下一个会话 |
| `资料/完全复刻/待接线与排队项.md` **「⏳ 等用户拍板」Coffee 行** | 同上，从「等拍板」移到可执行队列 | 同上 |
| `账/商店_*.tsv`（商店域） | 若 `ShopData._shopItems` 那一行还不是 `D`，按 §11·6 裁定改 `D` | 任务表已核销，账要与之一致 |
| `账/战斗逻辑驱动.tsv` 或对应域 | `ResolveTargets` 那一条若记 `B`/`C`，按「判据推翻、原版就是如此」改 **`A`**（或 `D` + 时间限定） | 任务表已搬 §二；账若仍开 = 两份说法打架 |
| `账/*.tsv`（R1 intro 那一行，若有） | 若记 `C`，改 `A`（`ShellRuntime.cs:1031`） | 同上 |
| `CLAUDE.md` §三 / `资料/工作准则_操作经验.md` | 建议加一条：「**判『某个链没有』别只 `grep` 带扩展名的文件名** —— `Resources.Load` 不带扩展名（R1 就是这么误判成 `✗` 的）」 | 这是本轮抓到的最可复用的方法论坑 |
| `资料/完全复刻/待接线与排队项.md` | `GetCardEffectsToShow` 已有条目（引用正确），无需改；但建议把它的**三个前提**与我在 `战斗线 §一` 新增行**对齐一次**（今天我照抄了它的三个前提） | 避免两处写法漂移 |

**我没有动 `Assets/**`，也不需要动**：本轮只做了「核销 + 记账」，0 行代码改动。

---

## 五、复验（怎么重新跑）

```bash
# 1) 表格列数 + 行尾（14 份一起过；排除 GFM 转义的 \| ）
python -I -X utf8 d:/4/_tmp_view/aud1/check_tables.py
#   ⇒ 期望：col-issues 0；14 份全 LF / bareCR 0 / endsNL True

# 2) 本轮编辑脚本（幂等性：它带 find_one 断言，已改过的文件再跑会直接 assert 失败 —— 这是故意的）
python -I -X utf8 d:/4/_tmp_view/aud1/aud1_edit.py          # dry-run
python -I -X utf8 d:/4/_tmp_view/aud1/aud1_edit.py --apply  # 落盘
```

> ⚠️ 编辑前已把 14 份整目录备份到 **`_tmp_view/aud1/任务线_bak_aud1/`**（回滚 `cp -r` 回去即可）。
> 本轮**没有跑 Unity**（红线），也没有跑 `typecheck`（未改 `.cs`）—— 纯文档改动，按铁律 12 判据 3「纯文档 ⇒ 一条自检都不用跑」。
