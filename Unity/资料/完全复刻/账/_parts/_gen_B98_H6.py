# -*- coding: utf-8 -*-
# _gen_B98_H6.py — 生成账补丁 `B98_H6.tsv`（执行代理 B98_H6 · 第九十八会话 · 波 7 · 2026-10-29）
#
# 覆盖 6 条：`特效动画:503/521` · `轮抽:6/114/147` · `其他_基础设施:160`
# 做法：**从各自主表那一行原样起手**、只改该改的列 ⇒ 未改列逐字保真（⛔ 不手工重打整行）。
# 规矩：裸 `split("\t")`（与 `工具/merge_ledger_parts.py` 同口径）· LF · 11 列 · 先算进变量再写盘。
#
# 🔴 本波**零源码改动**（六条的残差全部落在调度台独占 / 黑名单文件里），补丁只做三件事：
#    ① 落点行号漂移订正（轮抽:114/147 · 特效:503）② 补本波现核的证据 ③ 记下「为什么没动代码」。
import io

BASE = r"D:/4/Unity/资料/完全复刻/账"
OUT = BASE + "/_parts/B98_H6.tsv"
HDR = ("域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注")


def load(fn):
    raw = io.open(BASE + "/" + fn, "rb").read()
    assert raw.count(b"\r\n") == 0, fn + " 不是纯 LF"
    return raw.decode("utf-8-sig").split("\n")


FX = load("特效动画.tsv")
DR = load("轮抽.tsv")
OT = load("其他_基础设施.tsv")


def row(lines, n, cls, mstart):
    c = lines[n - 1].split("\t")
    assert len(c) == 11, (n, len(c))
    assert c[1] == cls, (n, c[1])
    assert c[2].startswith(mstart), (n, c[2])
    return list(c)


def rep(c, j, old, new, tag):
    assert old in c[j], (tag, j, old[:60])
    assert c[j].count(old) == 1, (tag, "anchor 不唯一", old[:60])
    c[j] = c[j].replace(old, new)


out = [HDR]

# ============================================================ ① 特效动画:503
c = row(FX, 503, "CardScript", "ActivateTraitParticlesFromCodeInTarget(")
assert c[5] == "B", c[5]
# 判据出处：挂上本波判据件
c[3] = c[3] + " · 判据/B98_R1_特效3条闭合.md §503"
# 落点：B97_W28 那两处行号已 +3/+2 漂（现读 :9937 拍 _dhunt、:9939 = ctx.Emit）
rep(c, 6, "、`:9937` 的 `ctx.Emit(EvtKind.Death, …, amount: _dhunt)`",
    "、`:9939` 的 `ctx.Emit(EvtKind.Death, …, amount: _dhunt)`", "fx503-落点-emit")
rep(c, 6, ":9934` 里拍", ":9937` 里拍", "fx503-落点-拍")
c[6] = (c[6]
        + " · 消费侧待接 = `CardPresentation/Battle/BattleDriver.cs` 的 `PlayDeathFeel`"
          "（⛔ 本波不可动：`B98_H4` 在改该文件）")
c[7] = (c[7]
        + " ｜🔴 **2026-10-29（`B98_H6` 现核 · 未动代码的原因）**：① 督军那一档的发射点仍在 "
          "`RuleEngine/Core/RuleCore.cs:9783-9787`，`:9786` 现读 = "
          "`ctx.Emit(EvtKind.Death, p, slot, u.Name, instanceId: u.InstanceId);` —— **没有 `amount:`**（R1 §503 步骤 1 要补的那一格）；"
          "② `TraitParticles.PlayInTarget` **全仓 0 个生产调用点**（`grep -rn \"PlayInTarget\" --include=*.cs` 只命中它自己的定义 "
          "`CardPresentation/Core/TraitParticles.cs:194`）⇒ 消费侧那一跳还没人接；"
          "③ 两处落点**都在调度台独占 / 黑名单文件**里（`RuleEngine/Core/RuleCore.cs` 归 `B98_G1` · "
          "`CardPresentation/Battle/BattleDriver.cs` 归 `B98_H4`）⇒ 按铁律 13·3 本波**零代码**、交回调度台转派；"
          "施工步骤（①②③④）与可粘贴补丁仍按 `判据/B98_R1_特效3条闭合.md` §503，**行号已现核**："
          "`:9783`/`:9786` 未漂、`:9937`/`:9939` = `B97_W28` 的小兵发射（R1 写的 `:9937-9941` 已 +2 漂）。"
          "⛔ 另有两条别做：**不预置 Editor 探针**（无 consumer 时那正是「断言/探针建了没人接线」的先例，"
          "2026-10-28 一天内出过 4 次）；**不改 `EvtKind`**（会动 `ctx.Events`/`Fingerprint`）。")
assert c[9] == "B94_F3 · B94_B9 · B95_FX · B97_FXC｜B97_W28", c[9]
c[9] = c[9] + "｜B98_H6"
out.append("\t".join(c))

# ============================================================ ② 特效动画:521
c = row(FX, 521, "EntityScript", "GetCardEffectsToShow()")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 判据/B98_R1_特效3条闭合.md §521"
c[6] = (c[6]
        + " ｜ 残差四项**现读**（`B98_H6` 2026-10-29）：`RuleEngine/Core/EffectsToShow.cs:354`（oath/AuraKeyword 之外那格）"
          "· `:378`（附魔）· `:413` · `:427`（手牌）四处 **`Seq = 0` 硬写**；`:628` 的 `RowGroup` 字段在、"
          "但 `Of()` 不盖戳（生产点 = `RuleEngine/Core/EffectResolver.cs:11694 DoGive`）；"
          "`RuleEngine/Core/Enchantment.cs:71 ActiveCardEffect` 与 `RuleEngine/Core/CardInstance.cs:172 HandEffect` "
          "**都没有 `Seq` 字段**")
c[7] = (c[7]
        + " ｜🔴 **2026-10-29（`B98_H6` 现核 · 未动代码的原因）**：四个残差 S3-c / S3-d / S3-f / S7 的落点**全部**在 "
          "调度台独占或黑名单文件里 —— `EffectResolver.cs`（S3-c 的 `DoGive`、S3-f 的 `AttachHandEffect`）· "
          "`Enchantment.cs` · `CardInstance.cs` · `EffectsToShow.cs`（都在 `RuleEngine/**`）· "
          "`工具/ruleprobe/**`（S7）⇒ 本波**零代码**，交回调度台转派。"
          "⛔ 别只改 `EffectsToShow.cs` 那四处 `Seq = 0`（那四格的值本来就恒 0 —— 生产端不盖戳，改读取端是空转）；"
          "S3-c 落地后 `Editor/B97W16_Assert.cs` 的 **A9-b**（断「同一次 give 印 2 行」）要同步改断「合并」，否则新红。")
assert c[9] == "B90 · B93_F3 · B94_B9 · B95_FX · B97_FXC · B97_US2 · B97_W16｜B97_W18", c[9]
c[9] = c[9] + "｜B98_H6"
out.append("\t".join(c))

# ============================================================ ③ 轮抽:6
c = row(DR, 6, "DraftEventBase", "Expire(Action onComplete)")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 判据/B98_R2_非战斗11条裁定.md §一.8"
c[7] = (c[7]
        + " ｜[B98_H6 2026-10-29 现核] 三源复核「缺的只有到期时刻」这一步**仍然成立**："
          "① 方法体 `CardPresentation/Shell/DraftEventBase.cs:277 public override void Expire(Action onComplete)` 在位；"
          "② `Resources/draft_packs.json`（273,694 B）`endAt` / `endDate` / `expireDate` / `startDate` / `ExpireAt` **各 0 命中**；"
          "③ `CardPresentation/Shell/LiveOpsLocalCatalog.cs` 全文件 `draft` **0 命中**（本仓连一个 draft 事件的 SO 目录项都没有 ⇒ 连 `ILiveOps` 实例都不会建）；"
          "④ 到期链 `Shell/LiveOpsHandler.cs:487 ExpireEvent` 只由 `:503`（非弹窗支）/`:516`（弹窗支）排期 ⇒ **事件不存在时整条链不启动**。"
          "⇒ 维持 B（REMOTE · 可达性缺口，不是实现缺口）。**解封条件 = 拿到远端 draft 事件数据**；"
          "⛔ 不自己编到期时钟（用户 2026-10-24/25 裁定）。")
c[9] = c[9] + "｜B98_H6"
out.append("\t".join(c))

# ============================================================ ④ 轮抽:114
c = row(DR, 114, "DraftModeMenuPayStateTimed", "OnOpen()")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 判据/B98_R2_非战斗11条裁定.md §一.10"
rep(c, 6, "CardPresentation/Shell/DraftModeWindow.cs:4281（BuildTimedFamily，gated TimedDraftLocal.Enabled）",
    "CardPresentation/Shell/DraftModeWindow.cs:4297（`BuildTimedFamily`；gate = `:4299` 的 "
    "`if (!TimedDraftLocal.Enabled) return;`）· `:4327 BuildTimedArmyBar`（`:4332` 两条总量 + `:4333` 比值 + `:4336` 填充，`:4338 SetArmies`）"
    "· `:4304` 联盟计分占位（🔶 服务端排行榜本地 0）· `:4308-4312` 事件图（🔶 本地字符串名，原版 `drawData.EventAssets`）",
    "draft114-落点")
c[7] = (c[7]
        + " ｜[B98_H6 2026-10-29 现核] **落点行号已 +16 漂**（原 `:4281` → `:4297`；原备注里的 `:4311` → `:4327`），本波订正。"
          "**Timed 族在 player 里永不建**这一步也复核成立：`TimedDraftLocal.EnableForDemo`（定义 `Shell/DraftModeWindow.cs:4419`）"
          "**生产代码 0 调用点**，唯一置真入口是 Editor 断言里直接写 `TimedDraftLocal.Enabled = true`"
          "（`Editor/B93S4Assert.cs:268/274` · `Editor/B94B10_Assert.cs:368/386`）⇒ 维持 B（REMOTE）。"
          "⛔ 别把 `Enabled` 默认值改成 true（那会把 🔶 模拟内容塞进 Demo 变体、造出原版没有的页）。"
          "**解封条件 = 等远端 draft 事件数据 + Timed 宿主入口**。")
c[9] = c[9] + "｜B98_H6"
out.append("\t".join(c))

# ============================================================ ⑤ 轮抽:147
c = row(DR, 147, "DraftModeWarlordSelectionStateTimed", "OnOpen()")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 判据/B98_R2_非战斗11条裁定.md §一.11"
rep(c, 6, "CardPresentation/Shell/DraftModeWindow.cs:4281（BuildTimedFamily 内 :4299-4305：按 specificArmies 过滤 + RebuildWarlordCards）",
    "CardPresentation/Shell/DraftModeWindow.cs:4297（`BuildTimedFamily` 内 `:4315-4321`：按 `specificArmies` "
    "`RemoveAll` 过滤 `_warlords` → `:4321 RebuildWarlordCards()`）",
    "draft147-落点")
c[7] = (c[7]
        + " ｜[B98_H6 2026-10-29 现核] **落点行号已 +16 漂**（原 `:4299-4305` → `:4315-4321`），本波订正。"
          "残差只剩**标题幅面**，且它是**数据**缺口不是代码缺口：本仓**有**标签口"
          "（`CardPresentation/Shell/LiveOpsLocalCatalog.cs:200 TryGetLabel` / `:1153 GetEventLabel`），"
          "但 `draft` 在本地 SO 目录 / `draft_packs.json` / `LiveOpsLocalCatalog` 里**各 0 命中** ⇒ 拿不到 `EventLabels` 里的 Title 键；"
          "⛔ 自造键 = 编（用户 2026-10-25）。⇒ 维持 B（REMOTE），解封条件与 `轮抽:114` 同。")
c[9] = c[9] + "｜B98_H6"
out.append("\t".join(c))

# ============================================================ ⑥ 其他_基础设施:160
c = row(OT, 160, "Extensions", "AlmostEquals(Quaternion target")
assert c[5] == "C", c[5]
c[3] = c[3] + " · 判据/B98_R2_非战斗11条裁定.md §一.7"
c[7] = (c[7]
        + " ｜[B98_H6 2026-10-29] 维持 **C**（休眠）。三个成立条件本波逐条现核仍成立："
          "⑴ 原版 0 调用点（`decomp_full` 26,282 个 `.c` 只命中 `Extensions__AlmostEquals.c` + `NetworkingPeer` 三处它自己的 private 重载；"
          "`assets_full` 的 `m_MethodName` 0 命中）；⑵ 等价物在位（`CardPresentation/Core/AngleMath.cs:51 AngleDegTiny`，"
          "调用点 `CardPresentation/Core/CardView.cs:9975` 的 `> 0.01f`）；⑶ 窄带差 `[0.01°, 0.162°)` 记在案不实现（原版不可达）。"
          "⇒ 本波**零代码、零断言**（⛔ 别新建 `Extensions` 门面/helper —— 那才是死代码）。"
          "⚠️ **口径待调度台拍板（不是本代理能定的）**：本波派活单写「判据空 / 原版自己不可达 ⇒ 判 D」，"
          "而 `B94-R-L` / `B98_A7`（同日两次）裁定 **C = 休眠、⛔ 不判 D**；本条按后者维持 C，"
          "若调度台改采 D 口径 ⇒ **零代码、状态列一行改**。")
c[9] = c[9] + "｜B98_H6"
out.append("\t".join(c))

# ============================================================ 写盘前自检
assert len(out) == 7, len(out)
for i, ln in enumerate(out):
    cols = ln.split("\t")
    assert len(cols) == 11, (i, len(cols))
    assert "\n" not in ln and "\r" not in ln
data = ("\n".join(out) + "\n").encode("utf-8")
io.open(OUT, "wb").write(data)
print("WROTE", OUT, len(out) - 1, "rows", len(data), "bytes")
