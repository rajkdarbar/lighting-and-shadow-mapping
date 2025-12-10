# 💡 Lighting and Shadow Mapping in Unity
This project demonstrates how to implement **shadow mapping** from scratch in Unity for **directional light**, **spotlights**, and **point lights**. It uses **Percentage Closer Filtering (PCF)** to produce soft, visually smooth shadows, and leverages the **rasterization pipeline** to render lighting and shadows efficiently in real time.

---

## 🧩 Features

- Directional Light Shadow Mapping (orthographic projection)  
- Spotlight Shadow Mapping (perspective projection)  
- Point Light Shadow Mapping (cubemap rendering with perspective projection)  
- Percentage Closer Filtering (PCF) for smoother shadow edges  
- Blinn–Phong Lighting Model with an ambient term  
- Real-time rendering using Unity’s programmable pipeline (C# + HLSL)

---

## 🖼️ Example Outputs

### ☀️ Shadows from Directional Light  
<img src="Assets/Resources/Output Images/DirectionalLightShadow.png" width="600">

<br>

### 🔦 Shadows from Spotlight  
<img src="Assets/Resources/Output Images/SpotLightShadow.png" width="600">

<br>

### 💡 Shadows from Point Light  
<img src="Assets/Resources/Output Images/PointLightShadow.png" width="600">

---


## 🎬 Demo Video
Watch [this video](https://youtu.be/_YQvtcxrGvc) for a quick walkthrough of the project and how to use the repo.
