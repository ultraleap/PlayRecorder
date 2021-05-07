<p align="center">
  <img src="https://user-images.githubusercontent.com/6281246/117145497-206d9f00-adab-11eb-82a7-065c3fecdcc9.png" />
</p>

<p align="center">
<b>
  <a href="https://github.com/ultraleap/PlayRecorder/wiki/Getting-Started">✨ Getting Started</a> |
  <a href="https://github.com/ultraleap/PlayRecorder/wiki/Recording-Setup">🔴 Recording Setup</a> |
  <a href="https://github.com/ultraleap/PlayRecorder/wiki/Playback-Setup">▶ Playback Setup</a>
</b>
</p>

PlayRecorder lets you record Unity scene logic into binary files that work in **both** the editor or builds. Once a recording is made with a scene, even if that recording is created within a build, it can be played back within the editor, making it incredibly useful for recording data from user studies or analytics. Data can also be recorded within the editor, and then played back within a build.

All data recorded by PlayRecorder is entirely polymorphic, allowing for considerably large amounts of customisation with little effort. Due to this you can quite easily and quickly add your own custom data into the system, while still being sure it will be recorded.

Unlike traditional recording methods, such as video or audio, PlayRecorder allows data to be viewed post recording directly within your Unity scene. Finite object data could be created post-recording, such as velocity over time, as the overall information for the scene has already been recorded.

PlayRecorder includes the ability to record messages (or events in another word), allowing you to save small snippets of information very quickly. This allows for quickly assessing different stages or parts of your file without having to watch the entire thing or scrub through it.

The idea behind the overall structure is that PlayRecorder is its mostly own contained module. There should be next to no impact on your current project, be it code wise, pre-requirements, and overall performance overhead.

**[📚 Please refer to the wiki for further information. 📚](https://github.com/ultraleap/PlayRecorder/wiki)**

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
  - View all messages recorded across all currently loaded playback files.
  - Fully customisable color palette for both messages and window.
  - Quickly jump to both timestamps and different files.
- Statistics window
  - View all statistics recorded across all currently loaded playback files.
  - Basic graphs showing statistical value over time.
  - One click customisable CSV statistic exporting.

## Limitations
- PlayRecorder does not understand instantiated objects.
  - Everything needs to be within the scene before recording or playback starts.

## Requirements
- [Odin Serializer](https://github.com/TeamSirenix/odin-serializer)
  - This requires setting your project API Compatibility Level to .NET 4.x
- [Unity Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@1.0/manual/index.html)

Currently only verified to run on Windows and Standalone Windows builds. Other platforms may work however are beyond scope.

## Disclaimers
PlayRecorder is licensed under [Apache 2.0](LICENSE).

PlayRecorder is not (currently) actively maintained or managed, and provided as is. This is mostly a research focused passion project. Changes and fixes have no ETA.

Although unlikely, features and names of types may change between commits/versions. If you record files in one version of PlayRecorder, use the same version to play it back or you may encounter errors.
