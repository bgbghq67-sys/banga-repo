import { NextResponse } from "next/server";
import { getSession } from "@/lib/session-service";

type Params = {
  params: {
    sessionId: string;
  };
};

export async function GET(_request: Request, { params }: Params) {
  try {
    const session = await getSession(params.sessionId);
    if (!session) {
      return NextResponse.json(
        { ok: false, message: "Session not found" },
        { status: 404 }
      );
    }

    return NextResponse.json({ ok: true, session });
  } catch (error) {
    console.error("Failed to fetch session", error);
    return NextResponse.json(
      { ok: false, message: "Internal Server Error" },
      { status: 500 }
    );
  }
}

