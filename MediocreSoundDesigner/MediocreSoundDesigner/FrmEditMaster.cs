namespace MediocreSoundDesigner;

public partial class FrmEditMaster : Form
{
  protected Track track;

  public FrmEditMaster()
  {
    InitializeComponent();
  }

  // runs when any dialog opens: gives it the current theme
  protected override void OnLoad(EventArgs e)
  {
    StyleDialog(this);
    base.OnLoad(e);
  }

  public virtual void SetSound(Track sound)
  {
    track = sound;
    Init();
  }

  protected void ApplySettings()
  {
    BackColor = Settings.Color.WinBg;
    ForeColor = Settings.Color.WinFg;
  }

  public void btnCancel_Click(object sender, EventArgs e)
  {
    Cancel();
    DialogResult = DialogResult.Cancel;
    Close();
  }

  public void btnApply_Click(object sender, EventArgs e)
  {
    Apply();
    track?.PanDrawSamples?.Invalidate();
    DialogResult = DialogResult.OK;
    Close();
  }

  protected virtual void Init()
  {
  }

  protected virtual void Apply() { }

  protected virtual void Cancel() { }

  // Gives a dialog the dark or light look. Layout stays as the Designer made it.
  public static void StyleDialog(Form form, int minHeight = 400)
  {
    _ = minHeight;   // kept for compatibility

    // dark or light? The main window decides this when D or L is pressed
    bool dark = Settings.Color.WinBg.GetBrightness() < 0.5f;
    Color windowBack = dark ? Color.Black : Color.White;
    Color labelText = dark ? Color.FromArgb(255, 255, 192) : Color.Black;
    Color boxBack = dark ? Color.DimGray : Color.Gainsboro;
    Color boxText = dark ? Color.White : Color.Black;
    Color border = dark ? Color.White : Color.Gray;

    form.FormBorderStyle = FormBorderStyle.FixedDialog;
    form.MaximizeBox = false;
    form.MinimizeBox = false;
    form.BackColor = windowBack;
    form.ForeColor = labelText;

    StyleControls(form.Controls, windowBack, labelText, boxBack, boxText, border);
  }

  // colours every control, and the controls inside panels and group boxes too
  private static void StyleControls(Control.ControlCollection controls,
    Color windowBack, Color labelText, Color boxBack, Color boxText, Color border)
  {
    foreach (Control control in controls)
    {
      if (control is Label || control is CheckBox || control is RadioButton)
      {
        control.ForeColor = labelText;
      }
      else if (control is TextBox || control is NumericUpDown || control is ComboBox)
      {
        control.BackColor = boxBack;
        control.ForeColor = boxText;
      }
      else if (control is Button button && button.Text != "")     // buttons with no text (record icon, colour boxes) stay as they are
      {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = border;
        button.ForeColor = Color.White;
        button.BackColor = button.Text switch
        {
          "Cancel" => Color.Firebrick,                            // red in both themes
          "Apply" or "OK" or "Create" or "Yes" => Color.SeaGreen, // green in both themes
          _ => boxBack                                            // other buttons follow the theme
        };
        if (button.BackColor == boxBack) button.ForeColor = boxText;
      }
      else if ((control is Panel || control is GroupBox) && control.Controls.Count > 0)
      {
        control.BackColor = windowBack;                           // a panel that holds other controls
        control.ForeColor = labelText;
      }

      StyleControls(control.Controls, windowBack, labelText, boxBack, boxText, border);
    }
  }
}

// A message box with the same look (replaces MessageBox.Show)
public static class Popup
{
  public static DialogResult Show(
      string text,
      string title = "Mediocre Sound Designer",
      MessageBoxButtons buttons = MessageBoxButtons.OK,
      MessageBoxIcon icon = MessageBoxIcon.None)
  {
    Form form = new();
    form.Text = title;
    form.ClientSize = new Size(520, 250);
    form.StartPosition = FormStartPosition.CenterScreen;
    form.Font = new Font("Segoe UI", 12);

    Label label = new();
    label.Text = text;
    label.Size = new Size(440, 130);
    label.Location = new Point(40, 30);
    form.Controls.Add(label);

    string[] names = buttons == MessageBoxButtons.OK
        ? new[] { "OK" }
        : new[] { "Yes", "No", "Cancel" };

    for (int i = 0; i < names.Length; i++)
    {
      Button button = new();
      button.Text = names[i];
      button.DialogResult = Enum.Parse<DialogResult>(names[i]);
      button.Size = new Size(110, 42);
      button.Location = new Point(270 - names.Length * 75 + i * 150, 180);
      form.Controls.Add(button);
    }

    FrmEditMaster.StyleDialog(form, 250);
    DialogResult result = form.ShowDialog();
    form.Dispose();
    return result;
  }

  // same thing, for calls that pass "this" first
  public static DialogResult Show(
      IWin32Window owner,
      string text,
      string title = "Mediocre Sound Designer",
      MessageBoxButtons buttons = MessageBoxButtons.OK,
      MessageBoxIcon icon = MessageBoxIcon.None)
  {
    return Show(text, title, buttons, icon);
  }
}