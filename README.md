<div align="center">

# 📸 BANGA PHOTOBOOTH

### *Premium Korean-Style Photo Booth Experience*

<img src="Assets/Logo.png" alt="Banga Photobooth Logo" width="200"/>

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Next.js](https://img.shields.io/badge/Next.js-14-000000?style=for-the-badge&logo=nextdotjs&logoColor=white)](https://nextjs.org/)
[![Firebase](https://img.shields.io/badge/Firebase-Firestore-FFCA28?style=for-the-badge&logo=firebase&logoColor=black)](https://firebase.google.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red?style=for-the-badge)](LICENSE)

---

**Transform any event into an unforgettable experience with AI-powered photo strips!**

[🚀 Quick Start](#-quick-start) • [✨ Features](#-features) • [📖 Documentation](#-documentation) • [🛠️ Setup](#%EF%B8%8F-setup-guide)

</div>

---

## 🌟 What is Banga Photobooth?

**Banga Photobooth** is a professional-grade photo booth software designed for events, studios, and commercial use. It combines the charm of Korean-style photo strips with cutting-edge AI technology to deliver stunning results.

<div align="center">

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   👤 CAPTURE  →  🎨 STYLE  →  🖨️ PRINT  →  📱 SHARE           │
│                                                                 │
│   Take photos    Apply AI     Print high     Scan QR to        │
│   with ease      effects      quality        download          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

</div>

---

## ✨ Features

<table>
<tr>
<td width="50%">

### 📷 Photo Capture
- **Multi-shot capture** with countdown timer
- **Webcam & DSLR support** (Canon EOS series)
- **Live preview** with mirror mode option
- **"Smile!" audio cue** for perfect timing

</td>
<td width="50%">

### 🎨 AI Styling
- **Anime/Ghibli transformation** using ONNX models
- **Real-time processing** on GPU
- **Multiple art styles** to choose from
- **Side-by-side comparison** view

</td>
</tr>
<tr>
<td width="50%">

### 🖼️ Templates
- **Korean-style photo strips** (600x1800)
- **Standard 4R prints** (1200x1800)
- **Custom JSON-based templates**
- **QR code auto-placement**

</td>
<td width="50%">

### 🖨️ Professional Printing
- **DNP RX1HS printer** optimized
- **Auto-cut control** for strips
- **2-printer profile system**
- **High-quality 4x6 output**

</td>
</tr>
</table>

---

## 🏗️ Architecture

```
banga-repo/
├── 📱 Desktop App (WPF/.NET 8)
│   ├── MainWindow          # Welcome screen
│   ├── TemplateWindow      # Template selection
│   ├── CaptureWindow       # Photo capture with countdown
│   ├── PreviewWindow       # Review & select photos
│   ├── ChooseWindow        # Original vs AI selection
│   └── PrintWindow         # Print & generate QR
│
├── 🌐 Web Portal (Next.js 14)
│   ├── /dashboard          # Admin control panel
│   │   ├── /devices        # Device management
│   │   └── /control        # Session control
│   ├── /view/[sessionId]   # QR code photo viewer
│   └── /api                # REST API endpoints
│
├── 🎨 Assets
│   ├── /templates          # Photo strip templates (PNG + JSON)
│   ├── /Fonts              # Custom typography
│   └── *.onnx              # AI model files
│
└── 🔧 Services
    ├── DeviceRegistration  # Machine ID binding
    ├── SessionMonitor      # Session tracking
    └── PhotoUpload         # Cloud storage integration
```

---

## 🚀 Quick Start

### Prerequisites

| Requirement | Version |
|-------------|---------|
| Windows | 10/11 (64-bit) |
| .NET Runtime | 8.0+ |
| Node.js | 18+ (for web portal) |
| Printer | DNP DS-RX1HS (recommended) |

### Installation

#### Option 1: Using Installer (Recommended)
```powershell
# Run the installer
.\Installer\Output\BangaPhotobooth_Setup.exe
```

#### Option 2: Build from Source
```powershell
# Clone the repository
git clone https://github.com/bgbghq67-sys/banga-repo.git

# Build the desktop app
dotnet publish -c Release -r win-x64 --self-contained false

# Setup web portal
cd Website/web-portal
npm install
npm run build
```

---

## 🛠️ Setup Guide

### 1️⃣ Desktop App Configuration

Press `Ctrl+Shift+S` on the welcome screen to access Settings:

| Setting | Description |
|---------|-------------|
| **Camera Mode** | Webcam / Canon DSLR |
| **Printer Mode** | Simulation / Physical |
| **Printer (Strip)** | For 600x1800 with cut |
| **Printer (4R)** | For 1200x1800 no cut |
| **Invert Camera** | Mirror mode toggle |
| **API URL** | Web portal endpoint |

### 2️⃣ Web Portal Deployment

```bash
# Environment Variables Required
NEXT_PUBLIC_FIREBASE_API_KEY=your_key
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your_domain
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your_project
R2_ACCESS_KEY_ID=cloudflare_r2_key
R2_SECRET_ACCESS_KEY=cloudflare_r2_secret
R2_BUCKET=your_bucket_name
```

### 3️⃣ Firebase Setup

1. Create a Firebase project
2. Enable Firestore Database
3. Set security rules:
```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /devices/{deviceId} {
      allow read, write: if true;
    }
    match /sessions/{sessionId} {
      allow read, write: if true;
    }
  }
}
```

---

## 📱 Device Management

<div align="center">

```
┌──────────────────────────────────────────────────────────────┐
│                    ADMIN DASHBOARD                           │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Device Name      │ Machine ID    │ Sessions │ Status       │
│  ─────────────────┼───────────────┼──────────┼────────────  │
│  Seoul Store      │ A1B2-C3D4-... │    150   │ 🟢 Online    │
│  Busan Branch     │ E5F6-G7H8-... │     45   │ 🟡 Pending   │
│  Tokyo Booth      │ I9J0-K1L2-... │      0   │ 🔴 Offline   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

</div>

**How it works:**
1. App starts → Generates unique Machine ID
2. Machine ID displayed on lock screen
3. Admin activates device & assigns sessions
4. Each print decrements session count

---

## 🎨 Template System

Templates are defined using JSON configuration:

```json
{
  "resolution": { "width": 1200, "height": 1800 },
  "photoSlots": [
    { "x": 30, "y": 50, "width": 540, "height": 720 },
    { "x": 630, "y": 50, "width": 540, "height": 720 }
  ],
  "qrSlot": { "x": 500, "y": 1600, "width": 200, "height": 200 }
}
```

**Creating Custom Templates:**
1. Design your template in Photoshop/Figma (1200x1800 or 600x1800)
2. Export as PNG with transparent photo slots
3. Create matching JSON with slot coordinates
4. Place both files in `Assets/templates/`

---

## 📸 Print Modes

| Mode | Resolution | Printer | Cut | Use Case |
|------|-----------|---------|-----|----------|
| **Strip** | 600x1800 | DNP-Strip | ✂️ Yes | 2x6 photo strips |
| **4R** | 1200x1800 | DNP-4R | ❌ No | Full 4x6 prints |

**Strip Mode Logic:**
- 2 strips combined → 1200x1800 → Print with cut → 2 separate strips

---

## 🔒 Security

- **Machine ID Binding**: Each installation tied to hardware
- **Session-based Licensing**: Controlled print counts
- **Admin Panel Access**: Web-based management
- **No Local Data Storage**: Photos uploaded to cloud

---

## 📞 Support

<div align="center">

| Channel | Contact |
|---------|---------|
| 📧 Email | bgbghq67@gmail.com |
| 📖 Docs | [Setup Guide](SETUP_GUIDE.md) |

</div>

---

## 📄 License

This software is proprietary and licensed for commercial use only.
Unauthorized distribution or modification is prohibited.

---

<div align="center">

### Made with ❤️ 

**© 2025 Banga Photobooth. All rights reserved.**

<img src="Assets/Logo.png" alt="Logo" width="60"/>

</div>

