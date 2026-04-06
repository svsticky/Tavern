import { useState, useEffect } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import Tile from "./Tile";

interface PhotoSlideshowProps {
    images: string[];
    className?: string;
    autoPlayInterval?: number;
}

export default function PhotoSlideshow({ 
    images, 
    className, 
    autoPlayInterval = 5000 
}: PhotoSlideshowProps) {
    const [currentIndex, setCurrentIndex] = useState(0);

    const prevSlide = () => {
        const isFirstSlide = currentIndex === 0;
        const newIndex = isFirstSlide ? images.length - 1 : currentIndex - 1;
        setCurrentIndex(newIndex);
    };

    const nextSlide = () => {
        const isLastSlide = currentIndex === images.length - 1;
        const newIndex = isLastSlide ? 0 : currentIndex + 1;
        setCurrentIndex(newIndex);
    };

    useEffect(() => {
        const timer = setInterval(() => {
            nextSlide();
        }, autoPlayInterval);
        return () => clearInterval(timer);
    }, [currentIndex]);

    return (
        <Tile className={`relative group overflow-hidden p-0 ${className}`}>
            <div 
                className="w-full aspect-video md:aspect-[21/9] bg-center bg-cover duration-500 transition-all"
                style={{ 
                    backgroundImage: `url('${images[currentIndex]}')`,
                }}
            />

            <button 
                onClick={prevSlide}
                className="hidden group-hover:block absolute top-[50%] -translate-y-[-50%] left-5 text-2xl rounded-full p-2 bg-black/20 text-white cursor-pointer hover:bg-black/50 transition-colors"
            >
                <ChevronLeft size={30} />
            </button>

            <button 
                onClick={nextSlide}
                className="hidden group-hover:block absolute top-[50%] -translate-y-[-50%] right-5 text-2xl rounded-full p-2 bg-black/20 text-white cursor-pointer hover:bg-black/50 transition-colors"
            >
                <ChevronRight size={30} />
            </button>

            <div className="absolute bottom-4 left-0 right-0 flex justify-center gap-2">
                {images.map((_, index) => (
                    <div 
                        key={index}
                        onClick={() => setCurrentIndex(index)}
                        className={`transition-all w-3 h-3 bg-white rounded-full cursor-pointer ${currentIndex === index ? "p-1.5" : "bg-opacity-50"}`}
                    />
                ))}
            </div>
        </Tile>
    );
}