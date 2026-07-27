import os
import yaml
import cv2

def draw_yolo_boxes(config_path):
    # 讀 YAML
    with open(config_path, "r", encoding="utf-8") as f:
        cfg = yaml.safe_load(f)

    root = cfg.get("path") or os.path.dirname(config_path)
    train_dir = os.path.join(root, cfg["train"])
    val_dir = os.path.join(root, cfg["val"])

    verify_dir = os.path.join(root, "verify")
    os.makedirs(verify_dir, exist_ok=True)

    names = cfg["names"]
    colors = [(255, 0, 0), (0, 255, 0), (0, 128, 255), (255, 128, 0)]

    def process_split(img_dir):
        label_dir = img_dir.replace("images", "labels")

        imgs = [
            f for f in os.listdir(img_dir)
            if f.lower().endswith((".jpg", ".png", ".bmp"))
        ]

        for img_name in imgs:
            img_path = os.path.join(img_dir, img_name)
            lbl_path = os.path.join(label_dir, img_name.rsplit(".", 1)[0] + ".txt")

            img = cv2.imread(img_path)
            if img is None:
                print("⚠ 無法讀取圖片:", img_path)
                continue

            H, W = img.shape[:2]

            if not os.path.exists(lbl_path):
                print("⚠ 沒有標註檔:", lbl_path)
                continue

            with open(lbl_path, "r", encoding="utf-8") as f:
                lines = f.read().strip().splitlines()

            for line in lines:
                cls, cx, cy, w, h = line.split()
                cls = int(cls)
                cx, cy, w, h = map(float, (cx, cy, w, h))

                # yolo -> xyxy
                x1 = int((cx - w / 2) * W)
                y1 = int((cy - h / 2) * H)
                x2 = int((cx + w / 2) * W)
                y2 = int((cy + h / 2) * H)

                color = colors[cls % len(colors)]

                cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)
                cv2.putText(
                    img, names[cls], (x1, y1 - 5),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.8, color, 2
                )

            out_path = os.path.join(verify_dir, img_name)
            cv2.imwrite(out_path, img)
            print("✔ 輸出:", out_path)

    print("✨ 渲染 Train")
    process_split(train_dir)

    print("✨ 渲染 Val")
    process_split(val_dir)

    print("🌸 完成！所有渲染圖已輸出到：", verify_dir)


# 執行
draw_yolo_boxes(r"E:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\config.yaml")
