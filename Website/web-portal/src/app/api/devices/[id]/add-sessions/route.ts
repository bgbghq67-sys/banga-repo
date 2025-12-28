import { NextResponse } from "next/server";
import { doc, getDoc, updateDoc, increment } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// POST - Add sessions to device
export async function POST(
  request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const body = await request.json();
    const { sessions } = body;

    if (!sessions || typeof sessions !== "number" || sessions < 1) {
      return NextResponse.json({ ok: false, message: "Invalid session count" }, { status: 400 });
    }

    const docRef = doc(db, DEVICES_COLLECTION, id);
    const snapshot = await getDoc(docRef);

    if (!snapshot.exists()) {
      return NextResponse.json({ ok: false, message: "Device not found" }, { status: 404 });
    }

    await updateDoc(docRef, {
      remainingSessions: increment(sessions),
    });

    const updatedSnapshot = await getDoc(docRef);
    const updatedData = updatedSnapshot.data();

    return NextResponse.json({
      ok: true,
      message: `Added ${sessions} sessions`,
      newTotal: updatedData?.remainingSessions,
    });
  } catch (error) {
    console.error("Error adding sessions:", error);
    return NextResponse.json({ ok: false, message: "Failed to add sessions" }, { status: 500 });
  }
}








