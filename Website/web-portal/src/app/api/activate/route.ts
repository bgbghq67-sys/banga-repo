import { NextResponse } from "next/server";
import { doc, getDoc, setDoc, increment, updateDoc } from "firebase/firestore";
import { db } from "@/lib/firebase";

// Firestore path: settings/security
const SETTINGS_DOC_REF = doc(db, "settings", "security");

export async function POST(request: Request) {
  try {
    const body = await request.json();
    const { sessionsToAdd } = body; // Number to increment

    if (!sessionsToAdd || typeof sessionsToAdd !== 'number' || sessionsToAdd < 1) {
        return NextResponse.json({ ok: false, message: "Invalid session count" }, { status: 400 });
    }

    // Check if doc exists
    const snapshot = await getDoc(SETTINGS_DOC_REF);
    
    if (!snapshot.exists()) {
        // Initialize if not exists
        await setDoc(SETTINGS_DOC_REF, { 
            remainingSessions: sessionsToAdd,
            updatedAt: Date.now() 
        });
    } else {
        // Increment existing
        await updateDoc(SETTINGS_DOC_REF, { 
            remainingSessions: increment(sessionsToAdd),
            updatedAt: Date.now()
        });
    }

    return NextResponse.json({ ok: true, message: "Activated" });

  } catch (error) {
    console.error("Activation Error", error);
    return NextResponse.json({ ok: false, message: "Internal Error" }, { status: 500 });
  }
}

