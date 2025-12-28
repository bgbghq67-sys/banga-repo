import { NextResponse } from "next/server";
import { collection, getDocs, query, where, updateDoc, doc, increment } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// POST - Decrement session count for a device
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
      return NextResponse.json({ ok: false, message: "Device not found" }, { status: 404 });
    }

    const deviceDoc = snapshot.docs[0];
    const deviceData = deviceDoc.data();

    // Check if there are sessions remaining
    if (deviceData.remainingSessions <= 0) {
      return NextResponse.json({ ok: false, message: "No sessions remaining" }, { status: 403 });
    }

    // Decrement sessions
    await updateDoc(doc(db, DEVICES_COLLECTION, deviceDoc.id), {
      remainingSessions: increment(-1),
      lastSeen: Date.now(),
    });

    return NextResponse.json({
      ok: true,
      remainingSessions: deviceData.remainingSessions - 1,
    });
  } catch (error) {
    console.error("Decrement error:", error);
    return NextResponse.json({ ok: false, message: "Internal error" }, { status: 500 });
  }
}








