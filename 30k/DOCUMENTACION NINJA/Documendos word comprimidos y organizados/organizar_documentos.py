import os
import pandas as pd
from docx import Document

def read_docx(file_path):
    doc = Document(file_path)
    text = ""
    for paragraph in doc.paragraphs:
        text += paragraph.text + "\n"
    return text

# Cambia esta ruta a la carpeta donde están tus archivos Word
directory = "C:/Users/PC/Documents/PDF CONVERSACION CLAUDE/DOCUMENTACION NINJA"
documents = {}

# Usar un conjunto para almacenar textos únicos
unique_texts = set()

# Leer los archivos .docx
for filename in os.listdir(directory):
    if filename.endswith(".docx"):
        file_path = os.path.join(directory, filename)
        text = read_docx(file_path)
        unique_texts.add(text)  # Agregar solo textos únicos
        print(f"Procesando: {filename}")  # Mensaje de progreso

# Clasificar el contenido (puedes ajustar las palabras clave según tus necesidades)
categories = {
    "Códigos": [],
    "Tutoriales": [],
    "Errores Comunes": []
}

for text in unique_texts:
    if "código" in text.lower():
        categories["Códigos"].append(text)
    elif "tutorial" in text.lower():
        categories["Tutoriales"].append(text)
    elif "error" in text.lower():
        categories["Errores Comunes"].append(text)

# Guardar el contenido organizado en archivos separados
for category, texts in categories.items():
    with open(f"{category}.txt", "w", encoding="utf-8") as f:  # Especificar la codificación utf-8
        for text in texts:
            f.write(text + "\n")

# Mensaje de finalización
print("Proceso completado. Los archivos han sido generados.")