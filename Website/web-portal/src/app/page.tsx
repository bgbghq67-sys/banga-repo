import Link from "next/link";

export default function Home() {
  return (
    <main className="min-h-screen bg-gradient-to-br from-slate-100 via-white to-amber-100 px-6 py-16 text-slate-900">
      <div className="mx-auto flex max-w-5xl flex-col gap-12 rounded-3xl bg-white/70 p-10 shadow-xl backdrop-blur">
        <div className="space-y-6">
          <p className="text-sm font-semibold uppercase tracking-[0.3em] text-slate-500">
            Banga Photobooth
          </p>
          <h1 className="text-4xl font-bold leading-tight md:text-5xl">
            Cloud gallery & downloads for every photobooth session.
          </h1>
          <p className="text-lg text-slate-600">
            Each capture flows into Cloudflare R2, is indexed in Firebase, and
            becomes a shareable gallery with expiring download links. Guests scan
            the QR code, review their photos, and optionally grab the full ZIP.
          </p>
          <div className="flex flex-wrap gap-4">
            <Link
              href="/view/demo"
              className="inline-flex items-center rounded-full bg-blue-600 px-6 py-3 text-white shadow hover:bg-blue-500"
            >
              Preview a Session
            </Link>
            <Link
              href="https://docs.google.com/document/d/1aB_session_flow"
              className="inline-flex items-center rounded-full border border-slate-200 px-6 py-3 text-slate-600 hover:border-slate-300"
            >
              Integration Guide
            </Link>
          </div>
        </div>

        <div className="grid gap-8 md:grid-cols-3">
          {[
            {
              title: "Upload",
              copy: "WPF app streams raw, AI, and ZIP files into Cloudflare R2.",
            },
            {
              title: "Sign",
              copy: "Backend issues short-lived signed URLs and stores metadata in Firebase.",
            },
            {
              title: "Share",
              copy: "Guests visit /view/{sessionId} to browse thumbnails or download everything.",
            },
          ].map((card) => (
            <div
              key={card.title}
              className="rounded-2xl border border-slate-100 bg-white p-6 shadow-sm"
            >
              <p className="text-sm font-semibold uppercase tracking-wide text-slate-500">
                {card.title}
              </p>
              <p className="mt-3 text-base text-slate-600">{card.copy}</p>
            </div>
          ))}
        </div>
        </div>
      </main>
  );
}
