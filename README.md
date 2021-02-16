# PlayRecorder
PlayRecorder lets you record Unity scene logic into binary files that work in **both** the editor or builds. Once a recording is made with a scene, even if that recording is created within a build, it can be played back within the editor, making it incredibly useful for recording data from user studies or analytics. Data can also be recorded within the editor, and then played back within a build.

All data recorded by PlayRecorder is entirely polymorphic, allowing for considerably large amounts of customisation with little effort. Due to this you can quite easily and quickly add your own custom data into the system, while still being sure it will be recorded.

Unlike traditional recording methods, such as video or audio, PlayRecorder allows data to be viewed post recording directly within your Unity scene. Finite object data could be created post-recording, such as velocity over time, as the overall information for the scene has already been recorded.

PlayRecorder includes the ability to record messages (or events in another word), allowing you to save small snippets of information very quickly. This allows for quickly assessing different stages or parts of your file without having to watch the entire thing or scrub through it.

The idea behind the overall structure is that PlayRecorder is it's mostly own contained module. There should be next to no impact on your current project, be it code wise, pre-requirements, and overall performance overhead.

## How It Works
PlayRecorder has a component (called ```RecordComponent```) and data (called ```RecordFrame```) layout, where individual components are attached to your objects and control their own data stores and logic.

The ```RecordingManager``` controls when components record their updates, and eventually collates all the data from the components and saves it into binary files.
- ```RecordComponent```s control a ```RecordItem``` (the raw data).
- ```RecordItem```s in turn can have as many ```RecordPart``` as they wish.
- ```RecordPart```s store the individual frames for the recording.

The ```PlaybackManager``` loads recorded files back into the scene, sends the respective data to components, and then ticks through all changes. Ideally recordings are meant to be loaded back into the scene that they were recorded from, and the system will automatically understand how to assign objects, however as long as the type of RecordComponent matches between the recording and the scene then it can be played back.
- ```RecordComponent```s also control their playback logic.
  - This logic can be customised freely to match requirements of the component.

Please refer to [Expanding Your Components](#expanding-your-components) for more information on customising the system to meet your needs.

## What's Included
- Recording and Playback system
  - Customise recording (and playback) frame rates.
  - All events can be controlled from either the editor windows, or through code.
  - Multiple files can be loaded into the playback system and swapped between at runtime.
- RecordComponents
  - RecordComponent - Only records the Unity enable/disable events.
  - TransformRecordComponent - Records all transform changes.
- Message system
  - Allows for timestamped events to be recorded to objects.
- Timeline window
  - View all messages recorded across all currently loaded files.
  - Fully customisable color palette for both messages and window.
  - Quickly jump to both timestamps and different files.

## Limitation
- PlayRecorder does not understand instantiated objects.
  - Everything needs to be within the scene before recording or playback starts.

## Requirements
- [Odin Serializer](https://github.com/TeamSirenix/odin-serializer)
- [Unity Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@1.0/manual/index.html)

Currently only verified to run on Windows and Standalone Windows builds. Other platforms may work however are beyond scope.

## Getting Started
- Add Odin Serializer to your project.
  - Ensure it is using the default namespace.
- Add PlayRecorder as a submodule within your project Assets folder.
- Add the RecordingManager script to a new empty gameobject and set your recording folder and name.
  - Specify a custom frame rate if you wish.
  - All recordings are prefixed with a unix style date/time stamp to prevent overwriting.
  - All recording folders are relative to the Unity [dataPath](https://docs.unity3d.com/ScriptReference/Application-dataPath.html) location.
- Assign a RecordComponent to any object within your scene, for example a TransformRecordComponent to your camera (which will record the transform), and give it a unique descriptor.
  - By default an empty RecordComponent will only record the enable/disable events and any messages fed to it.
- Go into play mode and press Start Recording, once done press Stop Recording. (Both functions can be trigger through through code)

## Playing Your Recording

- Add the PlaybackManager script to a new empty gameobject.
- Add your recorded files to the manager and press the Update Files button.
  - Binary files are automatically found by Unity, as long as they are within your Assets folder they should be found with any Asset Database refresh.
- Assign your recorded components to the ones in your scene (this should be done automatically based upon your descriptors).
- Go into play mode and press Play.

## Addons
PlayRecorder includes a few addons by default, including a [Leap HandModel](https://github.com/leapmotion/unitymodules) RecordComponent. Addons usually require specific extra plugins and are therefore disabled by default to prevent compilation errors.

- By default Leap, SteamVR, and any other addon is disabled.
- To enable an addon go to ```Edit -> Project Settings -> Player -> Scripting Define Symbols``` and add ```PR_*plugin*``` (e.g. ```PR_LEAP```).

## Expanding Your Components
Every segment of the system can be expanded, from the components, right down to the individual frames that are being recorded. Make sure you expand off the base data structures and everything should be saved as long as the data can be serialised. You should not have to manually add the code to the system, it should all automatically understand what it is as long as it resides in your project with PlayRecorder.
- For a good example of how to expand RecordComponents, with both recording and playback changes, refer to the ```TransformRecordComponent```.
- For a good example of how to expand RecordItem, RecordPart, and RecordFrame, refer to the ```Hands``` script inside of ```Data```.
- A simple ```Stopwatch``` editor window example is included to show how to hook into the PlaybackManager for playback analytics.

## Disclaimers
PlayRecorder is not actively maintained or managed, and provided as is. Changes and fixes have no ETA.

Although unlikely, features and names of types may change between commits/versions. If you record files in one version of PlayRecorder, use the same version to play it back or you may encounter errors.
