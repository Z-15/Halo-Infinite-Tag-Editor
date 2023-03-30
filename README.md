# Halo Infinite Tag Editor
Halo Infinite Tag Editor allows you to modify the tag files located within modules. This was accomplished by using Krevil's Infinite Module Editor to load and understand module data, as well as Gamergotten's Infinite Runtime Tag Viewer to understand tag structures. The Infinite Module Editor portion also requires Krevi's fork of Crauzer's OodleSharp. If you wish to contribute to this repository, you'll need to download that project as well and scope to it in Visual Studios. I made sure to seperate the work of others where I could so people could better understand what work is theirs and what is mine. There is also a credit section that will link you directly to their projects.

  - [Krevil's Infinite Module Editor](https://github.com/Krevil/InfiniteModuleEditor)
  - [Gamergotten's Infinite Runtime Tag Viewer](https://github.com/Gamergotten/Infinite-runtime-tagviewer)
  - [Krevil's fork of Crauzer's OodleSharp](https://github.com/Krevil/OodleSharp)
  - [Soupstream's Havok-Script-Tools](https://github.com/soupstream/havok-script-tools)

# Download
[![Download latest build](https://github.com/Z-15/Halo-Infinite-Tag-Editor/actions/workflows/dotnet.yml/badge.svg)](https://nightly.link/Z-15/Halo-Infinite-Tag-Editor/workflows/dotnet-desktop/master/HITE.zip)
  
# Instructions
**Module Editing Steps:**
  1. Open a module.
  2. Select the tag.
  3. Make changes.
  4. Hit save.

**Importing Tag Steps:**
  1. Open the module the tag is located in.
  2. Select the tag to open it in the tag viewer.
  3. Click import tag and select the desired file.
  4. Click save tag to save the imported tag to the module.
 
 Note: **ALWAYS MAKE BACKUPS OF YOUR MODULES!**
# Current Features and Issues

**Features:**
  - You can open any module and load almost every tag. 
  - Most value types are editable and save properly
  - You can import tag files into modules. (Unsure if you can replace one tag with another)
  - Backup Module button to easily make backups.
  - Tags can be exported to be easily shared with others.
  - You can now read Havok Script directly from tags containing it.
  - All hashes found within the tags can be easily searched to find info and relations.
  - There are various dump options used for the various Halo Infinite tools.
  
**Issues:**
  - Tag References are broken, more research is needed to get them functioning properly.
  - Tag Blocks themselves can't be edited, only the values inside can. More research is needed.
  - I need to add in input validation so incorrect values can't be entered.
  - If you find any issues, please submit a ticket or message me on Discord: xxZxx#0001
