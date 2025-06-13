using OpenCvSharp;
using StretchTracker.Core;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace StretchTracker.UI
{
    public partial class StretchWindow : System.Windows.Window
    {
        private VideoCapture _capture;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly DatabaseManager _dbManager;
        private DateTime _startTime;
        private int _stretchesDetected = 0;
        private readonly AppSettings _settings;

        // Motion detection parameters
        private Mat _previousFrame;
        private DateTime _lastStretchDetectedTime = DateTime.MinValue;
        private readonly TimeSpan _detectionCooldown = TimeSpan.FromSeconds(2.0);
        private double _motionThreshold = 150000;
        private bool _isCalibrating = true;
        private int _calibrationFrames = 0;
        private readonly int _requiredCalibrationFrames = 30;
        private double _backgroundMotion = 0;
        private bool _isCompleting = false;

        // Additional parameters to reduce sensitivity to small movements
        private int _consecutiveMotionFrames = 0;
        private readonly int _requiredConsecutiveFrames = 3;
        private double _motionSum = 0; // Track cumulative motion
        private readonly double _requiredMotionSum = 400000; // Increased from 300000
        private readonly int _maxMotionFrames = 10; // Maximum frames to consider for a single stretch

        public StretchWindow(DatabaseManager dbManager)
        {
            InitializeComponent();

            _dbManager = dbManager;
            _startTime = DateTime.Now;
            _settings = AppSettings.Load();

            Loaded += StretchWindow_Loaded;
            Closing += StretchWindow_Closing;

            // Set required stretches from settings
            _settings.RequiredStretchCount = Math.Max(3, _settings.RequiredStretchCount);

            // Update UI
            StatusTextBlock.Text = "Starting camera...";
            UpdateProgressUI(0);
        }

        private void StretchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start webcam
            try
            {
                _capture = new VideoCapture(0); // 0 = default camera
                if (!_capture.IsOpened())
                {
                    MessageBox.Show("Unable to access webcam. Please check your camera connection and permissions.",
                        "Camera Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Set resolution 
                _capture.Set(VideoCaptureProperties.FrameWidth, 640);
                _capture.Set(VideoCaptureProperties.FrameHeight, 480);

                StatusTextBlock.Text = "Camera initialized. Please remain still while calibrating...";
                CalibrationOverlay.Visibility = Visibility.Visible;

                // Start processing in a separate thread
                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ProcessFrames(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing camera: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void ProcessFrames(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var frame = new Mat())
                    {
                        if (_capture.Read(frame))
                        {
                            // Skip empty frames
                            if (frame.Empty())
                                continue;

                            // Process frame for motion
                            bool isStretching = await ProcessFrameForMotionAsync(frame);

                            // Convert frame to display it
                            var bitmap = MatToBitmapSource(frame);
                            await Dispatcher.InvokeAsync(() => WebcamPreview.Source = bitmap);

                            // Update the calibration overlay visibility
                            if (_isCalibrating && _calibrationFrames >= _requiredCalibrationFrames)
                            {
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    CalibrationOverlay.Visibility = Visibility.Collapsed;
                                });
                            }

                            // Detect stretching based on motion
                            if (isStretching)
                            {
                                // Only count stretches if we're not in the cooldown period to avoid duplicates
                                if ((DateTime.Now - _lastStretchDetectedTime) > _detectionCooldown && !_isCalibrating)
                                {
                                    _stretchesDetected++;
                                    _lastStretchDetectedTime = DateTime.Now;

                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        UpdateProgressUI(_stretchesDetected);
                                        StatusTextBlock.Text = $"Stretch detected! {_stretchesDetected}/{_settings.RequiredStretchCount}";
                                    });

                                    // Complete when enough stretches are detected
                                    if (_stretchesDetected >= _settings.RequiredStretchCount && !_isCompleting)
                                    {
                                        _isCompleting = true; // Prevent multiple completions

                                        // Record session completion in database
                                        int sessionDuration = (int)(DateTime.Now - _startTime).TotalSeconds;
                                        _dbManager.RecordStretchSession(true, sessionDuration);

                                        // Show success message and close window on the UI thread
                                        await Dispatcher.InvokeAsync(() =>
                                        {
                                            try
                                            {
                                                MessageBox.Show("Great job! Stretching session completed.",
                                                    "Session Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                                                // Use BeginInvoke to close the window after the MessageBox is dismissed
                                                Dispatcher.BeginInvoke(new Action(() =>
                                                {
                                                    try
                                                    {
                                                        Close();
                                                    }
                                                    catch
                                                    {
                                                        // Ignore errors during closing
                                                    }
                                                }));
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"Error showing completion message: {ex.Message}");
                                                // Try to close the window anyway
                                                Dispatcher.BeginInvoke(new Action(() => Close()));
                                            }
                                        });

                                        // Break out of the processing loop
                                        break;
                                    }
                                }
                            }
                        }

                        // Small delay to reduce CPU usage
                        await Task.Delay(30, cancellationToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Normal cancellation, ignore
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusTextBlock.Text = $"Error processing frames: {ex.Message}";
                });
            }
        }

        private void UpdateProgressUI(int currentCount)
        {
            // Update progress bar
            double progressPercentage = (double)currentCount / _settings.RequiredStretchCount * 100;
            ProgressBar.Value = progressPercentage;

            // Update counter text
            CounterText.Text = $"{currentCount}/{_settings.RequiredStretchCount}";

            // Update progress text
            ProgressText.Text = $"Stretch progress: {currentCount} of {_settings.RequiredStretchCount}";
        }

        private async Task<bool> ProcessFrameForMotionAsync(Mat frame)
        {
            try
            {
                // Convert to grayscale for processing
                using (Mat grayFrame = new Mat())
                {
                    Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(grayFrame, grayFrame, new Size(21, 21), 0);

                    if (_previousFrame == null)
                    {
                        _previousFrame = new Mat();
                        grayFrame.CopyTo(_previousFrame);
                        return false;
                    }

                    // Compute difference between current and previous frame
                    using (Mat diff = new Mat())
                    {
                        Cv2.Absdiff(grayFrame, _previousFrame, diff);

                        // Apply higher threshold to ignore small movements (like eye blinking)
                        Cv2.Threshold(diff, diff, 30, 255, ThresholdTypes.Binary);

                        // Apply morphological operations to remove noise and small movements
                        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                        Cv2.Erode(diff, diff, kernel, iterations: 1);
                        Cv2.Dilate(diff, diff, kernel, iterations: 2);

                        // Compute amount of motion (sum of all white pixels)
                        double motionAmount = Cv2.Sum(diff)[0];

                        // Update calibration if needed
                        if (_isCalibrating)
                        {
                            _calibrationFrames++;
                            _backgroundMotion = (_backgroundMotion * (_calibrationFrames - 1) + motionAmount) / _calibrationFrames;

                            if (_calibrationFrames >= _requiredCalibrationFrames)
                            {
                                _isCalibrating = false; // Mark calibration as complete
                                _motionThreshold = Math.Max(150000, _backgroundMotion * 8); // Lowered from 150000 and reduced multiplier from 10

                                await Dispatcher.InvokeAsync(() =>
                                {
                                    CalibrationOverlay.Visibility = Visibility.Collapsed; // Hide calibration overlay
                                    StatusTextBlock.Text = "Calibration complete. Make BIG stretching movements!";
                                });
                            }
                            else
                            {
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    StatusTextBlock.Text = $"Calibrating... {_calibrationFrames}/{_requiredCalibrationFrames}";
                                });
                            }
                        }


                        // Visualize motion for debugging
                        Cv2.PutText(frame, $"Motion: {motionAmount:F0}", new Point(10, 30),
                                   HersheyFonts.HersheySimplex, 1, new Scalar(0, 255, 0), 2);

                        // Draw threshold
                        Cv2.PutText(frame, $"Threshold: {_motionThreshold:F0}", new Point(10, 60),
                                   HersheyFonts.HersheySimplex, 1, new Scalar(0, 255, 0), 2);

                        // Update the previous frame
                        grayFrame.CopyTo(_previousFrame);

                        // Check if motion exceeds threshold
                        bool currentFrameHasMotion = motionAmount > _motionThreshold && !_isCalibrating;

                        // Track consecutive frames with motion and accumulate motion
                        if (currentFrameHasMotion)
                        {
                            _consecutiveMotionFrames++;
                            _motionSum += motionAmount;

                            // Draw active stretching indicator
                            Cv2.PutText(frame, $"Motion: {_consecutiveMotionFrames}/{_requiredConsecutiveFrames}",
                                new Point(10, 90), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
                            Cv2.PutText(frame, $"Total Motion: {_motionSum:F0}/{_requiredMotionSum:F0}",
                                new Point(10, 120), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
                        }
                        else
                        {
                            // Reset if we've exceeded max frames or if motion stops
                            if (_consecutiveMotionFrames >= _maxMotionFrames)
                            {
                                _consecutiveMotionFrames = 0;
                                _motionSum = 0;
                            }
                            else if (_consecutiveMotionFrames > 0)
                            {
                                // Allow a small gap in motion (1-2 frames)
                                _consecutiveMotionFrames--;
                            }
                        }

                        // Only consider it stretching if we have enough consecutive frames AND enough total motion
                        bool isStretching = _consecutiveMotionFrames >= _requiredConsecutiveFrames && 
                                          _motionSum >= _requiredMotionSum;

                        // Reset motion tracking if we've detected a stretch
                        if (isStretching)
                        {
                            _consecutiveMotionFrames = 0;
                            _motionSum = 0;
                        }

                        // Visualize detection status
                        if (isStretching)
                        {
                            Cv2.PutText(frame, "STRETCHING DETECTED", new Point(frame.Width / 2 - 150, frame.Height - 30),
                                       HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
                        }

                        // Draw a border around the frame when calibrating
                        if (_isCalibrating)
                        {
                            Cv2.Rectangle(frame, new Point(0, 0), new Point(frame.Width - 1, frame.Height - 1),
                                         new Scalar(255, 0, 0), 3);
                        }

                        return isStretching;
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusTextBlock.Text = $"Motion detection error: {ex.Message}";
                });
                return false;
            }
        }

        // Custom conversion from OpenCV Mat to WPF BitmapSource
        private BitmapSource MatToBitmapSource(Mat image)
        {
            try
            {
                // Fallback method using MemoryStream and BitmapImage
                byte[] imageData;
                Cv2.ImEncode(".bmp", image, out imageData);

                using (var ms = new MemoryStream(imageData))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // Important for cross-thread access
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusTextBlock.Text = $"Image conversion error: {ex.Message}";
                });

                // If all else fails, return a blank image
                return BitmapSource.Create(
                    1, 1, 96, 96, PixelFormats.Rgb24, null,
                    new byte[3] { 0, 0, 0 }, 3);
            }
        }

        private void StretchWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Clean up resources
                _cancellationTokenSource?.Cancel();
                _capture?.Dispose();
                _previousFrame?.Dispose();

                // If window is closed before completion, record as incomplete
                // but only if we're not already in completion mode
                if (_stretchesDetected < _settings.RequiredStretchCount && !_isCompleting)
                {
                    int sessionDuration = (int)(DateTime.Now - _startTime).TotalSeconds;
                    _dbManager.RecordStretchSession(false, sessionDuration);
                }
            }
            catch
            {
                // Ignore errors during closing
            }
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set completing flag to avoid duplicate recordings
                _isCompleting = true;

                // Record as skipped
                int sessionDuration = (int)(DateTime.Now - _startTime).TotalSeconds;
                _dbManager.RecordStretchSession(false, sessionDuration);

                // Close window
                Close();
            }
            catch
            {
                // Force close even if there's an error
                Close();
            }
        }
    }
}