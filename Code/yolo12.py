from calendar import EPOCH

from torch import device
from ultralytics import YOLO
from ultralytics.models.rtdetr import train

# Load a COCO-pretrained YOLO12n model
model = YOLO("yolo12n.pt")

# Train the model on the COCO8 example dataset for 100 epochs
results = model.train(data="coco8.yaml", 
                      epochs=100, 
                      imgsz=640, 
                      batch=4,
                      device=0,
                      worker=0)

# Run inference with the YOLO12n model on the 'bus.jpg' image
results = model("path/to/bus.jpg")