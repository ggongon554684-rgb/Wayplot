using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class PhotoViewerDialog : Form
{
    private readonly IReadOnlyList<ParcelPhotoThumbnailDto> _photos;
    private readonly Func<Guid, Task<ServiceResult<ParcelPhotoImageDto>>> _loadImage;
    private int _index;
    private bool _loading;

    public PhotoViewerDialog(
        IReadOnlyList<ParcelPhotoThumbnailDto> photos,
        int initialIndex,
        Func<Guid, Task<ServiceResult<ParcelPhotoImageDto>>> loadImage)
    {
        _photos = photos;
        _index = initialIndex;
        _loadImage = loadImage;
        InitializeComponent();
        ApplyTheme();

        btnPrevious.Click += async (_, _) =>
        {
            if (_index > 0)
            {
                _index--;
                await LoadCurrentPhotoAsync();
            }
        };
        btnNext.Click += async (_, _) =>
        {
            if (_index < _photos.Count - 1)
            {
                _index++;
                await LoadCurrentPhotoAsync();
            }
        };
        btnClose.Click += (_, _) => Close();
        Shown += async (_, _) => await LoadCurrentPhotoAsync();
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblFileName.Font = Theme.BodyBold;
        lblFileName.ForeColor = Theme.Navy;
        lblStatus.ForeColor = Theme.Muted;
        picturePhoto.BackColor = Theme.Surface;
        ButtonStyle.Apply(btnPrevious, primary: false);
        ButtonStyle.Apply(btnNext, primary: false);
        ButtonStyle.Apply(btnClose, primary: true);
    }

    private async Task LoadCurrentPhotoAsync()
    {
        if (_photos.Count == 0 || _loading || IsDisposed)
            return;

        _loading = true;
        lblFileName.Text = $"{_photos[_index].FileName} ({_index + 1} of {_photos.Count})";
        lblStatus.Text = "Loading photo...";
        progressPhoto.Visible = true;
        UpdateNavigation();
        try
        {
            var result = await _loadImage(_photos[_index].Id);
            if (IsDisposed)
                return;
            if (!result.Ok || result.Value is null)
            {
                lblStatus.Text = result.Message;
                return;
            }

            using var stream = new MemoryStream(result.Value.ImageBytes);
            using var decoded = Image.FromStream(stream);
            var next = new Bitmap(decoded);
            var previous = picturePhoto.Image;
            picturePhoto.Image = next;
            previous?.Dispose();
            lblStatus.Text = string.Empty;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                lblStatus.Text = $"Could not load photo. {ex.Message}";
        }
        finally
        {
            _loading = false;
            if (!IsDisposed)
            {
                progressPhoto.Visible = false;
                UpdateNavigation();
            }
        }
    }

    private void UpdateNavigation()
    {
        btnPrevious.Enabled = !_loading && _index > 0;
        btnNext.Enabled = !_loading && _index < _photos.Count - 1;
    }

    private void DisposeViewerImage()
    {
        picturePhoto.Image?.Dispose();
        picturePhoto.Image = null;
    }
}
