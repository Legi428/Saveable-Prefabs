# Saveable Prefabs

This package adds save support to instantiated prefabs (regular or items) using the native Game Creator 2 save system.
Based on the fantastic Reminstance package. https://github.com/neoneper/Reminstance

### Features

- Save instantiated prefabs
- Load instantiated prefabs using async methods
- Dismiss instantiated prefabs that are destroyed before saving
- Robust parenting system using the `InstanceGuid` component
- Not replacing any Game Creator 2 components
- Give each prefab instance unique Ids in certain components
- Saved information:
    - UniqueIds on Game Creator 2 components
    - Position
    - Rotation
    - Name
    - Parent structure
    - Home scene (what scene the prefab was instantiated in)
- Support Game Creator 2 components:
    - Remember
    - Local List Variables
    - Local Name Variables
    - Marker

### Dependencies

This package requires Game Creator 2 and its Inventory module to work correctly.

Additionally, you have to install this package separately in the Unity Package
Manager: https://github.com/Legi428/Unique-GameObjects

Supported Unity versions (see next section for more information):

- Unity 2022.3.35f1 and newer
- Unity 6000.0.7f1 and newer

### Constraints/Known Issues

- While the package has been optimized in many areas, there may still be delays when loading numerous saved prefabs.

- Reparenting is achieved by locating the parent using its name and position in the hierarchy. If GameObject names
  change
  within the hierarchy where a saved prefab exists, the prefab may respawn at the root level which can lead to weird
  behaviour.

### How to Contribute:

Feel free to contribute to this development by forking this repository or just create issues!
Join and show support to the original creator DoubleHitGames Discord in their: https://discord.gg/muMDQP6qQB

### How to Install:

Use the Package Manager to install the following two packages:

`https://github.com/Legi428/Saveable-Prefabs.git`
`https://github.com/Legi428/Unique-GameObjects.git`

# How to use?:

- Add the `PrefabGuid` component to any non-item prefabs that you want to save after instantiation.
- To have the most robust reparenting of prefab instances, add the `InstanceGuid` component to any GameObject you'll
  have be the parent of newly instantiated prefabs.

*You can see all prefabs with the `PrefabGuid` component in `Game Creator -> Settings -> Saveable Prefabs`.*

- For regular non-item prefabs use the `Game Objects -> Instantiate Saveable Prefab` to instantiate a saveable prefab.
  The prefab
  selector already filters out incompatible prefabs. If you don't see a certain prefab, make sure it has a `PrefabGuid`
  component on its root GameObject **and** that Unity has finished indexing your project.
