extends Node
## 通用随机切图脚本。
## 挂载本脚本后，程序会收集与脚本所在节点同属一个父节点的所有 TextureRect 子节点，
## 并在 _ready 时从任意张图片中随机选择一张显示，其余图片全部隐藏。
## 图片数量不限：今后新增差分图片，只需在场景中添加一个新的 TextureRect 节点即可，
## 无需修改本脚本。每个 TextureRect 自己的尺寸、位置等排版参数会原样保留。

func _ready() -> void:
	var texture_rects: Array[TextureRect] = []
	for child in get_parent().get_children():
		if child is TextureRect:
			texture_rects.append(child)
	if texture_rects.is_empty():
		return
	for texture_rect in texture_rects:
		texture_rect.visible = false
	var chosen: TextureRect = texture_rects.pick_random()
	chosen.visible = true
