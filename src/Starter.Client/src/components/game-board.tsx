import { useState, useEffect, useCallback, useRef } from 'react';
import { useSSE } from '@/hooks/use-sse';
import { getUser, createUser, type User } from '@/lib/auth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from '@/components/ui/context-menu';

interface Token {
  id: string;
  userId: string;
  username: string;
  color: string;
  x: number; // pixel position
  y: number; // pixel position
}

interface GameState {
  background: string;
  tokens: Token[];
}

const QUADRANT_SIZE = 32; // 32px x 32px quadrants
const BOARD_SIZE = 4000; // Large board size in pixels
const INITIAL_X = BOARD_SIZE / 2;
const INITIAL_Y = BOARD_SIZE / 2;

export function GameBoard() {
  const [user, setUser] = useState<User | null>(null);
  const [username, setUsername] = useState('');
  const [gameState, setGameState] = useState<GameState>({
    background: '',
    tokens: []
  });
  const [backgroundUrl, setBackgroundUrl] = useState('');
  const [panOffset, setPanOffset] = useState({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [contextMenuPosition, setContextMenuPosition] = useState<{ x: number; y: number } | null>(null);
  const boardRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLDivElement>(null);

  // SSE URL
  const sseUrl = import.meta.env.VITE_API_URL 
    ? `${import.meta.env.VITE_API_URL}/api/game/events`
    : '/api/game/events';
  const { isConnected, lastMessage } = useSSE(sseUrl);

  // Load user from localStorage
  useEffect(() => {
    const storedUser = getUser();
    if (storedUser) {
      setUser(storedUser);
    }
  }, []);

  // Convert screen coordinates to board coordinates
  const screenToBoardCoords = useCallback((screenX: number, screenY: number) => {
    if (!boardRef.current || !canvasRef.current) return { x: 0, y: 0 };
    
    const boardRect = boardRef.current.getBoundingClientRect();
    const relativeX = screenX - boardRect.left;
    const relativeY = screenY - boardRect.top;
    
    // Account for pan offset
    const boardX = relativeX - panOffset.x;
    const boardY = relativeY - panOffset.y;
    
    // Snap to grid
    const snappedX = Math.round(boardX / QUADRANT_SIZE) * QUADRANT_SIZE;
    const snappedY = Math.round(boardY / QUADRANT_SIZE) * QUADRANT_SIZE;
    
    return {
      x: Math.max(0, Math.min(BOARD_SIZE - QUADRANT_SIZE, snappedX)),
      y: Math.max(0, Math.min(BOARD_SIZE - QUADRANT_SIZE, snappedY))
    };
  }, [panOffset]);

  // Handle WebSocket messages
  useEffect(() => {
    if (!lastMessage) return;

    switch (lastMessage.type) {
      case 'game_state':
        setGameState(lastMessage.payload);
        break;
      case 'token_moved':
        setGameState(prev => ({
          ...prev,
          tokens: prev.tokens.map(t =>
            t.id === lastMessage.payload.tokenId
              ? { ...t, x: lastMessage.payload.x, y: lastMessage.payload.y }
              : t
          )
        }));
        break;
      case 'token_added':
        setGameState(prev => ({
          ...prev,
          tokens: [...prev.tokens, lastMessage.payload]
        }));
        break;
      case 'background_changed':
        setGameState(prev => ({
          ...prev,
          background: lastMessage.payload.url
        }));
        break;
    }
  }, [lastMessage]);

  // Sync state to WebSocket when connected
  useEffect(() => {
    if (isConnected && user) {
      sendMessage({
        type: 'join_game',
        payload: { userId: user.id, username: user.username }
      });
    }
  }, [isConnected, user, sendMessage]);

  const handleLogin = () => {
    if (!username.trim()) return;
    const newUser = getUser() || createUser(username.trim());
    setUser(newUser);
    setUsername('');
  };

  const handleSetBackground = async () => {
    if (!backgroundUrl.trim()) return;
    const newState = { ...gameState, background: backgroundUrl };
    setGameState(newState);
    setBackgroundUrl('');
    
    try {
      const apiUrl = import.meta.env.VITE_API_URL || '';
      await fetch(`${apiUrl}/api/game/background`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: backgroundUrl })
      });
    } catch (error) {
      console.error('Failed to set background:', error);
    }
  };

  const handleAddToken = useCallback(async (x: number, y: number) => {
    if (!user) return;
    
    // Check if user already has a token
    const existingToken = gameState.tokens.find(t => t.userId === user.id);
    if (existingToken) {
      // Update existing token position
      try {
        const apiUrl = import.meta.env.VITE_API_URL || '';
        await fetch(`${apiUrl}/api/game/token/move`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ tokenId: existingToken.id, x, y })
        });
      } catch (error) {
        console.error('Failed to move token:', error);
      }
      return;
    }

    const newToken: Token = {
      id: crypto.randomUUID(),
      userId: user.id,
      username: user.username,
      color: user.color,
      x,
      y
    };

    try {
      const apiUrl = import.meta.env.VITE_API_URL || '';
      await fetch(`${apiUrl}/api/game/token/add`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          id: newToken.id,
          userId: newToken.userId,
          username: newToken.username,
          color: newToken.color,
          x: newToken.x,
          y: newToken.y
        })
      });
    } catch (error) {
      console.error('Failed to add token:', error);
    }
  }, [user, gameState.tokens]);

  // Mouse drag handlers for panning
  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    if (e.button !== 0) return; // Only left mouse button
    // Don't start dragging if right-clicking (context menu)
    if (e.button === 2) return;
    setIsDragging(true);
    setDragStart({
      x: e.clientX - panOffset.x,
      y: e.clientY - panOffset.y
    });
  }, [panOffset]);

  // Handle context menu - track position for token placement
  const handleContextMenu = useCallback((e: React.MouseEvent) => {
    const coords = screenToBoardCoords(e.clientX, e.clientY);
    setContextMenuPosition(coords);
  }, [screenToBoardCoords]);

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDragging) return;
    setPanOffset({
      x: e.clientX - dragStart.x,
      y: e.clientY - dragStart.y
    });
  }, [isDragging, dragStart]);

  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  useEffect(() => {
    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, handleMouseMove, handleMouseUp]);

  const handleTokenMove = useCallback(async (tokenId: string, direction: 'up' | 'down' | 'left' | 'right') => {
    const token = gameState.tokens.find(t => t.id === tokenId);
    if (!token) return;

    let newX = token.x;
    let newY = token.y;

    // Snap to quadrant grid (32px)
    const snapX = Math.round(token.x / QUADRANT_SIZE) * QUADRANT_SIZE;
    const snapY = Math.round(token.y / QUADRANT_SIZE) * QUADRANT_SIZE;

    switch (direction) {
      case 'up':
        newY = Math.max(0, snapY - QUADRANT_SIZE);
        break;
      case 'down':
        newY = Math.min(BOARD_SIZE - QUADRANT_SIZE, snapY + QUADRANT_SIZE);
        break;
      case 'left':
        newX = Math.max(0, snapX - QUADRANT_SIZE);
        break;
      case 'right':
        newX = Math.min(BOARD_SIZE - QUADRANT_SIZE, snapX + QUADRANT_SIZE);
        break;
    }

    try {
      const apiUrl = import.meta.env.VITE_API_URL || '';
      await fetch(`${apiUrl}/api/game/token/move`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tokenId, x: newX, y: newY })
      });
    } catch (error) {
      console.error('Failed to move token:', error);
    }
  }, [gameState.tokens]);

  // Arrow key controls
  useEffect(() => {
    if (!user) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      const userToken = gameState.tokens.find(t => t.userId === user.id);
      if (!userToken) return;

      switch (e.key) {
        case 'ArrowUp':
          e.preventDefault();
          handleTokenMove(userToken.id, 'up');
          break;
        case 'ArrowDown':
          e.preventDefault();
          handleTokenMove(userToken.id, 'down');
          break;
        case 'ArrowLeft':
          e.preventDefault();
          handleTokenMove(userToken.id, 'left');
          break;
        case 'ArrowRight':
          e.preventDefault();
          handleTokenMove(userToken.id, 'right');
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [user, gameState.tokens, handleTokenMove]);

  // Generate quadrant grid
  const renderGrid = () => {
    const quadrants = [];
    for (let y = 0; y < BOARD_SIZE; y += QUADRANT_SIZE) {
      for (let x = 0; x < BOARD_SIZE; x += QUADRANT_SIZE) {
        quadrants.push(
          <div
            key={`${x}-${y}`}
            className="absolute border border-gray-400/20"
            style={{
              left: x,
              top: y,
              width: QUADRANT_SIZE,
              height: QUADRANT_SIZE
            }}
          />
        );
      }
    }
    return quadrants;
  };

  if (!user) {
    return (
      <Card className="p-6 max-w-md mx-auto mt-8">
        <h2 className="text-2xl font-bold mb-4">Login to Game Board</h2>
        <div className="flex gap-2">
          <Input
            placeholder="Enter username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
          />
          <Button onClick={handleLogin}>Login</Button>
        </div>
      </Card>
    );
  }

  const userToken = gameState.tokens.find(t => t.userId === user.id);
  const hasToken = !!userToken;

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="p-4 border-b flex items-center justify-between flex-shrink-0">
        <div>
          <span className="font-semibold">Logged in as: {user.username}</span>
          {isConnected && (
            <span className="ml-4 text-green-500 text-sm">● Connected</span>
          )}
          {!isConnected && (
            <span className="ml-4 text-yellow-500 text-sm">● Connecting...</span>
          )}
        </div>
        <div className="flex gap-2">
          <Input
            placeholder="Background image URL"
            value={backgroundUrl}
            onChange={(e) => setBackgroundUrl(e.target.value)}
            className="w-64"
            onKeyDown={(e) => e.key === 'Enter' && handleSetBackground()}
          />
          <Button onClick={handleSetBackground} variant="outline">
            Set Background
          </Button>
        </div>
      </div>

      <ContextMenu>
        <ContextMenuTrigger asChild>
          <div
            ref={boardRef}
            className="flex-1 relative overflow-hidden cursor-grab active:cursor-grabbing"
            onMouseDown={handleMouseDown}
            onContextMenu={handleContextMenu}
          >
            <div
              ref={canvasRef}
              className="absolute"
              style={{
                width: BOARD_SIZE,
                height: BOARD_SIZE,
                transform: `translate(${panOffset.x}px, ${panOffset.y}px)`,
                backgroundImage: gameState.background
                  ? `url(${gameState.background})`
                  : 'none',
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                backgroundRepeat: 'no-repeat'
              }}
            >
              {/* Grid overlay */}
              <div className="absolute inset-0 pointer-events-none">
                {renderGrid()}
              </div>

              {/* Tokens */}
              {gameState.tokens.map((token) => (
                <div
                  key={token.id}
                  className="absolute transition-all duration-150 ease-out pointer-events-none"
                  style={{
                    left: token.x,
                    top: token.y,
                    transform: 'translate(-50%, -50%)'
                  }}
                >
                  <div
                    className="w-8 h-8 rounded-full border-2 border-white shadow-lg flex items-center justify-center text-white text-xs font-bold"
                    style={{ backgroundColor: token.color }}
                    title={token.username}
                  >
                    {token.username[0].toUpperCase()}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </ContextMenuTrigger>
        <ContextMenuContent>
          <ContextMenuItem
            onSelect={(e) => {
              e.preventDefault();
              if (contextMenuPosition) {
                handleAddToken(contextMenuPosition.x, contextMenuPosition.y);
                setContextMenuPosition(null);
              }
            }}
          >
            {hasToken ? 'Move My Character Here' : 'Add My Character'}
          </ContextMenuItem>
        </ContextMenuContent>
      </ContextMenu>

      {hasToken && (
        <div className="p-4 border-t text-sm text-muted-foreground flex-shrink-0">
          Drag the board to pan around. Use arrow keys to move your token (locked to 32px grid).
        </div>
      )}
    </div>
  );
}
