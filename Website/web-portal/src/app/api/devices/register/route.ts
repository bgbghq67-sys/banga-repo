import { NextResponse } from "next/server";
import { collection, getDocs, addDoc, query, where, updateDoc, doc } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// GET - Health check for keep-alive pings
export async function GET() {
  return NextResponse.json({ ok: true, message: "Register endpoint is alive", timestamp: Date.now() });
}

// POST - Register device (called by Desktop App on startup)
export async function POST(request: Request) {
  try {
    const body = await request.json();
    const { machineId, machineName } = body;

    if (!machineId || typeof machineId !== "string") {
      return NextResponse.json({ ok: false, message: "Machine ID is required" }, { status: 400 });
    }

    // Check if device already exists
    const devicesRef = collection(db, DEVICES_COLLECTION);
    const q = query(devicesRef, where("machineId", "==", machineId));
    const snapshot = await getDocs(q);

    if (!snapshot.empty) {
      // Device exists - update lastSeen and return info
      const deviceDoc = snapshot.docs[0];
      const deviceData = deviceDoc.data();

      await updateDoc(doc(db, DEVICES_COLLECTION, deviceDoc.id), {
        lastSeen: Date.now(),
      });

      return NextResponse.json({
        ok: true,
        isNew: false,
        deviceId: deviceDoc.id,
        deviceName: deviceData.name,
        remainingSessions: deviceData.remainingSessions,
        activated: deviceData.remainingSessions > 0,
      });
    }

    // New device - create it
    const newDevice = {
      name: machineName || `New Device (${machineId.substring(0, 8)}...)`,
      machineId: machineId,
      remainingSessions: 0,
      activated: false,
      createdAt: Date.now(),
      lastSeen: Date.now(),
    };

    const docRef = await addDoc(collection(db, DEVICES_COLLECTION), newDevice);

    return NextResponse.json({
      ok: true,
      isNew: true,
      deviceId: docRef.id,
      deviceName: newDevice.name,
      remainingSessions: 0,
      activated: false,
    });
  } catch (error) {
    console.error("Register error:", error);
    return NextResponse.json({ ok: false, message: "Internal error" }, { status: 500 });
  }
}








