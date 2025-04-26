# VRDC-systems

This is a repository of systems developed for use by VR Drone Club. Come visit us over at [our discord](https://discord.gg/4b5qHKHffA)

## Installation
This is intended to operate as a folder that exists inside other projects. Clone or unzip this directly into your assets folder. If you use git, you may set this up as a subrepo or set up ignore rules. If you use plastic SCM, you will likely want to ignore all the git-related items resulting from cloning.

## Notable items
This repo contains a handful of systems. Most notably:
- DronePickup is a script that allows drones to pick up items. It is similar to VRCPickup, though it operates off of a trigger collider for the proximity.
- Railgun can grab and launch pickups
- ColorPicker is a system which allows players to define primary, secondary, and effect colors. Any script can then subscribe to it and display the player's colors in any form
- ColorApplicator automatically applies the colors chosen in ColorPicker to objects in the world. It supports mesh renderers by setting material property blocks, as well as particle systems and trail renderers.
- ColorPick is a shader that takes material property blocks from the ColorApplicator and uses an RGB mask to apply multiple different custom colors to a single mesh renderer
- EffectPicker allows you to choose personal particle effects to represent you. Scripts in the world can then interface with it to deploy those particle effects at locations. It supports both bursts (provide a player/position/rotation and it will happen) and trails (provide a player/transform and it will attach that player's trail to that transform)

Contributions and pull requests are welcome within the spirit of [Unlicense](https://unlicense.org/)
