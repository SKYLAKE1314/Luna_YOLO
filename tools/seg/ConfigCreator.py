import os
import shutil
import random
import yaml


# =========================================
# CONFIG
# =========================================
dataset_path = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets"

train_ratio = 0.8
val_ratio = 0.2

# segmentation classes
class_names = [
    "1"
]

# =========================================
# PATH
# =========================================
train_images_dir = os.path.join(
    dataset_path,
    "train/images"
)

train_labels_dir = os.path.join(
    dataset_path,
    "train/labels"
)

val_images_dir = os.path.join(
    dataset_path,
    "val/images"
)

val_labels_dir = os.path.join(
    dataset_path,
    "val/labels"
)

# =========================================
# CREATE FOLDER
# =========================================
os.makedirs(val_images_dir, exist_ok=True)
os.makedirs(val_labels_dir, exist_ok=True)

# =========================================
# IMAGE LIST
# =========================================
image_exts = (
    ".jpg",
    ".jpeg",
    ".png",
    ".bmp",
    ".webp"
)

images = [

    f for f in os.listdir(train_images_dir)

    if f.lower().endswith(image_exts)
]

# =========================================
# SHUFFLE
# =========================================
random.shuffle(images)

# =========================================
# SPLIT
# =========================================
num_val = int(len(images) * val_ratio)

val_images = images[:num_val]

print(f"[INFO] Total Images : {len(images)}")
print(f"[INFO] Val Images   : {num_val}")

# =========================================
# MOVE FILES
# =========================================
for img_name in val_images:

    base_name = os.path.splitext(img_name)[0]

    # ---------------------------------
    # IMAGE
    # ---------------------------------
    src_img = os.path.join(
        train_images_dir,
        img_name
    )

    dst_img = os.path.join(
        val_images_dir,
        img_name
    )

    shutil.move(src_img, dst_img)

    # ---------------------------------
    # LABEL
    # ---------------------------------
    src_label = os.path.join(
        train_labels_dir,
        base_name + ".txt"
    )

    dst_label = os.path.join(
        val_labels_dir,
        base_name + ".txt"
    )

    if os.path.exists(src_label):

        shutil.move(src_label, dst_label)

    else:

        print(f"[WARN] Missing Label : {src_label}")

# =========================================
# YAML
# =========================================
config = {

    'path': dataset_path.replace("\\", "/"),

    'train': 'train/images',

    'val': 'val/images',

    'nc': len(class_names),

    'names': class_names
}

yaml_path = os.path.join(
    dataset_path,
    "seg_config.yaml"
)

with open(
    yaml_path,
    'w',
    encoding='utf-8'
) as f:

    yaml.dump(
        config,
        f,
        sort_keys=False,
        allow_unicode=True
    )

# =========================================
# DONE
# =========================================
print("===================================")
print("Segmentation Dataset Split Done")
print("===================================")

print(f"Train : {len(images) - num_val}")

print(f"Val   : {num_val}")

print(f"YAML  : {yaml_path}")