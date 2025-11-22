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

    // Check connection status using readyState
    const checkConnection = () => {
      if (eventSource.readyState === EventSource.OPEN) {
        setIsConnected(true);
        return true;
      } else if (eventSource.readyState === EventSource.CONNECTING) {
        setIsConnected(false);
        return false;
      } else if (eventSource.readyState === EventSource.CLOSED) {
        setIsConnected(false);
        return false;
      }
      return false;
    };

    // Check immediately and periodically until connected
    let interval: NodeJS.Timeout | null = null;
    if (!checkConnection()) {
      interval = setInterval(() => {
        if (checkConnection()) {
          if (interval) clearInterval(interval);
        }
      }, 100);
    }

    eventSource.onopen = () => {
      setIsConnected(true);
      console.log('SSE connected');
      clearInterval(interval);
    };

    eventSource.onmessage = (event) => {
      setIsConnected(true); // If we receive a message, we're connected
      if (interval) clearInterval(interval);
      try {
        const data = JSON.parse(event.data);
        // Ignore ping messages, they're just for connection confirmation
        if (data.type === 'ping') {
          console.log('SSE connection confirmed');
          return;
        }
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
      setIsConnected(true); // If we receive an event, we're connected
      clearInterval(interval);
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
      setIsConnected(true);
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
      setIsConnected(true);
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
      setIsConnected(true);
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
      console.error('SSE error:', error, 'readyState:', eventSource.readyState);
      clearInterval(interval);
      if (eventSource.readyState === EventSource.CLOSED) {
        setIsConnected(false);
      }
      // EventSource will automatically attempt to reconnect
    };

    return () => {
      clearInterval(interval);
      eventSource.close();
      eventSourceRef.current = null;
    };
  }, [url]);

  return { isConnected, lastMessage };
}

