import os
import json


# =========================================
# JSON(LabelMe Polygon) -> YOLO Seg
# =========================================
class JSON2YOLOSeg:

    def __init__(self, classes, output_dir):

        self.classes = classes

        self.class_map = {
            name: idx
            for idx, name in enumerate(classes)
        }

        self.output_dir = output_dir

        os.makedirs(self.output_dir, exist_ok=True)

    # =========================================
    # Convert Single JSON
    # =========================================
    def convert(self, json_path):

        try:

            with open(json_path, 'r', encoding='utf-8') as f:

                data = json.load(f)

        except Exception as e:

            print(f"[ERR] Failed to read: {json_path}")
            print(e)

            return

        image_width = data.get("imageWidth")
        image_height = data.get("imageHeight")

        if not image_width or not image_height:

            print(f"[WARN] Missing image size: {json_path}")

            return

        lines = []

        # =========================================
        # Parse Shapes
        # =========================================
        for shape in data.get("shapes", []):

            try:

                label = str(shape["label"]).strip()

                if label not in self.class_map:

                    print(f"[WARN] Unknown class: {label}")

                    continue

                class_id = self.class_map[label]

                shape_type = shape.get("shape_type", "")

                # only polygon
                if shape_type != "polygon":

                    print(f"[WARN] Skip non-polygon: {shape_type}")

                    continue

                points = shape.get("points", [])

                # polygon minimum 3 points
                if len(points) < 3:

                    print(f"[WARN] Invalid polygon: {json_path}")

                    continue

                seg_points = []

                for pt in points:

                    x = float(pt[0])
                    y = float(pt[1])

                    # normalize
                    nx = x / image_width
                    ny = y / image_height

                    # clamp
                    nx = max(0.0, min(1.0, nx))
                    ny = max(0.0, min(1.0, ny))

                    seg_points.append(f"{nx:.6f}")
                    seg_points.append(f"{ny:.6f}")

                line = f"{class_id} " + " ".join(seg_points)

                lines.append(line)

            except Exception as e:

                print(f"[ERR] Shape parse failed: {json_path}")
                print(e)

        # =========================================
        # Save TXT
        # =========================================
        base_name = os.path.splitext(
            os.path.basename(json_path)
        )[0]

        output_path = os.path.join(
            self.output_dir,
            base_name + ".txt"
        )

        try:

            with open(output_path, "w", encoding="utf-8") as f:

                f.write("\n".join(lines))

            print(f"[OK] {json_path} -> {output_path}")

        except Exception as e:

            print(f"[ERR] Save failed: {output_path}")
            print(e)

    # =========================================
    # Batch Convert
    # =========================================
    def batch_convert(self, folder_path):

        if not os.path.exists(folder_path):

            print(f"[WARN] Folder not found: {folder_path}")

            return

        files = sorted(os.listdir(folder_path))

        json_files = [
            f for f in files
            if f.lower().endswith(".json")
        ]

        print(f"[INFO] Found {len(json_files)} json files")

        for fn in json_files:

            json_path = os.path.join(folder_path, fn)

            self.convert(json_path)


# =========================================
# MAIN
# =========================================
if __name__ == "__main__":

    classes = ["1"]

    seg_converter = JSON2YOLOSeg(

        classes=classes,

        output_dir=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\train\labels"
    )

    seg_converter.batch_convert(

        r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\JSON"
    )

    print("=== DONE ===")