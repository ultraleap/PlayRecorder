Current Unity Version: 2019.3

Use this as a submodule within your project.

**Requirements**
- [Odin Serializer](https://github.com/TeamSirenix/odin-serializer)

**Getting Started**

- Add the RecordingManager script to a new empty gameobject.
- Assign a RecordComponent to any object within your scene, for example a TransformRecordComponent to your camera, and give it a unique descriptor.
- Hop into play mode and press Start Recording, once done press Stop Recording. (Both functions can be done through code)

**Play Your Recording**

- Add the PlaybackManager script to a new empty gameobject.
- Add your recorded files to the manager and press the Update Files button.
- Assign your recorded components to the ones in your scene (this should be done automatically based upon your descriptors).
- Hop into play mode and press Play.

Note, features and names of types may change between commits/versions. If you record files in one version of PlayRecorder, use the same version to play it back or you may encounter errors.