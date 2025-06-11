---
layout: default
title: Developer Guide
nav_order: 5
---

# Developer Guide

This section provides technical information for developers interested in understanding or contributing to the Stretch Tracker App.

## Project Structure

The application follows a modular structure:

### Core Components

- **Core/AppSettings.cs**: Manages user preferences and application configuration
- **Core/DatabaseManager.cs**: Handles SQLite database operations for persistent storage
- **Core/NotificationManager.cs**: Controls timed notifications and user interactions

### User Interface

- **MainWindow.xaml**: Main application window with overview and quick actions
- **StretchWindow.xaml**: Camera interface for detecting and recording stretches
- **StatsWindow.xaml**: Statistics and data visualization dashboard
- **SettingsWindow.xaml**: User configuration interface

## Movement Detection Architecture

### OpenCvSharp4 Implementation

The app currently uses **OpenCvSharp4** (version 4.9.0) for its primary motion detection functionality. This implementation is found in `StretchWindow.xaml.cs` and follows these key steps:

#### Camera Initialization
```csharp
// Initialize camera in a reliable way
_capture = new VideoCapture(0); // 0 = default camera
if (!_capture.IsOpened())
{
    MessageBox.Show("Unable to access webcam. Please check your camera connection and permissions.",
        "Camera Error", MessageBoxButton.OK, MessageBoxImage.Error);
    Close();
    return;
}

// Set resolution for consistent performance
_capture.Set(VideoCaptureProperties.FrameWidth, 640);
_capture.Set(VideoCaptureProperties.FrameHeight, 480);
```

#### Frame Processing Loop
```csharp
// Process frames asynchronously
private async void ProcessFrames(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        using (var frame = new Mat())
        {
            if (_capture.Read(frame))
            {
                // Process frame for motion
                bool isStretching = await ProcessFrameForMotionAsync(frame);
                
                // Update UI based on motion detection
                // ...
            }
        }
        
        // Small delay to reduce CPU usage
        await Task.Delay(30, cancellationToken);
    }
}
```

#### Optimizing Performance

For optimal performance, consider these approaches:

1. **Resolution Management**: Lower resolution (640x480) provides sufficient detail while maintaining performance
2. **Frame Rate Control**: Inserting delays between frame processing reduces CPU load
3. **Cancellation Support**: Using CancellationToken allows clean shutdown
4. **Resource Management**: Proper disposal of OpenCV resources is critical using `using` blocks
5. **UI Thread Management**: Always update UI elements via Dispatcher.InvokeAsync

### Key Data Structures

The motion detection system uses several important instance variables:

```csharp
// Motion detection parameters
private Mat _previousFrame;  // Stores the previous frame for comparison
private DateTime _lastStretchDetectedTime;  // Cooldown mechanism to prevent multiple counts
private double _motionThreshold;  // Dynamic threshold for motion detection
private bool _isCalibrating;  // Flag for calibration phase
private double _backgroundMotion;  // Baseline motion in environment
private int _consecutiveMotionFrames;  // Counter for sustained motion detection
private readonly int _requiredConsecutiveFrames;  // Required consecutive frames with motion
```

## TensorFlow Integration

### Current Implementation

The app currently includes TensorFlow.NET as a dependency and is in the process of enhancing the motion detection with pose estimation capabilities.

#### TensorFlow Dependencies

```xml
<ItemGroup>
  <PackageReference Include="TensorFlow.NET" Version="0.150.0" />
  <PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.16.0" />
</ItemGroup>
```

### Implementing Pose Detection

To expand the app's stretching detection capabilities:

1. **Model Selection**: We use MoveNet, a lightweight but effective pose estimation model
2. **Model Integration**: The TensorFlow model needs to be loaded at application startup
3. **Frame Processing**: Each webcam frame is processed through the pose detection model
4. **Pose Analysis**: The detected key points are analyzed to identify specific stretches

#### Planned Implementation

```csharp
// TensorFlow session and model
private TF.Session _tfSession;
private bool _tensorflowEnabled = false;

// Load TensorFlow model
private void InitializeTensorFlow()
{
    try
    {
        // Try to load the model
        _tfSession = LoadPoseDetectionModel();
        _tensorflowEnabled = true;
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"TensorFlow initialization failed: {ex.Message}");
        _tensorflowEnabled = false;
    }
}

// Process frame with TensorFlow if available
private async Task<bool> ProcessFrameWithTensorFlowAsync(Mat frame)
{
    if (!_tensorflowEnabled || _tfSession == null)
        return ProcessFrameForMotionAsync(frame); // Fall back to OpenCV

    // Convert frame to format expected by TensorFlow
    var tensor = ImageToTensor(frame);
    
    // Run inference
    var result = _tfSession.Run(
        new[] { _tfSession.Graph["input_tensor"][0] },
        new[] { tensor },
        new[] { _tfSession.Graph["output_tensor"][0] }
    );

    // Process pose keypoints to determine if stretching
    bool isStretching = AnalyzePoseForStretching(result);
    
    // Visualize keypoints on frame
    if (isStretching)
        DrawPoseOnFrame(frame, result);
        
    return isStretching;
}
```

### Implementation Roadmap

For developers looking to contribute to the TensorFlow integration, here are key areas to focus on:

1. **Model Integration**:
   - Implement model loading code in `StretchWindow.xaml.cs`
   - Add error handling for model load failures

2. **Frame Processing Pipeline**:
   - Create frame conversion utilities (`ImageToTensor`) to transform OpenCV Mat to TensorFlow tensor format
   - Implement efficient tensor conversion methods to minimize performance impact

3. **Pose Analysis Algorithm**:
   - Implement `AnalyzePoseForStretching` for analyzing keypoints to determine stretch movements
   - Define a `PoseData` class to encapsulate keypoint data and pose information

4. **UI Integration**:
   - Create visualization tools (`DrawPoseOnFrame`) to show keypoint tracking on the webcam feed
   - Add user feedback on pose quality and correctness

### Pose Classification Logic

The app uses the following approach to classify stretching movements:

1. **Keypoint Tracking**: Monitor the movement of key body points (shoulders, arms, etc.)
2. **Movement Analysis**: Calculate the displacement of joints between frames
3. **Pose Recognition**: Compare detected poses against known stretching patterns
4. **Threshold Application**: Apply configurable thresholds to determine valid stretches

## Testing the Movement Detection

To effectively test and improve the movement detection, follow these steps:

1. **Calibration Testing**:
   ```csharp
   [TestMethod]
   public void CalibrationTest()
   {
       // Initialize motion detector with mock camera feed
       var motionDetector = new MotionDetector();
       
       // Process calibration frames
       for (int i = 0; i < 30; i++)
       {
           var frame = GetTestFrame("calibration_" + i);
           motionDetector.ProcessFrame(frame);
       }
       
       // Check calibration results
       Assert.IsTrue(motionDetector.IsCalibrated);
       Assert.IsTrue(motionDetector.MotionThreshold > 0);
   }
   ```

2. **False Positive Testing**:
   - Test with static scenes where no motion should be detected
   - Test with minimal movements that shouldn't trigger detection
   
3. **True Positive Testing**:
   - Test with various stretching motions
   - Test under different lighting conditions

## Contributing

To contribute to the TensorFlow integration:

1. **Model Training**: Help train the pose detection model with more stretching examples
2. **Pose Classification**: Improve the logic for identifying specific stretches
3. **Performance Optimization**: Enhance the processing speed and efficiency
4. **UI Improvements**: Develop better visual feedback for detected poses

Please submit pull requests with clear descriptions of the changes and their purpose.