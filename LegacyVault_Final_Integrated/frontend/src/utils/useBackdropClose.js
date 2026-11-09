import { useRef } from "react";

export function useBackdropClose(onClose) {
  const downOnBackdrop = useRef(false);
  return {
    onMouseDown: (e) => {
      downOnBackdrop.current = e.target === e.currentTarget;
    },
    onClick: (e) => {
      const hit = downOnBackdrop.current && e.target === e.currentTarget;
      downOnBackdrop.current = false;
      if (hit) onClose();
    },
  };
}
