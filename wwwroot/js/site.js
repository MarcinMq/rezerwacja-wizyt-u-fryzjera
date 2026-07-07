(() => {
    const state = window.__barberMotion ??= {
        observer: null,
        observed: new WeakSet(),
        scrollBound: false
    };

    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function updateScrollMotion() {
        if (prefersReducedMotion) {
            return;
        }

        const viewportHeight = window.innerHeight || 1;
        document.querySelectorAll("[data-scroll-motion]").forEach((element) => {
            const rect = element.getBoundingClientRect();
            const center = rect.top + rect.height / 2;
            const progress = clamp((center - viewportHeight / 2) / (viewportHeight / 2 + rect.height / 2), -1, 1);
            const shift = Math.round(progress * -28);

            element.style.setProperty("--motion-shift", `${shift}px`);
            element.style.setProperty("--motion-shift-inverse", `${Math.round(shift * -0.65)}px`);
            element.style.setProperty("--motion-shift-small", `${Math.round(shift * 0.35)}px`);
        });
    }

    function revealVisibleElements() {
        const viewportHeight = window.innerHeight || 1;
        document.querySelectorAll("[data-reveal]:not(.is-visible)").forEach((element) => {
            const rect = element.getBoundingClientRect();
            const isInView = rect.top < viewportHeight * 0.88 && rect.bottom > viewportHeight * 0.08;

            if (isInView) {
                element.classList.add("is-visible");
                state.observer?.unobserve(element);
            }
        });
    }

    function bindScrollMotion() {
        if (state.scrollBound || prefersReducedMotion) {
            return;
        }

        let ticking = false;
        const requestUpdate = () => {
            if (ticking) {
                return;
            }

            ticking = true;
            window.requestAnimationFrame(() => {
                updateScrollMotion();
                revealVisibleElements();
                ticking = false;
            });
        };

        window.addEventListener("scroll", requestUpdate, { passive: true });
        window.addEventListener("resize", requestUpdate);
        state.scrollBound = true;
        updateScrollMotion();
        revealVisibleElements();
    }

    function bindReveal() {
        const elements = document.querySelectorAll("[data-reveal]");

        if (!("IntersectionObserver" in window) || prefersReducedMotion) {
            elements.forEach((element) => element.classList.add("is-visible"));
            return;
        }

        state.observer ??= new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) {
                    return;
                }

                entry.target.classList.add("is-visible");
                state.observer.unobserve(entry.target);
            });
        }, {
            rootMargin: "0px 0px -8% 0px",
            threshold: 0.14
        });

        elements.forEach((element) => {
            if (state.observed.has(element)) {
                return;
            }

            state.observed.add(element);
            state.observer.observe(element);
        });
    }

    function initBarberMotion() {
        if (prefersReducedMotion) {
            document.documentElement.classList.add("motion-reduced");
        } else {
            document.documentElement.classList.add("motion-ready");
        }

        bindReveal();
        bindScrollMotion();
        revealVisibleElements();

        window.setTimeout(() => {
            updateScrollMotion();
            revealVisibleElements();
        }, 80);

        window.setTimeout(() => {
            updateScrollMotion();
            revealVisibleElements();
        }, 420);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initBarberMotion, { once: true });
    } else {
        initBarberMotion();
    }

    document.addEventListener("enhancedload", initBarberMotion);
})();
