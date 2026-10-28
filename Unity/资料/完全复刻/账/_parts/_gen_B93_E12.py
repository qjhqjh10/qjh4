# -*- coding: utf-8 -*-
# B93_E12 账补丁生成器（只写 _parts/，⛔ 不动主表）
# 读 日常_奖励.tsv（裸 split("\t")，11 列），按行号取原行、改指定列，写出补丁后再读回环比对 11 列。
import io, os, sys

BASE = os.path.dirname(os.path.abspath(__file__))
LEDGER = os.path.join(BASE, "..", "日常_奖励.tsv")
OUT = os.path.join(BASE, "B93_E12.tsv")
HDR = ["域", "原版类", "方法", "判据出处", "判据要点", "状态", "我们的落点", "差异", "等级", "轮次", "备注"]

MARK = "2026-10-28 B93_E12"

# line -> (new_status or None, new_landing or None, append_to_diff or None, append_to_note or None)
UPD = {
162: ("A", "Shell/LiveOpsLocalCatalog.cs:1199（EventDataAssetProvider.GetAssets）",
      "补真身 `EventDataAssetProvider.GetAssets()` = `GetAllEvents().Select(e => e.GetBaseData())`（判据 `decomp_full/EventDataAssetProvider__GetAssets.c`；选择器 `EventDataAssetProvider.__c___GetAssets_b__0_0.c` 的体只有一句 = `ILiveOps` 槽 15 = `GetBaseData()`，`dump.cs:126037`）。原来的「返回 13 份 SO 的离线表」保留为**数据源**（`LiveOpsLocalCatalog.Events`），不再当等价物。",
      "已补真身 GetAssets()（账 162）"),
259: ("B", None,
      "复核：`.c` 四句逐句已照（`JoinState==NotJoined ∧ CurrentScore==0 ⇒ base.Expire` / `HasComponent(21)` 开窗 / `TryCollect`）。残差**只剩**「组件 21 那扇窗」——原版走 `LiveOpsAssetUtility.HasComponent(this, ExpirePopup)` + `GetComponentReference`，我们**没有按组件引用查表的 API**（`LiveOpsPopupPresenter` 按 label 走，没有 `HasComponent` 口）⇒ 该跳仍缺，⛔ 不是「判据读不出」。",
      "复核：只剩组件 21 那扇窗（无 HasComponent 等价物）"),
266: ("B", None,
      "复核：`GetSqlFilter` 四段已拼齐；残差 = `isRanked` 近似 + 本地无联盟/黑名单源（数据面 P2/P3）。",
      "复核：残差=isRanked 近似 + 无联盟/黑名单源"),
269: ("B", None,
      "复核：残差 = `GameModes.SetResultCount`/`SetLossStreak` 的服务端记账改本地 `LocalDefeatStreak`（🔶 服务器已关服）+ 云脚本 0x17f 无重试。",
      "复核：残差=服务端记账本地化 + 云脚本无重试"),
272: ("B", None,
      "复核：`LocalScore` 已补；残差 = `SaveGame.armyStats`（我们的 `PlayEventSaveGame` 无该字段）+ 云脚本 0x180（P3）。",
      "复核：残差=armyStats 字段 + 云脚本 0x180"),
275: ("B", None,
      "复核：C4=1 / C5=LossStreak / 键表 C1..C5 照抄；但 `MsgStart` 没有 eventId/连败/ELO 字段 ⇒ 写了也没有消费方（如实记）。",
      "复核：MsgStart 无消费方，如实记"),
276: ("B", None,
      "复核：两条闸照抄；云脚本 0x455 → 本地推进 `collectedRewards` 并交奖励表（无重试/无防重复提交）。",
      "复核：残差=云脚本 0x455 本地化"),
322: ("A", "Shell/LiveOpsEvent.cs:862",
      "账上「`Il2CppClass<TData>+0x188` 的泛型分派未识别」**已解** = `Activator.CreateInstance<TSave>()` + 取 TData 虚方法 `GetID()`（VA `0x181016CBC` / `0x181016CCF` / `0x181016CEC`，见方法注释）⇒ 体逐句照抄、无残差。",
      "0x188 泛型分派已解（Activator + GetID）"),
335: ("A", "Shell/LiveOpsEvent.cs:1138",
      "账上「缺 `data.mainMenuOptions` 那一格」**不成立**：`LiveopsMainMenuData` 是只有 3 个成员的 record（`Event` / `Priority` / `ContainerRef`，判据 `cpp2il/out_cpp2il_dev_ilspy/LiveopsMainMenuData.cs:58-66`），`mainMenuOptions` 与它无关（那只被 `OpenWindow()` 当 `GameWindowOptions` 用）⇒ 本方法与我们逐句一致。",
      "订正：mainMenuOptions 不属于 LiveopsMainMenuData"),
336: ("B", None,
      "体可读且我们逐句一致（`GetTranslation(key, FixForRTL:true, ignoreRTLnumbers:true, applyParameters:false, …)`）；**残差只剩 RTL 那三个开关**（本工程未做 RTL 支持）⇒ 保持 B。",
      "残差只剩 RTL 三个开关"),
337: ("B", None,
      "`switch` 三档 + 空串**已逐句照抄**（`.c` 的 `switch(uVar1) case 1/2/3` 与我们对位一致）；残差 = 三个键名仍是从官方本地化表**对位推断**（三个 `DAT_` 是元数据槽，静态读不出）。",
      "switch 已照抄；三个键名仍是推断"),
339: ("A", "Shell/LiveOpsEvent.cs:777",
      "`milis = X*3600000 + Y` 的 **X/Y 已坐实** = `Data.filter.eventDuration` × 3,600,000 + `SaveGame.triggerTime`（VA `0x181016ED7` imul / `0x181016EF8` add）⇒ 账上「X/Y 仍未解」过期。",
      "X/Y 已坐实（eventDuration + triggerTime）"),
346: ("A", "Shell/LiveOpsEvent.cs:686",
      "三档分派（OnPreview⇒20 / Finished⇒21 / 其余⇒10）逐句一致；落地缝 = `LiveOpsPopupPresenter.Open`（已接线，`LiveOpsHandler.cs:455` 同一个口）。⚠️「GUID→prefab 本地能解」已于 2026-10-27 更正，13 份 SO 全 Expired 是**正确结果**、不是缺口。",
      "三档分派已一致；落地缝=LiveOpsPopupPresenter"),
355: ("A", "Shell/LiveOpsEvent.cs:1326（+ 接口 `:1318`）",
      "基类 `LiveOpsEvent.SelectEvent()` **已补**（恒用 `MainWindow`(10)，与 `OpenWindow()` 按状态选 label 是**两回事**）。⚠️ **遗留一行改动（不在本代理白名单）**：`Shell/GameNewsEvent.cs:146` 的 `public virtual void SelectEvent()` 应改成 `override` + 调 `base.SelectEvent()`，否则经 `ILiveOps.SelectEvent` 调用会落到基类、绕过它的徽记重算（且现在会有一条 CS0114 隐藏告警）。",
      "基类 SelectEvent 已补；GameNewsEvent 需改 override（遗留一行）"),
394: ("B", None,
      "主链（`refreshSemaphore.WaitAsync` → `ConfigManager.GetConfig` → `Manager.BuildHandler`）逐跳一致；下游 `_BuildHandler_d__17` 已在账 407 逐句落地。残差 = 刷新尾部是否发 `EventsRefreshed` 未核（`_RefreshHandler_d__29__MoveNext.c` 里搜不到该符号）。",
      "主链一致；尾部 EventsRefreshed 未核"),
406: ("A", "Shell/LiveOpsHandler.cs:629",
      "**A 候选已核**：`b__25_0` 有独立产物（`decomp_generic_trial/out/LiveOpsHandler_object____ScheduleHandlerRefresh_b__25_0__1019bc0.c`）且与我们 `() => { var _ = RefreshHandler(); }` 一致。",
      "A 候选已核（独立产物一致）"),
407: ("A", "Shell/LiveOpsManager.cs:290",
      "live/blocked 分派**已定案并落地**：`!ev.IsActive()` ⇒ `RegisterEventTriggers()` + blockedEvents；`IsTriggeredEvent(ev) ‖ CheckEventFilter()` ⇒ events + `ToggleEventActivation(true,false,false)`；否则 blockedEvents。**槽号全部反查到 `dump.cs:126037`**（4=IsActive / 8=RegisterEventTriggers / 9=CheckEventFilter / 13=ToggleEventActivation），表归属由 `SetupEvents(events,blocked,shared)` 槽 5 的形参序坐实。⚠️「`.c` 与 ILSpy 冲突」**不是真冲突** —— ILSpy 那条 `BuildHandler` 只是状态机 kickoff（无体）。另补 `handler.RegisterManager(this)`：原版 `.c` 在 `SetupEvents` 前调槽 0，而**我们原来全仓 0 个调用** ⇒ `LiveOpsHandler.Manager` 恒 null ⇒ `RefreshHandler` 永远走「Manager 是 null ⇒ 无法建 handler」那条错误支。",
      "live/blocked 已定案（槽 4/8/9/13）+ 补 RegisterManager"),
410: ("A", "Shell/LiveOpsManager.cs:522",
      "2 参重载**已建**：`GetAllEvents(location, parent)` = `.Where(ev => ev.GetBaseData().location.Match(location, parent))`（判据 `decomp_full/LiveOpsManager__GetAllEvents.c` + `EventLocation.Match`）。`parentId` 从 `LiveOpsLocalEvent.LocationParentId` 补 —— 我们的 `LiveOpsEventData.location` 只存 `type` 那个 int，两步合起来 = 原版那个 `EventLocation` 对象。",
      "2 参重载已建"),
413: ("A", "Shell/LiveOpsManager.cs:213",
      "核：`.c` = `GetAllEvents().FirstOrDefault(x => x.GetID() == id)`；我们按 handler 逐个 `h.GetEvent(id)` 取第一个命中，**语义等价**（两边都是字典序 + 首命中）。",
      "语义等价（跨 handler 首命中）"),
415: ("A", "Shell/LiveOpsManager.cs:557",
      "**已建**。四步逐条反查到槽号：`ShowInMainMenu`(25) → `EventState ∈ {OnPreview(1),Active(2),Finished(3)}`(23) → `Select(GetMainMenuData)`(26) → `OrderByDescending(Priority)`。降序判据 = 与 `BuildHandler` 的 `OrderByDescending` 用的是**同一个辅助函数** `FUN_180c99380`（键选择器同样由 `FUN_181a60060` 建），且 `GameModes__GetActiveEvent.c` 用同一函数后 `FirstOrDefault()` 拿到的是最高优先那个。⚠️ 返回类型是 `List<>`（原版 `IOrderedEnumerable<>`），排序用带序号的稳定排序等价其稳定性。",
      "已建（四步槽 25/23/26 + 稳定降序）"),
420: ("A", "Shell/LiveOpsManager.cs:130",
      "那个虚调用**已坐实 = `ILiveOpsHandler` 槽 7 = `ClearEvents()`**（判据 `d:/2/tools/il2cpp_out/dump.cs:53773` 的槽表 + `LiveOpsManager__ResetHandlers.c` 的 `(offset+7)*0x10` 且只传 `this`+MethodInfo），⛔ **不是** `ResetEvents()` —— 后者根本不在接口上（是 `LiveOpsHandler` 的类方法）。代码已按槽 7 改（原来是 `as LiveOpsHandler` 再调 `ResetEvents()`）。",
      "虚槽 7 = ClearEvents（已改）"),
560: ("A", "Shell/PlayEvent.cs:462",
      "判据可读部分已落地：`if (!Handler.IsFirstInitialization) return;` + `EventState >= Finished` 先 return。⚠️ 本行原落点 `Shell/LiveOpsHandler.cs:965` 是**错钉**（那里是 `LoadFromCatalog`）。",
      "落点错钉已纠正（PlayEvent.cs:462）"),
698: ("A", "Shell/LiveOpsSharedData.cs:355",
      "`ToDictionary` 的键**已坐实 = `x.GetType()`**（判据 `decomp_full/SharedEventDataManager.__c___Initialize_b__6_0.c` 的体只有一句 `System_Object__GetType(param_2)`）⇒ 我们的键也从「自报 `SharedType`」改成 `GetType()`（`GetContext<T>` 因此要求 T = 具体 context 类型，与原版一致）。源仍是显式注册（无 prefab 子件，差异照记）。",
      "键已坐实 = GetType()"),
699: ("A", "Shell/LiveOpsSharedData.cs:392",
      "**已坐实两处**：① 遍历登记表的**全部** context（`.c` 的 `while` 一路 `MoveNext` 到枚举结束，中途没有按类型筛的闸）；② 交给 context 的是**整份 `SharedEventData`**（`*plVar1`），⛔ 不是 `energyData` 那一段 —— `SharedDataContext.UnpackData(SharedEventData)` 的形参类型（`dump.cs:54677`）直接坐实。抽象方法据此改名 `UnpackData` 并换形参。",
      "整份 SharedEventData + 全部 context（UnpackData）"),
703: ("A", "Shell/LiveOpsFilter.cs:575",
      "**A 候选已核**：`TryTrigger` 体 = `if (ValidateSignal(signal)) callback?.Invoke();`，与我们逐句一致（原来那条「ILSpy 残留控制流」的判断已升级为逐句可读）。",
      "A 候选已核（逐句一致）"),
705: ("A", "Shell/LiveOpsFilter.cs:584",
      "**A 候选已核**：`protected abstract bool ValidateSignal(T signal)` 的签名/可见性/抽象性与我们一致。",
      "A 候选已核（签名一致）"),
}


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8")
    if "\r\n" in text:
        lines = text.split("\r\n")
    else:
        lines = text.split("\n")
    return lines, ("\r\n" in text)


def main():
    lines, crlf = read_rows(LEDGER)
    hdr = lines[0].split("\t")
    assert hdr == HDR, ("主表表头不符", hdr)

    out = [HDR]
    for ln in sorted(UPD):
        row = lines[ln - 1].split("\t")
        assert len(row) == 11, ("原行不是 11 列", ln, len(row))
        assert row[2].strip() != "" and "(" in row[2], ("方法列不像完整签名", ln, row[2])
        new_status, new_landing, add_diff, add_note = UPD[ln]
        if new_status:
            row[5] = new_status
        if new_landing:
            row[6] = new_landing
        if add_diff:
            row[7] = (row[7].rstrip() + "｜" if row[7].strip() else "") + "**" + MARK + "**：" + add_diff
        if add_note:
            row[10] = (row[10].rstrip() + "｜" if row[10].strip() else "") + MARK + "：" + add_note
        assert len(row) == 11
        assert "\t" not in "".join(row)
        out.append(row)

    body = "\n".join("\t".join(r) for r in out)
    io.open(OUT, "wb").write(body.encode("utf-8"))

    # 读回环比对：11 列、内容逐字节相同
    back = io.open(OUT, "rb").read().decode("utf-8").split("\n")
    assert len(back) == len(out), (len(back), len(out))
    for i, r in enumerate(out):
        cells = back[i].split("\t")
        assert len(cells) == 11, ("回读列数不符", i, len(cells))
        assert cells == r, ("回读内容不符", i)
    print("OK rows=%d  out=%s  firstLineIsHeader=%s" % (len(out) - 1, OUT, back[0].split("\t") == HDR))


if __name__ == "__main__":
    main()
