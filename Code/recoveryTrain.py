import ultralytics

from ultralytics import YOLO
import ultralytics.models

# Load a model
model = YOLO("path/to/last.pt")  # load a partially trained model

# Resume training
results = model.train(resume=True)