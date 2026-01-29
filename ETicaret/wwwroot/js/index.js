document.addEventListener('DOMContentLoaded', function () {
    initIndexPage();
    initCarousel();
});

function initCarousel() {
    const carousel = document.getElementById('heroCarousel');
    const wrapper = document.getElementById('carouselWrapper');
    const items = carousel.querySelectorAll('.carousel-item-custom');
    const indicators = carousel.querySelectorAll('.carousel-indicator');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    let currentIndex = 0;
    let interval;

    function updateCarousel() {
        wrapper.style.transform = `translateX(-${currentIndex * 100}%)`;

        indicators.forEach((ind, i) => {
            if (i === currentIndex) {
                ind.classList.add('bg-primary', 'scale-125');
                ind.classList.remove('bg-white/40');
            } else {
                ind.classList.remove('bg-primary', 'scale-125');
                ind.classList.add('bg-white/40');
            }
        });

        const currentSlideContent = items[currentIndex].querySelectorAll('h2, p, a');
        currentSlideContent.forEach(el => {
            el.classList.remove('translate-y-0', 'opacity-100');
            el.classList.add('translate-y-4', 'opacity-0');

            void el.offsetWidth;
            el.classList.remove('translate-y-4', 'opacity-0');
            el.classList.add('translate-y-0', 'opacity-100');
        });
    }

    function nextSlide() {
        currentIndex = (currentIndex + 1) % items.length;
        updateCarousel();
    }

    function prevSlide() {
        currentIndex = (currentIndex - 1 + items.length) % items.length;
        updateCarousel();
    }

    function startAutoPlay() {
        stopAutoPlay();
        interval = setInterval(nextSlide, 5000);
    }

    function stopAutoPlay() {
        if (interval) clearInterval(interval);
    }

    nextBtn.addEventListener('click', () => {
        nextSlide();
        startAutoPlay();
    });

    prevBtn.addEventListener('click', () => {
        prevSlide();
        startAutoPlay();
    });

    indicators.forEach(indicator => {
        indicator.addEventListener('click', () => {
            currentIndex = parseInt(indicator.dataset.slide);
            updateCarousel();
            startAutoPlay();
        });
    });

    carousel.addEventListener('mouseenter', stopAutoPlay);
    carousel.addEventListener('mouseleave', startAutoPlay);

    startAutoPlay();
    updateCarousel();
}

function initIndexPage() {
    const countdownDate = new Date().getTime() + 24 * 60 * 60 * 1000;

    const timer = setInterval(function () {
        const now = new Date().getTime();
        const distance = countdownDate - now;

        if (distance < 0) {
            clearInterval(timer);
            document.getElementById("days").innerHTML = "00";
            document.getElementById("hours").innerHTML = "00";
            document.getElementById("minutes").innerHTML = "00";
            document.getElementById("seconds").innerHTML = "00";
            return;
        }

        const days = Math.floor(distance / (1000 * 60 * 60 * 24));
        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        document.getElementById("days").innerHTML = days.toString().padStart(2, '0');
        document.getElementById("hours").innerHTML = hours.toString().padStart(2, '0');
        document.getElementById("minutes").innerHTML = minutes.toString().padStart(2, '0');
        document.getElementById("seconds").innerHTML = seconds.toString().padStart(2, '0');
    }, 1000);
};