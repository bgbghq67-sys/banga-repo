import {
  collection,
  doc,
  getDoc,
  setDoc,
  Timestamp,
} from "firebase/firestore";
import { z } from "zod";
import { db } from "./firebase";
import { getSignedObjectUrl } from "./r2";

const sessionFileSchema = z.object({
  key: z.string().min(1),
  label: z.string().min(1),
  type: z.enum(["photo", "zip"]).default("photo"),
});

export const sessionPayloadSchema = z.object({
  sessionId: z.string().min(4),
  createdAt: z.number().optional(),
  files: z.array(sessionFileSchema).min(1),
});

export type SessionPayload = z.infer<typeof sessionPayloadSchema>;

const sessionsCollection = collection(db, "sessions");

export async function saveSession(payload: SessionPayload) {
  const data = {
    ...payload,
    createdAt: payload.createdAt
      ? Timestamp.fromMillis(payload.createdAt)
      : Timestamp.now(),
  };

  await setDoc(doc(sessionsCollection, payload.sessionId), data, {
    merge: true,
  });
}

export async function getSession(sessionId: string) {
  const snapshot = await getDoc(doc(sessionsCollection, sessionId));
  if (!snapshot.exists()) {
    return null;
  }

  const data = snapshot.data() as SessionPayload & {
    createdAt: Timestamp;
  };

  const signedFiles = await Promise.all(
    data.files.map(async (file) => ({
      label: file.label,
      type: file.type,
      url: await getSignedObjectUrl(file.key),
    }))
  );

  return {
    sessionId,
    createdAt: data.createdAt.toMillis(),
    photos: signedFiles.filter((file) => file.type === "photo"),
    zipUrl: signedFiles.find((file) => file.type === "zip")?.url ?? null,
  };
}

