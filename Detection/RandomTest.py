from ultralytics import YOLO
import cv2, numpy as np

model = YOLO(r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\runs\detect\train4\weights\best.pt")
img = cv2.imread(r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\XML\03.jpg")
H,W = img.shape[:2]
crop_size = 1024
stride = 800  # overlap

all_boxes=[]
for y in range(0, H, stride):
    for x in range(0, W, stride):
        crop = img[y:y+crop_size, x:x+crop_size]
        res = model.predict(crop, imgsz=crop_size, conf=0.5, iou=0.7)[0]
        for box in res.boxes: # adapt per ultralytics version
            x1,y1,x2,y2=box.xyxy[0].cpu().numpy()
            score=box.conf[0].cpu().numpy()
            cls=int(box.cls[0].cpu().numpy())
            # map back
            all_boxes.append([x1+x, y1+y, x2+x, y2+y, score, cls])

# 再做 NMS 合併 (e.g. torchvision.ops.nms)
