from ultralytics import YOLO

# Load a pretrained YOLO11n model
model = YOLO(r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\runs\detect\train22\weights\best.pt")

# Run inference on 'bus.jpg' with arguments

model.predict(r"Z:\VisionTek\Ultralytics\Ultralytics_YOLO\Datasets\XML\37.bmp", 
              save=True, 
              imgsz=1920, 
              conf=0.01,
              iou=0.3,
              max_det=300,#單次最大檢測數
              augment=True,)#default false