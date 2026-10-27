#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 `账/_parts/B9_E4BDR3.tsv`（第三批：175/403/505/507/508/498/510 · 68 的消费点 · 532 的消费点）。

定位 = `原版类 + 方法`（⛔ 行号）。`状态` / `等级` 原样带走；只改 `差异` / `备注` / `轮次`（+ `我们的落点`）。
"""
import io
import os

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B9_E4BDR3.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]
ROT = "E4BDR3 2026-10-27"

PATCH = [
    # --- 505 ---
    dict(cls="CardScript", meth="ApplyMovementDisappearAnimRotations(Quaternion originalRotation, Vector3 axis, MinionMovementInfo.AnimationDefinition animationDefinition, float animTime, float currentTime)",
         landing="CardPresentation/Battle/BattleDriver.cs（`MinionAxisRot`）",
         diff="[E4BDR3 2026-10-27] **已改（1 行）**：`Quaternion.SlerpUnclamped` → **`Quaternion.LerpUnclamped`**"
              "（判据 = `CardScript__ApplyMovementDisappearAnimRotations.c:54`；ILSpy `CardScript.cs:12713-12758` 同）。"
              "⚠️ 原注释写「必须 `SlerpUnclamped`」—— **钳制那一半说对了、算子说错了**，已就地订正为"
              "「必须 `Unclamped`（`Slerp`/`Lerp` 的钳制版都会把曲线中键 −0.952 抹掉），原版用 `LerpUnclamped` ⇒ 照抄」。",
         note="影响面：`{Lerp,Slerp}` 在单位四元数下端点相同、中段速度曲线不同；原版是 Lerp ⇒ 照抄。"
              "自检口：`MinionAxisRot` 是 `static`，行为通过「三轴旋转补间在两个不同 t 的 `transform.rotation`」观察（现有断言已覆盖端点）。",
         ),
    # --- 508 ---
    dict(cls="CardScript", meth="DissolveWholeCard(float timeToDissolve)", append=True,
         add="｜[E4BDR3 2026-10-27] **③④⑤ 三句已补**（落点 = `BattleDriver.StopCardUiExtrasForDissolve`，"
             "`DissolveWholeCardInField` 与 `DoPlay` 战术支**两处**都调它）：⑤ `Card2DController.ToggleHighlight(false)` "
             "有**真对应层** ⇒ `CardView.SetHighlight(CardHighlightState.Normal)`（关状态环/整卡着色/高亮缩放）；"
             "③ `StopShowingCodex` 与 ④ `StopReflected` **我们这侧没有那两层**（Codex/Oath 角标已判 D —— "
             "`trait_collection.json` 的 `customObject` 全空引用 ⇒ 原版 `ShowCodex` 第一句就 return；"
             "`reflectedPrefab` 在原版也是空引用）⇒ **出声一次**、⛔ 不假装做了。"
             "另：`FadeCardItems` 淡的是 `fadeGroup`（2D 那一层）= 我们的 `SetAlpha2D`（上一批已按判据改，⛔ 不是整卡 `SetAlpha`）。",
         note_add="自检：`BattleScene.Run` 里溶解那两处都走 `animateFeel` 档；批处理档卡当场销毁 ⇒ ③④⑤ **没有可观察窗口**（如实记，"
                  "所以本轮**没有**给这三句加断言；`StopCardUiExtrasForDissolve` 有 `ResetDissolveExtrasWarnForTest` 可清零出声闩）。"),
    # --- 403 ---
    dict(cls="SwarmVFXController", meth="Initialize(CardScript card, Action onFinish)",
         landing="CardPresentation/Battle/BattleDriver.cs（`PlaySwarmMergeFeel` 的 ②·b）",
         diff="[E4BDR3 2026-10-27] **③ `ToggleBody3D(false)` 已补**：`PlaySwarmMergeFeel` 的 ②·b 调 "
              "`v.SetBody3DVisible(false)`（等价物 = `CardView.SetBody3DVisible` → `Show(_body3D,false)`）。"
              "⚠️ 原版那一跳还连带 `effectsAnchor` 的显隐（`ToggleBody3D` 内部），我们这侧 `effectsAnchor` **没有消费者**"
              "（特效都挂 `CardView.transform`）⇒ 如实记。"
              "**② 材质交接 = 不换**：原版 `CardMaterialHelper.ConfigureMaterial(mat, card)` 写的就是**两格贴图**"
              "（`_CardImage` = 卡面立绘纹理 · `_MatCap` = 按 `CardTier` 取的 matcap —— 见 `CardView.ConfigureRemnantMaterials` 的 doc），"
              "而它作用的对象是 **`SwarmVFXController` 自己的 `renderer(+0x20)`**；我们的 `WarpforgeEffectPlayer` **没有**"
              "「按卡改写特效材质」这条路径（特效材质来自效果库原件）⇒ **查不到判据 ⇒ 不换**（运行时出声一次，⛔ 不静默）。",
         note="断言：`BattleScene.Run` 虫群合并那一节新增 ⑤（`Body3DVisible == false`，**含前提断言**「合并前那具体是显示着的」）"
              "＋验完把体状态还原（用**合并前**的值，不是现读值）。⚠️ 若「前提」那条红 ⇒ 说明那一节的视图没有 3D 体"
              "（`use3DBoard` 档不同），本条要换到 3D 档验。",
         ),
    # --- 507 ---
    dict(cls="CardScript", meth="CardToMinionAnim()", append=True,
         add="｜[E4BDR3 2026-10-27] **只补了第③步 `ToggleBody3D(true)`，`PlayInvokeAnimation` 那一半仍未接**（两条硬理由）："
             "① 那支 clip 的名字在 `BattleCardUI__PlayInvokeAnimation.c:20-22` 里是 **`BattleCardUI` 静态块里的字符串**"
             "（`**(DAT_1842c3528 + 0xb8)`）—— **没读成字面量**，无法证明它就是 `Card Hand To Board`"
             "（旁证很强：同一方法末尾 `DelayedResetMaterial(CardScript.GetHandToBoardAnimEventTime(card))`，"
             "用的正是那条动画的事件时刻 —— 但**旁证不是字面量**，与 508 的 `Vector3.one` 同档）；"
             "② 我们的等价片 `CardFeel.HandToBoard` **开头就** `if (card.Face == CardFace.Board) return null;`，"
             "而 AI / 自动打出这条路的落地视图**已经是 Board 形态**（`SyncBoard` 建的）⇒ 直接复用**不会播**；"
             "要播得先把视图退回手牌形态（= 改形态语义，牵动 `SyncBoard` / 卡面 / 高亮三条链）⇒ ⛔ 不擅自做。"
             "落点：`LandAiDeployFlight` 里 `v.SetBody3DVisible(true)` + `WarnInvokeAnimNotWiredOnce()`（出声一次）。"
             "⚠️ 玩家部署那条路**已经**在 `StartHandToBoardClip`（0.3 s）播过同一支 ⇒ 本行**只服务 AI / 自动打出**，⛔ 别加两处（会播两遍）。",
         note_add="要闭合：① 先解 `BattleCardUI` 静态块的 clip 名字面量（VA 反汇编 + `.rdata` 读串）；② 再定「AI 落地视图要不要先退回手牌形态」"
                  "（那是形态语义改动，得单独立账）。"),
    # --- 175 ---
    dict(cls="CardAnimController", meth="RotateAnim(AnimInfo animInfo, Transform towardsTransform, bool actingCardIsPlayer)",
         landing="CardPresentation/Battle/BattleDriver.cs（`ptIsBoardCenter` 分支 `:23315-…`）",
         diff="[E4BDR3 2026-10-27] **按施工口径「0 行行为改动」只补守护**：`pt==5(boardCenter)` 那一支算出角之后多写一格"
              "**只读读数** `_lastBoardCenterAngleDeg`（`LastBoardCenterAngleForTest` / `ResetBoardCenterAngleProbeForTest`），"
              "行为一个字节没改。⚠️ 现读瘦身表 **99 拍里 0 条 `pt==5`** ⇒ 这一支今天**不可达**，所以断言只钉「那一支真的被走到并算出了 [0,360) 的角」"
              "＋**灭自证**（`pt=0` 时读数必须仍是 −1 —— 证明读数不是恒有值），⛔ 不拿内网的 `pos` 去复算那串三角（`pos` 取不到）。"
              "两条近似仍在（端点用对面棋盘 `DropTargetWorld(0/SlotCount-1)` 代替 `MinionManager.GetLeftMostPos/RightMostPos`；"
              "当场设 `carrier.rotation` 而原版是 `tm` 长补间）—— 闭合它们分别要引擎带出端点、要载体旋转泵，属另两条账。",
         note_add="断言位置：`BattleScene.Run` 的「偏差6」那一节（`PlayAbilityBeatForTest(… pt: 5 …)` + `pt: 0` 灭自证）。"),
    # --- 498 ---
    dict(cls="BattleManager", meth="StopTargetedSpellAnim()", append=True,
         add="｜[E4BDR3 2026-10-27] **按指令「判据待读，别动手」⇒ 本波 0 行改动**。前置仍是："
             "① 逐卡读 1,351 张 SO 的 `targetedSpellAnim` PPtr（`allcards` 本地可读）落一张 `卡id → PPtr/GUID` 表；"
             "② 全表为空 ⇒ 本行连同 `StartTargetedSpellAnim` 一起判 **D**（原版本身没有）；③ 确有值 ⇒ 先做 `StartTargetedSpellAnim` 再做本行。"
             "今天没有任何读数口能答「非空的 `targetedSpellAnim` 有几张」。",
         note_add="⛔ 别照名字造一件（这条与 510 不同：510 是**已证 0 调用点**，本条是**判据本身没读**）。"),
    # --- 510 ---
    dict(cls="CardScript", meth="ForceDisableCanActAnim()", append=True,
         add="｜[E4BDR3 2026-10-27] **D 的证据本波亲自复核过（与 J4FX 独立同结论）**："
             "① `grep -rl ForceDisableCanActAnim d:/2/tools/decomp_full` ⇒ **只命中 `CardScript__ForceDisableCanActAnim.c` 自己**；"
             "② ILSpy 全树 ⇒ 只有 `CardScript.cs` 的定义；③ `assets_full`（24.7 万文件）全树 `grep -rl` ⇒ **0 命中**，"
             "**且带正控**（同一棵树搜 `CardHandToBoardAnimationFinished` ⇒ 命中 "
             "`bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3614292623624764332.json`）。"
             "⇒ 0 代码调用点 + 0 序列化字符串引用 ⇒ **铁律 11 例外①（原版本身不触发）**，本波**不写代码**。",
         note_add="若调度台仍要形状：`public void ForceDisableCanAct() { _canActWanted = false; ApplyCanAct(); }`（1 行、零调用者）—— ⛔ 不是必须。"),
    # --- 68 的消费点：StopMusic ---
    dict(cls="MusicManager", meth="StopMusic(bool instant)", append=True,
         add="｜[E4BDR3 2026-10-27] **调用点已接**（原账「还差调用点」那半已不成立）："
             "`BattleDriver.Begin` 在 `BuildHud()` 那一段之后调 `ShellRuntime.Instance.StopMenuMusic(instant: false)`。"
             "🔴 **`instant` 实参本波现读**：`BattleManager__OnInitializationComplete.c:32` = "
             "`MusicManager__StopMusic(<实例>, **0**, 0)` ⇒ 第二实参 `instant = 0` = **false** ⇒ 走 `AudioSourcesController.FadeOut()`（3 s 淡出），"
             "⛔ 不是 `Stop()`。`Instance == null`（批处理 / 直接起战斗场景）时**出声一次**说明「这一档本来没有壳在放音乐」。",
         note_add="真机验收：菜单 → 战斗，战斗 BGM 起来时**不再有 idle 主题叠着**（账 68 的原症状）。"),
    # --- 532 的消费点：ResolveRemoveEffect ---
    dict(cls="BattleManager", meth="ResolveRemoveEffect(CardScript targetCard, CardEffect cardEffect)",
         landing="RuleEngine/Core/Enchantment.cs（`Enchantments.Remove` 成功分支发 `EvtKind.EnchantmentRemoved`）"
                 "+ CardPresentation/Battle/BattleDriver.cs（`PlaySignal` 的消费支）",
         diff="[E4BDR3 2026-10-27] **②③ 两个消费点已接线**：`BattleDriver.PlaySignal` 在 `StatusApplied` 那一支之后新增 "
              "`if (e.Kind == EvtKind.EnchantmentRemoved)` 分支 —— ② `CancelPendingAnimsOf(e.Player, e.Slot)` **无条件**（原版连走四条动作道）；"
              "③ `if (e.Amount == 15 || e.Amount == 16) RemoveDestroyableAnims(e.Player, e.Slot, e.CardId)`"
              "（原版那道 `(buffType - 15) > 1 ⇒ return` 的等价闸）。⚠️ 引擎约定：**`e.CardId` 里装的是 trait 名**"
              "（关键词/名字塞 `CardId`，⛔ 不是 `Keyword`）；⚠️ 原版 `ResolveRemoveEffect` **全程没有 `Broadcast*`** ⇒ "
              "`EvtKind.EnchantmentRemoved` 是**我们为表现层新开的窄口**（枚举那一条已打 `🔶 自己设计`，调度台 2026-10-27 裁定）。",
         note="断言（同步点补到 `RuleEngineTest_EngineX13`）：摘成功后 `ctx.Signals` 有一条 `EnchantmentRemoved` · `CardId == \"pray\"` · "
              "`Amount == 15` · `(Player,Slot)` = 载体格；再一条「`permanentBuff` 拒绝摘 ⇒ **不发**」。"
              "`BattleScene.Run` 要确认第 21 档没让未登记事件计数变红。",
         ),
]

TABLES = ["特效动画.tsv", "音频_语音.tsv", "战斗逻辑驱动.tsv", "战斗界面_HUD.tsv"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n")
    return text.split("\n")


def main():
    hits = {}
    for tbl in TABLES:
        p = os.path.join(LEDGER, tbl)
        if not os.path.exists(p):
            print("MISSING", p); continue
        for ln in read_rows(p):
            f = ln.split("\t")
            if len(f) != 11: continue
            hits.setdefault((f[1], f[2]), []).append(f)
    out = ["\t".join(COLS)]
    for it in PATCH:
        got = hits.get((it["cls"], it["meth"]), [])
        if len(got) != 1:
            print("!! 命中 %d 处：%s | %s" % (len(got), it["cls"], it["meth"]))
            for g in got: print("     ", g[5], g[6][:50])
            continue
        f = list(got[0])
        if it.get("append"):
            f[7] = f[7] + it["add"]
            if it.get("note_add"):
                f[10] = (it["note_add"] + "｜" + f[10]) if f[10] else it["note_add"]
        else:
            f[7] = it["diff"]
            if it.get("note"):
                f[10] = it["note"] + ("｜原备注：" + f[10] if f[10] else "")
            if it.get("landing"):
                f[6] = it["landing"]
        f[9] = (f[9] + "·" + ROT) if f[9] else ROT
        out.append("\t".join(f))
    io.open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))
    print("wrote", OUT, len(out) - 1, "rows")


main()
