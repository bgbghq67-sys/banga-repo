import { NextResponse } from "next/server";
import { doc, getDoc, updateDoc, deleteDoc } from "firebase/firestore";
import { db } from "@/lib/firebase";

const DEVICES_COLLECTION = "devices";

// GET - Get single device
export async function GET(
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

    return NextResponse.json({
      ok: true,
      device: { id: snapshot.id, ...snapshot.data() },
    });
  } catch (error) {
    console.error("Error fetching device:", error);
    return NextResponse.json({ ok: false, message: "Failed to fetch device" }, { status: 500 });
  }
}

// PUT - Update device
export async function PUT(
  request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  try {
    const { id } = await params;
    const body = await request.json();
    const { name, remainingSessions } = body;

    const docRef = doc(db, DEVICES_COLLECTION, id);
    const snapshot = await getDoc(docRef);

    if (!snapshot.exists()) {
      return NextResponse.json({ ok: false, message: "Device not found" }, { status: 404 });
    }

    const updates: Record<string, unknown> = {};
    if (name !== undefined) updates.name = name.trim();
    if (remainingSessions !== undefined) updates.remainingSessions = remainingSessions;

    await updateDoc(docRef, updates);

    return NextResponse.json({ ok: true, message: "Device updated" });
  } catch (error) {
    console.error("Error updating device:", error);
    return NextResponse.json({ ok: false, message: "Failed to update device" }, { status: 500 });
  }
}

// DELETE - Delete device
export async function DELETE(
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

    await deleteDoc(docRef);

    return NextResponse.json({ ok: true, message: "Device deleted" });
  } catch (error) {
    console.error("Error deleting device:", error);
    return NextResponse.json({ ok: false, message: "Failed to delete device" }, { status: 500 });
  }
}








