<p align="center">
  <img src="https://user-images.githubusercontent.com/6281246/117145497-206d9f00-adab-11eb-82a7-065c3fecdcc9.png" />
</p>

PlayRecorder lets you record Unity scene logic into binary files that work in **both** the editor or builds. Once a recording is made with a scene, even if that recording is created within a build, it can be played back within the editor, making it incredibly useful for recording data from user studies or analytics. Data can also be recorded within the editor, and then played back within a build.

All data recorded by PlayRecorder is entirely polymorphic, allowing for considerably large amounts of customisation with little effort. Due to this you can quite easily and quickly add your own custom data into the system, while still being sure it will be recorded.

Unlike traditional recording methods, such as video or audio, PlayRecorder allows data to be viewed post recording directly within your Unity scene. Finite object data could be created post-recording, such as velocity over time, as the overall information for the scene has already been recorded.

PlayRecorder includes the ability to record messages (or events in another word), allowing you to save small snippets of information very quickly. This allows for quickly assessing different stages or parts of your file without having to watch the entire thing or scrub through it.

The idea behind the overall structure is that PlayRecorder is its mostly own contained module. There should be next to no impact on your current project, be it code wise, pre-requirements, and overall performance overhead.

📚 Please refer to the [wiki](wiki) for further information. 📚

## What's Included
- Recording and Playback system
  - Customise recording (and playback) frame rates.
  - All events can be controlled from either the editor windows, or through code.
  - Multiple files can be loaded into the playback system and swapped between at runtime.
- [RecordComponents](Scripts/Components)
  - RecordComponent - Only records the Unity enable/disable events.
  - TransformRecordComponent - Records all transform changes.
  - SkinnedMeshRecordComponent - Records all transform changes of a SkinnedMesh component.
  - AnimationRecordComponent - Records play events of an Animation component.
  - AnimatorRecordComponent - Records an Animator and state changes.
- RecordFrames
  - Multiple basic types of frames to help you quickly expand the system to meet your data needs.
- Message and Statistics system
  - Allows for timestamped events to be recorded to objects.
  - Store time based and final statistical values into recordings, one click CSV exporting of values.
- Timeline window
  - View all messages recorded across all currently loaded files.
  - Fully customisable color palette for both messages and window.
  - Quickly jump to both timestamps and different files.

## Limitation
- PlayRecorder does not understand instantiated objects.
  - Everything needs to be within the scene before recording or playback starts.

## Requirements
- [Odin Serializer](https://github.com/TeamSirenix/odin-serializer)
  - This requires setting your project API Compatibility Level to .NET 4.x
- [Unity Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@1.0/manual/index.html)

Currently only verified to run on Windows and Standalone Windows builds. Other platforms may work however are beyond scope.

## Getting Started
- Add Odin Serializer to your project.
  - Ensure it is using the default namespace.
  - Change your project's API Compatibility Level to .NET 4.x (```Edit -> Project Settings -> Player -> Other Settings -> API Compatibility Level```)
- Add the Editor Coroutines package to your project.
  - ```Window -> Package Manager -> Search for Editor Coroutines -> Click Install```
- Add PlayRecorder as a submodule within your project Assets folder.
- Add the RecordingManager script to a new empty gameobject and set your recording folder and name.
  - Specify a custom frame rate if you wish.
  - All recordings are prefixed with a unix style date/time stamp to prevent overwriting.
  - All recording folders are relative to the Unity [dataPath](https://docs.unity3d.com/ScriptReference/Application-dataPath.html) location.
- Assign a RecordComponent to any object within your scene, for example a TransformRecordComponent to your camera (which will record the transform), and give it a unique descriptor.
  - By default an empty RecordComponent will only record the enable/disable events and any messages fed to it.
- Go into play mode and press Start Recording, once done press Stop Recording. (Both functions can be triggered through through code)

## Playing Your Recording

- Add the PlaybackManager script to a new empty gameobject.
- Add your recorded files to the manager and press the Update Files button.
  - There are two ways to do this, either by pressing the plus button on the top right and then selecting your file you want to load, or by dragging the file onto the Recorded Files header.
  - Binary files are automatically found by Unity, as long as your recordings are within your Assets folder they should be found with any Asset Database refresh.
- Assign your recorded components to the ones in your scene (this should be done automatically based upon your descriptors).
- Go into play mode and press Play.
  - To access the Timeline and see your messages go to ```Tools -> PlayRecorder -> Timeline```.
  - Note that by default, no ```RecordComponent``` will record any messages. These have to be defined in your code, and are advised to be outside of ```RecordComponents```.

## Addons
PlayRecorder includes a few addons by default, including a [LeapMotion HandModel](https://github.com/leapmotion/unitymodules) RecordComponent. Addons usually require specific extra plugins and are therefore disabled by default to prevent compilation errors.

- By default all addons are disabled.
- To enable an addon go to ```Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols``` and add ```PR_*plugin*``` (e.g. ```PR_LEAP```).

### Current Addons
- [Ultraleap hand recorder](Addons/Leap)
  - Records either individual model based hands, or the entire service provider frames.
- [SteamVR Skeletal Hand recorder](Addons/SteamVR)

## Expanding Your Components
Every segment of the system can be expanded, from the components, right down to the individual frames that are being recorded. Make sure you expand off the base data structures and everything should be saved as long as the data can be serialised. You should not have to manually add the code to the system, it should all automatically understand what it is as long as it resides in your project with PlayRecorder.
- For a good example of how to expand RecordComponents, with both recording and playback changes, refer to the ```TransformRecordComponent```.
- For a good example of how to expand RecordItem, RecordPart, and RecordFrame, refer to the ```Hands``` script inside of ```Data```.
- A simple ```Stopwatch``` editor window example is included to show how to hook into the PlaybackManager for playback analytics.

## Disclaimers
PlayRecorder is licensed under [Apache 2.0](LICENSE).

PlayRecorder is not (currently) actively maintained or managed, and provided as is. This is mostly a research focused passion project. Changes and fixes have no ETA.

Although unlikely, features and names of types may change between commits/versions. If you record files in one version of PlayRecorder, use the same version to play it back or you may encounter errors.
