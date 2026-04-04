"use client";

import { useDefaultRenderTool } from "@copilotkit/react-core/v2";

type ToolRenderStatus = "inProgress" | "executing" | "complete";

type CitationSource = {
  title: string;
  url: string;
  snippet: string;
};

type ToolRenderProps = {
  name: string;
  parameters: unknown;
  status: ToolRenderStatus;
  result: string | undefined;
};

const citationToolNames = new Set(["DisplaySources", "display_sources", "displaySources"]);

export function ToolRenderers() {
  useDefaultRenderTool(
    {
      render: (props) => {
        if (citationToolNames.has(props.name)) {
          return <CitationToolPanel {...props} />;
        }

        return <FallbackToolPanel {...props} />;
      },
    },
    [],
  );

  return null;
}

function CitationToolPanel({ parameters, result, status }: ToolRenderProps) {
  const sources = extractSources(parameters, result);
  const heading = status === "complete" ? "Sources attached" : "Collecting sources";

  return (
    <section className="citation-tool" data-testid="citation-tool-panel">
      <div className="citation-tool__header">
        <span className={`citation-tool__status citation-tool__status--${status}`} />
        <div>
          <p className="citation-tool__eyebrow">References</p>
          <h2 className="citation-tool__title">{heading}</h2>
        </div>
      </div>

      {sources.length === 0 ? (
        <p className="citation-tool__placeholder">Waiting for the backend to attach source details.</p>
      ) : (
        <div className="citation-tool__list">
          {sources.map((source, index) => (
            <a
              key={`${source.url}-${index}`}
              className="citation-card"
              href={source.url}
              rel="noreferrer"
              target="_blank"
              data-testid="citation-card"
            >
              <span className="citation-card__index">{index + 1}</span>
              <div className="citation-card__content">
                <div className="citation-card__title">{source.title}</div>
                <div className="citation-card__snippet">{source.snippet}</div>
                <div className="citation-card__url">{source.url}</div>
              </div>
            </a>
          ))}
        </div>
      )}
    </section>
  );
}

function FallbackToolPanel({ name, parameters, result, status }: ToolRenderProps) {
  return (
    <section className="tool-fallback" data-testid="tool-fallback-panel">
      <div className="tool-fallback__row">
        <span className="tool-fallback__name">{name}</span>
        <span className="tool-fallback__badge">{status}</span>
      </div>

      {parameters !== undefined ? (
        <pre className="tool-fallback__payload">{JSON.stringify(parameters, null, 2)}</pre>
      ) : null}

      {result ? <pre className="tool-fallback__payload">{result}</pre> : null}
    </section>
  );
}

function extractSources(parameters: unknown, result: string | undefined): CitationSource[] {
  const fromParameters = normalizeSources(parameters);
  if (fromParameters.length > 0) {
    return fromParameters;
  }

  if (!result) {
    return [];
  }

  try {
    return normalizeSources(JSON.parse(result));
  } catch {
    return [];
  }
}

function normalizeSources(payload: unknown): CitationSource[] {
  if (!payload || typeof payload !== "object") {
    return [];
  }

  const sources = (payload as { sources?: unknown }).sources;
  if (!Array.isArray(sources)) {
    return [];
  }

  return sources
    .map((source) => ({
      title: asString((source as { title?: unknown }).title),
      url: asString((source as { url?: unknown }).url),
      snippet: asString((source as { snippet?: unknown }).snippet),
    }))
    .filter((source) => source.title.length > 0 && source.url.length > 0);
}

function asString(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}