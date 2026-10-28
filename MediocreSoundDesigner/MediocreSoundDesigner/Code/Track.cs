//reference - https://learn.microsoft.com/en-us/dotnet/api/system.drawing.solidbrush?view=net-11.0-pp


using NAudio.Wave;
namespace MediocreSoundDesigner;
public enum SampleRegion
{
	BEGINNING_ONLY,
	END_ONLY,
	BEGINNING_AND_END,
	ANYWHERE,
}
public class Track : IDisposable
{
	// Fields
	private string file;
	private TimeSpan duration;
	private float offset;
	private float timeScale;
	private int sampleRate;
	private float[] samples;
	private float pitch;
	private int bits;
	private int channels;
	private AudioFileReader fileReader;
	private WaveOut outputDevice;
	private Panel panContainer;
	private int drawTimescale;

	// consts
	private static readonly Color SAMPLES_DRAW_COLOR = Color.Cyan;
	private static readonly Color SAMPLES_DRAW_COLOR_DISABLED = Color.LightGray;

	// properties
	public int DrawOffset { get; set; }
	public bool IsDisabled { get; private set; }
	public Panel PanDrawSamples { get; private set; }
	public float Volume { get; private set; }

	private string tempFilePath =>
		Path.Combine(Path.GetTempPath(), "MediocreSoundDesigner","temp.wav");
	public PlaybackState State => outputDevice?.PlaybackState ?? PlaybackState.Stopped;
	public int SampleValuesPerSecond
	{
		get
		{
			WaveFormat format = fileReader?.WaveFormat ?? new WaveFormat();
				return format.SampleRate * format.Channels;
		}
	}

	// Ctor
	public Track(string file)
	{
		fileReader = new AudioFileReader(file);
		int sampleCount = (int)(fileReader.Length / 4);
		samples = new float[sampleCount];
		int samplesRead = ((ISampleProvider)fileReader).Read(samples);

		if (samplesRead < sampleCount)
			Array.Resize(ref samples, samplesRead);

		this.file = file;
		duration = fileReader.TotalTime;
		offset = 0;
		timeScale = 1.0f;
		Volume = 1.0f;
		pitch = 1.0f;
		bits = fileReader.WaveFormat.BitsPerSample;
		channels = fileReader.WaveFormat.Channels;
		sampleRate = fileReader.WaveFormat.SampleRate;
		IsDisabled = false;
	}

	public Track(float[] samples)
	{
		this.samples = samples;

		file = "";
		duration = TimeSpan.Zero;
		offset = 0;
		timeScale = 1.0f;
		Volume = 1.0f;
		pitch = 1.0f;
		bits = 32;
		channels = 1;
		sampleRate = 0;
		IsDisabled = false;
	}

	public void LinkPanels(Panel panDrawSamples, Panel panContainer)
	{
		PanDrawSamples = panDrawSamples;
		this.panContainer = panContainer;
	}

	// Editing
	public float[] CloneSamples()
	{
		return (float[])samples.Clone();
	}

	public void ReplaceSamples(float[] samples)
	{
		this.samples = samples;
	}

	public void Reverse()
	{
		for (int i = 0; i < samples.Length / 2; i++)
		{
			var temp = samples[i];
			samples[i] = samples[samples.Length - 1 - i];
			samples[samples.Length - 1 - i] = temp;
		}
	}

	public void Flip()
	{
		for (int i = 0; i < samples.Length; i++)
			samples[i] *= -1;
	}

	// Keep only the part between two pixel positions
	public void Trim(int startPx, int endPx, int scale)
	{
		int start = Math.Clamp((startPx - DrawOffset) * scale, 0, samples.Length);
		int end = Math.Clamp((endPx - DrawOffset) * scale, 0, samples.Length);
		start -= start % 2;   // stay on a left/right pair so channels don't swap
		end -= end % 2;
		if (end <= start) return;

		samples = samples[start..end];
		DrawOffset = Math.Max(DrawOffset, startPx);   // stays where it was
	}

	// Remove the part between two pixel positions
	public void DeletePart(int startPx, int endPx, int scale)
	{
		int start = Math.Clamp((startPx - DrawOffset) * scale, 0, samples.Length);
		int end = Math.Clamp((endPx - DrawOffset) * scale, 0, samples.Length);
		start -= start % 2;
		end -= end % 2;
		if (end <= start) return;

		samples = [.. samples[..start], .. samples[end..]];
	}

	// Draw a blue box over the selected part
	public void ShowSelection(Graphics g, int startPx, int endPx)
	{
		using Brush brush = new SolidBrush(Color.FromArgb(35, 65, 55, 135));
		g.FillRectangle(brush, startPx, 0, endPx - startPx, PanDrawSamples.Height);
	}

	public void AlterTimeScale(float scale)
	{
	}

	public void SetVolume(float newVolume)
	{
		Volume = newVolume;
	}

	public void EliminateSilence(
		SampleRegion region,
		float minDuration,
		float thres)
	{
		float[] newSamples = CloneSamples();

		if (region is SampleRegion.BEGINNING_ONLY
			or SampleRegion.BEGINNING_AND_END)
		{
			int silentDurationInNumSamples = 0;

			for (int i = 0; i < newSamples.Length; i++)
			{
				if (Math.Abs(newSamples[i]) <= thres)
					silentDurationInNumSamples++;
				else
					break;
			}

			if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
			{
				newSamples = newSamples[silentDurationInNumSamples..];
			}
		}

		if (region is SampleRegion.END_ONLY
			or SampleRegion.BEGINNING_AND_END)
		{
			int silentDurationInNumSamples = 0;

			for (int i = newSamples.Length - 1; i >= 0; i--)
			{
				if (Math.Abs(newSamples[i]) <= thres)
					silentDurationInNumSamples++;
				else
					break;
			}

			if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
			{
				newSamples = newSamples[0..^silentDurationInNumSamples];
			}
		}

		if (region is SampleRegion.ANYWHERE)
		{
			int silentDurationInNumSamples = 0;
			int silentRegionStart = 0;
			int i;

			for (i = 0; i < newSamples.Length; i++)
			{
				if (Math.Abs(newSamples[i]) <= thres)
				{
					silentDurationInNumSamples++;
				}
				else
				{
					if ((float)silentDurationInNumSamples / sampleRate>= minDuration)
					{
						newSamples = [..newSamples[0..silentRegionStart],..newSamples[i..]];
						i = 0;
					}

					silentRegionStart = i + 1;
					silentDurationInNumSamples = 0;
				}
			}

			if ((float)silentDurationInNumSamples / sampleRate >= minDuration)
			{
				newSamples = [..newSamples[0..silentRegionStart],	..newSamples[i..]];
			}
		}

		samples = newSamples;
	}

	// play
	public void Play()
	{
		// Resume the same player when playback is paused.
		if (State == PlaybackState.Paused)
		{
			outputDevice.Play();
			return;
		}

		// Start a new playback from the beginning.
		outputDevice?.Dispose();
		fileReader?.Dispose();
		SaveToTempFile();
		fileReader = new AudioFileReader(tempFilePath);
		outputDevice = new WaveOut();
		outputDevice.Init(fileReader);
		outputDevice.Play();
	}

	public void Pause()
	{
		outputDevice?.Pause();
	}

	public void Reset()
	{
	}

	public int GetPlayheadPos()
	{
		if (outputDevice == null || fileReader == null)
			return 0;

		// Find the total number of sample values in the playback file.
		int readerBytesPerValue =
			fileReader.WaveFormat.BitsPerSample / 8;

		long totalValues =
			fileReader.Length / readerBytesPerValue;

		// Leave the marker at the end after playback finishes.
		if (State == PlaybackState.Stopped &&
			fileReader.Position >= fileReader.Length)
		{
			return (int)totalValues;
		}

		// Convert the audio device's byte position into sample values.
		int outputBytesPerValue =
			outputDevice.OutputWaveFormat.BitsPerSample / 8;

		long playedValues =
			outputDevice.GetPosition() / outputBytesPerValue;

		return (int)Math.Clamp(playedValues, 0, totalValues);
	}

	public double GetPlaybackProgress()
	{
		double totalSeconds = GetDurationSeconds();

		if (totalSeconds <= 0)
			return 0;

		return Math.Clamp(
			GetPlaybackSeconds() / totalSeconds,
			0,
			1);
	}

	public double GetPlaybackSeconds()
	{
		if (outputDevice == null || fileReader == null)
			return 0;

		// Use the same position as the ruler's playback marker.
		double seconds =
			GetPlayheadPos() / (double)SampleValuesPerSecond;

		return Math.Min(seconds, GetDurationSeconds());
	}

	public double GetDurationSeconds()
	{
		return fileReader?.TotalTime.TotalSeconds ?? 0;
	}

	public void ToggleDisabled()
	{
		IsDisabled = !IsDisabled;
	}

	// File operations
	private void SaveToTempFile()
	{
		Directory.CreateDirectory(
			Path.GetDirectoryName(tempFilePath));

		float[] adjustedSamples = GetAdjustedSamples();

		using var writer = new WaveFileWriter(
			tempFilePath,
			fileReader?.WaveFormat ?? new WaveFormat());

		// Include the leading silence and the complete shifted audio.
		writer.WriteSamples(
			adjustedSamples,
			0,
			adjustedSamples.Length);
	}

	public void SaveToFile(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));

		using var writer = new WaveFileWriter(
			path,
			fileReader?.WaveFormat ?? new WaveFormat());

		writer.WriteSamples(GetAdjustedSamples(), 0, samples.Length);
	}

	public float[] GetAdjustedSamples()
	{
		float[] adjSamples = new float[samples.Length];

		for (int s = 0; s < samples.Length; s++)
			adjSamples[s] = samples[s] * Volume;

		float[] deadSpace = new float[DrawOffset * drawTimescale];

		for (int i = 0; i < DrawOffset; i++)
			deadSpace[i] = 0.0f;

		return [.. deadSpace, .. adjSamples];
	}

	// Rendering
	public void ShiftSampleOver(int amount)
	{
		DrawOffset += amount;
		DrawOffset = Math.Max(0, DrawOffset);
	}

	public void DrawSamples(Graphics g, int drawTimescale)
	{
		if (PanDrawSamples is null)
			return;
		this.drawTimescale = drawTimescale;
		float heightAdj = PanDrawSamples.Height / 2;
		if (samples != null && samples.Length > 0)
		{
			int x = DrawOffset;
			Pen pen = new(IsDisabled? SAMPLES_DRAW_COLOR_DISABLED: SAMPLES_DRAW_COLOR);
			List<Point> points = new();
			for (int s = 0; s < samples.Length; s += drawTimescale)
			{
				var sample = (int)(samples[s] * heightAdj * Volume);
				points.Add(new(x, PanDrawSamples.Height / 2));
				points.Add(new(x, PanDrawSamples.Height / 2 - sample));
				x++;
			}
			g.DrawLines(pen, points.ToArray());
			pen.Dispose();
		}
	}

	// Cleanup
	public void Dispose()
	{
		outputDevice?.Dispose();
		fileReader?.Dispose();
	}
}