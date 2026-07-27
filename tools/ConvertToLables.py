import os
import json
import xml.etree.ElementTree as ET
from collections import Counter
from typing import List, Optional, Set

# optional imports
try:
    from PIL import Image
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False

try:
    import cv2
    CV2_AVAILABLE = True
except Exception:
    CV2_AVAILABLE = False

# pillow-heif optional for HEIC (pip install pillow-heif)
try:
    import pillow_heif  # noqa: F401
    HEIF_AVAILABLE = True
except Exception:
    HEIF_AVAILABLE = False

# -----------------------
# CONFIG
# -----------------------
JSON_FOLDER = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\JSON"
XML_FOLDER  = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\XML"
# set this to the folder containing your images (not JSON)
IMAGE_FOLDER = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\train\images"
OUT_FOLDER = r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\train\labels"
CLASSES_TXT = os.path.join(OUT_FOLDER, "classes.txt")

RECURSIVE_SEARCH = True
DEFAULT_IMAGE_EXTS = ['.jpg', '.jpeg', '.png', '.bmp', '.tif', '.tiff', '.webp', '.heic']

# ordering for auto-detected classes: 'frequency' | 'sorted' | 'appearance'
AUTO_CLASS_ORDER = 'frequency'

# -----------------------
# Utility: image size & find image
# -----------------------
def normalize_ext(ext: str):
    return ext.lower()

def get_image_size_with_pil(path):
    with Image.open(path) as im:
        return im.size[0], im.size[1]  # width, height

def get_image_size_with_cv2(path):
    img = cv2.imread(path)
    if img is None:
        return None
    h, w = img.shape[:2]
    return w, h

def get_image_size(path):
    """
    Return (width, height) or None if cannot read.
    Try PIL first (better format support), fallback to cv2.
    """
    if PIL_AVAILABLE:
        try:
            return get_image_size_with_pil(path)
        except Exception:
            pass
    if CV2_AVAILABLE:
        try:
            return get_image_size_with_cv2(path)
        except Exception:
            pass
    return None

def find_image_file_by_name(base_name_no_ext: str, image_folder: str, exts=None, recursive=False):
    """
    Find image file in image_folder by base name (no ext).
    Returns full path or None.
    """
    if exts is None:
        exts = DEFAULT_IMAGE_EXTS

    # direct tries (non-recursive)
    for ext in exts:
        candidate = os.path.join(image_folder, base_name_no_ext + ext)
        if os.path.exists(candidate):
            return candidate
        candidate_up = os.path.join(image_folder, base_name_no_ext + ext.upper())
        if os.path.exists(candidate_up):
            return candidate_up

    if not recursive:
        return None

    # recursive search
    exts_lower = {e.lower() for e in exts}
    for root, _, files in os.walk(image_folder):
        for f in files:
            name_no_ext, ext = os.path.splitext(f)
            if name_no_ext == base_name_no_ext and normalize_ext(ext) in exts_lower:
                return os.path.join(root, f)
    return None

# -----------------------
# Auto-detect classes
# -----------------------
def detect_classes_from_json_folder(json_folder: str) -> Counter:
    counter = Counter()
    if not os.path.exists(json_folder):
        return counter
    for fn in os.listdir(json_folder):
        if not fn.lower().endswith('.json'):
            continue
        path = os.path.join(json_folder, fn)
        try:
            with open(path, 'r', encoding='utf-8') as f:
                data = json.load(f)
        except Exception:
            continue
        for region in data.get('regions', []):
            tags = region.get('tags') or []
            for t in tags:
                if t is None:
                    continue
                name = str(t).strip()
                if name:
                    counter[name] += 1
                    
        # LabelMe format support
        for shape in data.get('shapes', []):
            label = shape.get('label')
            if label is not None:
                name = str(label).strip()
                if name:
                    counter[name] += 1
    return counter

def detect_classes_from_xml_folder(xml_folder: str) -> Counter:
    counter = Counter()
    if not os.path.exists(xml_folder):
        return counter
    for fn in os.listdir(xml_folder):
        if not fn.lower().endswith('.xml'):
            continue
        path = os.path.join(xml_folder, fn)
        try:
            tree = ET.parse(path)
            root = tree.getroot()
        except Exception:
            continue
        for obj in root.findall('object'):
            name_elem = obj.find('name')
            if name_elem is None or name_elem.text is None:
                continue
            name = name_elem.text.strip()
            if name:
                counter[name] += 1
    return counter

def auto_detect_classes(json_folder: Optional[str] = None,
                        xml_folder: Optional[str] = None,
                        order: str = 'frequency') -> List[str]:
    total = Counter()
    appearance: List[str] = []
    seen: Set[str] = set()

    if json_folder:
        c = detect_classes_from_json_folder(json_folder)
        for k, v in c.items():
            total[k] += v
            if k not in seen:
                appearance.append(k); seen.add(k)
    if xml_folder:
        c = detect_classes_from_xml_folder(xml_folder)
        for k, v in c.items():
            total[k] += v
            if k not in seen:
                appearance.append(k); seen.add(k)

    if order == 'frequency':
        items = [k for k,_ in total.most_common()]
    elif order == 'sorted':
        items = sorted(list(total.keys()))
    elif order == 'appearance':
        items = appearance
    else:
        items = sorted(list(total.keys()))

    return items

def save_classes_list(classes: List[str], out_path: str):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, 'w', encoding='utf-8') as f:
        for cls in classes:
            f.write(f"{cls}\n")
    print(f"[OK] Saved {len(classes)} classes to {out_path}")
    print("Classes:", classes)

# -----------------------
# JSON -> YOLO converter (use filename matching only)
# -----------------------
class JSON2YOLO:
    def __init__(self, classes: list, output_dir: str, image_folder: str = None,
                 recursive_search: bool = False, exts=None):
        self.classes = classes
        self.class_map = {name: i for i, name in enumerate(classes)}
        self.output_dir = output_dir
        self.image_folder = image_folder
        self.recursive_search = recursive_search
        self.exts = exts or DEFAULT_IMAGE_EXTS
        os.makedirs(self.output_dir, exist_ok=True)

    def convert(self, json_path):
        try:
            with open(json_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
        except Exception as e:
            print(f"[ERR] Failed to read JSON {json_path}: {e}")
            return

        # find image by json filename (no asset usage)
        base = os.path.splitext(os.path.basename(json_path))[0]
        if not self.image_folder:
            print(f"[WARN] image_folder not set, cannot locate image for {json_path}.")
            return
        image_path = find_image_file_by_name(base, self.image_folder, self.exts, recursive=self.recursive_search)
        if not image_path:
            print(f"[WARN] {json_path}: 找不到對應影像（通過檔名匹配）。跳過.")
            return

        size = get_image_size(image_path)
        if not size:
            print(f"[WARN] {json_path}: 無法讀取影像尺寸 {image_path}. 跳過.")
            return
        image_width, image_height = size

        lines = []
        for region in data.get('regions', []):
            tags = region.get('tags') or []
            bbox = region.get('boundingBox') or region.get('bounding_box') or {}
            if not bbox:
                continue
            left = float(bbox.get('left', 0))
            top  = float(bbox.get('top', 0))
            width = float(bbox.get('width', 0))
            height = float(bbox.get('height', 0))
            if width <= 0 or height <= 0:
                continue
            for class_name in tags:
                if class_name not in self.class_map:
                    # skip unknown classes
                    continue
                class_id = self.class_map[class_name]
                x_center = (left + width / 2.0) / image_width
                y_center = (top  + height / 2.0) / image_height
                w = width / image_width
                h = height / image_height
                lines.append(f"{class_id} {x_center:.6f} {y_center:.6f} {w:.6f} {h:.6f}")

        # LabelMe format support
        for shape in data.get('shapes', []):
            label = shape.get('label')
            if label not in self.class_map:
                continue
            points = shape.get('points', [])
            if not points:
                continue
            
            min_x = min(p[0] for p in points)
            max_x = max(p[0] for p in points)
            min_y = min(p[1] for p in points)
            max_y = max(p[1] for p in points)
            
            width = max_x - min_x
            height = max_y - min_y
            
            if width <= 0 or height <= 0:
                continue
                
            class_id = self.class_map[label]
            x_center = ((min_x + max_x) / 2.0) / image_width
            y_center = ((min_y + max_y) / 2.0) / image_height
            w = width / image_width
            h = height / image_height
            lines.append(f"{class_id} {x_center:.6f} {y_center:.6f} {w:.6f} {h:.6f}")

        output_path = os.path.join(self.output_dir, base + '.txt')
        with open(output_path, 'w', encoding='utf-8') as out:
            out.write('\n'.join(lines))

        print(f"[OK] Converted {json_path} -> {output_path} ({len(lines)} boxes)")

    def batch_convert(self, folder_path):
        if not os.path.exists(folder_path):
            print(f"[WARN] JSON folder not found: {folder_path}")
            return
        for fn in sorted(os.listdir(folder_path)):
            if not fn.lower().endswith('.json'):
                continue
            json_path = os.path.join(folder_path, fn)
            self.convert(json_path)

# -----------------------
# XML -> YOLO converter (use filename matching only)
# -----------------------
class XML2YOLO:
    def __init__(self, classes: list, output_dir: str, image_folder: str = None,
                 recursive_search: bool = False, exts=None):
        self.classes = classes
        self.class_map = {name: i for i, name in enumerate(classes)}
        self.output_dir = output_dir
        self.image_folder = image_folder
        self.recursive_search = recursive_search
        self.exts = exts or DEFAULT_IMAGE_EXTS
        os.makedirs(self.output_dir, exist_ok=True)

    def convert(self, xml_path):
        try:
            tree = ET.parse(xml_path)
            root = tree.getroot()
        except Exception as e:
            print(f"[ERR] Failed to parse XML {xml_path}: {e}")
            return

        # find image by xml filename (no xml path/filename usage)
        base = os.path.splitext(os.path.basename(xml_path))[0]
        if not self.image_folder:
            print(f"[WARN] image_folder not set, cannot locate image for {xml_path}.")
            return
        image_path = find_image_file_by_name(base, self.image_folder, self.exts, recursive=self.recursive_search)
        if not image_path:
            print(f"[WARN] {xml_path}: 找不到對應影像（通過檔名匹配）。跳過.")
            return

        size = get_image_size(image_path)
        if not size:
            print(f"[WARN] {xml_path}: 無法讀取影像尺寸 {image_path}. 跳過.")
            return
        image_width, image_height = size

        lines = []
        for obj in root.findall('object'):
            name_elem = obj.find('name')
            if name_elem is None or name_elem.text is None:
                continue
            class_name = name_elem.text.strip()
            if class_name not in self.class_map:
                continue
            class_id = self.class_map[class_name]
            bndbox = obj.find('bndbox')
            if bndbox is None:
                continue
            try:
                xmin = float(bndbox.find('xmin').text)
                ymin = float(bndbox.find('ymin').text)
                xmax = float(bndbox.find('xmax').text)
                ymax = float(bndbox.find('ymax').text)
            except Exception:
                continue

            w = (xmax - xmin) / image_width
            h = (ymax - ymin) / image_height
            x_center = ((xmin + xmax) / 2.0) / image_width
            y_center = ((ymin + ymax) / 2.0) / image_height

            if w <= 0 or h <= 0:
                continue

            lines.append(f"{class_id} {x_center:.6f} {y_center:.6f} {w:.6f} {h:.6f}")

        output_path = os.path.join(self.output_dir, base + '.txt')
        with open(output_path, 'w', encoding='utf-8') as out:
            out.write('\n'.join(lines))

        print(f"[OK] Converted {xml_path} -> {output_path} ({len(lines)} boxes)")

    def batch_convert(self, folder_path):
        if not os.path.exists(folder_path):
            print(f"[WARN] XML folder not found: {folder_path}")
            return
        for fn in sorted(os.listdir(folder_path)):
            if not fn.lower().endswith('.xml'):
                continue
            xml_path = os.path.join(folder_path, fn)
            self.convert(xml_path)

# -----------------------
# Main flow
# -----------------------
def main():
    print("=== convert_all_with_autodetect.py ===")
    print(f"PIL available: {PIL_AVAILABLE}, cv2 available: {CV2_AVAILABLE}, pillow-heif: {HEIF_AVAILABLE}")
    print("Scanning annotations to auto-detect classes...")

    total_counter = Counter()
    total_counter.update(detect_classes_from_json_folder(JSON_FOLDER))
    total_counter.update(detect_classes_from_xml_folder(XML_FOLDER))

    if not total_counter:
        print("[WARN] No classes found in JSON/XML folders. Exiting.")
        return

    if AUTO_CLASS_ORDER == 'frequency':
        classes = [k for k,_ in total_counter.most_common()]
    elif AUTO_CLASS_ORDER == 'sorted':
        classes = sorted(list(total_counter.keys()))
    elif AUTO_CLASS_ORDER == 'appearance':
        # fall back to frequency order for simplicity
        classes = [k for k,_ in total_counter.most_common()]
    else:
        classes = [k for k,_ in total_counter.most_common()]

    # save classes file
    save_classes_list(classes, CLASSES_TXT)

    # report counts
    print("Class counts sample:", dict(total_counter.most_common(10)))

    # create converters and run
    json_conv = JSON2YOLO(classes, OUT_FOLDER, image_folder=IMAGE_FOLDER, recursive_search=RECURSIVE_SEARCH)
    xml_conv  = XML2YOLO(classes, OUT_FOLDER, image_folder=IMAGE_FOLDER, recursive_search=RECURSIVE_SEARCH)

    # run
    if os.path.exists(JSON_FOLDER):
        print("Converting JSON annotations...")
        json_conv.batch_convert(JSON_FOLDER)
    else:
        print(f"[WARN] JSON folder not found: {JSON_FOLDER}")

    if os.path.exists(XML_FOLDER):
        print("Converting XML annotations...")
        xml_conv.batch_convert(XML_FOLDER)
    else:
        print(f"[WARN] XML folder not found: {XML_FOLDER}")

    print("=== All done ===")

if __name__ == "__main__":
    main()
