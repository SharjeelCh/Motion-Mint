# Stretch Tracker App

A modern desktop application that reminds users to take regular stretching breaks and helps them maintain good physical health while working at a computer.

## Features

- **Customizable Tracker Intervals**: Set Trackers from minutes to hours based on your needs
- **Computer Vision Detection**: Uses webcam and motion detection to verify stretching movements
- **Machine Learning Integration**: TensorFlow.NET for advanced pose detection capabilities
- **Progress Tracking**: Records and displays stretching streaks and completion rates
- **Statistics Dashboard**: Visual representation of your stretching history and habits
- **Minimal Distraction**: Simple notifications that don't interrupt your workflow
- **System Tray Integration**: Runs in the background with system tray access

## Technology Stack

- **Framework**: .NET 9.0 with WPF
- **Database**: SQLite via Microsoft.EntityFrameworkCore.Sqlite
- **Computer Vision**: OpenCvSharp4 for motion detection
- **Machine Learning**: TensorFlow.NET for pose detection capabilities
- **UI Components**: Native WPF with modern styling
- **Notifications**: Windows system notifications

## Getting Started

### Prerequisites

- Visual Studio 2022 or later
- .NET 9.0 SDK
- Windows OS (Windows 10 or later recommended)

### Setup

1. Clone the repository
2. Open `MotionMint.sln` in Visual Studio
3. Restore NuGet packages
4. Build the solution

### Running the Project

You can run the project in two ways:

**Using Visual Studio:**
1. Open `MotionMint.sln` in Visual Studio
2. Press F5 or click the "Start" button to run the application

**Using Command Line:**
1. Navigate to the src directory:
   ```
   cd src
   ```
2. Restore the NuGet packages:
   ```
   dotnet restore
   ```
3. Build the project:
   ```
   dotnet build
   ```
4. Run the application:
   ```
   dotnet run
   ```

**Important Notes:**
- Make sure you have a webcam connected as the app uses it for motion detection
- The app will run in the system tray, so look for its icon there after launching
- The app uses OpenCV and TensorFlow for motion detection, so ensure your system meets the requirements for these libraries
