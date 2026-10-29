# -*- coding: utf-8 -*-
# _gen_B98_K5.py — 生成账补丁 `B98_K5.tsv`（执行代理 B98_K5 · 第九十八会话 · 波 9 · 轮抽线 · 2026-10-29）
#
# 覆盖 3 条：`轮抽:6`（DraftEventBase.Expire）· `轮抽:114`（DraftModeMenuPayStateTimed.OnOpen）
#          · `轮抽:147`（DraftModeWarlordSelectionStateTimed.OnOpen）
# 做法：**从主表那一行原样起手**、只改该改的列 ⇒ 未改列逐字保真（⛔ 不手工重打整行）。
# 规矩：裸 `split("\t")`（与 `工具/merge_ledger_parts.py` 同口径）· LF · 11 列 · 先算进变量再写盘。
#
# 🔴 本轮**有源码改动**（与本波多数只改账的代理不同）：
#     新建 `CardPresentation/Shell/DraftLocalEvent.cs`（本地模拟的 draft 活动实例 + 到期判据）
#     新建 `CardPresentation/Shell/DraftLocalEventPump.cs`（player 启动装泵，每秒 Tick）
#     新建 `CardPresentation/Resources/draft_local_event.json`（模拟数据，独立文件；⛔ 没碰 draft_packs.json）
#     新建 `CardPresentation/Editor/B98K5_Assert.cs`（断言，宿主未接线）
#     改 `CardPresentation/Shell/DraftModeWindow.cs`：`:4321` 行内追加 `ApplyTimedWarlordTitle();`（零行数变化）
#        ＋ `ApplyTimedWarlordTitle()` 追加在**文件末尾的 partial 块**（既有账落点零漂移）
import io

BASE = r"D:/4/Unity/资料/完全复刻/账"
OUT = BASE + "/_parts/B98_K5.tsv"
HDR = "域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注"


def load(fn):
    raw = io.open(BASE + "/" + fn, "rb").read()
    assert raw.count(b"\r\n") == 0, fn + " 不是纯 LF"
    return raw.decode("utf-8-sig").split("\n")


DR = load("轮抽.tsv")


def row(lines, n, cls, mstart):
    c = lines[n - 1].split("\t")
    assert len(c) == 11, (n, len(c))
    assert c[1] == cls, (n, c[1])
    assert c[2].startswith(mstart), (n, c[2])
    return list(c)


out = [HDR]

# ============================================================ ① 轮抽:6  DraftEventBase.Expire
c = row(DR, 6, "DraftEventBase", "Expire(")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 报告/B98_K5.md §一 · `Resources/draft_local_event.json`"
c[5] = "A"
c[6] = (c[6]
        + " · 🆕 2026-10-29（B98_K5）：**触发源已落** —— "
          "`CardPresentation/Shell/DraftLocalEvent.cs:197`（`EnsureLoaded` = 造活动实例：eventId + startDate/endDate/expireDate）"
          " · `:323`（`Tick` = 到期判据 `GetState(NowUtc) == LiveOpState.Finished`，与 `LiveOpsHandler.ScheduleEventPopups` 的过期支同一句）"
          " · `:361`（`FireExpire` = 调 `Expire(handleOnComplete)`，回调形状 = `LiveOpsHandler.ExpireEvent` 的 `handleOnComplete`）"
          " · `CardPresentation/Shell/DraftLocalEventPump.cs:37`（`Install` = player 启动装泵，每秒 `Tick` 一次）")
c[7] = (c[7]
        + " ｜[B98_K5 2026-10-29] **触发源已补（本地模拟）**：方法体不用改（`:277` 逐条在位），"
          "缺的那一格「活动到期时刻」由 `Resources/draft_local_event.json` 提供（🔶 服务器已关服 ⇒ 用户 2026-10-28 授权的唯一自设计门类）："
          "活动实例 `DraftLocalEvent.Body : DraftEventBase<DraftEventSave, DraftEventData>` + `dates={start,end,expirable,expireDate}`；"
          "到点走同一条链（`Tick` → `Expire` → `LiveOpsSignals.EventExpired` 广播 + `onComplete`）。"
          "⚠️ **仍存的有意差异**：原版要求事件先被 `QueueEventPopup` 排过、`ExpireEvent` 才调 `Expire`；"
          "本地这份**不经过** `LiveOpsHandler`（那里没有任何 draft 事件、且不在本代理白名单）⇒ 直接调，已在代码文件头写明。"
          "⚠️ **顺手发现（未改）**：`CurrentDraftState` 的反推只给 0/5/20 ⇒ `Expire` 里 `== OngoingRun(15)` 那一支仍不可达"
          "（到期时不会 `FinishRun` 收尾正在跑的一局）；要补得动 `DraftEventBase.cs` 的 `CurrentDraftState`（会让该文件下游账落点漂移）⇒ 留调度台定。"
          "⚠️ **不做奖励**：`Expire → FinishRun → UpdateState` 全程无发奖步（`UpdateState` 已钉「收尾一律 Finished(20)，没有领奖步」）"
          "⇒ 到期结算里没有任何涉及发奖的路径需要出声。")
c[9] = c[9] + "｜B98_K5" if "B98_K5" not in c[9] else c[9]
c[10] = (c[10]
         + " ｜[B98_K5 2026-10-29] **A（触发源由本地模拟补上）**：账补丁 `B98_K5.tsv`；断言 = `Editor/B98K5_Assert.cs` "
           "A 段（活动实例/eventId/到期时刻/`EventPlayMode`）+ B 段（`GetState` 四档：Planned/Active/Finished/Expired）+ "
           "C 段（`Tick` 触发 `Expire`、`EventExpired` 广播、重复 Tick 不重复过期 ★）+ F 段（`draft_packs.json` 仍无 `endAt`/`expireDate`，⛔ 模拟数据没污染原版表）。"
           "**宿主未接线**（`Editor/MainMenuScene.cs` 的 `Run()` 加 `B98K5_Assert.Run();` + 两行 Pass/Fail 累加）。"
           "灭自证：C 段把 `LiveOpsClock` 注入到到期前 ⇒ `ExpireCount` 必须仍为 0；E/D 段关掉 `SimulationEnabled` ⇒ 断言必须红。"
           "typecheck 0/0（运行时 + 编辑器）。")
out.append("\t".join(c))

# ============================================================ ② 轮抽:114  DraftModeMenuPayStateTimed.OnOpen
c = row(DR, 114, "DraftModeMenuPayStateTimed", "OnOpen(")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 报告/B98_K5.md §二"
c[5] = "A"
c[6] = (c[6]
        + " · 🆕 2026-10-29（B98_K5）：**生产调用点已落** —— "
          "`CardPresentation/Shell/DraftLocalEvent.cs:289`（`ApplyToTimedLocal` = 装载模拟事件时调 `TimedDraftLocal.EnableForDemo(a0,a1)` "
          "并灌 `Totals` / `PremiumCost` / `PremiumTenCost` / `PremiumCurrency` / `EventImage`）"
          " · `:197`（`EnsureLoaded`）· `CardPresentation/Shell/DraftLocalEventPump.cs:37`（player 启动装泵时触发装载）")
c[7] = (c[7]
        + " ｜🔴[B98_K5 2026-10-29] **就地订正**：上面「⚠️ 没做，反证仍然成立」与两处「Timed 族在 player 里永不建」**已过期** —— "
          "`TimedDraftLocal.EnableForDemo` 现在**有生产调用点**（`DraftLocalEvent.ApplyToTimedLocal`），"
          "由本地模拟事件（`Resources/draft_local_event.json` 的 `timed: true`）在装载时调用 ⇒ "
          "`TimedDraftLocal.Enabled` 在 player 里为真 ⇒ `BuildTimedFamily`（`:4297`，gate `:4299`）**会建**。"
          "✅ **没有把 `Enabled` 的默认值改成 true**（仍 `false`）：点亮来自数据驱动的装载，关掉模拟（`SimulationEnabled=false`）即回到旧状态。"
          "🔶 残留（全是服务端量，已标 🔶）：`specificArmies` / 两阵营总分 / Premium 价 / 事件图 = 模拟值。")
c[9] = c[9] + "｜B98_K5" if "B98_K5" not in c[9] else c[9]
c[10] = (c[10]
         + " ｜[B98_K5 2026-10-29] **A（生产入口补齐；数值仍是 🔶 模拟）**：账补丁 `B98_K5.tsv`；断言 = `Editor/B98K5_Assert.cs` "
           "D 段（D1 模拟事件声明 timed；D2 ★ 装载后 `TimedDraftLocal.Enabled` 被置真；D3 ★ `Armies` 两格 = `Ultramarines`/`Goff`；"
           "D4 ★ 灭自证：`SimulationEnabled=false` ⇒ `Enabled` 仍 false）。**宿主未接线**。"
           "⚠️ 副作用（如实）：Timed 支开着时，选将页卡池会按 `specificArmies` 过滤（原版 Timed 事件本就如此）⇒ player 里只出这两个阵营的战将；"
           "关掉模拟即恢复。typecheck 0/0。")
out.append("\t".join(c))

# ============================================================ ③ 轮抽:147  DraftModeWarlordSelectionStateTimed.OnOpen
c = row(DR, 147, "DraftModeWarlordSelectionStateTimed", "OnOpen(")
assert c[5] == "B", c[5]
c[3] = c[3] + " · 报告/B98_K5.md §三"
c[5] = "A"
c[6] = (c[6]
        + " · 🆕 2026-10-29（B98_K5）：**标题幅面已接** —— "
          "`CardPresentation/Shell/DraftModeWindow.cs:4610`（`ApplyTimedWarlordTitle`，新成员，追加在文件末尾 partial 块 ⇒ 既有落点零漂）"
          "，调用点 = `:4321` 行内（`RebuildWarlordCards(); ApplyTimedWarlordTitle();`，零行数变化）；"
          "词条来源 = `CardPresentation/Shell/DraftLocalEvent.cs:382`（`TitleText` → `LiveOpsAssetUtility.GetEventLabel(carrier, Title)` "
          "= 原版那一跳）+ `:420`（`LabelCarrier` = 模拟事件的 `EventLabels` 载体）")
c[7] = (c[7]
        + " ｜[B98_K5 2026-10-29] **标题幅面已补**：原版 `title = LiveOpsAssetUtility.GetLabel(eventData, Title)` 那一跳现在走的是"
          "**模拟事件的 `EventLabels`**（`DraftLocalEvent.LabelCarrier`）⇒ `GetEventLabel(…, Title)` 真被调到、返回的词条真被写进页面标题。"
          "🔶 词条键由我们挑，但**指向既有词条** `Demo/DraftMode/SelectWarlord`（页面本来就印的那一句）⇒ **画面文字没有被编**。"
          "取不到 ⇒ **不动页面既有标题 + 出声**（⛔ 不编词条、⛔ 不拿别的键顶）。"
          "⚠️ `LiveOpsLocalKind` 没有 draft 这一档，载体用 `Parent` 占位并在代码里写明。")
c[9] = c[9] + "｜B98_K5" if "B98_K5" not in c[9] else c[9]
c[10] = (c[10]
         + " ｜[B98_K5 2026-10-29] **A（标题幅面补齐）**：账补丁 `B98_K5.tsv`；断言 = `Editor/B98K5_Assert.cs` "
           "E 段（E1 ★ 标题非空；E2 ★ `LastTitleTerm` = `Demo/DraftMode/SelectWarlord` 且显示文字 = `Loc.T(该键)`；"
           "E3 ★ 灭自证：关掉模拟 ⇒ `TitleText()` 为空）。**宿主未接线**。⚠️ 幅面（字号/对齐/矩形）沿用页面既有那颗 `Title` 标签"
           "（`:1285`，判据 = prefab 的 `WlTitleR`），本波只换文字。typecheck 0/0。")
out.append("\t".join(c))

data = ("\n".join(out) + "\n").encode("utf-8")
assert data.count(b"\r\n") == 0
io.open(OUT, "wb").write(data)
print("wrote", OUT, len(data), "bytes,", len(out) - 1, "rows")
for i, l in enumerate(out):
    assert len(l.split("\t")) == 11, (i, len(l.split("\t")))
print("11 columns OK")
