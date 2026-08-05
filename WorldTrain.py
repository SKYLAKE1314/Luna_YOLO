from ultralytics import YOLOWorld

# 1. 載入預訓練 YOLO-World 模型權重 (建議使用 .pt 進行微調)
model = YOLOWorld("yolov8s-worldv2.pt")

# (可選) 若有指定字符 Prompt 類別，可提前設定 offline prompt
# model.set_classes(["character"]) 

# 2. 針對「小圖 / 單個字符追蹤」優化的微調訓練參數
results = model.train(
    data=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\config.yaml",
    
    # --- 基礎訓練設置 ---
    epochs=200,             # 訓練輪數
    imgsz=640,              # 圖像尺寸
    batch=32,                # 小資料集時建議減小 Batch (例如 2 或 4)，避免內存與 Batch Normalization 不穩定
    device=0,               # 指定 GPU 設備 (CPU 可設為 'cpu')
    workers=0,              # Windows 環境下設為 0
    pretrained=True,        # 載入預訓練權重進行 Fine-tuning
    amp=False,              # 關鍵修正！關閉自動混合精度(FP16)，改用 FP32 避免梯度爆炸/NaN 崩潰
    
    # --- 優化器與學習率微調 (防 NaN 穩定設置) ---
    optimizer="AdamW",      # YOLO-World 微調推薦 AdamW
    lr0=0.0005,             # 降微調學習率 (0.001 -> 0.0005)，避免小數據集過大梯度引發 NaN
    lrf=0.01,               # 最終學習率比例
    cos_lr=True,            # 餘弦退火學習率
    warmup_epochs=5.0,      # 增加預熱輪數 (5 輪)，使權重在初期更平穩過渡
    
    # --- 損失函數與邊界框定位 (穩定權重) ---
    box=7.5,                # 恢復穩定的邊界框定位損失權重
    cls=0.5,                # 分類損失權重
    dfl=1.5,                # 恢復穩定的 DFL 損失權重
    single_cls=False,       # 若標註為單一類別可設為 True
    rect=True,              # 啟用矩形訓練
    
    # --- 數據增強微調 (針對字符特徵與單圖 Crop 特化) ---
    mosaic=0.0,             # 單個字符小圖關閉 Mosaic (0.0)，防止裁切切碎字符
    mixup=0.0,              # 關閉 Mixup
    copy_paste=0.0,         # 關閉 Copy-Paste
    degrees=5.0,            # 縮小隨機旋轉角度 (±5°)
    translate=0.05,         # 降低平移幅度
    scale=0.2,              # 溫和的尺度縮放
    fliplr=0.0,             # 水平翻轉
    flipud=0.0,             # 垂直翻轉
    hsv_h=0.01,             # 微量色相增強
    hsv_s=0.3,              # 飽和度變化
    hsv_v=0.2,              # 亮度變化
    erasing=0.1,            # 降低隨機擦除比例
    
    # --- 儲存與記錄 ---
    save=True,
    save_period=-1,
    seed=0,
    deterministic=True,
)

