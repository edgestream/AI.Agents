"use client";

import { useEffect, useState } from "react";

interface TokenUsage {
  available: boolean;
  timestamp?: string;
  inputTokens?: number;
  outputTokens?: number;
  totalTokens?: number;
}

export function TokenUsageDisplay() {
  const [usage, setUsage] = useState<TokenUsage>({ available: false });
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // Get backend URL from environment or use default
    const backendUrl = process.env.NEXT_PUBLIC_BACKEND_URL || "http://localhost:8000";
    
    // Poll for token usage every 2 seconds
    const interval = setInterval(async () => {
      try {
        const response = await fetch(`${backendUrl}/api/token-usage/latest`);
        if (response.ok) {
          const data = await response.json();
          if (data.available) {
            setUsage(data);
            setIsVisible(true);
            // Hide after 10 seconds
            setTimeout(() => setIsVisible(false), 10000);
          }
        }
      } catch (error) {
        console.error('Failed to fetch token usage:', error);
      }
    }, 2000);

    return () => clearInterval(interval);
  }, []);

  if (!isVisible || !usage.available) {
    return null;
  }

  return (
    <div className="fixed bottom-4 right-4 bg-white border border-gray-200 rounded-lg shadow-lg p-3 text-sm z-50">
      <div className="font-semibold text-gray-700 mb-2">Token Usage</div>
      <div className="flex flex-col gap-1 text-gray-600">
        <div className="flex items-center gap-2">
          <span className="text-green-600" title="Input tokens">↑</span>
          <span>{usage.inputTokens?.toLocaleString() || 0}</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-blue-600" title="Output tokens">↓</span>
          <span>{usage.outputTokens?.toLocaleString() || 0}</span>
        </div>
        <div className="flex items-center gap-2 text-xs text-gray-500 pt-1 border-t border-gray-100">
          <span>Total:</span>
          <span>{usage.totalTokens?.toLocaleString() || 0}</span>
        </div>
      </div>
    </div>
  );
}
