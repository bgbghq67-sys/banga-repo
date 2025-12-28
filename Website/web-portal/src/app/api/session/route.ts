import { NextResponse } from "next/server";
import { saveSession, SessionPayload } from "@/lib/session-service";
import { uploadFile } from "@/lib/r2";
import { z } from "zod";

// Polyfill for environments where crypto.randomUUID might not be available (though it should be in Node 18+)
const generateId = () => {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return Math.random().toString(36).substring(2) + Date.now().toString(36);
};

export async function POST(request: Request) {
  try {
    const formData = await request.formData();
    
    // Get or generate sessionId
    let sessionId = formData.get("sessionId") as string;
    if (!sessionId) {
      // Generate a short random ID (e.g. 6 chars) for the session if not provided
      sessionId = Math.random().toString(36).substring(2, 8).toUpperCase();
    }

    const files: SessionPayload["files"] = [];
    const timestamp = Date.now();

    // Iterate over form data entries
    for (const [key, value] of Array.from(formData.entries())) {
      // Check if value is a File-like object (has arrayBuffer method and name property)
      if (value && typeof value === 'object' && 'arrayBuffer' in value && 'name' in value) {
        const file = value as File; // Type casting for TS, but runtime check above handles it
        const buffer = Buffer.from(await file.arrayBuffer());
        const fileExtension = file.name.split(".").pop() || "jpg";
        
        // Determine type and label based on form field name or file name
        let type: "photo" | "zip" = "photo";
        let label = key;

        if (key === "zip" || file.name.endsWith(".zip")) {
          // Skip zip upload if needed, but if sent, process it.
          // If client stops sending zip, this block won't be entered.
          type = "zip";
          label = "Session Archive";
        } else if (key.includes("ai") || file.name.includes("ai")) {
          label = "AI Photo";
        } else {
            label = "Original Photo";
        }

        // Construct R2 key: sessions/{sessionId}/{filename}
        const r2Key = `sessions/${sessionId}/${key}_${timestamp}.${fileExtension}`;
        
        // Upload to R2
        await uploadFile(r2Key, buffer, file.type || "application/octet-stream");

        files.push({
          key: r2Key,
          label: label,
          type: type,
        });
      }
    }

    if (files.length === 0) {
       return NextResponse.json(
        { ok: false, message: "No files uploaded" },
        { status: 400 }
      );
    }

    const payload: SessionPayload = {
      sessionId,
      createdAt: timestamp,
      files,
    };

    await saveSession(payload);

    const link = `${process.env.NEXT_PUBLIC_APP_URL ?? ""}/view/${sessionId}`;

    return NextResponse.json(
      {
        ok: true,
        sessionId,
        link,
      },
      { status: 201 }
    );
  } catch (error) {
    console.error("Failed to save session", error);
    return NextResponse.json(
      { ok: false, message: "Internal Server Error" },
      { status: 500 }
    );
  }
}
