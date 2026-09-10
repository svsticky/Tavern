import {
  createContext,
  useCallback,
  useContext,
  useRef,
  useState,
  type ReactNode,
} from "react";

type RegisterFn = (id: number, el: HTMLElement) => () => void;

// Kept as two separate contexts on purpose: `register` must stay
// referentially stable so a tile's ref callback (and the ResizeObserver it
// creates) isn't torn down and recreated every time `minHeight` changes.
const RegisterContext = createContext<RegisterFn | null>(null);
const MinHeightContext = createContext<number | undefined>(undefined);

let nextId = 0;

/**
 * Wraps a grid of `ActivityTile` components so their date rows share a
 * single reserved height: tall enough for the longest date in the group,
 * but no taller than that (so single-line dates don't get extra
 * whitespace unless another tile in the same group actually wraps).
 */
export function DateRowHeightGroup({ children }: { children: ReactNode }) {
  const heights = useRef<Map<number, number>>(new Map());
  const [minHeight, setMinHeight] = useState<number>();

  const recompute = useCallback(() => {
    const values = Array.from(heights.current.values());
    setMinHeight(values.length ? Math.max(...values) : undefined);
  }, []);

  const register = useCallback<RegisterFn>(
    (id, el) => {
      const observer = new ResizeObserver(([entry]) => {
        heights.current.set(id, entry.contentRect.height);
        recompute();
      });
      observer.observe(el);

      return () => {
        observer.disconnect();
        heights.current.delete(id);
        recompute();
      };
    },
    [recompute],
  );

  return (
    <RegisterContext.Provider value={register}>
      <MinHeightContext.Provider value={minHeight}>
        {children}
      </MinHeightContext.Provider>
    </RegisterContext.Provider>
  );
}

/**
 * Attach the returned `ref` to a date row's text content, and apply the
 * returned `minHeight` (in px) to that row's container. Outside of a
 * `DateRowHeightGroup`, `minHeight` stays `undefined` and rows size to
 * their own content.
 */
export function useDateRowHeight() {
  const register = useContext(RegisterContext);
  const minHeight = useContext(MinHeightContext);
  const idRef = useRef<number>(undefined);
  if (idRef.current === undefined) {
    idRef.current = ++nextId;
  }

  const ref = useCallback(
    (el: HTMLElement | null) => {
      if (!register || !el) return;
      return register(idRef.current!, el);
    },
    [register],
  );

  return { ref, minHeight };
}
