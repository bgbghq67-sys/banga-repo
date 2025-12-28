import { NextResponse } from "next/server";
import { collection, getDocs, addDoc } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// GET - List all devices
export async function GET() {
  try {
    const devicesRef = collection(db, DEVICES_COLLECTION);
    const snapshot = await getDocs(devicesRef);

    const devices = snapshot.docs.map((doc) => ({
      id: doc.id,
      ...doc.data(),
    }));

    // Sort client-side to avoid index requirement
    devices.sort((a, b) => ((b as any).createdAt || 0) - ((a as any).createdAt || 0));

    return NextResponse.json({ ok: true, devices });
  } catch (error) {
    console.error("Error fetching devices:", error);
    return NextResponse.json({ ok: false, message: "Failed to fetch devices", error: String(error) }, { status: 500 });
  }
}

// POST - Create new device
export async function POST(request: Request) {
  try {
    const body = await request.json();
    const { name, remainingSessions = 100 } = body;

    if (!name || typeof name !== "string" || name.trim() === "") {
      return NextResponse.json({ ok: false, message: "Device name is required" }, { status: 400 });
    }

    const newDevice = {
      name: name.trim(),
      machineId: null,
      remainingSessions: remainingSessions,
      activated: false,
      createdAt: Date.now(),
      lastSeen: null,
    };

    const docRef = await addDoc(collection(db, DEVICES_COLLECTION), newDevice);

    return NextResponse.json({
      ok: true,
      device: { id: docRef.id, ...newDevice },
    });
  } catch (error) {
    console.error("Error creating device:", error);
    return NextResponse.json({ ok: false, message: "Failed to create device" }, { status: 500 });
  }
}

