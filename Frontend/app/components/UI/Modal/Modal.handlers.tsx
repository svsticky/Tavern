export const handleModalKeyDown = (event: KeyboardEvent, onClose: () => void) => {
  if (event.key === "Escape") {
    onClose();
  }
};

export const createModalKeyDownHandler = (onClose: () => void) => {
  return (event: KeyboardEvent) => handleModalKeyDown(event, onClose);
};
