// ===============================
// cotizacion-ducha.js (VERSIÓN PREMIUM)
// ===============================

let material = 'ALUMINIO';
let seccion = 'DUCHA';

// ===============================
// INIT CORE
// ===============================
Cotizacion.init({
    maxMetros: 5.95,
    urls: {
        getProductos: urlGetTubos,
        calcular: urlCalcular,
        reserva: urlReservaCrear
    },
    getMaterial: () => material,
    getSeccion: () => seccion,

    mapPayload: (data) => {
        // 🔥 PROTECCIÓN: Captura directa del DOM para evitar Metros: 0
        const largoCalculado = parseFloat(document.getElementById('largo')?.value || 0);
        const piezas = parseInt(document.getElementById('piezas')?.value || 1);

        return {
            PasamanoProductoId: data.tuboProductoId,
            TipoVenta: "METRO",
            Metros: largoCalculado,
            CantidadPiezas: piezas,
            Accesorios: data.accesorios.map(a => ({
                ProductoId: a.productoId,
                Cantidad: a.cantidad
            }))
        };
    }
});

// ===============================
// WIZARD
// ===============================
function irPasoWizard(numero) {
    document.querySelectorAll('.wizard-step').forEach(step => {
        const stepNum = parseInt(step.dataset.step);
        step.classList.remove('active', 'completed');
        if (stepNum < numero) step.classList.add('completed');
        if (stepNum === numero) step.classList.add('active');
    });
}

function mostrarSoloPaso(pasoId) {
    ['paso1', 'pasoMedida', 'paso2', 'resultado'].forEach(id => {
        document.getElementById(id)?.classList.add('d-none');
    });
    document.getElementById(pasoId)?.classList.remove('d-none');
}

// ===============================
// EVENTOS CORE
// ===============================
document.addEventListener('productoSeleccionado', () => {

    // 1. Ocultar botones de cabecera
    document.querySelector('#headerCotizacion .d-flex.gap-2')?.classList.add('d-none');
    mostrarSoloPaso('pasoMedida');
    irPasoWizard(2);

    const hint = document.getElementById('hintUsuario');
    const destino = document.getElementById('pasoMedida');

    if (hint && destino) {
        destino.style.position = "relative";
        hint.classList.remove('d-none');
        hint.style.display = "block";

        // 🔥 POSICIONAMIENTO: Ajustado para la punta hacia arriba
        hint.style.bottom = "auto";
        hint.style.top = "160px"; // Ajusta este valor si tapa el "1.00 m"
    }

    // Función para matar el aviso al interactuar
    const eliminarAviso = () => {
        if (!hint?.classList.contains('d-none')) {
            hint?.classList.add('d-none');
            document.removeEventListener('click', eliminarAviso);
            // También limpiamos los botones
            document.querySelectorAll('.medida-control-btn').forEach(btn => {
                btn.removeEventListener('click', eliminarAviso);
            });
        }
    };

    // El aviso se va si hacen clic o tocan los controles de medida
    setTimeout(() => {
        document.addEventListener('click', eliminarAviso);
        document.querySelectorAll('.medida-control-btn').forEach(btn => {
            btn.addEventListener('click', eliminarAviso);
        });
    }, 500);

    document.getElementById('pasoMedida').scrollIntoView({ behavior: 'smooth' });
});

// ===============================
// MOSTRAR RESULTADO (CALCADO DE CORTINAS)
// ===============================
document.addEventListener('mostrarResultado', (e) => {
    const r = e.detail;

    // 1. Imagen y descripción principal
    document.getElementById('resTuboDesc').innerText = r.tubo ?? '';

    // Capturamos la dimensión o el nombre de la sección (DUCHA/TOALLERA)
    document.getElementById('resTuboDim').innerText = Cotizacion.getDimension() ?? seccion;

    const imgSeleccionada = document.querySelector('.tubo-card.active img');
    document.getElementById('resTuboImg').src = imgSeleccionada?.src ?? '/img/home/placeholder.png';

    // 2. Detalle del Tubo (Textos grises con precio verde)
    const cantTexto = r.tipoVenta === 'METRO' ? `${r.metrosTotales}m` : `${r.cantidadVarillas} var.`;
    document.getElementById('resResumenTubo').innerHTML = `
        Modalidad: ${r.tipoVenta}<br>
        Cant: ${cantTexto} <span class="text-success fw-bold ms-2">S/ ${r.subtotalTubo}</span>
    `;

    // 3. Lista de Accesorios (Tarjetas oscuras)
    const contAcc = document.getElementById('resAccesorios');
    contAcc.innerHTML = '';
    if (r.accesorios && r.accesorios.length > 0) {
        r.accesorios.forEach(a => {
            contAcc.innerHTML += `
                <div class="d-flex justify-content-between align-items-center p-3 rounded-3" style="background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05);">
                    <div>
                        <strong class="text-white text-uppercase" style="font-size: 0.85rem;">${a.descripcion}</strong><br>
                        <small class="text-white-50" style="font-size: 0.75rem;">Cant: ${a.cantidad}</small>
                    </div>
                    <span class="text-success fw-bold">S/ ${a.subtotal}</span>
                </div>
            `;
        });
    } else {
        contAcc.innerHTML = '<div class="text-center text-white-50 py-2 small">Sin accesorios adicionales</div>';
    }

    // 4. Totales y Visibilidad
    document.getElementById('resTotal').innerText = `S/ ${r.total}`;

    document.getElementById('headerCotizacion')?.classList.add('d-none');
    document.getElementById('zonaCotizacion')?.classList.add('d-none');
    document.getElementById('resultado')?.classList.remove('d-none');

    irPasoWizard(4);

    document.getElementById('btnReservar')?.classList.remove('d-none');
    document.getElementById('resultado')?.scrollIntoView({ behavior: 'smooth' });
});

// ===============================
// MODALES Y ACCESORIOS
// ===============================
function seleccionarSeccion(s) {
    seccion = s;
    document.getElementById('lblSeccion').innerText = s;
    Cotizacion.reset();
    Cotizacion.recargarProductos();
    bootstrap.Modal.getInstance(document.getElementById('seccionModal'))?.hide();
}

function seleccionarMaterial(m) {
    material = m;
    document.getElementById('lblMaterial').innerText = m;
    Cotizacion.reset();
    Cotizacion.recargarProductos();
    bootstrap.Modal.getInstance(document.getElementById('materialModal'))?.hide();
}

async function cargarAccesorios() {
    const dimension = Cotizacion.getDimension();
    if (!dimension) return;

    const res = await fetch(`${urlGetAccesorios}?seccion=${seccion}&dimension=${encodeURIComponent(dimension)}`);
    const data = await res.json();
    const cont = document.getElementById('contenedorAccesorios');
    cont.innerHTML = '';

    data.forEach(a => {
        cont.innerHTML += `
            <div class="col-6 col-md-4 col-lg-3">
                <div class="tubo-card accesorio-card" data-id="${a.idProducto}" data-desc="${a.descripcion}" data-dim="${a.dimension}">
                    <img src="${a.imagen ?? '/img/home/placeholder.jpg'}">
                    <b>${a.descripcion}</b>
                    <small class="d-block text-muted">${a.dimension}</small>
                    <div class="precio mb-3">S/ ${a.precio}</div>
                    <div class="d-flex justify-content-center align-items-center gap-2 mb-3">
                        <button type="button" class="medida-control-btn" onclick="UI.cambiarCantidadCard(this,-1)">−</button>
                        <input type="number" class="medida-input-clean cantidad-acc" value="1" min="1" readonly>
                        <button type="button" class="medida-control-btn" onclick="UI.cambiarCantidadCard(this,1)">+</button>
                    </div>
                    <button class="btn btn-primary w-100" onclick="agregarAccesorioDesdeCard(this)">Agregar</button>
                </div>
            </div>`;
    });
}

function agregarAccesorioDesdeCard(btn) {
    const card = btn.closest('.accesorio-card');
    UI?.animacionPremium(card);
    const cantidad = parseInt(card.querySelector('.cantidad-acc').value) || 1;
    Cotizacion.agregarAccesorio(card.dataset.id, card.dataset.desc, cantidad, card.dataset.dim);
    card.querySelector('.cantidad-acc').value = 1;
}

// ===============================
// LÓGICA DE MEDIDAS (CONTINUA)
// ===============================
let intervaloMedida;
let estaPresionado = false;

function iniciarCambioContinuo(e, funcion, valor) {
    // 🔥 BLOQUEO PARA CELULAR: evita suma doble
    if (e.type === 'mousedown' && 'ontouchstart' in window) return;

    detenerCambioContinuo();
    estaPresionado = true;

    // Ejecuta el primer cambio inmediatamente
    funcion(valor);

    // Configura la repetición táctil
    intervaloMedida = setInterval(() => {
        if (estaPresionado) funcion(valor);
    }, 150);
}

function detenerCambioContinuo() {
    estaPresionado = false;
    if (intervaloMedida) {
        clearInterval(intervaloMedida);
        intervaloMedida = null;
    }
}

function cambiarMetro(delta) {
    const input = document.getElementById('metrosInput');
    let v = parseInt(input.value) || 0;
    v += delta;
    if (v < 0) v = 0;
    if (v > 5) v = 5;
    input.value = v;

    const cmInput = document.getElementById('cmInput');
    if (v === 5 && parseInt(cmInput.value) > 95) cmInput.value = 95;
    input.dispatchEvent(new Event('input'));
}

function cambiarCm(delta) {
    const input = document.getElementById('cmInput');
    const metros = parseInt(document.getElementById('metrosInput').value) || 0;
    let v = parseInt(input.value) || 0;
    v += delta;
    if (v < 0) v = 0;

    let tope = (metros === 5) ? 95 : 99;
    if (v > tope) v = tope;

    input.value = v;
    input.dispatchEvent(new Event('input'));
}

// ===============================
// ACTUALIZACIÓN VISUAL DE MEDIDAS
// ===============================
function actualizarVisual() {
    const metrosInput = document.getElementById('metrosInput');
    const cmInput = document.getElementById('cmInput');
    const largoInput = document.getElementById('largo');
    const lblMetros = document.getElementById('lblMetros');
    const tuboVisual = document.getElementById('tuboVisual');

    // Calculamos el total
    let m = parseInt(metrosInput.value) || 0;
    let c = parseInt(cmInput.value) || 0;
    let total = m + (c / 100);

    // Aseguramos los límites
    if (total < 0.1) total = 0.1;
    if (total > 5.95) total = 5.95;

    // 1. Actualizamos el valor oculto que va al servidor
    if (largoInput) largoInput.value = total.toFixed(2);

    // 2. Actualizamos el número gigante en pantalla
    if (lblMetros) lblMetros.innerText = total.toFixed(2);

    // 3. Animamos la barrita azul
    if (tuboVisual) {
        let porcentaje = (total / 5.95) * 100;
        tuboVisual.style.width = porcentaje + "%";
    }
}

// ===============================
// DOM READY
// ===============================
document.addEventListener('DOMContentLoaded', () => {
    irPasoWizard(1);

    // Modal inicial obligatorio
    new bootstrap.Modal(document.getElementById('seccionModal')).show();

    // Eventos para la actualización visual
    const mInput = document.getElementById('metrosInput');
    const cInput = document.getElementById('cmInput');

    if (mInput) mInput.addEventListener('input', actualizarVisual);
    if (cInput) cInput.addEventListener('input', actualizarVisual);

    // Forzamos la primera actualización para que inicie en 1.00m
    actualizarVisual();

    document.getElementById('btnPasoMedida')?.addEventListener('click', async () => {
        if (!Cotizacion.getProductoId()) { alert('Selecciona un producto'); return; }
        await cargarAccesorios();
        mostrarSoloPaso('paso2');
        irPasoWizard(3);
    });
});

// ==========================================
// 🚀 LÓGICA DEL MULTIPLICADOR EN EL TICKET
// ==========================================
let precioBaseUnidad = 0; // Guardará el costo de 1 solo combo

// Escuchamos cuando el sistema termina de calcular
document.addEventListener('mostrarResultado', (e) => {
    const r = e.detail;

    // 1. Guardamos el precio original que vino del servidor (de 1 unidad)
    precioBaseUnidad = parseFloat(r.total) || 0;

    // 2. Siempre reseteamos el contador a 1 cuando se genera un ticket nuevo
    const inputPiezas = document.getElementById('piezas');
    if (inputPiezas) inputPiezas.value = 1;
});

// Función que ejecutan los botones +/- del Ticket
function cambiarPiezasTicket(delta) {
    const input = document.getElementById('piezas');
    const displayTotal = document.getElementById('resTotal');

    if (!input || !displayTotal) return;

    let q = parseInt(input.value) || 1;
    q += delta;

    // Seguridad: Mínimo 1 combo
    if (q < 1) q = 1;

    input.value = q;

    // Hacemos la matemática premium (Precio de 1 x Cantidad)
    const nuevoTotal = (precioBaseUnidad * q).toFixed(2);

    // Actualizamos el texto gigante del ticket
    displayTotal.innerText = `S/ ${nuevoTotal}`;

    // Efecto visual de "pulso" para que el cliente vea que el precio cambió
    displayTotal.style.transition = "transform 0.1s ease";
    displayTotal.style.transform = "scale(1.1)";
    setTimeout(() => {
        displayTotal.style.transform = "scale(1)";
    }, 100);
}