import { useCallback, useRef, useState } from 'react';
import type { PointerEvent as ReactPointerEvent, WheelEvent as ReactWheelEvent } from 'react';

export interface ChartZoomRange {
  startIndex: number;
  endIndex: number;
}

const MIN_VISIBLE_POINTS = 3;

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function distance(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

/**
 * Zoom compartilhado pelos gráficos: pinça touch/trackpad, Ctrl+roda, Brush e
 * duplo clique para restaurar. O estado usa índices porque é o contrato nativo
 * do Brush do Recharts e funciona igual para blocos, horas e dias.
 */
export function useChartZoom(length: number) {
  const lastIndex = Math.max(0, length - 1);
  const [view, setView] = useState({ startRatio: 0, endRatio: 1 });
  const pointers = useRef(new Map<number, { x: number; y: number }>());
  const pinch = useRef<{
    distance: number;
    range: ChartZoomRange;
    centerRatio: number;
  } | null>(null);

  const range: ChartZoomRange = {
    startIndex: Math.round(view.startRatio * lastIndex),
    endIndex: Math.round(view.endRatio * lastIndex),
  };

  const setRange = useCallback((next: ChartZoomRange) => {
    if (lastIndex === 0) return setView({ startRatio: 0, endRatio: 1 });
    setView({
      startRatio: clamp(next.startIndex / lastIndex, 0, 1),
      endRatio: clamp(next.endIndex / lastIndex, 0, 1),
    });
  }, [lastIndex]);

  const reset = useCallback(() => setView({ startRatio: 0, endRatio: 1 }), []);

  const zoomAround = useCallback(
    (scale: number, centerRatio: number, origin = range) => {
      if (length <= MIN_VISIBLE_POINTS) return;
      const currentCount = origin.endIndex - origin.startIndex + 1;
      const nextCount = clamp(
        Math.round(currentCount * scale),
        MIN_VISIBLE_POINTS,
        length,
      );
      const center = origin.startIndex + centerRatio * Math.max(0, currentCount - 1);
      let startIndex = Math.round(center - centerRatio * Math.max(0, nextCount - 1));
      startIndex = clamp(startIndex, 0, length - nextCount);
      setRange({ startIndex, endIndex: startIndex + nextCount - 1 });
    },
    [length, range, setRange],
  );

  const onWheel = useCallback(
    (event: ReactWheelEvent<HTMLDivElement>) => {
      // Navegadores convertem a pinça do touchpad em wheel + ctrlKey. A roda
      // comum continua rolando a página; Ctrl+roda também oferece mouse fallback.
      if (!event.ctrlKey) return;
      event.preventDefault();
      const rect = event.currentTarget.getBoundingClientRect();
      const centerRatio = clamp((event.clientX - rect.left) / Math.max(rect.width, 1), 0, 1);
      zoomAround(Math.exp(event.deltaY * 0.006), centerRatio);
    },
    [zoomAround],
  );

  const onPointerDown = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
    if (event.pointerType !== 'touch') return;
    event.currentTarget.setPointerCapture(event.pointerId);
    pointers.current.set(event.pointerId, { x: event.clientX, y: event.clientY });
    const active = [...pointers.current.values()];
    if (active.length === 2) {
      const rect = event.currentTarget.getBoundingClientRect();
      pinch.current = {
        distance: Math.max(1, distance(active[0], active[1])),
        range,
        centerRatio: clamp(((active[0].x + active[1].x) / 2 - rect.left) / Math.max(rect.width, 1), 0, 1),
      };
    }
  }, [range]);

  const onPointerMove = useCallback(
    (event: ReactPointerEvent<HTMLDivElement>) => {
      if (!pointers.current.has(event.pointerId)) return;
      pointers.current.set(event.pointerId, { x: event.clientX, y: event.clientY });
      const active = [...pointers.current.values()];
      if (active.length !== 2 || !pinch.current) return;
      event.preventDefault();
      const currentDistance = Math.max(1, distance(active[0], active[1]));
      zoomAround(pinch.current.distance / currentDistance, pinch.current.centerRatio, pinch.current.range);
    },
    [zoomAround],
  );

  const onPointerEnd = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
    pointers.current.delete(event.pointerId);
    if (pointers.current.size < 2) pinch.current = null;
  }, []);

  return {
    range,
    setRange,
    reset,
    isZoomed: view.startRatio > 0.001 || view.endRatio < 0.999,
    handlers: {
      onWheel,
      onPointerDown,
      onPointerMove,
      onPointerUp: onPointerEnd,
      onPointerCancel: onPointerEnd,
      onDoubleClick: reset,
    },
  };
}
