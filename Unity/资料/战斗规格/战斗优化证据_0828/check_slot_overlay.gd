extends SceneTree
## F1 槽位叠图验证工具 (2026-08-28): 项目 18 槽框 vs 背景 RT 板面贴合度
## 输出: tools_dev/out_slot/{full_debug.png, full_board.png, zoom_*.png} + 槽中心像素色表
## 运行: Godot_v4.7.2-stable_win64.exe --path D:/warpforge --resolution 1920x1080 \
##       --script res://tools_dev/check_slot_overlay.gd --log-file C:/Users/qjh36/AppData/Local/Temp/slot_diag.log

var _battle: Control = null

func _initialize() -> void:
	await process_frame
	_run()

func _wait(n: int) -> void:
	for i in n:
		await process_frame
	await create_timer(0.02).timeout

func _click(p: Vector2) -> void:
	var e := InputEventMouseButton.new()
	e.button_index = MOUSE_BUTTON_LEFT
	e.pressed = true
	e.position = p
	e.global_position = p
	root.push_input(e)
	e.pressed = false
	root.push_input(e)

func _snap(fname: String) -> String:
	var img: Image = root.get_viewport().get_texture().get_image()
	var path := ProjectSettings.globalize_path("res://tools_dev/out_slot/" + fname)
	img.save_png(path)
	return path

## 8×8 盒内像素均值 [r,g,b]
func _avg_box(img: Image, c: Vector2, half: int) -> Array:
	var r := 0.0
	var g := 0.0
	var bl := 0.0
	var n := 0
	var x0 := int(c.x) - half
	var y0 := int(c.y) - half
	for y in range(maxi(0, y0), mini(img.get_height(), y0 + half * 2)):
		for x in range(maxi(0, x0), mini(img.get_width(), x0 + half * 2)):
			var px := img.get_pixel(x, y)
			r += px.r
			g += px.g
			bl += px.b
			n += 1
	if n == 0:
		return [0.0, 0.0, 0.0]
	return [r / n, g / n, bl / n]

func _run() -> void:
	print("=== F1 槽位叠图验证 ===")
	if root.get_node_or_null("GameData") == null:
		root.add_child(load("res://autoload/game_data.gd").new())
	if root.get_node_or_null("SFX") == null:
		root.add_child(load("res://autoload/sfx.gd").new())
	await _wait(30)
	root.get_node("GameData").pending_mode = "tutorial"
	root.get_node("GameData").tutorial_stage = 0
	_battle = load("res://scenes/battle.tscn").instantiate()
	root.add_child(_battle)
	await _wait(150)
	var b = _battle
	_click(Vector2(1352, 953))   # Mulligan 确认
	await _wait(80)
	var ctx: Dictionary = b.get("_ctx")
	print("[slot] turn=%d active=%d" % [int(ctx.get("turn", -1)), int(ctx.get("active", -1))])
	# 18 槽实算 rect vs 权威 (149.3/131.9 步进, y 708/466)
	for p in 2:
		for i in RuleCore.BOARD_SIZE:
			var c: Vector2 = b.call("_slot_center", p, i)
			var sz: Vector2 = b.call("_slot_card_size", p, false)
			print("[slot] p=%d i=%d center=(%.1f,%.1f) size=(%.1f,%.1f)" % [p, i, c.x, c.y, sz.x, sz.y])
	# 截图① 全屏含网格
	print("[slot] save %s" % _snap("full_debug.png"))
	# 截图② 纯板面 (关 Front 层=网格/槽框都在 Front)
	var front: Variant = b.get("_front_layer")
	if front != null:
		(front as CanvasLayer).visible = false
	await process_frame
	await process_frame
	print("[slot] save %s" % _snap("full_board.png"))
	(front as CanvasLayer).visible = true
	await process_frame
	# ---- 像素采样: 槽中心 8×8 均值色 + 行均值 (贴合规判定) ----
	var img: Image = Image.load_from_file(
		ProjectSettings.globalize_path("res://tools_dev/out_slot/full_board.png"))
	if img != null:
		for p in 2:
			var row_sum := 0.0
			var row_n := 0
			var cells := []
			for i in RuleCore.BOARD_SIZE:
				var cc: Vector2 = b.call("_slot_center", p, i)
				var avg := _avg_box(img, cc, 4)
				cells.append([i, int(avg[0] * 255), int(avg[1] * 255), int(avg[2] * 255)])
				row_sum += (avg[0] + avg[1] + avg[2]) / 3.0
				row_n += 1
			print("[slot] p=%d row_mean_lum=%.1f cells=%s" % [p, row_sum / row_n, str(cells)])
	# ---- 4 张放大裁图 (槽区) ----
	var fp := ProjectSettings.globalize_path("res://tools_dev/out_slot/full_debug.png")
	var dbg: Image = Image.load_from_file(fp)
	if dbg != null:
		var zooms := [
			["zoom_p0l.png", 300.0, 600.0, 660.0, 820.0],
			["zoom_p0r.png", 1260.0, 600.0, 1620.0, 820.0],
			["zoom_p1l.png", 360.0, 360.0, 660.0, 580.0],
			["zoom_p1r.png", 1260.0, 360.0, 1560.0, 580.0],
		]
		for z in zooms:
			var rect := Rect2(z[1], z[2], z[3] - z[1], z[4] - z[2])
			var sub := dbg.get_region(rect)
			var zp := ProjectSettings.globalize_path("res://tools_dev/out_slot/" + str(z[0]))
			sub.resize(int(rect.size.x * 1.5), int(rect.size.y * 1.5), Image.INTERPOLATE_NEAREST)
			sub.save_png(zp)
			print("[slot] save %s" % str(z[0]))
	print("=== F1 done ===")
	quit(0)
