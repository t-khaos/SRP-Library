# TRP

A custom render pipeline based on Unity SRP, which is used for personal learning and practical rendering algorithm projects.

# Gallery

### Deferred Render Pipeline

| RT    | R           | G           | B           | A         | Format   |
| ----- | ----------- | ----------- | ----------- | --------- | -------- |
| 0     | BaseColor.X | BaseColor.Y | BaseColor.Z | Metallic  | R8G8B8A8 |
| 1     | Normal.X    | Normal.X    | Normal.Y    | Normal.Y  | R16G15   |
| 2     | Emission.X  | Emission.Y  | Emission.Z  | Roughness | R8G8B8A8 |
| 3     | AO          | /           | /           | /         | R8G8B8A8 |
| Depth | Depth       | Depth       | Depth       | Depth     | Depth    |

### PBR & IBL

- Metal/Roughness Workflow

- Cook-Torrance BRDF

![PBR-IBL](./Gallery/PBR-IBL.png)

# Reference