import { NextResponse } from "next/server";
import { doc, getDoc, setDoc, updateDoc } from "firebase/firestore";
import { db } from "@/lib/firebase";

// Firestore path: settings/security
const OTP_DOC_REF = doc(db, "settings", "security");

export async function POST(request: Request) {
  try {
    const body = await request.json();
    const { action, code } = body;

    if (action === "generate") {
      // Generate a random 4-digit code
      const newCode = Math.floor(1000 + Math.random() * 9000).toString();
      
      await setDoc(OTP_DOC_REF, { 
        otp: newCode,
        generatedAt: Date.now() 
      }, { merge: true });

      return NextResponse.json({ ok: true, code: newCode });
    } 
    
    else if (action === "verify") {
      if (!code) {
        return NextResponse.json({ ok: false, message: "Code required" }, { status: 400 });
      }

      const snapshot = await getDoc(OTP_DOC_REF);
      if (!snapshot.exists()) {
        return NextResponse.json({ ok: false, message: "No OTP set" }, { status: 404 });
      }

      const data = snapshot.data();
      if (data.otp === code) {
        // Optional: Invalidate OTP after use
        // await updateDoc(OTP_DOC_REF, { otp: null });
        return NextResponse.json({ ok: true, message: "Verified" });
      } else {
        return NextResponse.json({ ok: false, message: "Invalid Code" }, { status: 401 });
      }
    }

    return NextResponse.json({ ok: false, message: "Invalid action" }, { status: 400 });

  } catch (error) {
    console.error("OTP Error", error);
    return NextResponse.json({ ok: false, message: "Internal Error" }, { status: 500 });
  }
}

