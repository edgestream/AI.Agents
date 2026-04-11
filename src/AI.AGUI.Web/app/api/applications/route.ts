const BACKEND_URL = process.env.BACKEND_URL ?? "http://localhost:8000";

export async function GET() {
  try {
    const res = await fetch(`${BACKEND_URL}/applications`, { cache: "no-store" });
    const data = await res.json();
    return Response.json(data);
  } catch {
    return Response.json([], { status: 200 });
  }
}
