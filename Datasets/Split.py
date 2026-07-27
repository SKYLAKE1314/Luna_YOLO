import os
import random
import shutil

# -------------------- 設定區 --------------------
# 訓練集占比
train_ratio = 0.8

# 原始 train 資料夾路徑
train_images_dir = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\train\images"
train_labels_dir = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\train\labels"

# 目標 val 資料夾路徑
val_images_dir = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\val\images"
val_labels_dir = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\val\labels"
# -------------------------------------------------

# 建立 val 資料夾（如果不存在）
os.makedirs(val_images_dir, exist_ok=True)
os.makedirs(val_labels_dir, exist_ok=True)

image_extensions = [".jpg", ".jpeg", ".png", ".bmp"]
all_images = [f for f in os.listdir(train_images_dir) 
              if os.path.splitext(f)[1].lower() in image_extensions]

random.shuffle(all_images)

num_train = int(len(all_images) * train_ratio)
num_val = len(all_images) - num_train

val_images = all_images[num_train:]

print(f"總圖片數: {len(all_images)}")
print(f"拆到 val 的數量: {len(val_images)}")

for img_name in val_images:
    img_path = os.path.join(train_images_dir, img_name)
    label_name = os.path.splitext(img_name)[0] + ".txt"
    label_path = os.path.join(train_labels_dir, label_name)
    
    val_img_path = os.path.join(val_images_dir, img_name)
    val_label_path = os.path.join(val_labels_dir, label_name)
    
    shutil.move(img_path, val_img_path)
    
    if os.path.exists(label_path):
        shutil.move(label_path, val_label_path)
    else:
        print(f"找不到對應標注檔 -> {label_name}")

print("split success!")
