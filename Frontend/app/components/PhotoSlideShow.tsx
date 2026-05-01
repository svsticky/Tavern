import { ChevronLeft, ChevronRight } from "lucide-react";
import { useEffect, useState } from "react";
import Tile from "./Tiles/Tile";

/**
 * Props for the PhotoSlideshow component.
 * @interface PhotoSlideshowProps
 * @property {string[]} images - An array of image URLs to display in the slideshow.
 * @property {string} [className] - Optional CSS classes for the outer container.
 * @property {number} [autoPlayInterval=5000] - Duration in milliseconds between automatic slide transitions.
 */
interface PhotoSlideshowProps {
  images: string[];
  className?: string;
  autoPlayInterval?: number;
}

/**
 * A responsive, automated image carousel component.
 *
 * @component
 * @param {PhotoSlideshowProps} props - The component properties.
 * @param {string[]} props.images - An array of image URLs to display in the slideshow.
 * @param {string} [props.className] - Optional CSS classes for the outer container.
 * @param {number} [props.autoPlayInterval=5000] - Duration in milliseconds between automatic slide transitions.
 */
export default function PhotoSlideshow({
  images,
  className,
  autoPlayInterval = 5000,
}: PhotoSlideshowProps) {
  const [currentIndex, setCurrentIndex] = useState(0);

  const prevSlide = () => {
    setCurrentIndex((prev) => (prev === 0 ? images.length - 1 : prev - 1));
  };

  const nextSlide = () => {
    setCurrentIndex((prev) => (prev === images.length - 1 ? 0 : prev + 1));
  };

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentIndex((prev) => (prev === images.length - 1 ? 0 : prev + 1));
    }, autoPlayInterval);

    return () => clearInterval(timer);
  }, [autoPlayInterval, images.length]);

  return (
    <Tile className={`relative group overflow-hidden p-0 ${className}`}>
      <div
        className="w-full aspect-video md:aspect-[21/9] bg-center bg-cover duration-500 transition-all"
        style={{
          backgroundImage: `url('${images[currentIndex]}')`,
        }}
      />

      <button
        type="button"
        onClick={prevSlide}
        className="hidden group-hover:block absolute top-[50%] -translate-y-[-50%] left-5 text-2xl rounded-full p-2 bg-black/20 text-white cursor-pointer hover:bg-black/50 transition-colors"
      >
        <ChevronLeft size={30} />
      </button>

      <button
        type="button"
        onClick={nextSlide}
        className="hidden group-hover:block absolute top-[50%] -translate-y-[-50%] right-5 text-2xl rounded-full p-2 bg-black/20 text-white cursor-pointer hover:bg-black/50 transition-colors"
      >
        <ChevronRight size={30} />
      </button>

      <div className="absolute bottom-4 left-0 right-0 flex justify-center gap-2">
        {images.map((_, index) => (
          <button
            type="button"
            key={index}
            aria-label={`Go to slide ${index + 1}`}
            onClick={() => setCurrentIndex(index)}
            className={`transition-all w-3 h-3 bg-white rounded-full cursor-pointer border-none ${
              currentIndex === index ? "p-1.5" : "bg-opacity-50"
            }`}
          />
        ))}
      </div>
    </Tile>
  );
}
