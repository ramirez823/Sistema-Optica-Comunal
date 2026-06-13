// ============================================
// 1. Utilidades de formato (cédula CR)
// ============================================
(function () {
    function formatCedula(value) {
        var digits = (value || '').replace(/\D/g, '');
        if (digits.length === 0) return '';
        if (digits.length <= 1) return digits;
        if (digits.length <= 5) return digits.slice(0, 1) + '-' + digits.slice(1);
        return digits.slice(0, 1) + '-' + digits.slice(1, 5) + '-' + digits.slice(5, 9);
    }

    function onCedulaInput(e) {
        var el = e.target;
        var cursor = el.selectionStart;
        var oldLen = el.value.length;
        var newVal = formatCedula(el.value);
        el.value = newVal;
        var newCursor = Math.max(0, Math.min(cursor + (newVal.length - oldLen), newVal.length));
        el.setSelectionRange(newCursor, newCursor);
    }

    function initCedulaFormat() {
        document.querySelectorAll(
            'input#cedula, input#Cedula, input[name="cedula"], input[name="Cedula"], input[data-format-cedula], input.cedula-format'
        ).forEach(function (input) {
            if (input._cedulaFormatInit) return;
            input._cedulaFormatInit = true;
            input.addEventListener('input', onCedulaInput);
            input.addEventListener('paste', () => setTimeout(() => onCedulaInput({ target: input }), 0));
            if (input.value) input.value = formatCedula(input.value);
        });
    }

    document.readyState === 'loading'
        ? document.addEventListener('DOMContentLoaded', initCedulaFormat)
        : initCedulaFormat();

    document.addEventListener('DOMContentLoaded', () => setTimeout(initCedulaFormat, 100));
})();

// ============================================
// 2. Animaciones unificadas con anime.js
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    if (typeof anime === 'undefined') {
        console.warn('anime.js no está cargado');
        return;
    }

    // 2.1 Animación de entrada de la página (si existe el elemento #page-content)
    const pageContent = document.getElementById('page-content');
    if (pageContent) {
        anime({
            targets: pageContent,
            opacity: [0, 1],
            translateY: [12, 0],
            duration: 500,
            easing: 'easeOutCubic'
        });
    }

    // 2.2 Animación de elementos con clase .animate-on-scroll (una sola vez)
    const scrollElements = document.querySelectorAll('.animate-on-scroll');
    if (scrollElements.length) {
        const scrollObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    anime({
                        targets: entry.target,
                        opacity: [0, 1],
                        translateY: [20, 0],
                        duration: 600,
                        easing: 'easeOutCubic',
                        delay: entry.target.dataset.delay || 0
                    });
                    scrollObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15, rootMargin: '0px 0px -20px 0px' });
        scrollElements.forEach(el => scrollObserver.observe(el));
    }

    // 2.3 Stagger inicial para cards (solo una vez)
    const staggerCards = document.querySelectorAll('.module-card, .stat-card, .quick-card');
    if (staggerCards.length) {
        anime({
            targets: staggerCards,
            opacity: [0, 1],
            translateY: [20, 0],
            delay: anime.stagger(40, { start: 100 }),
            duration: 500,
            easing: 'easeOutQuad'
        });
    }

    // 2.4 Microinteracciones: hover suave en cards y botones
    const interactiveElements = document.querySelectorAll('.module-card, .stat-card, .quick-card, .btn');
    interactiveElements.forEach(el => {
        el.addEventListener('mouseenter', () => {
            anime({
                targets: el,
                scale: 1.02,
                duration: 200,
                easing: 'easeOutQuad'
            });
        });
        el.addEventListener('mouseleave', () => {
            anime({
                targets: el,
                scale: 1,
                duration: 200,
                easing: 'easeOutQuad'
            });
        });
    });
});

// ============================================
// 3. Transiciones suaves entre páginas
// ============================================
if (typeof anime !== 'undefined') {
    const handleInternalLink = (e) => {
        const link = e.currentTarget;
        const href = link.getAttribute('href');
        if (!href || href === '#' || href.startsWith('javascript:') || href.startsWith('http') || href.startsWith('mailto')) return;

        e.preventDefault();
        const pageContent = document.getElementById('page-content');
        if (pageContent) {
            anime({
                targets: pageContent,
                opacity: [1, 0],
                translateY: [0, -8],
                duration: 200,
                easing: 'easeInQuad',
                complete: () => {
                    window.location.href = href;
                }
            });
        } else {
            window.location.href = href;
        }
    };

    // Aplicar a todos los enlaces internos
    document.querySelectorAll('a:not([target="_blank"]):not([href^="http"]):not([href^="#"]):not([href^="javascript"])').forEach(link => {
        link.removeEventListener('click', handleInternalLink);
        link.addEventListener('click', handleInternalLink);
    });
}

// Re-aplicar animación de entrada cuando se carga la página desde el historial
window.addEventListener('pageshow', () => {
    if (typeof anime !== 'undefined') {
        const pageContent = document.getElementById('page-content');
        if (pageContent) {
            anime({
                targets: pageContent,
                opacity: [0, 1],
                translateY: [12, 0],
                duration: 400,
                easing: 'easeOutCubic'
            });
        }
    }
});

// ============================================
// 4. Feedback visual en formularios y alertas
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    // Spinner en botones de submit
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            const btn = form.querySelector('[type="submit"]');
            if (btn && !btn.dataset.noLoader) {
                const original = btn.innerHTML;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Procesando...';
                btn.disabled = true;
                setTimeout(() => {
                    btn.innerHTML = original;
                    btn.disabled = false;
                }, 6000);
            }
        });
    });

    // Auto-dismiss de alertas de éxito
    document.querySelectorAll('.alert.alert-success').forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert?.close();
        }, 5000);
    });
});

// ============================================
// 5. Ripple effect en botones (mejorado)
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    if (!document.querySelector('#ripple-style')) {
        const style = document.createElement('style');
        style.id = 'ripple-style';
        style.textContent = `
            @keyframes ripple-anim {
                to { transform: scale(2.5); opacity: 0; }
            }
        `;
        document.head.appendChild(style);
    }

    document.querySelectorAll('.btn').forEach(btn => {
        btn.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = btn.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                left: ${e.clientX - rect.left - size / 2}px;
                top: ${e.clientY - rect.top - size / 2}px;
                background: rgba(255,255,255,.15);
                border-radius: 50%;
                transform: scale(0);
                animation: ripple-anim .5s ease;
                pointer-events: none;
            `;
            if (getComputedStyle(btn).position === 'static') {
                btn.style.position = 'relative';
            }
            btn.style.overflow = 'hidden';
            btn.appendChild(ripple);
            setTimeout(() => ripple.remove(), 500);
        });
    });
});


// Mejora de transiciones y efecto de desenfoque en topbar (ya está en layout, pero aquí complementamos)

// Animación de entrada de página con anime.js (si no se ha ejecutado antes)
if (typeof anime !== 'undefined') {
    const pageContent = document.getElementById('page-content');
    if (pageContent && !pageContent.classList.contains('animated')) {
        pageContent.classList.add('animated');
        anime({
            targets: pageContent,
            opacity: [0, 1],
            translateY: [10, 0],
            duration: 500,
            easing: 'easeOutCubic'
        });
    }
}

