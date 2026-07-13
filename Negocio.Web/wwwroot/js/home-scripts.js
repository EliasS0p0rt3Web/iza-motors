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

// --- MOTOR DE BÚSQUEDA UNIVERSAL INTELIGENTE (ACTUALIZADO) ---
inputTop.addEventListener('input', function () {
    const val = this.value.trim().toLowerCase(); // Convertimos a minúsculas para comparar fácil

    if (val.length < 3) { // Activación estricta a partir de la 3ra letra como pediste
        listaTop.style.display = 'none';
        return;
    }

    // 1. 🧠 DICCIONARIO DE INTENCIONES (Secciones estratégicas de la Web)
    const navegacionInteligente = [
        { claves: ['cotizar', 'cotizador', 'precio', 'presupuesto', 'medidas', 'calcular'], titulo: 'Cotizador Inteligente', sub: 'Calcula tu presupuesto a medida al instante', url: '#cotizar', icono: 'bi-calculator text-success' },
        { claves: ['contacto', 'llamar', 'telefono', 'whatsapp', 'redes', 'direccion', 'ubicacion', 'mapa', 'taller', 'cantagallo'], titulo: 'Contacto y Ubicación', sub: 'Ver teléfonos, horarios y mapa del taller', url: '#contacto', icono: 'bi-telephone text-primary' },
        { claves: ['proyectos', 'trabajos', 'fotos', 'galeria', 'imagenes', 'acabados'], titulo: 'Nuestros Proyectos Reales', sub: 'Mira nuestros últimos trabajos instalados', url: '#trabajos-reales', icono: 'bi-images text-warning' },
        { claves: ['cómo pedir', 'como pedir', 'comprar', 'pasos', 'proceso', 'adelanto', 'reserva'], titulo: '¿Cómo realizar tu pedido?', sub: 'Guía paso a paso para comprar de forma segura', url: '#como-trabajamos', icono: 'bi-patch-check text-info' },
        { claves: ['envios', 'entregas', 'delivery', 'flete', 'estacion', 'tren', 'gratis'], titulo: 'Logística y Entregas', sub: 'Costos y restricciones de despacho', url: '#envios', icono: 'bi-truck text-success' },
        { claves: ['dudas', 'preguntas', 'faq', 'ayuda', 'garantia'], titulo: 'Preguntas Frecuentes', sub: 'Resolvemos tus dudas comunes', url: '#preguntas', icono: 'bi-question-circle text-secondary' }
    ];

    // 2. Filtramos si lo que escribe el usuario coincide con alguna palabra clave interna
    const sugerenciasMenu = navegacionInteligente.filter(item =>
        item.claves.some(clave => clave.includes(val))
    );

    // 3. Activamos la "Ola de Carga" (Skeleton Loading animado de inmediato)
    listaTop.innerHTML = `
        <div class="skeleton-item border-bottom border-light"><div class="skeleton-img"></div><div class="ms-3"><div class="skeleton-text-title"></div><div class="skeleton-text-price"></div></div></div>
        <div class="skeleton-item border-bottom border-light"><div class="skeleton-img"></div><div class="ms-3"><div class="skeleton-text-title"></div><div class="skeleton-text-price"></div></div></div>
    `;
    listaTop.style.display = 'block';

    // 4. Consultamos al Servidor en C# para traer productos reales de la Base de Datos
    fetch(`/ProductoPublic/Buscar?termino=${encodeURIComponent(val)}`)
        .then(res => res.json())
        .then(data => {
            setTimeout(() => {
                let htmlResultados = '';

                // A. Si hay sugerencias de navegación (Menú), las inyectamos primero con diseño premium
                if (sugerenciasMenu.length > 0) {
                    htmlResultados += sugerenciasMenu.map(m => `
                        <a href="${m.url}" class="d-flex align-items-center p-2 text-decoration-none hover-bg text-dark border-bottom border-light btn-scroll-topbar">
                            <div class="d-flex align-items-center justify-content-center bg-light rounded-2" style="width:40px; height:40px; flex-shrink:0;">
                                <i class="bi ${m.icono}" style="font-size: 1.2rem;"></i>
                            </div>
                            <div class="ms-3">
                                <div class="fw-bold text-dark" style="font-size:0.85rem;">${m.titulo}</div>
                                <div class="text-muted" style="font-size:0.72rem; line-height:1.2;">${m.sub}</div>
                            </div>
                        </a>
                    `).join('');
                }

                // B. Si hay productos devueltos por C#, los acoplamos abajo en la misma lista
                if (data.length > 0) {
                    htmlResultados += data.map(p => `
                        <a href="/ProductoPublic/ProductoDetalle?area=${p.area}&nombre=${encodeURIComponent(p.descripcion)}" 
                           class="d-flex align-items-center p-2 text-decoration-none hover-bg text-dark">
                            <img src="${p.imagenUrl || '/img/placeholder.jpg'}" 
                                 style="width:40px; height:40px; object-fit:cover; border-radius:8px; flex-shrink:0;">
                            <div class="ms-3">
                                <div class="fw-bold" style="font-size:0.85rem;">${p.descripcion}</div>
                                <div class="text-primary" style="font-size:0.75rem;">S/ ${Number(p.precioVenta).toFixed(2)}</div>
                            </div>
                        </a>
                    `).join('');
                }

                // C. Renderizamos la mezcla completa o mostramos error si todo está vacío
                if (htmlResultados !== '') {
                    listaTop.innerHTML = htmlResultados;

                    // Controlador de scroll suave para cuando le den clic a una sección del menú desde el buscador
                    document.querySelectorAll('.btn-scroll-topbar').forEach(btn => {
                        btn.addEventListener('click', function (e) {
                            const target = this.getAttribute('href');
                            if (target.startsWith('#')) {
                                e.preventDefault();
                                listaTop.style.display = 'none'; // Cierra la caja flotante del buscador
                                document.querySelector(target)?.scrollIntoView({ behavior: 'smooth' });
                            }
                        });
                    });
                } else {
                    listaTop.innerHTML = '<p class="p-3 m-0 text-muted small text-center">No se encontraron productos ni secciones.</p>';
                }
            }, 250); // Ajustado a 250ms para que la transición con mezcla sea ultra fluida
        });
});
document.addEventListener('click', (e) => {
    if (!inputTop.contains(e.target) && !listaTop.contains(e.target)) {
        listaTop.style.display = 'none';
    }
});


// 🚀 CONTROLADOR MAESTRO PARA ENLACES CON SCROLL DENTRO DEL MENÚ MÓVIL
// 🚀 CONTROLADOR MAESTRO MULTIENLACE PARA SCROLL DENTRO DEL MENÚ MÓVIL
document.addEventListener('DOMContentLoaded', () => {
    const linksMovil = document.querySelectorAll('.btn-scroll-movil');
    const elMenuLateral = document.getElementById('menuMovilLateral');

    if (linksMovil.length > 0 && elMenuLateral) {
        linksMovil.forEach(link => {
            link.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');

                // Si el enlace apunta a un ID interno, manejamos el cierre y scroll limpio
                if (targetId && targetId.startsWith('#')) {
                    e.preventDefault();

                    // 1. Instanciamos el menú lateral de Bootstrap y lo cerramos
                    const bsOffcanvas = bootstrap.Offcanvas.getInstance(elMenuLateral) || new bootstrap.Offcanvas(elMenuLateral);
                    bsOffcanvas.hide();

                    // 2. Esperamos los 350ms de la animación de cierre y mandamos el scroll suave
                    setTimeout(() => {
                        const targetSection = document.querySelector(targetId);
                        if (targetSection) {
                            targetSection.scrollIntoView({ behavior: 'smooth' });
                        }
                    }, 350);
                }
            });
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {
    // 1. Seleccionamos todas las pestañas del menú central
    const pestañas = document.querySelectorAll(".central-nav-tabs .nav-link-tab");

    pestañas.forEach(tab => {
        tab.addEventListener("click", function (e) {
            // Si el usuario presiona "Productos", dejamos que Bootstrap maneje su menú flotante
            if (this.id === "dropdownProductosPC") {
                // Removemos la línea activa de las secciones internas (Proyectos, Entregas, etc.)
                // para que visualmente se note que salió del scroll de la página de inicio
                pestañas.forEach(t => {
                    if (t.id !== "dropdownProductosPC") t.classList.remove("active");
                });

                // Opcional: Si quieres que "Productos" también tenga la línea fija al abrirse,
                // descomenta la línea de abajo:
                // this.classList.add("active");

                return;
            }

            // 2. Para las pestañas normales con scroll (#anclas):
            // Le quitamos la clase 'active' a todas las pestañas
            pestañas.forEach(t => t.classList.remove("active"));

            // 3. Se la ponemos a la pestaña que el usuario acaba de presionar
            this.classList.add("active");
        });
    });

    // Activar la pestaña correcta al cargar la página
    if (pestañas.length > 0 && window.location.hash === "") {
        // Por defecto, si está en el inicio limpio, marcamos la primera opción de scroll
        // (Buscamos la primera que no sea el dropdown de productos)
        const primeraAncla = Array.from(pestañas).find(t => t.id !== "dropdownProductosPC");
        if (primeraAncla) primeraAncla.classList.add("active");
    } else if (window.location.hash) {
        // Si entra directo desde la URL con un ancla (ej: eealuminios.com#envios)
        const tabActiva = document.querySelector(`.central-nav-tabs a[href="${window.location.hash}"]`);
        if (tabActiva) tabActiva.classList.add("active");
    }
});