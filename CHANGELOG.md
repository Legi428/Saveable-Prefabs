## [1.5.3] - 2024-08-20

### Fixed

- Add proper support for InstanceGuid components.

## [1.5.2] - 2024-08-08

### Changed

- Update to UniqueGameObjects version 1.3.0

## [1.5.1] - 2024-08-06

### Fixed

- Fix error caused my referencing a private field.

## [1.5.0] - 2024-08-06

### Changed

- Update Unique Game Objects package to 1.2.0.

### Fixed

- [**Breaking**] Add proper support for respawning saved item prefabs.

## [1.4.0] - 2024-06-28

### Changed

- [**Breaking**] Use the `Unique Game Objects` package version 1.1.0 instead of built in InstanceGuid component.

## [1.3.0] - 2024-06-26

### Added

- New InstanceGuid component that gives a GameObject a unique id.

### Changed

- [**Breaking**] The new InstanceGuid is now used to make parenting more robust.
- Prefabs are now saved in correct order given their sibling index.

## [1.2.0] - 2024-06-24

### Added

- Add support for Character and Marker components on a prefab.

### Changed

- [**Breaking**] Change save id information.
- [**Breaking**] Use the scene guid for more robust respawning.

## [1.1.0] - 2024-06-23

### Added

- Save the hierarchy depth of a prefab instance to save all instance in the correct order.
- Add support for Json save method.
- Replace reflection calls with dynamic methods to avoid performance hits.
- Add editorconfig and apply it to the entire project.
- Add support for the prefab instance name to be saved.
- Add support for the package in the GameCreator 2 uninstaller.

### Changed

- Update Readme.
- Make the "Instantiate Saveable Prefab" Instruction instantiate a regular prefab normally and not through the "Saveable
  Prefab" Manager.
- Use ListView to draw the settings page to improve performance.
- Replace reflection with dynamic methods to improve performance.
- Calculate the current scenes hash value only once per respawn.
- Use InstantiateAsync to at least have each prefab instantiated in parallel.
- Change the SaveIdMap to represent SaveUniqueID values rather than strings.

## [1.0.0] - 2024-06-21

### Added

- Initial release of the rewrite.
