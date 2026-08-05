from ultralytics import YOLOWorld

# 載入訓練好的 YOLO-World 模型權重檔 (.pt)
model = YOLOWorld(r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\runs\detect\train-12\weights\best.pt")

# 設定想偵測的文字或目標類別 ['text'] 
model.set_classes(['text'])

# 進行推論
results = model.predict(r"Z:\VisionTek\0728\test\test\260728094303627.jpg", conf=0.3)

# 顯示結果
results[0].show()
