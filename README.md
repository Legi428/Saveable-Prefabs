# Saveable Prefabs

This package adds save support to instantiated prefabs (regular or items) using the native Game Creator 2 save system.
Based on the fantastic Reminstance package. https://github.com/neoneper/Reminstance

### Features

- Save instantiated prefabs
- Load instantiated prefabs using async methods
- Dismiss instantiated prefabs that are destroyed before saving
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
    - Character
    - Marker

### Dependencies

This package requires Game Creator 2 and its Inventory module to work correctly.

Supported Unity versions (see next section for more information):

- Unity 2022.3.35f1 and newer
- Unity 6000.0.7f1 and newer

### Constraints/Known Issues

- While the package has been optimized in many areas, there may still be delays when loading numerous saved prefabs.

- Due to a Unity
  bug ([UUM-67809](https://issuetracker.unity3d.com/issues/instantiated-prefabs-recttransform-values-are-incorrect-when-object-dot-instantiateasync-is-used)),
  SkinnedMeshRenderer components are not created correctly. This issue affects characters
  that fail to animate properly upon respawn. The bug has been addressed in Unity 6000.0.7f1 and is scheduled for
  resolution in version 2022.3.35f1.

- Reparenting is achieved by locating the parent using its name and position in the hierarchy. If GameObject names
  change
  within the hierarchy where a saved prefab exists, the prefab may respawn at the root level which can lead to weird
  behaviour.

### How to Contribute:

Feel free to contribute to this development by forking this repository or just create issues!
Join and show support to the original creator DoubleHitGames Discord in their: https://discord.gg/muMDQP6qQB

### How to Install:

Use the Package Manager to install this package using the following git URL:

`https://github.com/Legi428/Saveable-Prefabs.git`

# How to use?:

- Add the `PrefabGuid` component to any non-item prefabs that you want to save after instantiation.

*You can see all prefabs with the `PrefabGuid` component in `Game Creator -> Settings -> Saveable Prefabs`.*

- For regular non-item prefabs use the `Game Objects -> Instantiate Saveable Prefab` to instantiate a saveable prefab.
  The prefab
  selector already filters out incompatible prefabs. If you don't see a certain prefab, make sure it has a `PrefabGuid`
  component on its root GameObject **and** that Unity has finished indexing your project.
