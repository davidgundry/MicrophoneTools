Microphone Tools
================

Microphone Tools is a straight forward modular plugin to use microphone audio in Unity.

Currently it supports
* Getting input from microphone as an AudioSource
* Recording microphone input to uncompressed WAV
* Detecting high input intensity (speech, etc.)

To use, create an empty GameObject. Then go to Components > MicrophoneTools > MicrophoneController. Additional modules can be added to the same GameObject in this way. The modules are described below:


MicrophoneController
--------------------

The core to the Microphone Tools system is the MicrophoneController script. This handles getting input from the microphone into an AudioSource. This AudioSource is automatically added to the Microphone GameObject when you add the script. The AudioSource is routed through a mixer that by default attenuates the volume to 0.

Other scripts can access the audio data from the AudioSource. Before doing so, check AudioSource.isPlaying is true, as this indicates that Microphone data is being piped through the AudioSource.

This script also calls the MicrophoneUI to manage authorising use of the Microphone. If no UI is attached, it assumes permission (though in the web player, a Unity authorisation window will still appear).


MicrophoneUI
------------

This is an interface that is implemented by a UI to work with the MicrophoneController. DefaultMicrophoneUI is an example UI. Various interface methods are called to show a pre-authorisation screen, and a microphone select screen (if useDefaultMic is false). These need to call methods on the MicrophoneController to pass on the results of user input.


MicrophoneRecorder
------------------

MicrophoneRecorder saves to file all input sent through the AudioSource. When the AudioSource starts, a new file is created. When AudioSource stops, a WAV header is written to the file and it is closed. This operation is controlled by the 'recording' public variable.

Public variables available in the Untiy editor control how to save the files. The format is [directory]/[datetime][fileprefix].wav. Files should be(?) saved in the appropriate folder for each device.


MicrophoneInput
---------------

MicrophoneInput is a script to detect basic properties about the input to the microphone. It detects input from when the intensity rises above an activation threshold, until the intensity drops below a deactivation threshold. This threshold is above an average measure of intensity.
