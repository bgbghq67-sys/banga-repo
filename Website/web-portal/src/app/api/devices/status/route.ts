import { NextResponse } from "next/server";
import { collection, getDocs, query, where, updateDoc, doc } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// POST - Check device status (called by Desktop App polling)
export async function POST(request: Request) {
  try {
    const body = await request.json();
    const { machineId } = body;

    if (!machineId || typeof machineId !== "string") {
      return NextResponse.json({ ok: false, message: "Machine ID is required" }, { status: 400 });
    }

    // Find device by machine ID
    const devicesRef = collection(db, DEVICES_COLLECTION);
    const q = query(devicesRef, where("machineId", "==", machineId));
    const snapshot = await getDocs(q);

    if (snapshot.empty) {
      return NextResponse.json({ 
        ok: false, 
        message: "Device not registered",
        remainingSessions: 0,
        activated: false 
      }, { status: 404 });
    }

    const deviceDoc = snapshot.docs[0];
    const deviceData = deviceDoc.data();

    // Update last seen
    await updateDoc(doc(db, DEVICES_COLLECTION, deviceDoc.id), {
      lastSeen: Date.now(),
    });

    return NextResponse.json({
      ok: true,
      deviceId: deviceDoc.id,
      deviceName: deviceData.name,
      remainingSessions: deviceData.remainingSessions,
      activated: deviceData.remainingSessions > 0,
    });
  } catch (error) {
    console.error("Status check error:", error);
    return NextResponse.json({ ok: false, message: "Internal error" }, { status: 500 });
  }
}








