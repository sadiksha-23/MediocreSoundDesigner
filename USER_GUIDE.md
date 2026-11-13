# Mediocre Sound Designer — User Guide

Mediocre Sound Designer lets you import, record, generate, edit, and play audio on a multitrack timeline.

## Main Window

- **Play/Pause** starts or pauses playback of the enabled tracks.
- **Reset** returns playback to the start.
- **Record** opens the audio recording window.
- **Import Audio** adds a WAV or MP3 file as a track.
- **Trim** keeps only the selected part of a track.
- **Delete Selection** removes the selected part of a track.
- **Zoom** changes how much audio is shown across the timeline.
- The **time ruler** shows the timeline position. The playhead shows the current playback position.

## Import and Manage Audio

1. Select **Import Audio** and choose a WAV or MP3 file.
2. Click a track to select it.
3. Drag across a waveform to select part of that track.
4. Right-click a track to change its color or delete the entire track.
5. Double-click a track to disable or enable it. Disabled tracks are skipped during playback and export.

Imported tracks display the name of their audio file. Generated tracks receive a name based on their instrument or settings.

## Select, Trim, and Delete Audio

1. Click and drag across a waveform to select a section.
2. Select **Trim** to keep only that section.
3. Select **Delete Selection** to remove that section and keep the rest of the audio.

To remove an entire track, right-click it and choose **Delete Track**.

## Generate Tracks

Use the generator buttons at the bottom of the window. Choose the settings in the dialog and select **Create**.

- **Beat:** Choose a drum kit, rhythm, tempo, number of bars, and start time.
- **Pluck:** Choose a guitar instrument, note, octave, duration, and start time.
- **Chord:** Choose a root note, chord type, octave, duration, and start time.
- **Synth:** Choose an instrument, note, octave, duration, and start time.

A **note** is the pitch, such as C or A. An **octave** sets how high or low that pitch sounds; moving up one octave makes the pitch twice as high. **Duration** sets how long the sound lasts. **Start time** sets where the track begins on the timeline.

Generated tracks are added to the timeline and can be played and edited like imported tracks.

## Record Audio

1. Select **Record** to open the recording window.
2. Press the red record button to start recording.
3. Press it again to stop.
4. Select **Apply** to add the recording to the timeline, or **Cancel** to discard it.

Recording requires a working microphone.

## Change Volume and Remove Silence

- Open **Edit > Volume** to adjust the selected track’s volume.
- Open **Edit > Eliminate Silence** to remove quiet sections. Choose where to look—beginning, end, both, or anywhere—and set the minimum silence duration and threshold.

## Apply Audio Effects

Open **Edit > Voice Tools** and choose an effect for the selected track:

- **Deeper Voice** lowers the pitch.
- **Higher Voice** raises the pitch.
- **Voice Only** uses a basic filter to emphasize centered vocal audio.
- **Remove Voice** attempts to reduce centered vocals in stereo audio.
- **Fade In** gradually increases the volume at the beginning.
- **Fade Out** gradually decreases the volume at the end.
- **Undo Effect** restores the audio from before the most recent effect.

Voice removal requires a stereo track.

## Save and Export

- Use **File > New Project** to start a new project.
- Use **File > Open Project** to open a saved `.soundproj` project.
- Use **File > Save Project** or **Save As** to save your editing project.
- Use **File > Export Audio** to export the enabled tracks as a WAV or MP3 file.

Saving a project preserves its tracks and project settings. Exporting creates an audio file.

## Switch Themes

- Press **D** to switch to dark mode.
- Press **L** to switch to light mode.

These shortcuts work when you are not typing in a text field.