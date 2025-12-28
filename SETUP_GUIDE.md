# Banga Photobooth - Setup & Deployment Guide

This guide provides step-by-step instructions for setting up the **Banga Photobooth** hardware and software.

## 📦 Package Contents
When you extract the release zip file, you should see:
- `Banga Photobooth.exe` (Main Application)
- `Assets/` (Fonts, Templates, AI Models)
- `Drivers/` (Printer Drivers)
- `Install_Banga.bat` (Automated Installer)
- `config.json` (Configuration File)

---

## 🖥️ Hardware Requirements
- **PC/Laptop**: Windows 10 or 11 (64-bit).
- **Camera**: Canon EOS DSLR (e.g., 2000D, 1300D) connected via USB.
- **Printer**: DNP DS-RX1HS connected via USB.
- **Internet**: Stable Wi-Fi connection (Required for QR Code generation).

## 🛠️ Software Prerequisites
Before running the installer, ensure you have:
1. **.NET 8.0 Desktop Runtime**: [Download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (Look for ".NET Desktop Runtime" for Windows x64).
2. **Google Chrome**: Installed for any web-based configuration if needed.

---

## 🚀 Installation Steps

### Step 1: Prepare the Folder
1. Copy the entire `Banga Photobooth` folder to a permanent location on the PC (e.g., `C:\Banga Photobooth`).
   > ⚠️ **Do not run directly from a USB stick** or a temporary zip folder.

### Step 2: Run the Installer
1. Right-click `Install_Banga.bat` and select **Run as Administrator**.
2. Follow the on-screen prompts.
   - The script will automatically install the **DNP Printer Drivers**.
   - It will install necessary **Fonts** (Poppins).
   - It will create a **Desktop Shortcut**.

### Step 3: Configure the Printer (Critical!)
After the driver installation:
1. Go to **Control Panel** > **Devices and Printers**.
2. Right-click **DNP DS-RX1HS** (or similar) and select **Printing Preferences**.
3. **Layout**: Set to **Landscape**.
4. **Paper Size**: Set to **4x6** (or 6x4).
5. **Advanced**: If doing photo strips, look for "2-inch cut" options.
6. Click **Apply** and **OK**.

### Step 4: Connect the Camera (Canon EOS)

Since this application treats the camera as a high-quality webcam, you **MUST** install the Canon software driver first.

1. **Install EOS Webcam Utility**:
   - Download and install **Canon EOS Webcam Utility** from the official Canon website.
   - **Restart the computer** after installation.

2. **Camera Setup**:
   - Connect the camera via USB.
   - Turn the camera **ON**.
   - Set the Mode Dial to **Movie Mode** (Video Icon) 🎥.
     - *Note: If your camera doesn't have a specific movie mode on the dial, set to Manual (M) and switch the Live View lever to Movie.*
   - Ensure the lens cap is off.

3. **Important Checks**:
   - **Close "EOS Utility"**: If the standard Canon remote shooting software opens automatically, **QUIT IT COMPLETELY**. It conflicts with the Webcam Utility.
   - **Test**: Open the "Camera" app in Windows. You should see "EOS Webcam Utility" as a video source.

---

## ⚙️ Configuration (Admin Panel)

1. Open **Banga Photobooth** from the desktop.
2. Click the **Settings (Gear Icon)** in the top-right corner (or press the secret shortcut if configured).
3. **Admin Password**: Default is usually `1234` (or check `config.json`).
4. In the Admin Panel, you can check:
   - **Camera**: Verify the status shows "Connected".
   - **Printer**: Select the correct printer from the dropdown.
   - **AI Style**: Choose the AI filter style.
   - **Paper Count**: Reset or view paper usage.

---

## 🌐 Cloud & QR Code Setup
The application is connected to a Vercel-hosted backend for QR code generation.
- **Base URL**: The app is pre-configured to point to your Vercel deployment (`https://banga-photobooth-web-portal.vercel.app` or similar).
- **Testing**: 
  1. Take a test photo session.
  2. Wait for the "Uploading..." message.
  3. If a QR code appears, the connection is working.

---

## ❓ Troubleshooting

**Issue: "Camera not detected"**
- Check USB cable.
- Ensure camera battery is charged (use AC adapter if possible).
- Close EOS Utility completely from the system tray.

**Issue: "Printing failed"**
- Check if printer has paper and ribbon.
- Ensure printer is "Online" in Windows Printers settings.
- Restart the printer.

**Issue: "QR Code does not appear / Upload failed"**
- Check internet connection.
- Verify the backend server is running/accessible.
- Check `camera_debug.log` in the app folder for specific error messages.

---

## 📞 Support
For technical support, please contact the developer.

