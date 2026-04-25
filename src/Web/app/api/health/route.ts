const noStoreHeaders = {
  "cache-control": "no-store",
  "content-type": "text/plain; charset=utf-8",
};

function getBackendHealthUrl() {
  const backendUrl = (process.env.BACKEND_URL || "http://localhost:8000").replace(/\/+$/, "");
  return `${backendUrl}/api/health`;
}

export async function GET() {
  try {
    const response = await fetch(getBackendHealthUrl(), {
      cache: "no-store",
    });
    const body = await response.text();

    return new Response(body, {
      status: response.status,
      headers: {
        "cache-control": "no-store",
        "content-type": response.headers.get("content-type") || noStoreHeaders["content-type"],
      },
    });
  } catch {
    return new Response("Backend unavailable", {
      status: 503,
      headers: noStoreHeaders,
    });
  }
}