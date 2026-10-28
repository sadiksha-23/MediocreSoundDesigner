using MediocreSoundDesigner.Properties;
using System.Text.Json;

namespace MediocreSoundDesigner;

public partial class FrmMain : Form
{
  // fields
  private TrackManager trackManager;
  private ToolTip topButtonTips;

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

    trackManager.DrawTimescale = trkDrawTimescale.Value;
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
    if (trackManager.SelectedTrack is not null)
    {
      if (keyData == Keys.Left)
      {
        trackManager.SelectedTrack.ShiftSampleOver(-5);
        trackManager.SelectedTrack.PanDrawSamples.Invalidate();
        return true;
      }
      else if (keyData == Keys.Right)
      {
        trackManager.SelectedTrack.ShiftSampleOver(+5);
        trackManager.SelectedTrack.PanDrawSamples.Invalidate();
        return true;
      }
      else if (keyData == Keys.Delete)
      {
        if (trackManager.SelectedTrack is not null)
        {
          if (MessageBox.Show("This will delete the selected track. This is NOT undoable!", "Are You Sure?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes)
          {
            panTracks.Controls.Remove(trackManager.SelectedTrack.PanDrawSamples);
            trackManager.SelectedTrack.PanDrawSamples.Dispose();
            trackManager.SelectedTrack.Dispose();
            trackManager.RemTrack(trackManager.SelectedTrack);
            UpdateSizes();
            return true;
          }
        }
      }
    }
    return base.ProcessCmdKey(ref msg, keyData);
  }

  private void FrmMain_SizeChanged(object sender, EventArgs e)
  {
    UpdateSizes();
  }

  private void btnPlay_Click(object sender, EventArgs e)
  {
    if (trackManager.State == NAudio.Wave.PlaybackState.Stopped)
    {
      trackManager.PlayAll();
    }
    else if (trackManager.State == NAudio.Wave.PlaybackState.Paused)
    {
      trackManager.ResumeAll();
    }
    else if (trackManager.State == NAudio.Wave.PlaybackState.Playing)
    {
      trackManager.PauseAll();
    }
  }

  private void menuFileOpen_Click(object sender, EventArgs e)
  {
    ofdLoadFile.DefaultExt = "wav";
    if (ofdLoadFile.ShowDialog() == DialogResult.OK)
    {
      Track newTrack = new(ofdLoadFile.FileName);
      CreateTrackPanel(newTrack);
      trackManager.AddTrack(newTrack);
      UpdateSizes();
      newTrack.PanDrawSamples.Invalidate();
    }
  }
  private void menuFileSave_Click(object sender, EventArgs e)
  {
    sfdSaveFile.DefaultExt = "wav";
    if (sfdSaveFile.ShowDialog() == DialogResult.OK)
    {
      trackManager.SaveToFile(sfdSaveFile.FileName);
    }
  }
  private void menuFileSettings_Click(object sender, EventArgs e)
  {
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
    UpdateSizes();
    newTrack.PanDrawSamples.Invalidate();
  }

  private void tmrUpdatePlaybackState_Tick(object sender, EventArgs e)
  {
    bool hasTracks = trackManager.Tracks.Count > 0;

    btnPlay.Visible = true;
    btnPlay.Enabled = hasTracks;
    buttonReset.Enabled = hasTracks;
    btnPlay.ImageIndex = trackManager.State == NAudio.Wave.PlaybackState.Playing ? 5 : 4;

    topButtonTips.SetToolTip(
        btnPlay,
        trackManager.State == NAudio.Wave.PlaybackState.Playing ? "Pause" : "Play");

    lblPlayhead.Visible = hasTracks;
    if (hasTracks)
    {
      lblPlayhead.Left = trackManager.GetPlayheadPos();
    }
  }

  private void trkDrawTimescale_Scroll(object sender, EventArgs e)
  {
    trackManager.DrawTimescale = trkDrawTimescale.Maximum - trkDrawTimescale.Value;
    trackManager.RedrawAll();
  }

  // Helper methods
  private Panel CreateTrackPanel(Track track)
  {
    Panel pan = new()
    {
      BackColor = Settings.Color.TrackBg_Unselected,
      Width = panTracks.Width,
      Left = 0,
      Top = 0,
    };
    pan.Click += (sender, e) =>
    {
      trackManager.SelectTrack(track);
    };
    pan.DoubleClick += (sender, e) =>
    {
      track.ToggleDisabled();
      track.PanDrawSamples.Invalidate();
    };
    pan.Paint += (sender, e) =>
    {
      track.DrawSamples(e.Graphics, trackManager.DrawTimescale);
    };
    panTracks.Controls.Add(pan);
    track.LinkPanels(pan, panTracks);
    trackManager.SelectTrack(track);

    return pan;
  }

  private void UpdateSizes()
  {
    if (trackManager is null) { return; }
    const int PADDING = 15;

    panTracks.Left = PADDING;
    panTracks.Width = ClientSize.Width - (PADDING * 2);

    int top = 0;
    foreach (Track track in trackManager.Tracks)
    {
      track.PanDrawSamples.Left = 0;
      track.PanDrawSamples.Width = panTracks.Width;
      track.PanDrawSamples.Height = (panTracks.Height / trackManager.Tracks.Count) - 1;
      track.PanDrawSamples.Top = top;
      track.PanDrawSamples.Invalidate();
      top += track.PanDrawSamples.Height;
    }
    trkDrawTimescale.Width = panTracks.Width;
    trkDrawTimescale.Left = PADDING;
    lblPlayhead.Left = 0;
    lblPlayhead.Height = panTracks.Height;
    lblPlayhead.BringToFront();

    int gap = 14;
    int rowWidth = buttonDrums.Width + buttonGuitar.Width +
                   buttonPiano.Width + buttonSynth.Width + gap * 3;
    int left = (ClientSize.Width - rowWidth) / 2;
    int y = 935;
    lbInstPanel.Size = new Size(rowWidth, 24);
    lbInstPanel.Location = new Point(left, y - lbInstPanel.Height - 10);

    buttonDrums.Location = new Point(left, y);
    buttonGuitar.Location = new Point(left + buttonDrums.Width + gap, y);
    buttonPiano.Location = new Point(left + buttonDrums.Width + buttonGuitar.Width + gap * 2, y);
    buttonSynth.Location = new Point(left + buttonDrums.Width + buttonGuitar.Width + buttonPiano.Width + gap * 3, y);

    int toolbarGap = 10;
    int toolbarWidth = btnPlay.Width + buttonReset.Width +
                       buttonRecord.Width + buttonTrim.Width + toolbarGap * 3;
    int toolbarLeft = (ClientSize.Width - toolbarWidth) / 2;
    int toolbarTop = 39;

    btnPlay.Location = new Point(toolbarLeft, toolbarTop);
    buttonReset.Location = new Point(toolbarLeft + btnPlay.Width + toolbarGap, toolbarTop);
    buttonRecord.Location = new Point(toolbarLeft + btnPlay.Width + buttonReset.Width + toolbarGap * 2, toolbarTop);
    buttonTrim.Location = new Point(toolbarLeft + btnPlay.Width + buttonReset.Width + buttonRecord.Width + toolbarGap * 3, toolbarTop);
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
    trackManager.ResetAll();
    btnPlay.ImageIndex = 4;
    lblPlayhead.Left = 0;
  }
}
