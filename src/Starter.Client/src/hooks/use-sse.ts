import { useEffect, useRef, useState, useCallback } from 'react';

export interface SSEMessage {
  type: string;
  payload: any;
}

export function useSSE(url: string | null) {
  const [isConnected, setIsConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<SSEMessage | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);

  useEffect(() => {
    if (!url) return;

    const eventSource = new EventSource(url);
    eventSourceRef.current = eventSource;

    eventSource.onopen = () => {
      setIsConnected(true);
      console.log('SSE connected');
    };

    eventSource.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        setLastMessage({
          type: 'message',
          payload: data
        });
      } catch (error) {
        console.error('Failed to parse SSE message:', error);
      }
    };

    // Handle custom event types - SSE sends event type as the event name
    eventSource.addEventListener('game_state', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        setLastMessage({
          type: 'game_state',
          payload: data
        });
      } catch (error) {
        console.error('Failed to parse game_state event:', error);
      }
    });

    eventSource.addEventListener('token_moved', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        setLastMessage({
          type: 'token_moved',
          payload: data
        });
      } catch (error) {
        console.error('Failed to parse token_moved event:', error);
      }
    });

    eventSource.addEventListener('token_added', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        setLastMessage({
          type: 'token_added',
          payload: data
        });
      } catch (error) {
        console.error('Failed to parse token_added event:', error);
      }
    });

    eventSource.addEventListener('background_changed', (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        setLastMessage({
          type: 'background_changed',
          payload: data
        });
      } catch (error) {
        console.error('Failed to parse background_changed event:', error);
      }
    });

    eventSource.onerror = (error) => {
      console.error('SSE error:', error);
      setIsConnected(false);
      // EventSource will automatically attempt to reconnect
    };

    return () => {
      eventSource.close();
      eventSourceRef.current = null;
    };
  }, [url]);

  return { isConnected, lastMessage };
}

