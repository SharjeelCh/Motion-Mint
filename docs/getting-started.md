---
layout: default
title: Getting Started
nav_order: 3
---

# Getting Started

This guide will help you set up and start using the Stretch Tracker App.

## System Requirements

- Windows 10 or later
- .NET Runtime 9.0 or later
- Webcam (integrated or external)
- Minimum 4GB RAM
- 100MB free disk space

## Installation

1. Download the latest release from the [GitHub releases page](https://github.com/nitin27may/StretchTracker/releases)
2. Run the installer and follow the on-screen instructions
3. Launch the app from the Start menu or desktop shortcut

## First-Time Setup

When you first launch the app, you'll be guided through a brief setup process:

1. **Permissions**: The app will request permission to access your webcam
2. **Settings**: Configure your preferred reminder intervals
3. **Calibration**: A brief calibration will adjust the motion detection to your environment
4. **Tutorial**: A quick walkthrough of the app's features

{: .important-title }
> AI Technology Onboarding
>
> During first-time setup, the app will initialize its computer vision and machine learning systems. This includes:
> - Loading the TensorFlow pose detection model
> - Calibrating the OpenCV motion detection for your specific environment
> - Optimizing detection parameters for your webcam and lighting conditions

## Understanding the Technology

<div class="code-example" markdown="1">
{: .highlight }
> **How It Works**
>
> The Stretch Tracker App uses advanced computer vision and machine learning to verify your stretching movements. When it's time to stretch, the app will analyze your movements through the webcam, using:
>
> 1. **Computer vision algorithms** to detect motion patterns
> 2. **Machine learning models** to recognize specific stretching poses
</div>

The technology works in harmony to:

- **Detect genuine stretches**: The app can tell the difference between actual stretching and other movements
- **Adapt to your environment**: The detection system calibrates to your lighting and background
- **Provide accurate feedback**: The AI-driven system ensures you're properly completing stretches
- **Respect your privacy**: All processing happens locally on your device; no video is transmitted or stored

## Basic Usage

1. **Starting the app**: The app will run in the background, accessible from the system tray
2. **Receiving reminders**: At your set intervals, you'll receive a notification to stretch
3. **Performing stretches**: Click on the notification to open the stretch window
4. **Completing a session**: Follow the on-screen instructions to complete your stretches

## Settings Configuration

You can customize the app through the Settings window:

1. **Open Settings**: Click the gear icon in the main window or right-click the system tray icon and select "Settings"
2. **Adjust reminder frequency**: Set how often you want to be reminded to stretch
3. **Configure stretch requirements**: Set how many stretches are needed to complete a session
4. **Adjust detection sensitivity**: Fine-tune how the app detects your movements
5. **Save changes**: Your new settings will be applied immediately

## Troubleshooting

If you encounter issues with the app, try these steps:

1. **Webcam not working**: Ensure no other applications are using your webcam
2. **Detection problems**: Try adjusting the detection sensitivity in Settings
3. **App not starting**: Verify you have the required .NET Runtime installed
4. **Other issues**: Check the [GitHub issues page](https://github.com/nitin27may/StretchTracker/issues) or submit a new issue