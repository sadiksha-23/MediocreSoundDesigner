//reference - https://github.com/naudio/naudio
//			- https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.devicedpi?view=windowsdesktop-10.0
//			- https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.scrollablecontrol.autoscroll?view=windowsdesktop-10.0
//			- 



using MediocreSoundDesigner.Properties;
using NAudio.Wave;

namespace MediocreSoundDesigner;

public partial class FrmMain : Form
{
	private TrackManager trackManager;
	private Panel panTimeRuler; //the numbered ruler 
	private Label lblZoom; // track currentlu played
	private int selStart;
	private int selEnd;
	private Track playbackTrack; 
	private int playheadPixel; //audio position converted intp pixels
	private int firstVisiblePixel; // where the visible time begins 
	private readonly Dictionary<Track, int> sampleRates = new();

	private sealed class DrawingPanel : Panel
	{
		public DrawingPanel()
		{
			DoubleBuffered = true;
			ResizeRedraw = true;
		}
	}

	public FrmMain()
	{
		InitializeComponent();
		Setup();
	}

	private void Setup()
	{
		DoubleBuffered = true;
		trackManager = new(panTracks);

		// The  ruler shows playback. made the old slider for zoom in and out
		trkDrawTimescale.Minimum = 1;
		trkDrawTimescale.Maximum = 1000;
		trkDrawTimescale.Value = 500;
		trkDrawTimescale.TickFrequency = 100;
		trackManager.DrawTimescale = trkDrawTimescale.Value;

		lblZoom = new Label
		{
			Text = "Zoom",
			AutoSize = true,
			ForeColor = Color.White
		};
		Controls.Add(lblZoom);

		//ruler 
		panTimeRuler = new DrawingPanel
		{
			Height = 36 * DeviceDpi / 88,
			BackColor = Color.FromArgb(12,14,15), //pantimeruler
			ForeColor = Color.Yellow //ruler text and color
		};
		panTimeRuler.Paint += DrawTimeRuler; // it connects to methood that draws its content 
		Controls.Add(panTimeRuler);

		//vertical line that is moving 
		panTracks.AutoScroll = false;
		//lblPlayhead.AutoSize = false;
		//lblPlayhead.Text = "";
		lblPlayhead.BackColor = Color.White;	////
		lblPlayhead.Width = 2;  // for scaling 
		lblPlayhead.Top = 0;
		lblPlayhead.Visible = true;

		//btnPlay.BackgroundImage = Resources.play;
		btnPlay.Click -= btnPlay_Click;
		btnPlay.Click += btnPlay_Click;

		// 
		tmrUpdate.Stop();    // stops the existing timer temporialy
		tmrUpdate.Interval = 30;
		tmrUpdate.Tick -= tmrUpdatePlaybackState_Tick;
		tmrUpdate.Tick += tmrUpdatePlaybackState_Tick;

		menuEdit.DropDownItems.Add("Trim to Selection", null, menuEditTrim_Click);
		menuEdit.DropDownItems.Add("Delete Selection", null, menuEditDeleteSel_Click);

		UpdateSizes();
		tmrUpdate.Start();
	}

	/// hwt this function does
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		Track track = GetSelectedTrack();
		if (track != null)
		{
			if (keyData == Keys.Left || keyData == Keys.Right)
			{
				track.ShiftSampleOver(keyData == Keys.Left ? -5 : 5);
				track.PanDrawSamples.Invalidate();
				return true;
			}

			if (keyData == Keys.Delete)
			{
				if (MessageBox.Show(
					"This will delete th",
					"wrong", MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Exclamation) == DialogResult.Yes)
					RemoveTrack(track);

				return true;
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
		if (playbackTrack?.State == PlaybackState.Playing)
		{
			playbackTrack.Pause();
		}
		else
		{
			if (playbackTrack == null ||
				playbackTrack.State == PlaybackState.Stopped)
			{
				playbackTrack?.Dispose();
				playbackTrack = GetSelectedTrack();

				if (playbackTrack == null)
					return;

				playheadPixel = 0;
				firstVisiblePixel = 0;
				trackManager.RedrawAll();
			}

			playbackTrack.Play();
		}

		UpdatePlaybackView();
	}

	private void menuFileOpen_Click(object sender, EventArgs e)
	{
		ofdLoadFile.DefaultExt = "wav";

		if (ofdLoadFile.ShowDialog() != DialogResult.OK)
			return;

		using var reader = new AudioFileReader(ofdLoadFile.FileName);

		int rate = reader.WaveFormat.SampleRate *
				   reader.WaveFormat.Channels;

		AddTrack(new Track(ofdLoadFile.FileName), rate);
	}

	private void menuFileSave_Click(object sender, EventArgs e)
	{
		sfdSaveFile.DefaultExt = "wav";

		if (sfdSaveFile.ShowDialog() == DialogResult.OK)
			trackManager.SaveToFile(sfdSaveFile.FileName);
	}

	private void menuEditFlip_Click(object sender, EventArgs e)
	{
		GetSelectedTrack()?.Flip();
		trackManager.RedrawAll();
	}

	private void menuEditReverse_Click(object sender, EventArgs e)
	{
		GetSelectedTrack()?.Reverse();
		trackManager.RedrawAll();
	}

	private void menuEditVolume_Click(object sender, EventArgs e)
	{
		Track track = GetSelectedTrack();
		if (track == null)
			return;

		using FrmEditVolume frm = new();
		frm.SetSound(track);
		frm.ShowDialog();
		trackManager.RedrawAll();
	}

	private void menuEditEliminateSilence_Click(object sender, EventArgs e)
	{
		Track track = GetSelectedTrack();
		if (track == null)
			return;

		using FrmEditEliminateSilence frm = new();
		frm.SetSound(track);
		frm.ShowDialog();
		trackManager.RedrawAll();
	}

	private void menuGenRecord_Click(object sender, EventArgs e)
	{
		Track track = new(Array.Empty<float>());
		AddTrack(track, GetGeneratedSampleRate());

		using FrmGenRecord frm = new();
		frm.SetSound(track);

		if (frm.ShowDialog() == DialogResult.Cancel)
			RemoveTrack(track);
		else
			track.PanDrawSamples.Invalidate();
	}

	private void menuGenNoise_Click(object sender, EventArgs e)
	{
		float[] samples = new float[100_000];
		Random rand = new();

		for (int i = 0; i < samples.Length; i++)
			samples[i] = rand.NextSingle() * 2.0f - 1.0f;

		AddTrack(new Track(samples), GetGeneratedSampleRate());
	}

	private void menuGenGradientNoise_Click(object sender, EventArgs e)
	{
		float[] samples = new float[250_000];
		Random rand = new();
		float cur = 0;
		const float inc = 0.005f;
		int dir = 1;

		for (int i = 0; i < samples.Length; i++)
		{
			cur += inc * dir;

			if (rand.NextSingle() < 0.5f)
				dir *= -1;

			if (cur > 0.9f)
				dir = -1;

			if (cur < -0.9f)
				dir = 1;

			samples[i] = cur;
		}

		AddTrack(new Track(samples), GetGeneratedSampleRate());
	}

	private static int GetGeneratedSampleRate()
	{
		// Matches Track.cs's default format for generated audio.
		WaveFormat format = new();
		return format.SampleRate * format.Channels;
	}

	private void tmrUpdatePlaybackState_Tick(object sender, EventArgs e)
	{
		UpdatePlaybackView();
	}

	private void UpdatePlaybackView()
	{
		if (trackManager == null || panTimeRuler == null)
			return;

		bool hasTracks = trackManager.Tracks.Count > 0;

		btnPlay.Visible = hasTracks;
		lblPlayhead.Visible = hasTracks;

		btnPlay.BackgroundImage =
			playbackTrack?.State == PlaybackState.Playing
				? Resources.pause
				: Resources.play;

		// Read the same track that the Play button started.
		playheadPixel = (playbackTrack?.GetPlayheadPos() ?? 0)
			/ Math.Max(1, trackManager.DrawTimescale);

		// Follow playback when it reaches the visible area's right edge.
		int visibleWidth = Math.Max(1, panTracks.ClientSize.Width - 20);
		int oldStart = firstVisiblePixel;

		if (playheadPixel < visibleWidth ||
			playheadPixel < firstVisiblePixel)
		{
			firstVisiblePixel = 0;
		}

		if (playheadPixel >= firstVisiblePixel + visibleWidth)
		{
			firstVisiblePixel =
				Math.Max(0, playheadPixel - visibleWidth / 4);
		}

		if (oldStart != firstVisiblePixel)
			trackManager.RedrawAll();

		lblPlayhead.Left = playheadPixel - firstVisiblePixel;
		lblPlayhead.BringToFront();

		panTimeRuler.Invalidate();
	}

	private void trkDrawTimescale_Scroll(object sender, EventArgs e)
	{
		if (trackManager == null)
			return;

		trackManager.DrawTimescale =
			Math.Max(1, trkDrawTimescale.Value);

		firstVisiblePixel = 0;
		trackManager.RedrawAll();
		UpdatePlaybackView();
	}

	private Track GetSelectedTrack()
	{
		if (trackManager == null || trackManager.Tracks.Count == 0)
			return null;

		return trackManager.Tracks.Contains(trackManager.SelectedTrack)
			? trackManager.SelectedTrack
			: trackManager.Tracks[0];
	}

	private void SelectTrack(Track track)
	{
		trackManager.SelectTrack(track);

		// Identical borders keep all waveform origins aligned.
		foreach (Track item in trackManager.Tracks)
			item.PanDrawSamples.BorderStyle = BorderStyle.None;

		if (playbackTrack?.State == PlaybackState.Stopped)
		{
			playbackTrack.Dispose();
			playbackTrack = null;
			firstVisiblePixel = 0;
			trackManager.RedrawAll();
		}

		UpdatePlaybackView();
	}

	private void AddTrack(Track track, int rate)
	{
		sampleRates[track] = rate;
		CreateTrackPanel(track);
		trackManager.AddTrack(track);
		SelectTrack(track);
		UpdateSizes();
	}

	private void RemoveTrack(Track track)
	{
		if (playbackTrack == track)
		{
			playbackTrack.Pause();
			playbackTrack = null;
			playheadPixel = 0;
			firstVisiblePixel = 0;
		}

		trackManager.RemTrack(track);
		sampleRates.Remove(track);

		panTracks.Controls.Remove(track.PanDrawSamples);
		track.PanDrawSamples.Dispose();
		track.Dispose();

		if (trackManager.Tracks.Count > 0)
			SelectTrack(GetSelectedTrack());

		UpdateSizes();
	}

	private Panel CreateTrackPanel(Track track)
	{
		Panel pan = new DrawingPanel
		{
			BackColor = Color.DimGray,
			BorderStyle = BorderStyle.None
		};

		pan.Click += (sender, e) => SelectTrack(track);
		pan.MouseDown += (sender, e) =>
		{
			SelectTrack(track);
			selStart = selEnd = e.X + firstVisiblePixel;
			pan.Invalidate();
		};
		pan.MouseMove += (sender, e) =>
		{
			if (e.Button == MouseButtons.Left)
			{
				selEnd = e.X + firstVisiblePixel;
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
			var saved = e.Graphics.Save();

			// Move the waveform by the same amount as the ruler.
			e.Graphics.TranslateTransform(-firstVisiblePixel, 0);
			track.DrawSamples(e.Graphics, trackManager.DrawTimescale);
			if (track == GetSelectedTrack() && selStart != selEnd)
			track.ShowSelection(e.Graphics, Math.Min(selStart, selEnd), Math.Max(selStart, selEnd));

			e.Graphics.Restore(saved);
		};

		panTracks.Controls.Add(pan);
		track.LinkPanels(pan, panTracks);

		return pan;
	}

	private void menuEditTrim_Click(object sender, EventArgs e)
	{
		Track track = GetSelectedTrack();
		if (track == null || selStart == selEnd) return;

		track.Trim(Math.Min(selStart, selEnd), Math.Max(selStart, selEnd), trackManager.DrawTimescale);
		AfterEdit(track);
	}

	private void menuEditDeleteSel_Click(object sender, EventArgs e)
	{
		Track track = GetSelectedTrack();
		if (track == null || selStart == selEnd) return;

		track.DeletePart(Math.Min(selStart, selEnd), Math.Max(selStart, selEnd), trackManager.DrawTimescale);
		AfterEdit(track);
	}

	private void AfterEdit(Track track)
	{
		selStart = selEnd = 0;                 // clear the selection
		if (playbackTrack == track)            // stop old audio so Play uses the edited audio
		{
			playbackTrack.Dispose();
			playbackTrack = null;
		}
		track.PanDrawSamples.Invalidate();
	}

	private void UpdateSizes()
	{
		// SizeChanged can run before Setup has finished.
		if (trackManager == null ||
			panTimeRuler == null ||
			lblZoom == null)
		{
			return;
		}

		int padding = 15 * DeviceDpi / 96;
		int width = Math.Max(1, ClientSize.Width - padding * 2);

		lblZoom.Left = padding;
		lblZoom.Top = trkDrawTimescale.Top + 6 * DeviceDpi / 96;

		trkDrawTimescale.Left = lblZoom.Right + 8;
		trkDrawTimescale.Width = Math.Min(
			250 * DeviceDpi / 96,
			Math.Max(1,
				ClientSize.Width - padding - trkDrawTimescale.Left));

		panTimeRuler.SetBounds(
			padding,
			trkDrawTimescale.Bottom + 4,
			width,
			36 * DeviceDpi / 96);

		btnPlay.Left =
			Math.Max(0, (ClientSize.Width - btnPlay.Width) / 2);

		btnPlay.Top = Math.Max(
			panTimeRuler.Bottom + 50,
			ClientSize.Height - btnPlay.Height - padding);

		panTracks.SetBounds(
			padding,
			panTimeRuler.Bottom,
			width,
			Math.Max(1,
				btnPlay.Top - padding - panTimeRuler.Bottom));

		int count = trackManager.Tracks.Count;

		for (int i = 0; i < count; i++)
		{
			int top = i * panTracks.ClientSize.Height / count;
			int bottom = (i + 1) * panTracks.ClientSize.Height / count;

			Panel panel = trackManager.Tracks[i].PanDrawSamples;

			panel.SetBounds(0,top,panTracks.ClientSize.Width,Math.Max(1, bottom - top - 1));
			panel.Invalidate();
		}

		lblPlayhead.Height = panTracks.ClientSize.Height;
		panTimeRuler.BringToFront();

		UpdatePlaybackView();
	}

	private void DrawTimeRuler(object sender, PaintEventArgs e)
	{
		if (panTimeRuler.Width < 20)
			return;
		Track track = playbackTrack ?? GetSelectedTrack();
		int valuesPerSecond = 44100;
		if (track != null && sampleRates.TryGetValue(track, out int rate))
			valuesPerSecond = rate;

		double pixelsPerSecond = (double)valuesPerSecond
			/ Math.Max(1, trackManager.DrawTimescale);

		// Choose readable spacing between time labels.
		double[] intervals =
		{
			0.001, 0.002, 0.005, 0.01, 0.02, 0.05,
			0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30,
			60, 120, 300, 600, 1800, 3600
		};

		double majorStep = 3600;
		foreach (double interval in intervals)
		{
			if (interval * pixelsPerSecond >= 90 * DeviceDpi / 96.0)
			{
				majorStep = interval;
				break;
			}
		}

		double minorStep = majorStep / 5;
		double tickSpacing = minorStep * pixelsPerSecond;

		// Match the waveform's origin, including the container border.
		int originX = panTimeRuler.PointToClient(panTracks.PointToScreen(Point.Empty)).X - firstVisiblePixel;
		int bottom = panTimeRuler.Height - 1;
		using Pen pen = new(panTimeRuler.ForeColor);
		e.Graphics.DrawLine(pen, 0, bottom, panTimeRuler.Width, bottom);
		int firstTick = (int)Math.Max(0, Math.Ceiling(-originX / tickSpacing));
		for (int i = firstTick; ; i++)
		{
			double seconds = i * minorStep;
			float x = (float)(originX + seconds * pixelsPerSecond);

			if (x >= panTimeRuler.Width)
				break;

			bool major = i % 5 == 0;
			e.Graphics.DrawLine(pen, x, bottom, x, bottom - (major ? 12 : 6));

			if (!major)
				continue;

			TimeSpan time =TimeSpan.FromSeconds(Math.Round(seconds, 3));

			string label = time.TotalHours >= 1
				? ((int)time.TotalHours) + time.ToString(@"\:mm\:ss")
				: time.ToString(@"m\:ss");

			if (majorStep < 1)
				label += time.ToString(@"\.fff");

			Size size = TextRenderer.MeasureText(
				label,panTimeRuler.Font,Size.Empty,TextFormatFlags.NoPadding);

			int labelX = Math.Max(0, (int)x - size.Width / 2);

			if (labelX + size.Width <= panTimeRuler.Width)
			{
				TextRenderer.DrawText(e.Graphics,label,panTimeRuler.Font,new Point(labelX, 2),panTimeRuler.ForeColor,TextFormatFlags.NoPadding);
			}
		}

		if (!lblPlayhead.Visible)
			return;

		// Draw the blue triangle directly above the vertical playback line.
		int markerX = panTimeRuler.PointToClient(
			lblPlayhead.PointToScreen(Point.Empty)).X;

		int sizeArrow = 7 * DeviceDpi / 96;

		Point[] arrow =
		{
			new Point(markerX - sizeArrow, bottom - sizeArrow),
			new Point(markerX + sizeArrow, bottom - sizeArrow),
			new Point(markerX, bottom)
		};
		e.Graphics.FillPolygon(Brushes.DodgerBlue, arrow);
	}

	private void FrmMain_Load(object sender, EventArgs e)
	{
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		tmrUpdate.Stop();
		foreach (Track track in trackManager.Tracks)
			track.Dispose();
		base.OnFormClosed(e);
	}
}