// Scroll animation functionality using TypeScript (compiled to JavaScript)

class ScrollAnimator {
    constructor(selector) {
        this.elements = document.querySelectorAll(selector);
        this.elementVisible = 150;
        this.init();
    }

    init() {
        // Listen for scroll events
        window.addEventListener('scroll', () => this.handleScrollAnimation());

        // Run once on page load
        document.addEventListener('DOMContentLoaded', () => this.handleScrollAnimation());
    }

    handleScrollAnimation() {
        this.elements.forEach((element) => {
            const elementTop = element.getBoundingClientRect().top;

            if (elementTop < window.innerHeight - this.elementVisible) {
                element.classList.add('animate');
            }
        });
    }
}

// Initialize scroll animator when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ScrollAnimator('.intro-para, .intro-image');
});
