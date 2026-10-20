import { useRef } from "react";

// Props for a modal backdrop. It only fires onClose when the press AND the release both happened on
// the backdrop itself, so selecting text inside an input and letting go outside the modal
// does not count as an "outside click".
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
