# Saveable Prefabs
This package adds save support to instantiated prefabs (regular or items) using the native GameCreator 2 save system.
Based on the fantastic Reminstance package. https://github.com/neoneper/Reminstance

### Dependencies
This package required GameCreator 2 and its Inventory 2 module to work correctly.

### How to Contribute:
Feel free to contribute to this development by fork this repository.
Join original creator Discord DoubleHitGames at: https://discord.gg/muMDQP6qQB

### How to Install:
- 1: Download package from link
- 2: Extract the package in Unity project and done!

# How to use?:
- Add `PrefabGuid` Component to any non-item prefabs that you want to save after instantiation and save the prefab.
*"You can see all prefabs using to instance in Game Creator tool -> Settings -> Saveable Prefabs."*

- For `Item` from the Inventory module you don't have to add anything.

- For regular prefabs use the `Game Objects->Instantiate Saveable Prefab` to instantiate a saveable prefab. The prefab selector already filters out incompatible prefabs. If you don't see a certain prefab, make sure it has a `PrefabGuid` component on its root GameObject and save the prefab.