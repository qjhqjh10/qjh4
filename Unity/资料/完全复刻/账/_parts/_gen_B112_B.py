#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B112_B 的账补丁（`账/_parts/B112_B.tsv`）。

三件事（对应报告 §B-81 / §B-82 / §B-83）：
  · `商店.tsv`  row 261 `ShopOfferContainer.OnOfferTimerEnded()`        —— 落点重钉（旧落点 BuildTimeCounter 属商品格族，不是本方法）
  · `商店.tsv`  row 344 `PriceDisplayButton.set_Interactable(bool)`     —— 落点补上本批新建的那颗组件
  · `商店.tsv`  新增 `PriceDisplayButton.get_Interactable()` / `get_OnClick()` —— 本批新建的类把这两个成员也落了
  · `未分类_其他.tsv` row 407/408 `LiveOpsUtility.GetDateForEventStage` / `GetDateForNextEventStage` —— **D → A**
    （旧判 D 的理由「不做 LiveOps 活动」已被 B110_W4 / B111_S3 / 本批连续推翻：本地纯逻辑、已逐句对齐）

🔴 **落点行号一律现读**（`line_of`），⛔ 不写死 —— 本件的三个新文件在写报告期间就各长过一两行，
   写死的行号会立刻过期（铁律 18·b 的同族）。
⛔ 不写主表；⛔ 不猜：每一行都按 (原版类, 方法) 精确定位，找不到就报错退出。
"""
import io

BASE = r"d:/4/Unity/资料/完全复刻/账"
SRC = r"d:/4/Unity/MyGame/Assets/CardPresentation"
OUT = BASE + r"/_parts/B112_B.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    assert b.count(b"\r\n") == 0, "主表应是纯 LF（现读不成立，先查行尾）"
    return [l for l in text.split("\n") if l != ""]


def find(rows, cls, method):
    hits = []
    for i, l in enumerate(rows):
        c = l.split("\t")
        if len(c) < 11:
            continue
        if c[1] == cls and c[2] == method:
            hits.append(i)
    if len(hits) != 1:
        raise SystemExit("定位失败：%s / %s 命中 %d 处" % (cls, method, len(hits)))
    return hits[0]


def line_of(rel, prefix):
    """在 `SRC/<rel>` 里找**行首（strip 后）以 `prefix` 开头**的那一行；必须正好 1 处。"""
    p = SRC + "/" + rel
    b = io.open(p, "rb").read()
    assert b.count(b"\r\n") == 0, "%s 不是纯 LF（先查行尾）" % rel
    hits = [i for i, l in enumerate(b.decode("utf-8").split("\n"), 1) if l.strip().startswith(prefix)]
    if len(hits) != 1:
        raise SystemExit("行号定位失败：%s 里 `%s` 命中 %d 处" % (rel, prefix, len(hits)))
    return hits[0]


# ---- 现读所有落点行号（⛔ 不写死）----
L_OFFER_TIMER_ENDED = line_of("Shell/OfferContainerPartial_B111S3.cs", "public static void OnOfferTimerEnded(")
L_ATTACH_PRICE = line_of("Shell/OfferContainerPartial_B112B.cs", "public static PriceDisplayButton AttachPriceButton(")
L_PDB_INTERACTABLE = line_of("Shell/PriceDisplayButtonPartial_B112B.cs", "public bool Interactable")
L_PDB_ONCLICK = line_of("Shell/PriceDisplayButtonPartial_B112B.cs", "public System.Action OnClick")
L_UTIL_STAGE = line_of("Shell/LiveOpsUtilityPartial_B112B.cs", "public static long GetDateForEventStage(ILiveOps")
L_UTIL_NEXT = line_of("Shell/LiveOpsUtilityPartial_B112B.cs", "public static long GetDateForNextEventStage(ILiveOps")
L_RAID_STAGE = line_of("Shell/RaidEvent.cs", "public static long GetDateForEventStage(LiveOpsEventData")
L_WB_INTERACTABLE = line_of("Shell/WindowButton.cs", "public bool Interactable")
L_WB_SOFTDISABLED = line_of("Shell/WindowButton.cs", "public void SetSoftDisabled(bool on)")
L_SHOPTAB_CELL = line_of("Shell/ShopTabPage.cs", "void BuildCell(Transform cell")

shop = read_rows(BASE + "/商店.tsv")
misc = read_rows(BASE + "/未分类_其他.tsv")

out = ["\t".join(COLS)]

# ---------------------------------------------------------------- 1) OnOfferTimerEnded
i = find(shop, "ShopOfferContainer", "OnOfferTimerEnded()")
r = shop[i].split("\t")
r[3] = ("decomp_full/ShopOfferContainer__OnOfferTimerEnded.c（全文两句）· "
        "cpp2il/out_il2csharp/Assembly-CSharp/ShopOfferContainer.cs:87-95")
r[4] = ("原版全文两句：`int n = liveOps.GetOwnedQuantity();`（`ILiveOps` 槽 0xc）→ `if (n != 0) return;` → "
        "`priceDisplayButton.Interactable = false;`（`PriceDisplayButton__set_Interactable(this+0x40, 0)`）")
r[6] = ("Shell/OfferContainerPartial_B111S3.cs:%d（OnOfferTimerEnded）· "
        "Shell/OfferContainerPartial_B112B.cs:%d（AttachPriceButton）· "
        "Shell/PriceDisplayButtonPartial_B112B.cs:%d（Interactable）"
        % (L_OFFER_TIMER_ENDED, L_ATTACH_PRICE, L_PDB_INTERACTABLE))
r[7] = ("真身由 `B111_S3` 落地（`B-67`）；本批（`B112_B` · `B-81`）补上它**最后一句的落点**：`Build` 现在按容器建一颗 "
        "`PriceDisplayButton`（命中区 + `Bind` 到可见的 `Button Gfx`），并把**实例**闭包进到期回调 ⇒ 置灰真的落在可见价签上。"
        "⚠️ 旧落点 `ShopTabPage.cs:306（BuildTimeCounter）` 属**商品格**那族的倒计时，不是本方法 ⇒ 已换。"
        "⚠️ 本机的持有量数据源仍在远端（服务端报价）⇒ 生产上 `OnOfferTimerEnded` 走「没有持有量数据源 ⇒ 出声」那一支。")
r[8] = "P1"
r[9] = "B111_S3 · B112_B"
r[10] = ("断言 = `Editor/B112_B_Assert.cs` B 段（B1 到期置灰 / B2 持有量 3 不置灰 / B3 可还原 / B4 灰落在可见 `Button Gfx` 上）"
         "＋ `Editor/B111_S3_Assert.cs` C 段（原注入点那一对）。判据与逐条 → `报告/B112_B.md`")
out.append("\t".join(r))

# ---------------------------------------------------------------- 2) set_Interactable
i = find(shop, "PriceDisplayButton", "set_Interactable(bool value)")
r = shop[i].split("\t")
r[6] = ("CardPresentation/Shell/PriceDisplayButtonPartial_B112B.cs:%d（Interactable）｜ "
        "CardPresentation/Shell/WindowButton.cs:%d（SetSoftDisabled）｜ "
        "Shell/ShopTabPage.cs:%d（BuildCell）" % (L_PDB_INTERACTABLE, L_WB_SOFTDISABLED, L_SHOPTAB_CELL))
r[9] = r[9] + " · B112_B"
r[10] = r[10] + (
    "｜[B112_B 2026-11-01] 本批把**组件本体**补进了本仓（`Shell/PriceDisplayButtonPartial_B112B.cs`，纯 C# 类、"
    "三个字段 + `Interactable`/`OnClick` 两个成员，语义逐字照抄）⇒ 本行的落点不再只有「异名等价物」。"
    "`OfferContainer.Build` 的价签现在真的会把 `Interactable = false` 落到**可见底图**上"
    "（灰走 `WindowButton` 的 `Everguild/UI/Greyscale` 材质那条路，同本行既有判据）。")
out.append("\t".join(r))

# ---------------------------------------------------------------- 3) get_Interactable（新增）
out.append("\t".join([
    "商店", "PriceDisplayButton", "get_Interactable()",
    "d:/2/tools/decomp_full/PriceDisplayButton__get_Interactable.c（全文）· "
    "cpp2il/out_il2csharp/Assembly-CSharp/PriceDisplayButton.cs:29-33",
    "体只有一句：`return this.button.m_Interactable;`（+0x28 那颗 `EverguildButton` 的 `Selectable.m_Interactable`）",
    "A",
    "Shell/PriceDisplayButtonPartial_B112B.cs:%d（Interactable）· CardPresentation/Shell/WindowButton.cs:%d（Interactable）"
    % (L_PDB_INTERACTABLE, L_WB_INTERACTABLE),
    "`button` 为空时原版 NRE ⇒ 本仓返回 `false` + setter 出声（同 `LiveOpsManager.IsTriggeredEvent` 口径）。",
    "P1", "B112_B",
    "本行是 B112_B 新建的类带来的**新增账行**（原表只有 `set_Interactable`）。"
    "断言 = `Editor/B112_B_Assert.cs` A1b（初值 true）/ B1（到期 false）/ B3（可还原）",
]))

# ---------------------------------------------------------------- 4) get_OnClick（新增）
out.append("\t".join([
    "商店", "PriceDisplayButton", "get_OnClick()",
    "d:/2/tools/decomp_full/PriceDisplayButton__get_OnClick.c（全文）· "
    "cpp2il/out_il2csharp/Assembly-CSharp/PriceDisplayButton.cs:18-23",
    "体只有一句：`return this.button.m_OnClick;`（`EverguildButton` 的 `ButtonClickedEvent`）",
    "A",
    "Shell/PriceDisplayButtonPartial_B112B.cs:%d（OnClick）" % L_PDB_ONCLICK,
    ("原版返回 `Button.ButtonClickedEvent`；本仓 `WindowButton.onClick` 是 `System.Action` ⇒ 只给 getter 转发"
     "（结构差异，非行为差异，见该文件头差异 ②）。⚠️ `ShopOfferContainer` 这条路里**没有人监听它**"
     "（70/70 实例 `m_PersistentCalls` 空表 + 全量反编译里该宿主 0 个 `add_OnClick`）⇒ 原版点了什么都不做。"),
    "P1", "B112_B",
    "断言 = `Editor/B112_B_Assert.cs` A 段（命中区存在且压过整卡那颗）；点击行为「什么都不做」由 `PriceButtonClicked` 出声",
]))

# ---------------------------------------------------------------- 5) GetDateForEventStage（D → A）
i = find(misc, "LiveOpsUtility", "GetDateForEventStage(ILiveOps liveOps, int stageIndex)")
r = misc[i].split("\t")
r[3] = ("decomp_full/LiveOpsUtility__GetDateForEventStage.c（全文）· "
        "cpp2il/out_il2csharp/Assembly-CSharp/LiveOpsUtility.cs:52-66")
r[4] = ("三句：`long v = ElementAtOrDefault(data.dates.eventStages, stageIndex)`（越界/负数 ⇒ 0）→ "
        "`if (v == 0) v = data.dates.endDate;`（`sub_1800021f0(0xf, ILiveOps, liveOps)` 再取一次 `GetBaseData()`，"
        "`+0x28` = dates、`+0x18` = endDate）→ `return v;`")
r[5] = "A"
r[6] = ("Shell/LiveOpsUtilityPartial_B112B.cs:%d（GetDateForEventStage）· "
        "Shell/RaidEvent.cs:%d（GetDateForEventStage —— 唯一实现，判据逐句写在那里的 doc 上）"
        % (L_UTIL_STAGE, L_RAID_STAGE))
r[7] = ("门面**转发**到 `RaidHeaderLogic.GetDateForEventStage(LiveOpsEventData, int)`（不复制判断，铁律 6）；"
        "原版第一实参是 `ILiveOps` ⇒ 门面这一层只做 `liveOps.GetBaseData()`。"
        "原版 `liveOps == null` 会 NRE，本仓出声 + 0。")
r[8] = "P2"
r[9] = "B112_B"
r[10] = ("⚠️ 旧判 **D（不复刻）** 已过期 —— 理由「本工程不做 LiveOps 活动」不成立：这是**纯本地算式**、"
         "输入只有事件数据（`B110_W4`/`B111_S3` 已用同一条理由把同类的 `IsTimedEvent`/`IsTriggeredEvent` 由 D 改 A）。"
         "断言 = `Editor/B112_B_Assert.cs` C1/C3/C4/C4b")
out.append("\t".join(r))

# ---------------------------------------------------------------- 6) GetDateForNextEventStage（D → A）
i = find(misc, "LiveOpsUtility", "GetDateForNextEventStage(ILiveOps liveOps)")
r = misc[i].split("\t")
r[3] = ("decomp_full/LiveOpsUtility__GetDateForNextEventStage.c（全文 + 两个闭包体）· "
        "cpp2il/out_il2csharp/Assembly-CSharp/LiveOpsUtility.cs:68-92,107-120")
r[4] = ("`currentTime = PlayerDataManager.singletonManager.currentServerTime;` → "
        "`data.dates.eventStages.Where(s => s > currentTime).OrderBy(s => s).FirstOrDefault()` —— "
        "① **严格大于**（`>` 不是 `>=`）② 取**最小**那一档 ③ 一条都没有 ⇒ **0**（**不回落 endDate**）")
r[5] = "A"
r[6] = "Shell/LiveOpsUtilityPartial_B112B.cs:%d（GetDateForNextEventStage）" % L_UTIL_NEXT
r[7] = ("原版**此前本仓 0 命中**（B-82），本批按 `.c` 逐句实现；「现在」走 `LiveOpsClock.NowUtc`（原版读服务器时间）。"
        "`liveOps`/`data`/`dates` 为空三格原版 NRE ⇒ 本仓出声 + 0。")
r[8] = "P2"
r[9] = "B112_B"
r[10] = ("⚠️ 旧判 **D（不复刻）** 已过期（同上一行）。断言 = `Editor/B112_B_Assert.cs` C2/C5/C6/C7 —— "
         "C6 咬死「等于现在的那一档不算」，C7 咬死「没有下一档时返回 0、⛔ 不回落 endDate」"
         "（🧨 给它补 endDate 兜底 ⇒ C7 红）")
out.append("\t".join(r))

io.open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))
print("wrote", OUT, len(out) - 1, "rows",
      "| lines:", L_OFFER_TIMER_ENDED, L_ATTACH_PRICE, L_PDB_INTERACTABLE, L_PDB_ONCLICK,
      L_UTIL_STAGE, L_UTIL_NEXT, L_RAID_STAGE, L_WB_INTERACTABLE, L_WB_SOFTDISABLED, L_SHOPTAB_CELL)
