# TrueLight-Ini-Generator

TrueLight Init Generator is a tool to automatically generate a plugin blacklist in `True Light.ini` for users of True Light. The generator searches a user's installed and enabled load order for any plugins that add light sources to interior cells orginating from Betheseda plugins. This includes `Skyrim.esm`, `Update.esm`, `Dawnguard.esm`, `Dragonborn.esm`, and `HearthFires.esm` as well as the Creation Club plugins. Any plugin that adds `Placed Object` records referencing a `Light` record to a vanilla interior cell will be added to the blacklist. If an existing `True Light.ini` is present, the generator will read the existing settings and whitelist to include them in the output. 

Whitelist generation is a planned future feature.

## Installation and Usage
Download the most recent  `TrueLightIniGenerator.exe` from Releases. Extract the executable to a directory of your choice and run it. No other user input is required. If an existing `True Light.ini` is found, it will be overwritten with the blacklist created by the generator. Otherwise, a new file will be created with the default settings and whitelist for True Light. Mod Organizer 2 users must run the executable from within MO2's UVFS. If `True Light.ini` does not exist, it will be created in `overwrite` within the `LightPlacer` directory.
