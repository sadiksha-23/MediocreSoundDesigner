using MediocreSoundDesigner.Properties;
using System.Text.Json;
using System.Drawing.Drawing2D;

namespace MediocreSoundDesigner;

public partial class FrmMain : Form
{
  // fields
  private TrackManager trackManager;
  private Panel panTimeRuler;
  private string currentProjectPath;
  private ToolTip topButtonTips;
  private Track playbackTrack;
  private int selectionStart;
  private int selectionEnd;
  private int firstVisiblePixel;
  private int playheadPixel;

  private sealed class DrawingPanel : Panel {
    public DrawingPanel() {
      DoubleBuffered = true;
      ResizeRedraw = true;
    }
  }

  // ctor
  public FrmMain()
  {
    InitializeComponent();
    Setup();
  }

  // setup
  private void Setup()
  {
    DoubleBuffered = true;
    trackManager = new(panTracks);

    trkDrawTimescale.Minimum = 1;
    trkDrawTimescale.Maximum = 1000;
    trkDrawTimescale.Value = 500;
    trkDrawTimescale.TickFrequency = 100;
    trackManager.DrawTimescale = trkDrawTimescale.Value;
    trkDrawTimescale.Scroll -= trkDrawTimescale_Scroll;
    trkDrawTimescale.Scroll += trkDrawTimescale_Scroll;

    panTracks.AutoScroll = false;
    lblPlayhead.Width = 2;
    lblPlayhead.Top = 0;

    if (MainMenuStrip is not null)
    {
      HideMenuImageMargins(MainMenuStrip.Items);
    }
    menuEdit.DropDownItems.Add("Trim to Selection", null, menuEditTrim_Click);
    menuEdit.DropDownItems.Add("Delete Selection", null, menuEditDeleteSel_Click);

    panTimeRuler = new DrawingPanel {
      Height = 32,
      BackColor = Color.FromArgb(45, 45, 45),
      ForeColor = Color.Gainsboro
    };
    panTimeRuler.Paint += DrawTimeRuler;
    Controls.Add(panTimeRuler);

    imageList1.ColorDepth = ColorDepth.Depth32Bit;
    imageList1.ImageSize = new Size(30, 30);
    imageList1.Images.Clear();

    // Instrument icons: indexes 0–3
    imageList1.Images.Add(buttonDrums.BackgroundImage);
    imageList1.Images.Add(buttonGuitar.BackgroundImage);
    imageList1.Images.Add(buttonPiano.BackgroundImage);
    imageList1.Images.Add(buttonSynth.BackgroundImage);

    // Toolbar icons: indexes 4–7
    imageList1.Images.Add(Resources.play);
    imageList1.Images.Add(Resources.pause);
    imageList1.Images.Add(buttonRecord.BackgroundImage);
    imageList1.Images.Add(buttonTrim.BackgroundImage);

    // Toolbar icons: indexes 8
    imageList1.Images.Add(Resources.reset);

    buttonDrums.BackgroundImage = null;
    buttonDrums.ImageList = imageList1;
    buttonDrums.ImageIndex = 0;
    buttonDrums.Text = "Drums";

    buttonGuitar.BackgroundImage = null;
    buttonGuitar.ImageList = imageList1;
    buttonGuitar.ImageIndex = 1;
    buttonGuitar.Text = "Guitar";

    buttonPiano.BackgroundImage = null;
    buttonPiano.ImageList = imageList1;
    buttonPiano.ImageIndex = 2;
    buttonPiano.Text = "Piano";

    buttonSynth.BackgroundImage = null;
    buttonSynth.ImageList = imageList1;
    buttonSynth.ImageIndex = 3;
    buttonSynth.Text = "Synth";

    Button[] instrumentButtons = { buttonDrums, buttonGuitar, buttonPiano, buttonSynth };

    foreach (Button button in instrumentButtons)
    {
      button.Enabled = true;
      button.AutoSize = false;
      button.Size = new Size(88, 80);
      button.UseVisualStyleBackColor = false;
      button.BackColor = Color.FromArgb(58, 58, 58);
      button.ForeColor = Color.White;
      button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.BorderColor = Color.FromArgb(82, 82, 82);
      button.TextImageRelation = TextImageRelation.Overlay;
      button.ImageAlign = ContentAlignment.TopCenter;
      button.TextAlign = ContentAlignment.BottomCenter;
    }

    btnPlay.BackgroundImage = null;
    btnPlay.ImageList = imageList1;
    btnPlay.ImageIndex = 4;

    buttonRecord.BackgroundImage = null;
    buttonRecord.ImageList = imageList1;
    buttonRecord.ImageIndex = 6;

    buttonTrim.BackgroundImage = null;
    buttonTrim.ImageList = imageList1;
    buttonTrim.ImageIndex = 7;

    buttonReset.BackgroundImage = null;
    buttonReset.ImageList = imageList1;
    buttonReset.ImageIndex = 8;
    buttonReset.ImageAlign = ContentAlignment.MiddleCenter;

    Button[] toolbarButtons = { btnPlay, buttonReset, buttonRecord, buttonTrim };

    foreach (Button button in toolbarButtons)
    {
      button.AutoSize = false;
      button.Size = new Size(48, 48);
      button.Text = "";
      button.ImageAlign = ContentAlignment.MiddleCenter;
      button.UseVisualStyleBackColor = false;
      button.BackColor = Color.FromArgb(58, 58, 58);
      button.FlatStyle = FlatStyle.Flat;
      button.FlatAppearance.BorderSize = 1;
      button.FlatAppearance.BorderColor = Color.FromArgb(82, 82, 82);
      button.FlatAppearance.MouseOverBackColor = Color.FromArgb(72, 72, 72);
    }

    topButtonTips = new ToolTip
    {
      ShowAlways = true,
      InitialDelay = 500,
      AutoPopDelay = 5000,
      ReshowDelay = 100
    };
    topButtonTips.SetToolTip(btnPlay, "Play/Pause");
    topButtonTips.SetToolTip(buttonRecord, "Record");
    topButtonTips.SetToolTip(buttonTrim, "Trim");
    topButtonTips.SetToolTip(buttonReset, "Reset to the start");

    tmrUpdate.Interval = 30;
    UpdateSizes();
    ApplySettings();
  }
  private void ApplySettings()
  {
    BackColor = Settings.Color.WinBg;
    panTracks.BackColor = Settings.Color.TrackBg_Unselected;
    lblPlayhead.BackColor = Settings.Color.Playline;
    lbInstPanel.BackColor = Settings.Color.WinBg;
    trackManager.ApplySettings();
  }

  // GUI controls
  protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
  {
    Track selected = GetSelectedTrack();
    if (selected is not null)
    {
      if (keyData == Keys.Left || keyData == Keys.Right)
      {
        selected.ShiftSampleOver(keyData == Keys.Left ? -5 : 5);
        selected.PanDrawSamples.Invalidate();
        return true;
      }
      if (keyData == Keys.Delete && MessageBox.Show(
        "This will delete the selected track. This is NOT undoable!",
        "Are You Sure?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes)
      {
        RemoveTrack(selected);
        return true;
      }
    }
    return base.ProcessCmdKey(ref msg, keyData);
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

  private void FrmMain_SizeChanged(object sender, EventArgs e)
  {
    UpdateSizes();
  }
  private void btnPlay_Click(object sender, EventArgs e)
  {
    Track selected = GetSelectedTrack();
    if (selected is null) return;

    if (playbackTrack is not null && playbackTrack != selected)
    {
      playbackTrack.Pause();
      playbackTrack = null;
    }

    playbackTrack ??= selected;
    if (playbackTrack.State == NAudio.Wave.PlaybackState.Playing)
    {
      playbackTrack.Pause();
    }
    else
    {
      playbackTrack.Play();
    }
    UpdatePlaybackView();
  }

  private void menuFileOpen_Click(object sender, EventArgs e)
  {
    ofdLoadFile.DefaultExt = "wav";
    if (ofdLoadFile.ShowDialog() == DialogResult.OK)
    {
      Track newTrack = new(ofdLoadFile.FileName);
      CreateTrackPanel(newTrack);
      trackManager.AddTrack(newTrack);
      SelectTrack(newTrack);
      UpdateSizes();
      newTrack.PanDrawSamples.Invalidate();
    }
  }
    private void menuFileSave_Click(object sender, EventArgs e) {
        if (trackManager.Tracks.Count == 0 || trackManager.Tracks.All(t => t.IsDisabled)) {
            MessageBox.Show("There are no enabled tracks to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        sfdSaveFile.Filter = "WAV Audio (*.wav)|*.wav|MP3 Audio (*.mp3)|*.mp3";
        sfdSaveFile.DefaultExt = "wav";
        sfdSaveFile.AddExtension = true;
        if (sfdSaveFile.ShowDialog() != DialogResult.OK) {
            return;
        }

        string ext = sfdSaveFile.FilterIndex == 2 ? ".mp3" : ".wav";
        string path = sfdSaveFile.FileName;
        string current = Path.GetExtension(path);
        if (current.Equals(".wav", StringComparison.OrdinalIgnoreCase) || current.Equals(".mp3", StringComparison.OrdinalIgnoreCase)){
            path = Path.ChangeExtension(path, ext);
        }
        else if (!path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) {
            path += ext;
        }

        try {
            Cursor = Cursors.WaitCursor;
            trackManager.SaveToFile(path);
        }
        catch (Exception ex) {
            MessageBox.Show($"Could not export the file:\n{ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally {
            Cursor = Cursors.Default;
        }
    }
    private void menuFileSettings_Click(object sender, EventArgs e) {
    FrmEditSettings frm = new();
    frm.SetSound(trackManager.SelectedTrack);
    if (frm.ShowDialog() == DialogResult.OK)
    {
      ApplySettings();
    }
  }

  private void menuEditFlip_Click(object sender, EventArgs e)
  {
    trackManager.SelectedTrack?.Flip();
  }
  private void menuEditReverse_Click(object sender, EventArgs e)
  {
    trackManager.SelectedTrack?.Reverse();
  }
  private void menuEditVolume_Click(object sender, EventArgs e)
  {
    if (trackManager.SelectedTrack is not null)
    {
      FrmEditVolume frm = new();
      frm.SetSound(trackManager.SelectedTrack);
      frm.ShowDialog();
    }
  }
  private void menuEditEliminateSilence_Click(object sender, EventArgs e)
  {
    if (trackManager.SelectedTrack is not null)
    {
      FrmEditEliminateSilence frm = new();
      frm.SetSound(trackManager.SelectedTrack);
      frm.ShowDialog();
    }
  }

  private void menuGenRecord_Click(object sender, EventArgs e)
  {
    Track newTrack = new(new float[0]);
    CreateTrackPanel(newTrack);
    trackManager.AddTrack(newTrack);
    SelectTrack(newTrack);
    UpdateSizes();
    newTrack.PanDrawSamples.Invalidate();

    FrmGenRecord frm = new();
    frm.SetSound(trackManager.SelectedTrack);
    if (frm.ShowDialog() == DialogResult.Cancel)
    {
      trackManager.RemTrack(newTrack);
    }
  }
  private void menuGenNoise_Click(object sender, EventArgs e)
  {
    const int NUM_SAMPLES = 100_000;
    float[] samples = new float[NUM_SAMPLES];
    Random rand = new();
    for (int i = 0; i < NUM_SAMPLES; i++)
    {
      samples[i] = rand.NextSingle() * 2.0f - 1.0f;
    }
    Track newTrack = new(samples);
    CreateTrackPanel(newTrack);
    trackManager.AddTrack(newTrack);
    SelectTrack(newTrack);
    UpdateSizes();
    newTrack.PanDrawSamples.Invalidate();
  }
  private void menuGenGradientNoise_Click(object sender, EventArgs e)
  {
    const int NUM_SAMPLES = 250_000;
    float[] samples = new float[NUM_SAMPLES];
    Random rand = new();
    float cur = 0;
    float inc = 0.005f;
    int dir = +1;
    for (int i = 0; i < NUM_SAMPLES; i++)
    {
      cur += (inc * dir);
      if (rand.NextSingle() < 0.5f)
      {
        dir *= -1;
      }
      if (cur > 0.9f)
      {
        dir = -1;
      }
      if (cur < -0.9f)
      {
        dir = +1;
      }
      samples[i] = cur;
    }
    Track newTrack = new(samples);
    CreateTrackPanel(newTrack);
    trackManager.AddTrack(newTrack);
    SelectTrack(newTrack);
    UpdateSizes();
    newTrack.PanDrawSamples.Invalidate();
  }
  private void tmrUpdatePlaybackState_Tick(object sender, EventArgs e)
  {
    UpdatePlaybackView();
  }
  private void trkDrawTimescale_Scroll(object sender, EventArgs e)
  {
    if (trackManager is null) return;
    trackManager.DrawTimescale = Math.Max(1, trkDrawTimescale.Value);
    firstVisiblePixel = 0;
    trackManager.RedrawAll();
    UpdatePlaybackView();
  }
  // Helper methods
  private Panel CreateTrackPanel(Track track)
  {
    DrawingPanel pan = new()
    {
      BackColor = track.TrackColor.IsEmpty ? Settings.Color.TrackBg_Unselected : track.TrackColor,
      Width = panTracks.Width,
      Left = 0,
      Top = 0,
      BorderStyle = BorderStyle.None
    };

    pan.Click += (sender, e) => SelectTrack(track);
    pan.MouseDown += (sender, e) =>
    {
      SelectTrack(track);
      selectionStart = selectionEnd = e.X + firstVisiblePixel;
      pan.Invalidate();
    };
    pan.MouseMove += (sender, e) =>
    {
      if (e.Button == MouseButtons.Left)
      {
        selectionEnd = e.X + firstVisiblePixel;
        pan.Invalidate();
      }
    };
    pan.DoubleClick += (sender, e) =>
    {
      track.ToggleDisabled();
      pan.Invalidate();
    };
    pan.Paint += (sender, e) =>
    {
      GraphicsState state = e.Graphics.Save();
      e.Graphics.TranslateTransform(-firstVisiblePixel, 0);
      track.DrawSamples(e.Graphics, trackManager.DrawTimescale);
      if (track == GetSelectedTrack() && selectionStart != selectionEnd)
      {
        track.ShowSelection(e.Graphics, Math.Min(selectionStart, selectionEnd), Math.Max(selectionStart, selectionEnd));
      }
      e.Graphics.Restore(state);
    };

    ContextMenuStrip colorMenu = new() { ShowImageMargin = false };
    colorMenu.Items.Add("Change track color...", null, (sender, e) =>
    {
      SelectTrack(track);
      using ColorDialog dialog = new() { Color = track.TrackColor, FullOpen = true };
      if (dialog.ShowDialog(this) == DialogResult.OK) track.SetTrackColor(dialog.Color);
    });
    pan.ContextMenuStrip = colorMenu;
    pan.Disposed += (sender, e) => colorMenu.Dispose();

    panTracks.Controls.Add(pan);
    track.LinkPanels(pan, panTracks);
    SelectTrack(track);
    return pan;
  }

  private void HideMenuImageMargins(ToolStripItemCollection items)
  {
    foreach (ToolStripItem item in items)
    {
      if (item is ToolStripMenuItem menuItem)
      {
        menuItem.DropDown.ShowImageMargin = false;
        HideMenuImageMargins(menuItem.DropDownItems);
      }
    }
  }

  private Track GetSelectedTrack()
  {
    if (trackManager is null || trackManager.Tracks.Count == 0) return null;
    return trackManager.Tracks.Contains(trackManager.SelectedTrack) ? trackManager.SelectedTrack : trackManager.Tracks[0];
  }

  private void SelectTrack(Track track)
  {
    if (track is null) return;
    if (playbackTrack is not null && playbackTrack != track)
    {
      playbackTrack.Pause();
      playbackTrack = null;
      firstVisiblePixel = 0;
    }
    trackManager.SelectTrack(track);
    foreach (Track item in trackManager.Tracks) item.PanDrawSamples.BorderStyle = BorderStyle.None;
    UpdatePlaybackView();
  }

  private void RemoveTrack(Track track)
  {
    if (track is null) return;
    if (playbackTrack == track)
    {
      playbackTrack.Reset();
      playbackTrack = null;
      firstVisiblePixel = 0;
      playheadPixel = 0;
    }
    bool wasSelected = trackManager.SelectedTrack == track;
    trackManager.RemTrack(track);
    panTracks.Controls.Remove(track.PanDrawSamples);
    track.PanDrawSamples.Dispose();
    track.Dispose();
    if (wasSelected && trackManager.Tracks.Count > 0) trackManager.SelectTrack(trackManager.Tracks[0]);
    UpdateSizes();
  }

  private void menuEditTrim_Click(object sender, EventArgs e)
  {
    Track track = GetSelectedTrack();
    if (track is null || selectionStart == selectionEnd) return;
    track.Trim(Math.Min(selectionStart, selectionEnd), Math.Max(selectionStart, selectionEnd), trackManager.DrawTimescale);
    AfterEdit(track);
  }

  private void menuEditDeleteSel_Click(object sender, EventArgs e)
  {
    Track track = GetSelectedTrack();
    if (track is null || selectionStart == selectionEnd) return;
    track.DeletePart(Math.Min(selectionStart, selectionEnd), Math.Max(selectionStart, selectionEnd), trackManager.DrawTimescale);
    AfterEdit(track);
  }

  private void AfterEdit(Track track)
  {
    selectionStart = selectionEnd = 0;
    if (playbackTrack == track)
    {
      track.Reset();
      playbackTrack = null;
      firstVisiblePixel = 0;
      playheadPixel = 0;
    }
    track.PanDrawSamples.Invalidate();
    UpdatePlaybackView();
  }

  private void UpdatePlaybackView()
  {
    if (trackManager is null || panTimeRuler is null) return;
    bool hasTracks = trackManager.Tracks.Count > 0;
    btnPlay.Enabled = hasTracks;
    buttonReset.Enabled = hasTracks;
    btnPlay.Visible = hasTracks;
    lblPlayhead.Visible = hasTracks;
    btnPlay.ImageIndex = playbackTrack?.State == NAudio.Wave.PlaybackState.Playing ? 5 : 4;
    topButtonTips.SetToolTip(btnPlay, playbackTrack?.State == NAudio.Wave.PlaybackState.Playing ? "Pause" : "Play");

    if (hasTracks)
    {
      playheadPixel = (playbackTrack?.GetPlayheadPos() ?? 0) / Math.Max(1, trackManager.DrawTimescale);
      int visibleWidth = Math.Max(1, panTracks.ClientSize.Width - 20);
      int oldStart = firstVisiblePixel;
      if (playheadPixel < visibleWidth || playheadPixel < firstVisiblePixel) firstVisiblePixel = 0;
      if (playheadPixel >= firstVisiblePixel + visibleWidth) firstVisiblePixel = Math.Max(0, playheadPixel - visibleWidth / 4);
      if (oldStart != firstVisiblePixel) trackManager.RedrawAll();
      lblPlayhead.Left = playheadPixel - firstVisiblePixel;
      lblPlayhead.BringToFront();
    }
    panTimeRuler.Invalidate();
  }

  private void RebuildTrackPanels()
  {
    List<Track> loadedTracks = new(trackManager.Tracks);
    trackManager.Tracks.Clear();

    foreach (Track track in loadedTracks)
    {
      CreateTrackPanel(track);
      trackManager.AddTrack(track);
      SelectTrack(track);
    }

    UpdateSizes();
    trackManager.RedrawAll();
  }
  private void UpdateSizes()
  {
    if (trackManager is null || panTimeRuler is null) return;

    int padding = 15 * DeviceDpi / 96;
    int contentWidth = Math.Max(1, ClientSize.Width - padding * 2);
    trkDrawTimescale.Left = padding;
    trkDrawTimescale.Width = contentWidth;

    int rulerTop = trkDrawTimescale.Bottom + 4;
    panTimeRuler.SetBounds(padding, rulerTop, contentWidth, 32 * DeviceDpi / 96);
    int tracksTop = panTimeRuler.Bottom + 4;
    int tracksBottom = buttonDrums.Top - 35;
    panTracks.SetBounds(padding, tracksTop, contentWidth, Math.Max(1, tracksBottom - tracksTop));

    int count = trackManager.Tracks.Count;
    for (int i = 0; i < count; i++)
    {
      int top = i * panTracks.ClientSize.Height / count;
      int bottom = (i + 1) * panTracks.ClientSize.Height / count;
      Panel panel = trackManager.Tracks[i].PanDrawSamples;
      panel.SetBounds(0, top, panTracks.ClientSize.Width, Math.Max(1, bottom - top - 1));
      panel.Invalidate();
    }

    lblPlayhead.Height = panTracks.ClientSize.Height;
    lblPlayhead.BringToFront();
    panTimeRuler.BringToFront();
    trkDrawTimescale.BringToFront();

    int gap = 14;
    int rowWidth = buttonDrums.Width + buttonGuitar.Width + buttonPiano.Width + buttonSynth.Width + gap * 3;
    int left = (ClientSize.Width - rowWidth) / 2;
    int instrumentsY = Math.Min(935 * DeviceDpi / 96, ClientSize.Height - buttonDrums.Height - 20 * DeviceDpi / 96);
    lbInstPanel.Size = new Size(rowWidth, 24 * DeviceDpi / 96);
    lbInstPanel.Location = new Point(left, instrumentsY - lbInstPanel.Height - 10 * DeviceDpi / 96);
    buttonDrums.Location = new Point(left, instrumentsY);
    buttonGuitar.Location = new Point(left + buttonDrums.Width + gap, instrumentsY);
    buttonPiano.Location = new Point(left + buttonDrums.Width + buttonGuitar.Width + gap * 2, instrumentsY);
    buttonSynth.Location = new Point(left + buttonDrums.Width + buttonGuitar.Width + buttonPiano.Width + gap * 3, instrumentsY);

    int toolbarGap = 10;
    int toolbarWidth = btnPlay.Width + buttonReset.Width + buttonRecord.Width + buttonTrim.Width + toolbarGap * 3;
    int toolbarLeft = (ClientSize.Width - toolbarWidth) / 2;
    int toolbarTop = 39 * DeviceDpi / 96;
    btnPlay.Location = new Point(toolbarLeft, toolbarTop);
    buttonReset.Location = new Point(toolbarLeft + btnPlay.Width + toolbarGap, toolbarTop);
    buttonRecord.Location = new Point(toolbarLeft + btnPlay.Width + buttonReset.Width + toolbarGap * 2, toolbarTop);
    buttonTrim.Location = new Point(toolbarLeft + btnPlay.Width + buttonReset.Width + buttonRecord.Width + toolbarGap * 3, toolbarTop);
    UpdatePlaybackView();
  }
  private void DrawTimeRuler(object sender, PaintEventArgs e)
  {
    if (trackManager is null || panTimeRuler.Width < 20) return;

    Track referenceTrack = playbackTrack ?? GetSelectedTrack();
    int valuesPerSecond = referenceTrack?.SampleValuesPerSecond ?? 44100;
    double pixelsPerSecond = (double)valuesPerSecond / Math.Max(1, trackManager.DrawTimescale);
    if (pixelsPerSecond <= 0) return;

    double[] intervals = { 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600 };
    double majorStep = intervals[^1];
    foreach (double interval in intervals)
    {
      if (interval * pixelsPerSecond >= 90 * DeviceDpi / 96.0) { majorStep = interval; break; }
    }

    double minorStep = majorStep / 5;
    double tickSpacing = minorStep * pixelsPerSecond;
    int originX = panTimeRuler.PointToClient(panTracks.PointToScreen(Point.Empty)).X - firstVisiblePixel;
    int bottom = panTimeRuler.Height - 1;
    using Pen pen = new(panTimeRuler.ForeColor);
    e.Graphics.DrawLine(pen, 0, bottom, panTimeRuler.Width, bottom);

    int firstTick = Math.Max(0, (int)Math.Ceiling(-originX / tickSpacing));
    for (int i = firstTick; ; i++)
    {
      double seconds = i * minorStep;
      float x = (float)(originX + seconds * pixelsPerSecond);
      if (x >= panTimeRuler.Width) break;
      bool major = i % 5 == 0;
      e.Graphics.DrawLine(pen, x, bottom, x, bottom - (major ? 12 : 6));
      if (!major) continue;

      TimeSpan time = TimeSpan.FromSeconds(Math.Round(seconds, 3));
      string label = time.TotalHours >= 1 ? ((int)time.TotalHours) + time.ToString(@"\:mm\:ss") : time.ToString(@"m\:ss");
      if (majorStep < 1) label += time.ToString(@"\.fff");
      Size size = TextRenderer.MeasureText(label, panTimeRuler.Font, Size.Empty, TextFormatFlags.NoPadding);
      int labelX = Math.Max(0, (int)x - size.Width / 2);
      if (labelX + size.Width <= panTimeRuler.Width)
        TextRenderer.DrawText(e.Graphics, label, panTimeRuler.Font, new Point(labelX, 2), panTimeRuler.ForeColor, TextFormatFlags.NoPadding);
    }

    if (lblPlayhead.Visible)
    {
      int markerX = panTimeRuler.PointToClient(lblPlayhead.PointToScreen(Point.Empty)).X;
      int size = 7 * DeviceDpi / 96;
      Point[] arrow = { new(markerX - size, bottom - size), new(markerX + size, bottom - size), new(markerX, bottom) };
      e.Graphics.FillPolygon(Brushes.DodgerBlue, arrow);
    }
  }

  private void flpInstruments_Paint(object sender, PaintEventArgs e)
  {

  }

  private void buttonSynth_Click(object sender, EventArgs e)
  {

  }

  private void buttonGuitar_Click(object sender, EventArgs e)
  {

  }

  private void buttonPiano_Click(object sender, EventArgs e)
  {

  }

  private void buttonDrums_Click(object sender, EventArgs e)
  {

  }

  private void lbInstPanel_TextChanged(object sender, EventArgs e)
  {

  }
  private void buttonReset_Click(object sender, EventArgs e)
  {
    (playbackTrack ?? GetSelectedTrack())?.Reset();
    playbackTrack = null;
    firstVisiblePixel = 0;
    playheadPixel = 0;
    trackManager.RedrawAll();
    UpdatePlaybackView();
  }
  private void saveToolStripMenuItem_Click(object sender, EventArgs e)
  {
    SaveCurrentProject(false);
  }

  private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    SaveCurrentProject(true);
  }

  private void openToolStripMenuItem_Click(object sender, EventArgs e)
  {
    using OpenFileDialog dialog = new()
    {
      Filter = "Sound Designer Project (*.soundproj)|*.soundproj",
      DefaultExt = "soundproj"
    };

    if (dialog.ShowDialog() != DialogResult.OK)
    {
      return;
    }

    try
    {
      playbackTrack = null;
      firstVisiblePixel = 0;
      trackManager.LoadProject(dialog.FileName);
      RebuildTrackPanels();
      currentProjectPath = dialog.FileName;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
    {
      MessageBox.Show($"Could not open the project:\n{ex.Message}", "Open Project", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  private void menuFileNewProject_Click(object sender, EventArgs e)
  {
    if (trackManager.Tracks.Count > 0)
    {
      DialogResult choice = MessageBox.Show("Save the current project before starting a new one?", "New Project", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

      if (choice == DialogResult.Cancel)
      {
        return;
      }

      if (choice == DialogResult.Yes && !SaveCurrentProject(false))
      {
        return;
      }
    }

    playbackTrack = null;
    firstVisiblePixel = 0;
    trackManager.NewProject();
    currentProjectPath = null;
    UpdateSizes();
  }
}