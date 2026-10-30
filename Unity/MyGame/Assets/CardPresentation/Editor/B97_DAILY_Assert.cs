// B97_DAILY_Assert.cs — 第九十七会话 · 波 1 · 执行代理 `B97_DAILY`（日常/奖励线）的自检断言集。
//
// 本批 37 条 B 里**真的动了代码 / 真的把判据补齐**的只有两条，其余 33 条是复核 + 反证（见
// `资料/完全复刻/报告/B97_DAILY.md` 与 `账/_parts/B97_DAILY.tsv`）：
//
//   · `日常_奖励 :745`（`ToastNotificationMission.Initialize(object)`）—— **B→A**。
//     残差一直是「`SupportMethods.ModifyLocalization` 那一跳没接」：方法本体早在（`Core/SupportMethods.cs:347`，
//     B96_WB 加在类尾），但 `ToastNotifications.InitializeMissionToast` 里还是 `c.Text = d.ProgressText` 直传
//     （`Shell/ToastNotifications.cs:861`）。本轮把它接上 ⇒ 原版那一句
//     `text.text = SupportMethods.ModifyLocalization(d.progress.GetLocalizedDescription())` 逐句到位。
//
//   · `日常_奖励 :510`（`MissionTimerDisplay.Update()`）—— **B→A**（判据补齐，零代码改动）。
//     唯一残差是「周常那一格 `displayRule` 未判」。2026-10-29 现读（AssetRipper 导出的 Unity 工程，带 pathID）：
//       `Assets/Missions Tab.prefab` 的 21 个 `MissionTimerDisplay` 实例里，周常那颗的层级是
//       `Missions Tab / Weekly Mission Holder / Weekly Mission / background / footer / TimerHolder / Timer`，
//       **`displayRule = -1`**（= `DisplayRule` 每位都置 1 ⇒ **恒显示**）；同 prefab 的每日行 3 颗 = `2`、
//       登录卡 `2`、骷髅卡 `-1` —— 与实现（周常 Timer 无条件建、每日按 `IsComplete` 分档）逐位一致。
//       `DisplayRule` 的枚举值现读 `MissionInfoDisplay.cs`（`WhenActive = 1` · `WhenComplete = 2`）。
//
// 跑法（与 `B94B4Assert` 同一套形状，宿主由调度台接线）：
//     Debug.Log(P + "B97_DAILYAssert 跑 " + B97_DAILYAssert.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");
//
// 🔴 期望值一律来自**原版判据**（`.c` 的分支 / prefab 的实例值 / 原版方法体里的那一跳），⛔ 不取我们自己的常量当期望。
// 🔴 每条**先断前提、再断结论**（⛔ 不用 `if (前提) Check(结论)` —— 前提不成立时会**静默跳过**）。
// 🔴 每条都配**灭自证**：把接线改回直传 / 把 `ShouldShow` 改回旧写法时，**必须至少有一条变红**。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CardPresentation
{
    /// <summary>`B97_DAILY` 的断言集。返回**实际执行的断言条数**（调用方打印它 = 自证真被跑到）。</summary>
    public static class B97_DAILYAssert
    {
        public static int Run(Action<bool, string> check)
        {
            if (check == null) return 0;
            int n = 0;
            Action<bool, string> C = (ok, msg) => { n++; check(ok, msg); };
            ModifyLocalizationIsSingleRule(C);
            MissionToastUsesModifyLocalization(C);
            DisplayRuleTable(C);
            return n;
        }

        // ============================================================ ① 账 745 的底：`ModifyLocalization` 只有一份
        static void ModifyLocalizationIsSingleRule(Action<bool, string> C)
        {
            var sm = FindType("CardPresentation.SupportMethods");
            C(sm != null, "前提：`CardPresentation.SupportMethods` 类型在（原版 `SupportMethods` 的本仓等价物）");
            if (sm == null) return;
            var ml = sm.GetMethod("ModifyLocalization", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                  null, new[] { typeof(string) }, null);
            C(ml != null, "前提：`SupportMethods.ModifyLocalization(string)` 在（原版同名方法；B96_WB 加在类尾 `:347`）");
            if (ml == null) return;
            C(ml.ReturnType == typeof(string), "`ModifyLocalization` 返回 `string`（原版 `:497` 行的纯函数，入参出参都是串）");

            var ci = FindType("CardPresentation.CardIcons");
            C(ci != null, "前提：`CardPresentation.CardIcons` 类型在（四支记号的唯一实现）");
            if (ci == null) return;
            var rm = ci.GetMethod("RewriteMarks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                  null, new[] { typeof(string), typeof(bool) }, null);
            C(rm != null, "前提：`CardIcons.RewriteMarks(string, bool)` 在（`((…))` / `$$…$$` / `[[…]]` / `(N)` 四支）");
            if (rm == null) return;

            int calls = CountCalls(ml, rm);
            // 🧨 灭自证：在 `ModifyLocalization` 里**另抄一份四支**（不再转调）⇒ 这里立刻红。
            C(calls >= 1, "`SupportMethods.ModifyLocalization` 的方法体**转调** `CardIcons.RewriteMarks`（实测 call 点 = " + calls
                        + "；铁律「两处写同一条规则 = 迟早不一致」⇒ ⛔ 不许在本方法里抄第二份四支）");

            // 空 / null 入参：原版 `IsNullOrEmpty ⇒ return baseString ?? ""`（`SupportMethods__ModifyLocalization.c:489-493`）。
            string rNull = null, rEmpty = null;
            try { rNull = (string)ml.Invoke(null, new object[] { null }); } catch (Exception e) { C(false, "`ModifyLocalization(null)` 抛了：" + e.GetType().Name); return; }
            try { rEmpty = (string)ml.Invoke(null, new object[] { "" }); } catch (Exception e) { C(false, "`ModifyLocalization(\"\")` 抛了：" + e.GetType().Name); return; }
            C(rNull == "", "`ModifyLocalization(null)` ⇒ `\"\"`（原版 `.c:489-493` 的 `baseString ?? \"\"`）");
            C(rEmpty == "", "`ModifyLocalization(\"\")` ⇒ `\"\"`（同上）");

            // 同一条规则的两个入口必须同值（含中文/英文两档语档各跑一遍）。
            var loc = FindType("CardPresentation.Loc");
            C(loc != null, "前提：`CardPresentation.Loc` 在（语档来源）");
            if (loc == null) return;
            var cur = loc.GetProperty("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            C(cur != null, "前提：`Loc.Current` 属性在（`ModifyLocalization` 用它选语档）");
            if (cur == null) return;
            object curVal;
            try { curVal = cur.GetValue(null, null); } catch (Exception e) { C(false, "读 `Loc.Current` 抛了：" + e.GetType().Name); return; }
            C(curVal != null, "`Loc.Current` 读得到值（当前语档）");
            if (curVal == null) return;

            object zh = null;
            try { zh = Enum.Parse(cur.PropertyType, "Chinese"); } catch (Exception) { }
            C(zh != null, "前提：`AvailableLanguages.Chinese` 在（取值 **110**，原版枚举口径 ⇒ ⛔ 别按 0..11 猜）");
            if (zh == null) return;

            var samples = new[] { "Deal 3 damage.", "((Blind_a_random_enemy))", "[[Blind]]", "$$27a4b748$$" };
            bool allSame = true, bothPassRan = true;
            for (int i = 0; i < samples.Length; i++)
            {
                string a = null, b = null, c = null;
                try
                {
                    a = (string)ml.Invoke(null, new object[] { samples[i] });        // 生产口（按当前语档）
                    b = (string)rm.Invoke(null, new object[] { samples[i], Equals(curVal, zh) }); // 按当前语档独立算一遍（第 2 实参是 **bool zh**，⛔ 不是 `AvailableLanguages` 枚举）
                    c = (string)rm.Invoke(null, new object[] { samples[i], true });                               // 中文档
                }
                catch (Exception) { bothPassRan = false; allSame = false; break; }   // 抛异常 ⇒ 两条都要红（⛔ 别让 `allSame` 停在上一次的值上、空转绿灯）
                if (a != b) allSame = false;
                if (Equals(curVal, zh) && a != c) allSame = false;                    // 当前语档 = 中文时还要与中文档一致
            }
            C(bothPassRan, "四组样本在两个入口上都能跑（不抛异常）");
            C(allSame, "`ModifyLocalization(x)` 与 `CardIcons.RewriteMarks(x, 当前语档)` **逐样本同值**（单点规则，⛔ 别分叉）");
        }

        // ============================================================ ② 账 745 本轮改的那一跳：飘窗文案必须过 `ModifyLocalization`
        static void MissionToastUsesModifyLocalization(Action<bool, string> C)
        {
            var tn = FindType("CardPresentation.ToastNotifications");
            C(tn != null, "前提：`CardPresentation.ToastNotifications` 类型在（原版 `ToastNotificationMission` 的宿主）");
            if (tn == null) return;
            var imt = tn.GetMethod("InitializeMissionToast", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            C(imt != null, "前提：`ToastNotifications.InitializeMissionToast(object)` 在（原版 `ToastNotificationMission.Initialize` 的等价入口）");
            if (imt == null) return;

            var md = tn.GetNestedType("MissionsData", BindingFlags.Public | BindingFlags.NonPublic);
            C(md != null, "前提：`ToastNotifications.MissionsData`（原版 `ToastNotificationMissionsData`）在");
            if (md == null) return;
            C(md.GetField("ProgressText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null,
              "`MissionsData.ProgressText` 存在（= `progress.GetLocalizedDescription()` 那一格，⛔ 别改名）");

            var sm = FindType("CardPresentation.SupportMethods");
            var ml = sm != null ? sm.GetMethod("ModifyLocalization", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                               null, new[] { typeof(string) }, null) : null;
            C(ml != null, "前提：`SupportMethods.ModifyLocalization(string)` 在（② 的被调方）");
            if (ml == null) return;

            int calls = CountCalls(imt, ml);
            // 🧨 灭自证：把 `:861` 改回 `c.Text = d.ProgressText;`（直传）⇒ 这里立刻红。
            C(calls == 1, "`InitializeMissionToast` 的方法体**恰好一处**转调 `SupportMethods.ModifyLocalization`（实测 = " + calls
                       + "；判据 = `ToastNotificationMission__Initialize.c` 那句 `text.text = SupportMethods.ModifyLocalization(…)`）");
        }

        // ============================================================ ③ 账 510：`displayRule` 三档语义（周常实例值 = -1）
        static void DisplayRuleTable(Action<bool, string> C)
        {
            var t = FindType("CardPresentation.MissionTimerDisplay");
            C(t != null, "前提：`CardPresentation.MissionTimerDisplay` 类型在（`MissionInfoDisplay` 的派生类，原版同名）");
            if (t == null) return;
            // ⚠️ `ShouldShow` 是**基类**（`MissionInfoDisplay`）上的 internal 实例方法 ——
            //    `Type.GetMethod` 对「继承来的非 public 方法」**不返回** ⇒ 必须沿基类链用 `DeclaredOnly` 自己找。
            MethodInfo ss = null;
            for (var b = t; b != null && ss == null; b = b.BaseType)
                ss = b.GetMethod("ShouldShow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                                 null, new[] { typeof(bool) }, null);
            C(ss != null, "前提：`MissionInfoDisplay.ShouldShow(bool)` 在（原版 `MissionInfoDisplay__Initialize.c` 的 show 判定；在基类上）");
            if (ss == null) return;

            FieldInfo fld = null; Type fldType = null;
            for (var b = t; b != null && fld == null; b = b.BaseType)
            {
                fld = b.GetField("displayRule", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (fld != null) fldType = fld.FieldType;
            }
            C(fld != null, "前提：`displayRule` 字段在（原版 `[SerializeField] protected DisplayRule displayRule`）");
            if (fld == null) return;
            C(fldType != null && fldType.IsEnum, "前提：`displayRule` 是枚举型（原版 `[Flags] enum DisplayRule`）");
            if (fldType == null || !fldType.IsEnum) return;

            // 枚举值现读（原版 `MissionInfoDisplay.cs:5-9`：`WhenActive = 1` · `WhenComplete = 2`）。
            object whenActive = null, whenComplete = null;
            try
            {
                whenActive = Enum.Parse(fldType, "WhenActive");
                whenComplete = Enum.Parse(fldType, "WhenComplete");
            }
            catch (Exception) { }
            C(whenActive != null && Convert.ToInt32(whenActive) == 1, "`DisplayRule.WhenActive = 1`（原版枚举值）");
            C(whenComplete != null && Convert.ToInt32(whenComplete) == 2, "`DisplayRule.WhenComplete = 2`（原版枚举值）");

            object inst = null;
            try { inst = Activator.CreateInstance(t, true); } catch (Exception e) { C(false, "`MissionTimerDisplay` 建实例抛了：" + e.GetType().Name); return; }
            C(inst != null, "前提：`MissionTimerDisplay` 能建实例（纯类、不是 MonoBehaviour ⇒ 反射可实例化）");
            if (inst == null) return;

            Func<int, bool, object> call = (dr, isComplete) =>
            {
                fld.SetValue(inst, Enum.ToObject(fldType, dr));
                return ss.Invoke(inst, new object[] { isComplete });
            };
            object rAllActive, rAllComplete, rDailyActive, rDailyComplete, rZero;
            try
            {
                rAllActive = call(-1, false); rAllComplete = call(-1, true);      // 周常那颗 Timer（prefab 实例值 -1）
                rDailyActive = call(2, false); rDailyComplete = call(2, true);    // 每日行 / 登录卡那颗 Timer（实例值 2）
                rZero = call(0, false);
            }
            catch (Exception ex) { C(false, "`ShouldShow` 反射调用抛了：" + ex.GetType().Name); return; }

            C(Equals(rAllActive, true) && Equals(rAllComplete, true),
              "`displayRule = -1` ⇒ **两态都显示**（= 周常 `Weekly Mission / … / TimerHolder / Timer` 的实例值；判据 = `Assets/Missions Tab.prefab` 现读层级链）");
            C(Equals(rDailyActive, false) && Equals(rDailyComplete, true),
              "`displayRule = 2 (WhenComplete)` ⇒ **只在自己已领取时显示**（= 每日行 3 颗 / 登录卡的 `Timer`；原版 `.c` 第一支）");
            C(Equals(rZero, false),
              "`displayRule = 0` ⇒ **恒藏**（原版 `.c` 第二支 `(dr & 1) == 0 ⇒ show = false`；⛔ 别写成 else 兜底）");

            // 文案那半：两条模板必须**不同**（`Missions/CollectCounter` vs `Missions/CompleteCounter`）。
            var desc = t.GetMethod("Description", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                   null, new[] { typeof(bool), typeof(bool) }, null);
            C(desc != null, "前提：`MissionTimerDisplay.Description(bool onlyShowNumber, bool isComplete)` 在（纯函数，四处倒计时共用）");
            if (desc == null) return;
            string tCollect = null, tComplete = null, tNumber = null;
            try
            {
                tCollect = (string)desc.Invoke(null, new object[] { false, false });
                tComplete = (string)desc.Invoke(null, new object[] { false, true });
                tNumber = (string)desc.Invoke(null, new object[] { true, false });
            }
            catch (Exception ex) { C(false, "`Description` 反射调用抛了：" + ex.GetType().Name); return; }
            C(tCollect != null && tComplete != null && tCollect != tComplete,
              "`Description(false,false) != Description(false,true)`（未领取 = `Missions/CollectCounter`、已领取 = `Missions/CompleteCounter`；"
            + "🔴 周常 `Timer` 的 `displayRule = -1` ⇒ 两态都显示 ⇒ **这一条对周常是活的**，⛔ 别再写死 `CollectCounter`）");
            C(tNumber == "{0}", "`Description(onlyShowNumber:true, _)` ⇒ `\"{0}\"`（原版那一支只剩时间串）");
        }

        // ============================================================ 反射小工具

        static Type FindType(string fullName)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                var ty = asms[i].GetType(fullName, false);
                if (ty != null) return ty;
            }
            return null;
        }

        static Dictionary<short, OpCode> _ops;
        static Dictionary<short, OpCode> Ops
        {
            get
            {
                if (_ops == null)
                {
                    _ops = new Dictionary<short, OpCode>();
                    var fs = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static); // 🔴 2026-10-30 订正：原写 `typeof(OpCode)`（那是对的类型、公开静态字段 0 个 ⇒ 表恒空 ⇒ `-1`）；opcode 常量全在 `OpCodes`（226 个）
                    for (int i = 0; i < fs.Length; i++)
                    {
                        var o = (OpCode)fs[i].GetValue(null);
                        if (o.Size == 0) continue;              // 空槽位
                        _ops[o.Value] = o;
                    }
                }
                return _ops;
            }
        }

        static int OperandSize(OpCode o)
        {
            switch (o.OperandType)
            {
                case OperandType.InlineNone: return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: return 1;
                case OperandType.InlineVar: return 2;
                case OperandType.ShortInlineR:
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType: return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR: return 8;
                default: return -1;                              // InlineSwitch 在调用处单独走
            }
        }

        /// <summary>数「`method` 的方法体里 call/callvirt 到 `target` 的次数」。
        /// 返回 -1 = 方法体读不到（本机判不了 ⇒ 断言会以「前提」形式红，⛔ 不静默当 0）。</summary>
        static int CountCalls(MethodBase method, MethodInfo target)
        {
            if (method == null || target == null) return -1;
            MethodBody body;
            try { body = method.GetMethodBody(); } catch (Exception) { return -1; }
            if (body == null) return -1;                         // abstract/extern/无体
            byte[] il;
            try { il = body.GetILAsByteArray(); } catch (Exception) { return -1; }
            if (il == null) return -1;

            var mod = method.Module;
            int i = 0, count = 0;
            while (i < il.Length)
            {
                short v = il[i];
                int opSize = 1;
                if (v == 0xFE)
                {
                    if (i + 1 >= il.Length) return -1;
                    v = unchecked((short)(0xFE00 | il[i + 1]));
                    opSize = 2;
                }
                OpCode op;
                if (!Ops.TryGetValue(v, out op)) return -1;
                i += opSize;
                if (op.OperandType == OperandType.InlineSwitch)
                {
                    if (i + 4 > il.Length) return -1;
                    int n = BitConverter.ToInt32(il, i);
                    i += 4 + 4 * n;
                    continue;
                }
                int osz = OperandSize(op);
                if (osz < 0) return -1;
                if (i + osz > il.Length) return -1;
                if (v == OpCodes.Call.Value || v == OpCodes.Callvirt.Value)
                {
                    int tok = BitConverter.ToInt32(il, i);
                    try
                    {
                        var mb = mod.ResolveMethod(tok);
                        if (mb != null && mb.MetadataToken == target.MetadataToken && mb.Module == target.Module) count++;
                    }
                    catch (Exception) { /* 解不出的 token 只跳过这一条 —— 不影响计数 */ }
                }
                i += osz;
            }
            return count;
        }
    }
}
