// 🧠 GUARDIÁN DE PRIMERA VISITA (PRELOADER CORPORATIVO CON LOGO)
(function () {
    const preloader = document.getElementById('custom-preloader');

    if (sessionStorage.getItem('preloader_visto') === 'true') {
        preloader.remove();
    } else {
        window.addEventListener('load', function () {
            // Sincronizado milimétricamente con el final del efecto zoom
            setTimeout(() => {
                preloader.style.opacity = '0';
                preloader.style.visibility = 'hidden';

                sessionStorage.setItem('preloader_visto', 'true');

                setTimeout(() => preloader.remove(), 600);
            }, 1900); // Corte justo antes de que termine el scale máximo
        });
    }
})();


document.addEventListener('DOMContentLoaded', () => {
    const btnVideo = document.getElementById('btnVideoToggle');
    const menuVideo = document.getElementById('videoMenu');

    if (btnVideo && menuVideo) {
        btnVideo.addEventListener('click', (e) => {
            e.stopPropagation();
            menuVideo.classList.toggle('active');
        });

        // Cerrar al hacer clic afuera
        document.addEventListener('click', (e) => {
            if (!menuVideo.contains(e.target) && e.target !== btnVideo) {
                menuVideo.classList.remove('active');
            }
        });
    }
});

// 💻 + 📱 FUNCIÓN MAESTRA RESPONSIVA CON 2 VIDEOS DIFERENTES
function abrirTutorialResponsivo(tipo) {
    // Detectamos si es celular (Pantalla menor a 768px según Bootstrap)
    const esCelular = window.innerWidth < 768;

    // Definimos las dos URLs independientes de YouTube
    const videoPC = 'https://youtu.be/NpPOvyiNbZA';
    const videoMovil = 'https://youtu.be/n6YDSwKCUt4'; // El link que ya tenías

    let urlDestino = '';
    let tiempo = 0;

    if (esCelular) {
        // --- LÓGICA PARA MÓVIL (Video exclusivo de celular) ---
        urlDestino = videoMovil;

        if (tipo === 'cotizador') tiempo = 0;   // Inicia en el seg 0
        if (tipo === 'catalogo') tiempo = 80;  // Minuto 1:20
        if (tipo === 'exhibidoras') tiempo = 165; // Minuto 2:45
    } else {
        // --- LÓGICA PARA PC (El nuevo video que me pasaste) ---
        urlDestino = videoPC;

        // Aquí ajustas los segundos exactos de tu nuevo video de PC
        if (tipo === 'cotizador') tiempo = 0;   // Ejemplo: Seg 0
        if (tipo === 'catalogo') tiempo = 45;  // Ejemplo: Seg 45
        if (tipo === 'exhibidoras') tiempo = 110; // Ejemplo: Minuto 1:50
    }

    // Abrimos el video correspondiente en el segundo exacto
    verVideo(`${urlDestino}?t=${tiempo}`);
}

// Mantiene tu función nativa para abrir en otra pestaña
function verVideo(url) {
    if (url) {
        window.open(url, '_blank');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const btnReserva = document.getElementById('reservaFloatMobile');

    if (btnReserva) {
        // 1. AUTO-DESENROLLAR: 3 segundos después de cargar la web se abre solo como "spam"
        setTimeout(() => {
            btnReserva.classList.add('expanded');

            // 2. AUTO-ENROLLAR: 5 segundos después se vuelve a cerrar solo si sigue abierto
            setTimeout(() => {
                btnReserva.classList.remove('expanded');
            }, 5000);

        }, 3000);

        // 3. CONTROL POR CLIC GEOMÉTRICO (Alineado perfectamente con YouTube)
        btnReserva.addEventListener('click', (e) => {
            // CASO 1: Si el botón está CERRADO, cualquier toque lo expande
            if (!btnReserva.classList.contains('expanded')) {
                e.preventDefault();
                btnReserva.classList.add('expanded');
                return;
            }

            // CASO 2: Si el botón está ABIERTO, calculamos el límite físico de YouTube
            // YouTube tiene 30px de margen derecho + 60px de su propio ancho = 90px del borde total de la pantalla
            const limiteVerticalYouTube = window.innerWidth - 90;

            // e.clientX nos da el punto exacto (X) donde el dedo tocó la pantalla
            if (e.clientX < limiteVerticalYouTube) {
                // SÍ, tocó más a la IZQUIERDA del área de YouTube -> IR A RESERVAS
                // Dejamos que el enlace actúe de forma normal sin bloquearlo
            } else {
                // NO, tocó en la zona de la DERECHA (la que se alinea con YouTube) -> ENCOGER
                e.preventDefault(); // Bloquea la navegación
                btnReserva.classList.remove('expanded'); // Se cierra como persiana
            }
        });
    }
});

const inputTop = document.getElementById('buscadorTop');
const listaTop = document.getElementById('listaResultadosTop');

// Ocultar al iniciar por seguridad
listaTop.style.display = 'none';

inputTop.addEventListener('input', function () {
    const val = this.value.trim();
    if (val.length < 2) {
        listaTop.style.display = 'none';
        return;
    }

    fetch(`/ProductoPublic/Buscar?termino=${encodeURIComponent(val)}`)
        .then(res => res.json())
        .then(data => {
            if (data.length > 0) {
                // QUITÉ EL 'border-bottom' DE AQUÍ
                listaTop.innerHTML = data.map(p => `
                    <a href="/ProductoPublic/ProductoDetalle?area=${p.area}&nombre=${encodeURIComponent(p.descripcion)}" 
                       class="d-flex align-items-center p-2 text-decoration-none hover-bg text-dark">
                        <img src="${p.imagenUrl || '/img/placeholder.jpg'}" 
                             style="width:40px; height:40px; object-fit:cover; border-radius:8px;">
                        <div class="ms-3">
                            <div class="fw-bold" style="font-size:0.85rem;">${p.descripcion}</div>
                            <div class="text-primary" style="font-size:0.75rem;">S/ ${Number(p.precioVenta).toFixed(2)}</div>
                        </div>
                    </a>
                `).join('');
                listaTop.style.display = 'block';
            } else {
                listaTop.innerHTML = '<p class="p-3 m-0 text-muted small">No hay resultados.</p>';
                listaTop.style.display = 'block';
            }
        });
});

document.addEventListener('click', (e) => {
    if (!inputTop.contains(e.target) && !listaTop.contains(e.target)) {
        listaTop.style.display = 'none';
    }
});