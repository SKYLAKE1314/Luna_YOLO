import os
import shutil
import random
import yaml

def create_config(dataset_path=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets", class_names=None, val_ratio=0.2, log_func=print):
    if class_names is None:
        class_names = ["no"]

    train_images_dir = os.path.join(dataset_path, "train", "images")
    train_labels_dir = os.path.join(dataset_path, "train", "labels")
    val_images_dir = os.path.join(dataset_path, "val", "images")
    val_labels_dir = os.path.join(dataset_path, "val", "labels")

    os.makedirs(val_images_dir, exist_ok=True)
    os.makedirs(val_labels_dir, exist_ok=True)

    if not os.path.exists(train_images_dir):
        log_func(f"⚠ 找不到訓練圖片目錄: {train_images_dir}")
        return os.path.join(dataset_path, "config.yaml")

    images = [f for f in os.listdir(train_images_dir) if f.lower().endswith(('.jpg', '.png', '.jpeg', '.bmp'))]
    random.shuffle(images)

    num_val = int(len(images) * val_ratio)
    val_images = images[:num_val]

    for img_name in val_images:
        base_name = os.path.splitext(img_name)[0]
        src_img = os.path.join(train_images_dir, img_name)
        dst_img = os.path.join(val_images_dir, img_name)
        if os.path.exists(src_img):
            shutil.move(src_img, dst_img)
        
        src_label = os.path.join(train_labels_dir, base_name + ".txt")
        dst_label = os.path.join(val_labels_dir, base_name + ".txt")
        if os.path.exists(src_label):
            shutil.move(src_label, dst_label)

    config = {
        'path': dataset_path.replace("\\", "/"),
        'train': 'train/images',
        'val': 'val/images',
        'nc': len(class_names),
        'names': class_names
    }

    yaml_path = os.path.join(dataset_path, "config.yaml")
    with open(yaml_path, 'w', encoding='utf-8') as f:
        yaml.dump(config, f, sort_keys=False, allow_unicode=True)

    log_func(f"✨ 拆分完成！訓練集: {len(images) - num_val}, 驗證集: {num_val}")
    log_func(f"📄 config.yaml 已生成於: {yaml_path}")
    return yaml_path

if __name__ == "__main__":
    create_config()
