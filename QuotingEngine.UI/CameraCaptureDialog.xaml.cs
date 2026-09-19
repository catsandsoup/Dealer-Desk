using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Helpers;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;

namespace QuotingEngine.UI;

public sealed partial class CameraCaptureDialog : ContentDialog
{
    private CameraHelper? _cameraHelper;
    private SoftwareBitmapSource? _imageSource;
    public byte[]? CapturedPhotoBytes { get; private set; }

    public CameraCaptureDialog()
    {
        this.InitializeComponent();
        _ = InitializeCameraAsync();
    }

    private async Task InitializeCameraAsync()
    {
        try
        {
            _cameraHelper = new CameraHelper();
            var result = await _cameraHelper.InitializeAndStartCaptureAsync();
            if (result == CameraHelperResult.Success)
            {
                _imageSource = new SoftwareBitmapSource();
                CameraPreview.Source = _imageSource;
                _cameraHelper.FrameArrived += CameraHelper_FrameArrived;
            }
            else
            {
                ErrorText.Text = $"Camera initialization failed: {result}";
                ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                IsPrimaryButtonEnabled = false;
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Error: {ex.Message}";
            ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            IsPrimaryButtonEnabled = false;
        }
    }

    private async void CameraHelper_FrameArrived(object? sender, FrameEventArgs e)
    {
        var softwareBitmap = e.VideoFrame?.SoftwareBitmap;
        if (softwareBitmap != null)
        {
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var localBitmap = softwareBitmap; // copy for lambda
            DispatcherQueue.TryEnqueue(async () =>
            {
                if (_imageSource != null)
                {
                    await _imageSource.SetBitmapAsync(localBitmap);
                }
            });
        }
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            if (_cameraHelper != null)
            {
                _cameraHelper.FrameArrived -= CameraHelper_FrameArrived;
                
                var tcs = new TaskCompletionSource<SoftwareBitmap?>();
                
                void Handler(object? s, FrameEventArgs e)
                {
                    _cameraHelper.FrameArrived -= Handler;
                    tcs.TrySetResult(e.VideoFrame?.SoftwareBitmap);
                }
                
                _cameraHelper.FrameArrived += Handler;
                
                var timeoutTask = Task.Delay(2000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == tcs.Task)
                {
                    var bitmap = await tcs.Task;
                    if (bitmap != null)
                    {
                        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        encoder.SetSoftwareBitmap(bitmap);
                        await encoder.FlushAsync();
                        
                        CapturedPhotoBytes = new byte[stream.Size];
                        using var reader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));
                        await reader.LoadAsync((uint)stream.Size);
                        reader.ReadBytes(CapturedPhotoBytes);
                    }
                }
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Just cancel
    }

    private async void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (_cameraHelper != null)
        {
            _cameraHelper.FrameArrived -= CameraHelper_FrameArrived;
            await _cameraHelper.CleanUpAsync();
            _cameraHelper = null;
        }
        
        if (_imageSource != null)
        {
            _imageSource.Dispose();
            _imageSource = null;
        }
    }
}
