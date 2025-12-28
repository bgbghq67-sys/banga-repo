import Link from "next/link";
import { notFound } from "next/navigation";
import Image from "next/image";
import { headers } from "next/headers";

type SessionPageProps = {
  params: {
    sessionId: string;
  };
};

type SessionApiResponse = {
  ok: boolean;
  session?: {
    sessionId: string;
    photos: { label: string; url: string }[];
    zipUrl: string | null;
  };
  message?: string;
};

async function fetchSession(sessionId: string) {
  const host = headers().get("host");
  const protocol = process.env.NODE_ENV === "production" ? "https" : "http";
  const baseUrl =
    process.env.NEXT_PUBLIC_APP_URL || `${protocol}://${host ?? "localhost:3000"}`;

  const res = await fetch(`${baseUrl}/api/session/${sessionId}`, {
    next: { revalidate: 0 },
  });

  const data = (await res.json()) as SessionApiResponse;
  if (!data.ok || !data.session) {
    return null;
  }
  return data.session;
}

export default async function SessionPage({ params }: SessionPageProps) {
  const session = await fetchSession(params.sessionId);
  if (!session) {
    notFound();
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-slate-100 to-amber-50 p-6 md:p-12">
      <div className="mx-auto max-w-6xl">
        <div className="mb-10 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm uppercase tracking-widest text-slate-500">
              Session
            </p>
            <h1 className="text-4xl font-bold text-slate-900">
              Session #{session.sessionId}
            </h1>
            <p className="text-slate-500">
              Tap a photo to view full size or download the entire set.
            </p>
          </div>
          {session.zipUrl && (
            <Link
              href={session.zipUrl}
              className="inline-flex items-center justify-center rounded-full bg-blue-600 px-6 py-3 text-lg font-semibold text-white shadow-lg transition hover:bg-blue-500"
            >
              Download All
            </Link>
          )}
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {session.photos.map((photo) => (
            <Link
              key={photo.url}
              href={photo.url}
              target="_blank"
              className="group overflow-hidden rounded-3xl border border-white/60 bg-white shadow-xl transition hover:-translate-y-1 hover:shadow-2xl"
            >
              <div className="relative aspect-[2/3] w-full">
                <Image
                  src={photo.url}
                  alt={photo.label}
                  fill
                  className="object-cover transition duration-300 group-hover:scale-105"
                  sizes="(max-width: 768px) 100vw, 50vw"
                />
              </div>
              <div className="border-t border-slate-100 px-6 py-4">
                <p className="font-medium text-slate-800">{photo.label}</p>
                <p className="text-sm text-slate-500">Tap to download</p>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </main>
  );
}

