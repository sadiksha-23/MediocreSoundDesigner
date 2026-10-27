using NAudio.Wave;
using System.Text.Json;

namespace MediocreSoundDesigner; 

public class TrackManager {
  private Track pseudoCombinedTrack;
  private Panel panContainer;

  public List<Track> Tracks { get; private set; }
    public Track SelectedTrack { get; private set; }
  public int DrawTimescale { get; set; }
  public PlaybackState State => pseudoCombinedTrack?.State ?? PlaybackState.Stopped;

  public TrackManager(Panel panContainer) {
		Tracks = new();
    SelectedTrack = null;
    pseudoCombinedTrack = null;
    this.panContainer = panContainer;
  }

	public void AddTrack(Track track) {
		Tracks.Add(track);
	}

	public void RemTrack(Track track) {
		Tracks.Remove(track);
	}

	public void SelectTrack(Track track) {
		SelectedTrack = track;
    Tracks.ForEach(track => {
      track.PanDrawSamples.BorderStyle = BorderStyle.None;
      track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Unselected;
    });
    track.PanDrawSamples.BorderStyle = BorderStyle.Fixed3D;
    track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Selected;
  }

  public void RedrawAll() {
    Tracks.ForEach(track => track.PanDrawSamples.Invalidate());
  }

  public void ApplySettings() {
    foreach (Track track in Tracks) {
      if (SelectedTrack == track) {
        track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Selected;
      }
      else {
        track.PanDrawSamples.BackColor = Settings.Color.TrackBg_Unselected;
      }
    }
    RedrawAll();
  }

    public void SaveToFile(string filename) {
        CombineTracks();
        pseudoCombinedTrack.SaveToFile(filename);
    }
    private bool SaveCurrentProject(bool saveAs)
    {
        string path = currentProjectPath;

        if (saveAs || path is null)
        {
            using SaveFileDialog dialog = new()
            {
                Filter = "Sound Designer Project (*.soundproj)|*.soundproj",
                DefaultExt = "soundproj",
                AddExtension = true,
                FileName = path is null ? "" : Path.GetFileName(path)
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            path = dialog.FileName;
        }

        try
        {
            trackManager.SaveProject(path);
            currentProjectPath = path;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show($"Could not save the project:\n{ex.Message}", "Save Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    public void NewProject()
    {
        pseudoCombinedTrack?.Dispose();
        pseudoCombinedTrack = null;

        foreach (Track track in Tracks)
        {
            track.PanDrawSamples?.Dispose();
            track.Dispose();
        }

        Tracks.Clear();
        SelectedTrack = null;
    }

    public void LoadProject(string filename)
    {
        ProjectData project;

        using (FileStream stream = File.OpenRead(filename))
        {
            project = JsonSerializer.Deserialize<ProjectData>(stream);
        }

        if (project is null || project.Version != 1 || project.Tracks is null || project.DrawTimescale <= 0)
        {
            throw new InvalidDataException("This project file is invalid or unsupported.");
        }

        List<Track> loadedTracks = new();

        foreach (TrackData data in project.Tracks)
        {
            if (data is null || data.Samples is null || data.SampleRate < 0 || data.Channels <= 0 || data.DrawOffset < 0 || !float.IsFinite(data.Volume) || data.Volume < 0)
            {
                throw new InvalidDataException("The project contains invalid track settings.");
            }

            if (data.Samples.Length % data.Channels != 0 || data.Samples.Any(sample => !float.IsFinite(sample)))
            {
                throw new InvalidDataException("The project contains invalid audio data.");
            }

            loadedTracks.Add(new Track(data));
        }

        // Replace the current project only after reading and checking the new one.
        pseudoCombinedTrack?.Dispose();
        pseudoCombinedTrack = null;

        foreach (Track track in Tracks)
        {
            track.PanDrawSamples?.Dispose();
            track.Dispose();
        }

        Tracks = loadedTracks;
        SelectedTrack = null;
        DrawTimescale = project.DrawTimescale;
    }
    public void PlayAll() {
    CombineTracks();
    pseudoCombinedTrack.Play();
  }
  private void CombineTracks() {
    pseudoCombinedTrack?.Dispose();
    List<float> combinedSamples = new();
    foreach (Track track in Tracks) {
      if (track.IsDisabled) {
        continue;
      }
      float[] trackSamples = (float[])track.GetAdjustedSamples().Clone();
      for (int i = 0; i < trackSamples.Length; i++) {
        if (combinedSamples.Count <= i) {
          combinedSamples.Add(0);
        }
        combinedSamples[i] += trackSamples[i];
      }
    }
    pseudoCombinedTrack = new(combinedSamples.ToArray());
    pseudoCombinedTrack.LinkPanels(null, panContainer);
  }
  public void PauseAll() {
    pseudoCombinedTrack?.Pause();
  }
  public int GetPlayheadPos() {
    return (pseudoCombinedTrack?.GetPlayheadPos() ?? 0) / DrawTimescale;
  }
}
