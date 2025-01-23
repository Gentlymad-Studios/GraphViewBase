# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.1] - 2023-02-18
### Added
- Initial commit

## [0.0.2] - 2023-02-24
### Fixed
- minor refactoring

## [0.0.3] - 2023-03-01
### Fixed
- fixed bug in osx specific code regarding the ShortcutHandler

## [0.0.4] - 2023-03-01
### Added
- Added ability to pan the view with alt/options key

## [0.0.5] - 2023-03-08
### Fixed
- fixed bug that resulted in "filled" ports and hidden edges when edge dragging did not exceed the drag threshold

## [0.0.6] - 2023-03-09
### Added
- Added highlighting port when hovering over it

## [0.0.7] - 2023-03-09
### Fixed
- removed some inconsistent styling
- made shortcut handler more generic so it can work on different key events
- exposed method to run shortcutHandler manually

## [0.0.8] - 2023-03-10
### Added
- Added EdgeDrop action, so it is possible to do stuff when an edge was dropped

## [0.0.9] - 2023-03-10
### Fixed
- removed debug log

## [0.1.0] - 2023-03-14
### Added
- added rename action and shortcut (F2)

## [0.1.1] - 2023-05-10
### Fixed
- Fixed Frame action being triggerd while in an editable VisualElement.

## [0.1.2] - 2023-11-01
### Added
- Added nicer line and connection drawing! Thanks to https://github.com/leissler and https://github.com/Saphirah for the work in their fork: https://github.com/leissler/GraphViewBase.

## [0.1.3] - 2023-11-24
### Fixed
- Fixed invalid cast errors when using port lists and CommentGroupNodes (fixes: https://github.com/Gentlymad-Studios/NewGraph/issues/33), thanks to https://github.com/leissler for the pull request!

## [0.1.4] - 2025-01-23
### Changed
- disabled MarkDirtyRepaint call in OnViewportChangedEvent for better performance
