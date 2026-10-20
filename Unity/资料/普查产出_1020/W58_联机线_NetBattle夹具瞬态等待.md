# W58-NETFIX2 · `NetBattleTest` 两条掉线夹具的瞬态等待修复

> 执行代理报告。⛔ 没改 `Net/**`、没改正本、没跑 Unity / `*.Run`、没动 git。
> 唯一改动文件：`Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`（15 增 / 4 删）。
> 判据：`资料/普查产出_1020/W58_诊断_NetBattle2条.md` §一 / §3·1。

---

## 结论

两条红（14② 结算掉线·本机 / 14③ 结算掉线·对照）**同一根因**：夹具在 `Pump()` 边界上等瞬态状态
`WaitingReconnect`，而客机（`Role == Client`）的退避支在**同一次 `Pump()` 内**就把它推进到
`Handshaking`（`_lastReconnMs` 初值 0 + 进程秒表 ⇒ `now - 0 >= 2000` 恒真）。

按诊断 §3·1 落地：夹具改用 **`OnPeerLost` 事件**取证，并额外交一条「**状态已离开 `InBattle`**」——
两条合起来**严格不弱于**旧断言（事件证明掉线被识别，状态证明已进入自动重连），且不再依赖
「状态恰好在 `Pump` 边界停留」这一偶然时序。⛔ **生产实现（`NetSession` 重连时序）一个字没动。**

---

## 两处改法

均在 `Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`（行号为改后现读）：

### 1 · 14② 结算掉线·本机 —— `:1451-1457`

```csharp
bool cLost = false;
f.cs.OnPeerLost += () => cLost = true;      // `+=` 追加，与 NetBattle.WireSession 的 HandlePeerLost 并存
f.hs.Transport.ClosePeer();
Ok(PumpOne(f.cs, () => cLost, 8000) && f.cs.State != NetState.InBattle,
   $"（结算掉线②）夹具：客机**确实看到**连接断了（`OnPeerLost` 已响；状态 {f.cs.State} = 已进入自动重连）");
f.cs.StopReconnecting();                    // 位置不变
```

### 2 · 14③ 结算掉线·对照 —— `:1484-1490`

```csharp
bool cLost = false;
f.cs.OnPeerLost += () => cLost = true;
f.hs.Transport.ClosePeer();
Ok(PumpOne(f.cs, () => cLost, 8000) && f.cs.State != NetState.InBattle,
   $"（结算掉线③）夹具：客机看到对面掉了（`OnPeerLost` 已响；状态 {f.cs.State} = 已进入自动重连）");
```

- 两处 `bool cLost` 各自位于**独立的 `{ }` 块**（14②/14③），是同级兄弟作用域，不冲突。
- `f.cs.OnPeerLost` 是 `NetSession.cs:52` 的 `public Action` 字段，`+=` 不顶掉 `NetBattle.WireSession`
  挂的 `HandlePeerLost`（`NetBattle.cs:320`）。
- `f.cs.StopReconnecting()` 位置不变（14② 紧跟断言；14③ 在紧随的「倒计时照常起」断言之后，原样未动）。
- ⛔ **其它断言一条没放宽**：同段 5 + 3 条 ★ 原样保留。

---

## grep 证据

```
$ grep -n "cLost" Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs
1451:                    bool cLost = false;
1452:                    f.cs.OnPeerLost += () => cLost = true;
1454:                    Ok(PumpOne(f.cs, () => cLost, 8000) && f.cs.State != NetState.InBattle,
1484:                    bool cLost = false;
1485:                    f.cs.OnPeerLost += () => cLost = true;
1487:                    Ok(PumpOne(f.cs, () => cLost, 8000) && f.cs.State != NetState.InBattle,
```

`git diff --numstat` ⇒ `15  4  Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`
（文件是 **LF**：`\r\n` 0 / `\n` 1816；diff 只有上述两处，**行尾没翻**；工作区只有这一个文件被本代理改动）。

---

## typecheck

```
$ TMPDIR=/tmp/wf_netfix2 bash d:/4/Unity/工具/typecheck.sh
运行时错误数: 0
编辑器错误数: 0
```

⇒ **0 / 0**。未跑任何 `*.Run`（按红线）。
