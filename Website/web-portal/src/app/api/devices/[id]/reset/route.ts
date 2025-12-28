import { NextResponse } from "next/server";
import { doc, getDoc, updateDoc } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// POST - Reset machine binding for device
export async function POST(
  request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const docRef = doc(db, DEVICES_COLLECTION, id);
    const snapshot = await getDoc(docRef);

    if (!snapshot.exists()) {
      return NextResponse.json({ ok: false, message: "Device not found" }, { status: 404 });
    }

    await updateDoc(docRef, {
      machineId: null,
      activated: false,
      lastSeen: null,
    });

    return NextResponse.json({ ok: true, message: "Machine binding reset" });
  } catch (error) {
    console.error("Error resetting machine:", error);
    return NextResponse.json({ ok: false, message: "Failed to reset machine" }, { status: 500 });
  }
}








