import os
import sys
import shutil
import json
import time
import cv2
import yaml
import torch
import numpy as np
from PySide6.QtCore import QThread, Signal
from PySide6.QtGui import QImage, QPixmap
from ultralytics import YOLO

# 確定路徑包含專案根目錄
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
PARENT_DIR = os.path.abspath(os.path.join(CURRENT_DIR, ".."))
if CURRENT_DIR not in sys.path:
    sys.path.insert(0, CURRENT_DIR)
if PARENT_DIR not in sys.path:
    sys.path.insert(0, PARENT_DIR)

from tools.ConvertToLables import (
    JSON2YOLO, XML2YOLO,
    auto_detect_classes, save_classes_list,
    RECURSIVE_SEARCH
)
from tools.seg.JSON2YOLOSeg import JSON2YOLOSeg
from ConfigCreator import create_config


# =========================================================
# Worker: 資料集轉換 & Auto Config & Seg Conversion
# =========================================================
class ConvertWorker(QThread):
    log_signal = Signal(str)
    progress_signal = Signal(int)
    finished_signal = Signal(bool, str)

    def __init__(self, task_type, anno_folder, image_folder, out_folder, use_auto, manual_classes, split_ratio=0.2):
        super().__init__()
        self.task_type = task_type  # 'detect' 或 'segment'
        self.anno_folder = anno_folder
        self.image_folder = image_folder
        self.out_folder = out_folder
        self.use_auto = use_auto
        self.manual_classes = manual_classes
        self.split_ratio = float(split_ratio)

    def run(self):
        try:
            self.log_signal.emit(f"開始 [{self.task_type.upper()}] 資料集轉換作業...")
            if not os.path.exists(self.anno_folder):
                self.finished_signal.emit(False, "標註資料夾不存在")
                return

            # 決策 Classes
            if self.use_auto:
                classes = auto_detect_classes(self.anno_folder, self.anno_folder)
                self.log_signal.emit(f"自動檢測到的類別名單 ({len(classes)}): {classes}")
            else:
                classes = self.manual_classes
                self.log_signal.emit(f"手動指定的類別名單 ({len(classes)}): {classes}")

            if not classes:
                self.finished_signal.emit(False, "類別名單為空，請檢查標註檔案或手動設定類別！")
                return

            # 建立目錄結構 train/images, train/labels, val/images, val/labels
            dataset_root = self.out_folder
            train_images = os.path.join(dataset_root, "train", "images")
            train_labels = os.path.join(dataset_root, "train", "labels")
            val_images = os.path.join(dataset_root, "val", "images")
            val_labels = os.path.join(dataset_root, "val", "labels")

            for p in [train_images, train_labels, val_images, val_labels]:
                os.makedirs(p, exist_ok=True)

            classes_txt_path = os.path.join(dataset_root, "classes.txt")
            save_classes_list(classes, classes_txt_path)
            self.log_signal.emit(f"📁 classes.txt 已保存至: {classes_txt_path}")

            # 搜尋標註檔
            anno_files = [f for f in os.listdir(self.anno_folder) if f.lower().endswith(('.json', '.xml'))]
            if not anno_files:
                self.finished_signal.emit(False, "標註資料夾中未找到 .json 或 .xml 標註檔案！")
                return

            total = len(anno_files)
            count = 0

            if self.task_type == 'segment':
                seg_conv = JSON2YOLOSeg(classes=classes, output_dir=train_labels)
                for f in anno_files:
                    if f.lower().endswith('.json'):
                        seg_conv.convert(os.path.join(self.anno_folder, f))
                        base = os.path.splitext(f)[0]
                        self._copy_image(base, train_images)
                    count += 1
                    self.progress_signal.emit(int(count / total * 80))
            else: # detect
                json_conv = JSON2YOLO(classes, output_dir=train_labels, image_folder=self.image_folder)
                xml_conv = XML2YOLO(classes, output_dir=train_labels, image_folder=self.image_folder)
                for f in anno_files:
                    full_p = os.path.join(self.anno_folder, f)
                    if f.lower().endswith('.json'):
                        json_conv.convert(full_p)
                    elif f.lower().endswith('.xml'):
                        xml_conv.convert(full_p)
                    base = os.path.splitext(f)[0]
                    self._copy_image(base, train_images)
                    count += 1
                    self.progress_signal.emit(int(count / total * 80))

            # 自動劃分 Val 集並生成 config.yaml
            self.log_signal.emit("⚖ 劃分 Train / Val 資料集...")
            import random
            all_train_imgs = [f for f in os.listdir(train_images) if f.lower().endswith(('.jpg', '.png', '.jpeg', '.bmp'))]
            random.shuffle(all_train_imgs)
            val_num = int(len(all_train_imgs) * self.split_ratio)
            val_select = all_train_imgs[:val_num]

            for img_f in val_select:
                base_f = os.path.splitext(img_f)[0]
                shutil.move(os.path.join(train_images, img_f), os.path.join(val_images, img_f))
                lbl_f = base_f + ".txt"
                src_lbl = os.path.join(train_labels, lbl_f)
                if os.path.exists(src_lbl):
                    shutil.move(src_lbl, os.path.join(val_labels, lbl_f))

            # 生成 config.yaml
            config_data = {
                'path': dataset_root.replace("\\", "/"),
                'train': 'train/images',
                'val': 'val/images',
                'nc': len(classes),
                'names': classes
            }
            yaml_path = os.path.join(dataset_root, "config.yaml")
            with open(yaml_path, 'w', encoding='utf-8') as f:
                yaml.dump(config_data, f, sort_keys=False, allow_unicode=True)

            self.progress_signal.emit(100)
            self.log_signal.emit(f"✨ 轉換完成！Train 圖片: {len(all_train_imgs)-val_num}, Val 圖片: {val_num}")
            self.log_signal.emit(f"📄 已生成配置文件: {yaml_path}")
            self.finished_signal.emit(True, yaml_path)

        except Exception as e:
            self.log_signal.emit(f"❌ 轉換過程發生錯誤: {e}")
            self.finished_signal.emit(False, str(e))

    def _copy_image(self, base_name, dest_dir):
        for ext in [".jpg", ".png", ".jpeg", ".bmp"]:
            src = os.path.join(self.image_folder, base_name + ext)
            if os.path.exists(src):
                shutil.copy(src, os.path.join(dest_dir, base_name + ext))
                return


# =========================================================
# Worker: 視覺化標註驗證 (DataCheck.py 可視化整合)
# =========================================================
class DataCheckWorker(QThread):
    log_signal = Signal(str)
    image_rendered_signal = Signal(str, str) # image_path, output_path
    finished_signal = Signal(str)

    def __init__(self, config_path):
        super().__init__()
        self.config_path = config_path

    def run(self):
        try:
            self.log_signal.emit(f"🔍 載入配置並開始畫框驗證: {self.config_path}")
            with open(self.config_path, "r", encoding="utf-8") as f:
                cfg = yaml.safe_load(f)

            root = cfg.get("path") or os.path.dirname(self.config_path)
            train_dir = os.path.join(root, cfg.get("train", "train/images"))
            if not os.path.isabs(train_dir):
                train_dir = os.path.join(root, cfg.get("train", "train/images"))

            verify_dir = os.path.join(root, "verify")
            os.makedirs(verify_dir, exist_ok=True)

            names = cfg.get("names", [])
            if isinstance(names, dict):
                names = [names[i] for i in sorted(names.keys())]

            colors = [(255, 60, 60), (60, 255, 60), (60, 120, 255), (255, 180, 0), (200, 60, 255)]

            if not os.path.exists(train_dir):
                self.log_signal.emit(f"⚠ 找不到訓練圖片目錄: {train_dir}")
                return

            lbl_dir = train_dir.replace("images", "labels")
            imgs = [f for f in os.listdir(train_dir) if f.lower().endswith(('.jpg', '.png', '.bmp'))]
            self.log_signal.emit(f"找到 {len(imgs)} 張圖片進行驗證畫框...")

            for img_name in imgs[:50]: # 最多渲染50張預覽
                img_path = os.path.join(train_dir, img_name)
                lbl_path = os.path.join(lbl_dir, os.path.splitext(img_name)[0] + ".txt")

                img = cv2.imread(img_path)
                if img is None:
                    continue

                H, W = img.shape[:2]
                if os.path.exists(lbl_path):
                    with open(lbl_path, "r", encoding="utf-8") as lf:
                        lines = lf.read().strip().splitlines()

                    for line in lines:
                        parts = line.split()
                        if len(parts) >= 5:
                            cls = int(parts[0])
                            vals = list(map(float, parts[1:5]))
                            if len(parts) > 5: # Polygon segmentation
                                pts = np.array(list(map(float, parts[1:])), dtype=np.float32).reshape(-1, 2)
                                pts[:, 0] *= W
                                pts[:, 1] *= H
                                color = colors[cls % len(colors)]
                                cv2.polylines(img, [pts.astype(np.int32)], True, color, 2)
                            else: # Box
                                cx, cy, w, h = vals
                                x1, y1 = int((cx - w/2) * W), int((cy - h/2) * H)
                                x2, y2 = int((cx + w/2) * W), int((cy + h/2) * H)
                                color = colors[cls % len(colors)]
                                cv2.rectangle(img, (x1, y1), (x2, y2), color, 2)
                                name = names[cls] if cls < len(names) else str(cls)
                                cv2.putText(img, name, (x1, max(y1 - 5, 15)), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

                out_path = os.path.join(verify_dir, img_name)
                cv2.imwrite(out_path, img)
                self.image_rendered_signal.emit(img_path, out_path)

            self.log_signal.emit(f"✨ 驗證完成！預覽渲染圖已存至: {verify_dir}")
            self.finished_signal.emit(verify_dir)

        except Exception as e:
            self.log_signal.emit(f"❌ 驗證畫框出錯: {e}")


# =========================================================
# Worker: YOLO 多任務訓練 (Detect, Segment, Classify)
# =========================================================
class TrainWorker(QThread):
    log_signal = Signal(str)
    progress_signal = Signal(int)
    epoch_metrics_signal = Signal(dict) # epoch, loss, map, etc.
    finished_signal = Signal(bool, str)

    def __init__(self, kwargs):
        super().__init__()
        self.kwargs = kwargs
        self._is_running = True
        self._is_paused = False

    def stop(self):
        self._is_running = False

    def pause(self):
        self._is_paused = True
        
    def resume(self):
        self._is_paused = False

    def run(self):
        self.log_signal.emit("啟動 Ultralytics YOLO 訓練流程...")
        try:
            model_path = self.kwargs.pop("model_path")
            self.log_signal.emit(f"載入模型結構/權重: {model_path}")
            model = YOLO(model_path)

            # 自訂 Ultralytics Callback 來捕捉訓練進度與處理暫停/取消
            def check_pause(trainer):
                while self._is_paused and self._is_running:
                    time.sleep(0.5)
                if not self._is_running:
                    trainer.stop = True

            def on_train_batch_end(trainer):
                check_pause(trainer)

            def on_train_epoch_end(trainer):
                check_pause(trainer)
                if not self._is_running:
                    return
                epoch = trainer.epoch + 1
                epochs = trainer.epochs
                pct = int((epoch / epochs) * 100)
                self.progress_signal.emit(pct)

                metrics = {"epoch": epoch, "total_epochs": epochs}
                try:
                    loss_dict = {}
                    # 1. 嘗試由 label_loss_items 取得 dict
                    if hasattr(trainer, "label_loss_items") and hasattr(trainer, "tloss") and trainer.tloss is not None:
                        try:
                            loss_dict = trainer.label_loss_items(trainer.tloss)
                        except Exception:
                            pass

                    # 2. 若 loss_items 本身就是 dict
                    if not loss_dict and hasattr(trainer, "loss_items") and trainer.loss_items is not None:
                        if isinstance(trainer.loss_items, dict):
                            loss_dict = trainer.loss_items

                    if loss_dict and isinstance(loss_dict, dict):
                        vals = []
                        for k, v in loss_dict.items():
                            val = float(v.detach().cpu().item()) if hasattr(v, "detach") else (float(v.item()) if hasattr(v, "item") else float(v))
                            vals.append(val)
                            k_lower = str(k).lower()
                            if "box" in k_lower: metrics["box_loss"] = val
                            elif "cls" in k_lower or "class" in k_lower: metrics["cls_loss"] = val
                            elif "dfl" in k_lower or "seg" in k_lower or "pose" in k_lower: metrics["dfl_loss"] = val
                        
                        if "box_loss" not in metrics and len(vals) >= 1: metrics["box_loss"] = vals[0]
                        if "cls_loss" not in metrics and len(vals) >= 2: metrics["cls_loss"] = vals[1]
                        if "dfl_loss" not in metrics and len(vals) >= 3: metrics["dfl_loss"] = vals[2]
                    else:
                        # 3. 若為 tensor / list / ndarray 序列結構
                        loss_raw = getattr(trainer, "loss_items", None)
                        if loss_raw is None and hasattr(trainer, "tloss"):
                            loss_raw = trainer.tloss
                        
                        if loss_raw is not None:
                            if hasattr(loss_raw, "detach"):
                                loss_arr = loss_raw.detach().cpu().tolist()
                            elif hasattr(loss_raw, "tolist"):
                                loss_arr = loss_raw.tolist()
                            elif isinstance(loss_raw, (list, tuple)):
                                loss_arr = list(loss_raw)
                            else:
                                loss_arr = [float(loss_raw)]
                            
                            clean_arr = []
                            for x in loss_arr:
                                if isinstance(x, (int, float)):
                                    clean_arr.append(float(x))
                                elif hasattr(x, "item"):
                                    clean_arr.append(float(x.item()))
                                elif hasattr(x, "detach"):
                                    clean_arr.append(float(x.detach().cpu().item()))

                            if len(clean_arr) >= 1: metrics["box_loss"] = clean_arr[0]
                            if len(clean_arr) >= 2: metrics["cls_loss"] = clean_arr[1]
                            if len(clean_arr) >= 3: metrics["dfl_loss"] = clean_arr[2]
                except Exception as le:
                    self.log_signal.emit(f"[WARN] 解析 loss 發生異常: {le}")

                try:
                    if hasattr(trainer, "metrics") and trainer.metrics:
                        m = trainer.metrics
                        if isinstance(m, dict):
                            metrics["map50"]    = float(m.get("metrics/mAP50(B)",    m.get("metrics/mAP50(M)",    m.get("mAP50", 0))))
                            metrics["map50_95"] = float(m.get("metrics/mAP50-95(B)", m.get("metrics/mAP50-95(M)", m.get("mAP50-95", 0))))
                except Exception as me:
                    self.log_signal.emit(f"[WARN] 解析 metrics 發生異常: {me}")

                self.epoch_metrics_signal.emit(metrics)
                box_str = f"{metrics['box_loss']:.4f}" if "box_loss" in metrics else "N/A"
                self.log_signal.emit(f"Epoch [{epoch}/{epochs}] 進度: {pct}% | Box Loss: {box_str}")

            model.add_callback("on_train_batch_end", on_train_batch_end)
            model.add_callback("on_train_epoch_end", on_train_epoch_end)

            # 執行訓練
            results = model.train(**self.kwargs)

            self.progress_signal.emit(100)
            self.log_signal.emit("✅ 訓練任務順利完成！模型與結果已自動儲存。")
            self.finished_signal.emit(True, "訓練成功！")

        except Exception as e:
            self.log_signal.emit(f"❌ 訓練過程發生異常: {e}")
            self.finished_signal.emit(False, str(e))


# =========================================================
# Worker: 推理與實時目標追蹤 (Predict & Track)
# =========================================================
class InferenceWorker(QThread):
    frame_signal = Signal(QImage, str) # rendered_frame, status_text
    log_signal = Signal(str)
    finished_signal = Signal()

    def __init__(self, model_path, source, mode="predict", tracker="bytetrack.yaml", conf=0.25, iou=0.45, device="0"):
        super().__init__()
        self.model_path = model_path
        self.source = source
        self.mode = mode # 'predict' 或 'track'
        self.tracker = tracker
        self.conf = float(conf)
        self.iou = float(iou)
        self.device = device
        self._is_running = True

    def stop(self):
        self._is_running = False

    def run(self):
        self.log_signal.emit(f"🎬 啟動 {self.mode.upper()} 推理/追蹤引擎...")
        try:
            model = YOLO(self.model_path)
            
            # 單圖或資料夾推斷
            if isinstance(self.source, str) and (self.source.endswith(('.jpg', '.png', '.jpeg', '.bmp')) or os.path.isdir(self.source)):
                if self.mode == "track":
                    results = model.track(source=self.source, tracker=self.tracker, conf=self.conf, iou=self.iou, device=self.device, stream=True)
                else:
                    results = model.predict(source=self.source, conf=self.conf, iou=self.iou, device=self.device, stream=True)

                for res in results:
                    if not self._is_running:
                        break
                    frame_bgr = res.plot()
                    qimg = self._cv_to_qimage(frame_bgr)
                    det_count = len(res.boxes) if res.boxes is not None else 0
                    info = f"檢測目標數: {det_count}"
                    if res.boxes is not None and res.boxes.id is not None:
                        info += f" | 追蹤ID數: {len(res.boxes.id)}"
                    self.frame_signal.emit(qimg, info)
                    time.sleep(0.03)

            # 影片或相機串流
            else:
                cap_src = 0 if str(self.source) == "0" else self.source
                cap = cv2.VideoCapture(cap_src)
                if not cap.isOpened():
                    self.log_signal.emit(f"❌ 無法開啟影像來源: {self.source}")
                    return

                fps_start_time = time.time()
                frame_count = 0

                while cap.isOpened() and self._is_running:
                    ret, frame = cap.read()
                    if not ret:
                        break

                    if self.mode == "track":
                        results = model.track(frame, tracker=self.tracker, conf=self.conf, iou=self.iou, device=self.device, verbose=False)
                    else:
                        results = model.predict(frame, conf=self.conf, iou=self.iou, device=self.device, verbose=False)

                    res_frame = results[0].plot() if len(results) > 0 else frame
                    qimg = self._cv_to_qimage(res_frame)

                    frame_count += 1
                    elapsed = time.time() - fps_start_time
                    fps = frame_count / elapsed if elapsed > 0 else 0
                    det_count = len(results[0].boxes) if len(results) > 0 and results[0].boxes is not None else 0
                    info = f"FPS: {fps:.1f} | 檢測目標: {det_count}"

                    self.frame_signal.emit(qimg, info)
                    time.sleep(0.01)

                cap.release()

            self.log_signal.emit("✨ 推理/追蹤流程結束")
            self.finished_signal.emit()

        except Exception as e:
            self.log_signal.emit(f"❌ 推理過程出錯: {e}")
            self.finished_signal.emit()

    def _cv_to_qimage(self, cv_img):
        rgb_img = cv2.cvtColor(cv_img, cv2.COLOR_BGR2RGB)
        h, w, ch = rgb_img.shape
        bytes_per_line = ch * w
        qimg = QImage(rgb_img.data, w, h, bytes_per_line, QImage.Format_RGB888)
        return qimg.copy()


# =========================================================
# Worker: 模型導出 (ONNX, TensorRT, OpenVINO 等)
# =========================================================
class ExportWorker(QThread):
    log_signal = Signal(str)
    finished_signal = Signal(bool, str)

    def __init__(self, model_path, fmt="onnx", imgsz=640, half=False, dynamic=False, simplify=True, opset=12):
        super().__init__()
        self.model_path = model_path
        self.fmt = fmt
        self.imgsz = int(imgsz)
        self.half = half
        self.dynamic = dynamic
        self.simplify = simplify
        self.opset = int(opset)

    def run(self):
        self.log_signal.emit(f" 開始導出模型 [{self.fmt.upper()}] 格式...")
        try:
            model = YOLO(self.model_path)
            export_path = model.export(
                format=self.fmt,
                imgsz=self.imgsz,
                half=self.half,
                dynamic=self.dynamic,
                simplify=self.simplify,
                opset=self.opset
            )
            self.log_signal.emit(f"✨ 模型導出成功: {export_path}")
            self.finished_signal.emit(True, str(export_path))
        except Exception as e:
            self.log_signal.emit(f"❌ 模型導出失敗: {e}")
            self.finished_signal.emit(False, str(e))


# =========================================================
# Worker: CUDA & GPU 硬體診斷 (cudatorch.py 整合)
# =========================================================
class CudaCheckWorker(QThread):
    info_signal = Signal(dict)

    def run(self):
        info = {
            "cuda_available": torch.cuda.is_available(),
            "device_count": torch.cuda.device_count() if torch.cuda.is_available() else 0,
            "device_name": torch.cuda.get_device_name(0) if torch.cuda.is_available() else "N/A",
            "torch_version": torch.__version__,
            "cuda_version": torch.version.cuda if torch.cuda.is_available() else "N/A"
        }
        self.info_signal.emit(info)
