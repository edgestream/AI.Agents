const noStoreHeaders = {
  "cache-control": "no-store",
  "content-type": "text/plain; charset=utf-8",
};

export async function GET() {
  return new Response("OK", {
    status: 200,
    headers: noStoreHeaders,
  });
}