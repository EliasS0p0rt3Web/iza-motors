// ui-core.js
window.UI = (function () {

    const sonidoAgregar = new Audio('/sounds/pop.mp3');

    function cambiarCantidadCard(btn, delta) {
        const card = btn.closest('.accesorio-card');
        if (!card) return;

        const input = card.querySelector('.cantidad-acc');
        let v = parseInt(input.value) || 1;

        v += delta;
        if (v < 1) v = 1;

        input.value = v;
    }

    function mostrarHint(texto, duration = 3000) {
        const hint = document.getElementById('hintUsuario');
        if (!hint) return;

        hint.innerText = texto;
        hint.classList.remove('d-none');

        setTimeout(() => {
            hint.classList.add('d-none');
        }, duration);
    }

    function guiarAElemento(elementId, mensaje = null) {
        const destino = document.getElementById(elementId);
        if (!destino) return;

        destino.scrollIntoView({
            behavior: 'smooth',
            block: 'center'
        });

        destino.classList.add('focus-medida');

        if (mensaje) {
            mostrarHint(mensaje);
        }

        setTimeout(() => {
            destino.classList.remove('focus-medida');
        }, 3000);
    }

    function animacionPremium(card, resumenId = 'listaAccesorios') {

        const img = card.querySelector('img');
        const resumen = document.getElementById(resumenId);

        if (!img || !resumen) return;

        const resumenRect = resumen.getBoundingClientRect();

        const preview = img.cloneNode(true);
        preview.classList.add('pop-preview');
        document.body.appendChild(preview);

        sonidoAgregar.currentTime = 0;
        sonidoAgregar.play();

        setTimeout(() => {
            preview.classList.add('show');
        }, 50);

        setTimeout(() => {
            preview.style.top = resumenRect.top + 'px';
            preview.style.left = resumenRect.left + 'px';
            preview.style.transform = 'scale(0.2)';
            preview.style.opacity = '0.2';
        }, 1000);

        setTimeout(() => {
            preview.remove();
            resumen.classList.add('focus-medida');
            setTimeout(() =>
                resumen.classList.remove('focus-medida'), 800);
        }, 1700);
    }
    function abrirModalSalir() {
        const modalElement = document.getElementById('modalSalir');
        if (!modalElement) return;

        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }

    return {
        cambiarCantidadCard,
        mostrarHint,
        guiarAElemento,
        animacionPremium,
        abrirModalSalir
    };

})();

