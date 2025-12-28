import { NextResponse } from "next/server";
import { doc, getDoc, updateDoc, increment } from "firebase/firestore";
import { db } from "@/lib/firebase";

const SETTINGS_DOC_REF = doc(db, "settings", "security");

export async function GET(request: Request) {
    try {
        const snapshot = await getDoc(SETTINGS_DOC_REF);
        if (!snapshot.exists()) {
            return NextResponse.json({ remainingSessions: 0 });
        }
        return NextResponse.json({ remainingSessions: snapshot.data().remainingSessions || 0 });
    } catch (error) {
        return NextResponse.json({ remainingSessions: 0 }, { status: 500 });
    }
}

export async function POST(request: Request) {
    try {
        const body = await request.json();
        const { action } = body;

        if (action === "decrement") {
             await updateDoc(SETTINGS_DOC_REF, { 
                remainingSessions: increment(-1),
                updatedAt: Date.now()
            });
            return NextResponse.json({ ok: true });
        }
        return NextResponse.json({ ok: false }, { status: 400 });
    } catch (error) {
        return NextResponse.json({ ok: false }, { status: 500 });
    }
}

