import numpy as np
from ultralytics import YOLOWorld

class YOLOWorldTextDetector:
    def __init__(self, model_path=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\runs\detect\train-7\weights\best.pt", classes=None, conf_thres=0.05, device=None):
        self.model = YOLOWorld(model_path)
        self.conf_thres = conf_thres
        self.device = device
        self.classes = classes if classes is not None else ['text']
        self.model.set_classes(self.classes)

    def set_classes(self, classes_list):
        self.classes = classes_list
        self.model.set_classes(classes_list)

    def detect(self, source, conf=None, iou=0.45, imgsz=640):
        conf_val = conf if conf is not None else self.conf_thres
        kwargs = {"conf": conf_val, "iou": iou, "imgsz": imgsz, "verbose": False}
        if self.device is not None:
            kwargs["device"] = self.device
        results = self.model.predict(source, **kwargs)
        if results[0].boxes is not None and len(results[0].boxes) > 0:
            return results[0].boxes.data[:, :5].cpu().numpy()
        return np.empty((0, 5))


if __name__ == "__main__":
    detector = YOLOWorldTextDetector(
        model_path=r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\runs\detect\train-7\weights\best.pt",
        classes=['text'],
        conf_thres=0.05
    )
    boxes = detector.detect(r"Z:\VisionTek\0728\test\test\260728094729323.jpg")
    print(boxes)
    # detector.model.predict(r"Z:\VisionTek\0728\test\test\260728094734268.jpg", conf=0.05)[0].show()
