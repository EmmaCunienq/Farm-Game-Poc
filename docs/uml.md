# Schéma UML — Farm Game POC

Architecture actuelle du code runtime (`Assets/Scripts`, hors outils éditeur), découpée en petits diagrammes lisibles.
Seuls les membres publics les plus utiles sont listés. Une classe venant d'un autre diagramme apparaît en boîte vide.

1. [Vue d'ensemble](#1-vue-densemble)
2. [Contrôle et entrées](#2-contrôle-et-entrées)
3. [Monde 2.5D](#3-monde-25d)
4. [Cultures et terrain](#4-cultures-et-terrain)
5. [Fabrication et décorations](#5-fabrication-et-décorations)
6. [Maison et personnage](#6-maison-et-personnage)
7. [Sauvegarde](#7-sauvegarde)
8. [Découpage des classes `partial`](#8-découpage-des-classes-partial)

## 1. Vue d'ensemble

```mermaid
flowchart LR
    Input[FarmInput] -->|FarmInputFrame| Poc
    Poc[FarmPoc<br/>MonoBehaviour · UI IMGUI] --> Sim[FarmSimulation<br/>règles du jeu]
    Poc --> World[FarmWorld25D<br/>scène 2.5D]
    Poc --> Save[FarmSave<br/>JSON]
    Poc --> Home[FarmHome · FarmAppearance]
    World -->|lit + événements| Sim
    World --> Art[FarmArt<br/>sprites]
    World --> Nav[FarmNavigation<br/>A*]
    Save --> Data[FarmSaveData]
    Sim <-->|Export / Restore| Data
```

## 2. Contrôle et entrées

`FarmPoc` est le seul `MonoBehaviour` : il lit les entrées, choisit l'écran affiché (`View`) et pilote tout le reste.

```mermaid
classDiagram
    direction LR
    class FarmPoc {
        <<MonoBehaviour>>
        +FarmSimulation State
        +FarmWorld25D World
        +FarmHome Home
        +FarmAppearance Appearance
        +FarmTool ActiveTool
        +bool Crafting
        +bool BuildPlacing
        +ProcessInput(FarmInputFrame f)
        +ProcessPointerClick(Vector2 screen, bool secondary) bool
        +OpenCrafting()
        +OpenFarmPlacement(int recipe)
        +SaveGame(bool manual) bool
        +LoadGame(bool backup) bool
    }
    class View {
        <<enumeration>>
        Play, Wheel, Inventory, Shop
        Sleep, Style, Decoration, Land
        Craft, Pause, LoadConfirm, QuitConfirm
    }
    class FarmInput {
        +bool UsingGamepad
        +bool UsingMouse
        +Read(float time) FarmInputFrame
    }
    class FarmInputFrame {
        <<struct>>
        +Vector2 Move
        +Vector2 PointerPosition
        +Vector2Int Navigate
        +bool Confirm, Cancel, Build
        +int DirectTool
    }
    class FarmToolWheel {
        <<static>>
        +Select(Vector2 direction, int previous) int
    }
    class FarmTool {
        <<enumeration>>
        Hoe, Seeds, Water
        Basket, House, Wardrobe
    }
    class FarmSimulation
    class FarmWorld25D
    class FarmSave
    class FarmArt
    class FarmHome
    class FarmAppearance

    FarmPoc --> View : écran courant
    FarmPoc *-- FarmInput
    FarmInput ..> FarmInputFrame : produit
    FarmPoc ..> FarmToolWheel
    FarmPoc --> FarmTool
    FarmPoc *-- FarmSimulation
    FarmPoc *-- FarmWorld25D
    FarmPoc *-- FarmSave
    FarmPoc *-- FarmArt
    FarmPoc *-- FarmHome
    FarmPoc *-- FarmAppearance
```

## 3. Monde 2.5D

`FarmWorld25D` construit la scène à partir de l'état de la simulation et se met à jour via ses événements.

```mermaid
classDiagram
    direction LR
    class FarmWorld25D {
        <<IDisposable>>
        +Camera Camera
        +Transform Player
        +bool Indoors
        +bool NearWorkbench
        +Update(float delta, Vector2 input, ...)
        +Refresh(int x, int z)
        +ApplyHome(FarmHome home)
        +PickInteraction(Vector2 screen) FarmInteraction
        +Navigation() FarmNavigation
        +ShowDecorGhost(int kind, int x, int z, bool valid)
        +ShowFurnitureGhost(FarmFurniture item, bool valid)
        +SetCraftTime(float seconds)
    }
    class FarmInteraction {
        <<enumeration>>
        None, House, Shop, Wardrobe
        DecorChest, Exit, Workbench
    }
    class FarmArt {
        <<IDisposable>>
        +GetSprite(Kind kind) Sprite
        +CropSprite(int kind) Sprite
        +HomeSprite(int kind, bool side) Sprite
        +AvatarPart(FarmAppearance a, int part) Sprite
    }
    class FarmNavigation {
        +FindPath(Vector2 from, Vector2 to, out path) bool
    }
    class FarmSimulation

    FarmWorld25D o-- FarmSimulation : lit, s'abonne aux événements
    FarmWorld25D o-- FarmArt
    FarmWorld25D ..> FarmNavigation : crée
    FarmWorld25D ..> FarmInteraction
```

## 4. Cultures et terrain

Partie « ferme » de `FarmSimulation` : temps, argent, parcelles et cultures.

```mermaid
classDiagram
    direction LR
    class FarmSimulation {
        +FarmPlot[,] Plots
        +int[] Seeds
        +int[] Products
        +int Day
        +double Hour
        +int Money
        +int UnlockedLand
        +Till(x, z) bool
        +Plant(x, z) bool
        +Water(x, z) bool
        +Harvest(x, z) bool
        +BuySeeds(kind, amount) bool
        +Sell(kind, amount) bool
        +BuyLand(lot) bool
        +AdvanceRealSeconds(double s)
    }
    class FarmPlot {
        +bool Tilled
        +int Crop
        +int WateredDay
        +double GrowthHours
        +PlantStage Stage
    }
    class PlantStage {
        <<enumeration>>
        Empty, Seed, Sprout, Mature
    }
    class CropDefinition {
        +string Name
        +double GrowthHours
        +int SeedPrice
        +int SalePrice
    }
    class FarmLand {
        <<static>>
        +At(x, z) int$
        +Cultivable(x, z, unlocked) bool$
    }
    class FarmLandDefinition {
        +string Name
        +int Price
        +int X, Z, Width, Depth
    }

    FarmSimulation *-- "165" FarmPlot : grille 15 × 11
    FarmPlot --> PlantStage
    FarmSimulation ..> "3" CropDefinition : Crops
    FarmSimulation ..> FarmLand
    FarmLand *-- "7" FarmLandDefinition : Lots
```

## 5. Fabrication et décorations

Partie « établi » de `FarmSimulation` (fichier `FarmCrafting.cs`).

```mermaid
classDiagram
    direction LR
    class FarmSimulation {
        +int[] Crafted
        +List~FarmDecor~ Decor
        +CanCraft(recipe) bool
        +Craft(recipe) bool
        +SellCrafted(recipe, amount) bool
        +PlaceDecor(kind, x, z, moving) bool
        +RemoveDecor(index) bool
    }
    class FarmCrafting {
        <<static>>
        +float CraftSeconds$
    }
    class CraftRecipe {
        +string Name
        +string Category
        +int[] Crops
        +int Coins
        +int SalePrice
        +float DecorHeight
        +bool Placeable
    }
    class FarmDecor {
        <<Serializable>>
        +int Kind
        +int X, Z
    }

    FarmCrafting *-- "6" CraftRecipe : Recipes
    FarmSimulation ..> FarmCrafting
    FarmSimulation *-- "0..60" FarmDecor
    FarmDecor ..> CraftRecipe : Kind = index
```

## 6. Maison et personnage

```mermaid
classDiagram
    direction LR
    class FarmHome {
        +int Wallpaper
        +int Floor
        +List~FarmFurniture~ Furniture
        +Starter() FarmHome$
        +CanPlace(item, ignore) bool
        +Place(item, replace) bool
    }
    class FarmFurniture {
        +int Kind
        +int X, Z
        +int Rotation
        +Contains(x, z) bool
    }
    class FarmFurnitureDefinition {
        +string Name
        +int Width, Depth
    }
    class FarmAppearance {
        +int Skin, Hair, HairColor
        +int Eyes, Mouth
        +int Top, Bottom, Shoes
        +Get(category) int
        +Set(category, value)
    }

    FarmHome *-- "*" FarmFurniture
    FarmHome *-- "8" FarmFurnitureDefinition : Catalog
    FarmFurniture ..> FarmFurnitureDefinition : Kind = index
```

## 7. Sauvegarde

`FarmSimulation.Export` remplit un `FarmSaveData`, que `FarmSave` écrit en JSON ; `Restore` fait l'inverse.

```mermaid
classDiagram
    direction LR
    class FarmSave {
        +string Path
        +Write(FarmSaveData data) bool
        +Read(out FarmSaveData data, bool backup) bool
    }
    class FarmSaveData {
        <<Serializable>>
        +int Version, Day, Money
        +int[] Seeds, Products, Crafted
        +FarmPlot[] Plots
        +FarmDecor[] Decor
        +FarmHome Home
        +FarmAppearance Appearance
        +Valid() bool
    }
    class FarmSimulation {
        +Export(FarmSaveData save)
        +Restore(FarmSaveData save)
    }
    class FarmPlot
    class FarmDecor
    class FarmHome
    class FarmAppearance

    FarmSave ..> FarmSaveData : lit / écrit
    FarmSimulation ..> FarmSaveData : Export / Restore
    FarmSaveData o-- FarmPlot
    FarmSaveData o-- FarmDecor
    FarmSaveData o-- FarmHome
    FarmSaveData o-- FarmAppearance
```

## 8. Découpage des classes `partial`

| Classe | Fichiers | Responsabilité de chaque fichier |
|---|---|---|
| `FarmPoc` | `FarmPoc.cs` | Boucle `Update`, vues, HUD, inventaire, boutique |
| | `FarmPocHome.cs` | Maison, garde-robe, sauvegarde / chargement, pause |
| | `FarmPocMouse.cs` | Clics, déplacement à la souris, interactions |
| | `FarmPocCrafting.cs` | Catalogue de l'établi et animation de fabrication |
| | `FarmPocBuild.cs` | Menu de construction et pose (meubles + décorations) |
| `FarmWorld25D` | `FarmWorld25D.cs` | Scène extérieure, caméra, personnage, éclairage |
| | `FarmInterior25D.cs` | Intérieur de la maison et meubles |
| | `FarmWorldMouse.cs` | Picking, points d'approche, collisions de marche |
| | `FarmWorldCrafting.cs` | Établi et son animation |
| | `FarmWorldPlacement.cs` | Décorations posées et fantômes de pose |
| `FarmArt` | `FarmArt.cs`, `FarmCharacterArt.cs`, `FarmHomeArt.cs` | Sprites généraux, avatar, maison |
| `FarmSimulation` | `FarmSimulation.cs`, `FarmCrafting.cs` | Cultures / temps / argent, fabrication / décorations |
