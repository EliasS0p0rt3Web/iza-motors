// ==========================================
// cotizacion-ducha.js (VERSIÓN PREMIUM V2)
// ==========================================

let material = 'ALUMINIO';
let seccion = 'DUCHA';
let precioBaseUnidad = 0; // Costo base calculado de 1 solo combo/metro con accesorios

const CONFIG_CORE = {
    maxMetros: 5.95,
    urls: {
        getProductos: urlGetTubos,
        calcular: urlCalcular,
        reserva: urlReservaCrear
    }
};

// ===============================
// INIT CORE
// ===============================
Cotizacion.init({
    maxMetros: CONFIG_CORE.maxMetros,
    urls: CONFIG_CORE.urls,
    getMaterial: () => material,
    getSeccion: () => seccion,

    mapPayload: (data) => {
        // 🔥 PROTECCIÓN: Captura directa y segura del DOM para evitar errores de envío
        const largoCalculado = parseFloat(document.getElementById('largo')?.value || 0);
        const piezas = parseInt(document.getElementById('piezas')?.value || 1);
        const productoId = data?.tuboProductoId || Cotizacion.getProductoId();

        return {
            PasamanoProductoId: productoId,
            TipoVenta: "METRO",
            Metros: largoCalculado,
            CantidadPiezas: piezas,
            Accesorios: (data?.accesorios || []).map(a => ({
                ProductoId: a.productoId,
                Cantidad: a.cantidad
            }))
        };
    }
});
// ⚡ Función privada para Baños encargada del click en las dimensiones
function seleccionarDimensionDucha(btn, dimensionTexto) {
    if (!btn) return;

    // 1. Apagamos todas las píldoras que estén dentro de la misma tarjeta de tubo
    const contenedor = btn.parentElement;
    contenedor.querySelectorAll('.btn-dimension-pill').forEach(p => p.classList.remove('active'));

    // 2. Encendemos la píldora presionada en verde
    btn.classList.add('active');

    // 3. Seteamos el estado en tu objeto de datos Core
    Cotizacion.setDimension(dimensionTexto);

    // 4. Capturamos el ID del producto de la tarjeta padre para que el backend sepa cuál es
    const cardPadre = btn.closest('.tubo-card');
    if (cardPadre) {
        Cotizacion.setProductoId(cardPadre.dataset.id);
    }
}
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
    // 🧠 Delegamos el control de las pantallas y las barras fixed al Core Global
    window.mostrarSoloPasoGlobal(pasoId, ['paso1', 'pasoMedida', 'paso2', 'resultado']);
}
// ===============================
// EVENTOS CORE
// ===============================
let timeoutAviso = null;

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
        hint.style.bottom = "auto";
        hint.style.top = "160px";
    }

    // Control seguro de eliminación del aviso sin duplicar listeners
    const eliminarAviso = () => {
        if (hint && !hint.classList.contains('d-none')) {
            hint.classList.add('d-none');
        }
        document.removeEventListener('click', eliminarAviso);
        document.querySelectorAll('.medida-control-btn').forEach(btn => {
            btn.removeEventListener('click', eliminarAviso);
        });
    };

    if (timeoutAviso) clearTimeout(timeoutAviso);
    timeoutAviso = setTimeout(() => {
        document.addEventListener('click', eliminarAviso);
        document.querySelectorAll('.medida-control-btn').forEach(btn => {
            btn.addEventListener('click', eliminarAviso);
        });
    }, 400);

    destino?.scrollIntoView({ behavior: 'smooth' });
});

// ===============================
// MOSTRAR RESULTADO (TICKET)
// ===============================

// Variable global privada para medir el tiempo de respuesta
let tiempoClickCalcular = 0;

// Interceptor para levantar el Spinner de carga estético al presionar Calcular
document.addEventListener('click', (e) => {
    if (e.target && e.target.id === 'btnCalcular') {
        if (Cotizacion.getProductoId()) {
            tiempoClickCalcular = Date.now();
            const modalCarga = new bootstrap.Modal(document.getElementById('loadingModal'));
            modalCarga.show();
        }
    }
});

document.addEventListener('mostrarResultado', (e) => {
    const r = e.detail;
    const TIEMPO_MINIMO = 1500; // 1.5 segundos obligatorios premium
    const tiempoPasado = Date.now() - tiempoClickCalcular;

    const renderFinalTicket = () => {
        // Apagamos el spinner de carga de manera limpia
        const modalCargaElement = document.getElementById('loadingModal');
        const modalCargaInstancia = bootstrap.Modal.getInstance(modalCargaElement);
        if (modalCargaInstancia) modalCargaInstancia.hide();

        // Guardamos el precio total base enviado por el servidor para 1 unidad
        precioBaseUnidad = parseFloat(r.total) || 0;

        // Forzamos reseteo del input de piezas a 1 en el DOM
        const inputPiezas = document.getElementById('piezas');
        if (inputPiezas) inputPiezas.value = 1;

        // 1. Imagen y descripción principal
        document.getElementById('resTuboDesc').innerText = r.tubo ?? '';
        document.getElementById('resTuboDim').innerText = Cotizacion.getDimension() ?? seccion;

        const imgSeleccionada = document.querySelector('.tubo-card.active img');
        document.getElementById('resTuboImg').src = imgSeleccionada?.src ?? '/img/home/placeholder.png';

        // 2. Detalle del Tubo
        const cantTexto = r.tipoVenta === 'METRO' ? `${r.metrosTotales}m` : `${r.cantidadVarillas} var.`;
        document.getElementById('resResumenTubo').innerHTML = `
            Modalidad: ${r.tipoVenta}<br>
            Cant: ${cantTexto} <span class="text-success fw-bold ms-2">S/ ${r.subtotalTubo}</span>
        `;

        // 3. Lista de Accesorios
        const contAcc = document.getElementById('resAccesorios');
        contAcc.innerHTML = '';

        if (r.accesorios && r.accesorios.length > 0) {
            let htmlAccesorios = '';
            r.accesorios.forEach(a => {
                htmlAccesorios += `
                    <div class="d-flex justify-content-between align-items-center p-3 rounded-3" style="background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05);">
                        <div>
                            <strong class="text-white text-uppercase" style="font-size: 0.85rem;">${a.descripcion}</strong><br>
                            <small class="text-white-50" style="font-size: 0.75rem;">Cant: ${a.cantidad}</small>
                        </div>
                        <span class="text-success fw-bold">S/ ${a.subtotal}</span>
                    </div>`;
            });
            contAcc.innerHTML = htmlAccesorios;
        } else {
            contAcc.innerHTML = '<div class="text-center text-white-50 py-2 small">Sin accesorios adicionales</div>';
        }

        // 4. Totales y Visibilidad
        document.getElementById('resTotal').innerText = `S/ ${precioBaseUnidad.toFixed(2)}`;

        document.getElementById('headerCotizacion')?.classList.add('d-none');
        document.getElementById('zonaCotizacion')?.classList.add('d-none');
        document.getElementById('resultado')?.classList.remove('d-none');

        mostrarSoloPaso('resultado');
        irPasoWizard(4);

        document.getElementById('btnReservar')?.classList.remove('d-none');
        document.getElementById('resultado')?.scrollIntoView({ behavior: 'smooth' });
    };

    // Evaluación de retraso inteligente
    if (tiempoPasado >= TIEMPO_MINIMO) {
        renderFinalTicket();
    } else {
        setTimeout(renderFinalTicket, TIEMPO_MINIMO - tiempoPasado);
    }
});
// ===============================
// MODALES Y ACCESORIOS
// ===============================
function seleccionarSeccion(s) {
    seccion = s;
    const lbl = document.getElementById('lblSeccion');
    if (lbl) lbl.innerText = s;
    resetearFlujoLgico();
    bootstrap.Modal.getInstance(document.getElementById('seccionModal'))?.hide();
}

function seleccionarMaterial(m) {
    material = m;
    const lbl = document.getElementById('lblMaterial');
    if (lbl) lbl.innerText = m;
    resetearFlujoLgico();
    bootstrap.Modal.getInstance(document.getElementById('materialModal'))?.hide();
}

function resetearFlujoLgico() {
    const inputPiezas = document.getElementById('piezas');
    if (inputPiezas) inputPiezas.value = 1;
    Cotizacion.reset();
    Cotizacion.recargarProductos();
}

async function cargarAccesorios() {
    const dimension = Cotizacion.getDimension();
    if (!dimension) return;

    try {
        const res = await fetch(`${urlGetAccesorios}?seccion=${seccion}&dimension=${encodeURIComponent(dimension)}`);
        const data = await res.json();
        const cont = document.getElementById('contenedorAccesorios');
        if (!cont) return;

        cont.innerHTML = '';
        let htmlCards = '';

        data.forEach(a => {
            htmlCards += `
                <div class="col-6 col-md-4 col-lg-3">
                    <div class="tubo-card accesorio-card" data-id="${a.idProducto}" data-desc="${a.descripcion}" data-dim="${a.dimension}">
                        <img src="${a.imagen ?? '/img/home/placeholder.jpg'}" alt="${a.descripcion}">
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
        cont.innerHTML = htmlCards;
    } catch (err) {
        console.error("Error cargando accesorios premium:", err);
    }
}

function agregarAccesorioDesdeCard(btn) {
    const card = btn.closest('.accesorio-card');
    if (!card) return;

    const productoId = card.dataset.id;
    const descripcion = card.dataset.desc;
    const dimension = card.dataset.dim;
    const cantidad = parseInt(card.querySelector('.cantidad-acc').value) || 1;

    // 🔮 FÍSICA EN CALIENTE: La tarjeta colapsa en un balín y vuela a la burbuja global del carrito
    const burbujaCarrito = document.getElementById('btnCarritoFlotante');

    if (card && burbujaCarrito && !burbujaCarrito.classList.contains('d-none')) {
        const rectCard = card.getBoundingClientRect();
        const rectCar = burbujaCarrito.getBoundingClientRect();

        const bolitaVoladora = document.createElement('div');
        bolitaVoladora.className = 'tarjeta-convertida-bolita';

        bolitaVoladora.style.top = rectCard.top + 'px';
        bolitaVoladora.style.left = rectCard.left + 'px';
        bolitaVoladora.style.width = rectCard.width + 'px';
        bolitaVoladora.style.height = rectCard.height + 'px';

        document.body.appendChild(bolitaVoladora);

        requestAnimationFrame(() => {
            bolitaVoladora.style.top = (rectCar.top + 20) + 'px';
            bolitaVoladora.style.left = (rectCar.left + 20) + 'px';
            bolitaVoladora.style.width = '24px';
            bolitaVoladora.style.height = '24px';
            bolitaVoladora.style.borderRadius = '50%';
            bolitaVoladora.style.transform = 'scale(0.4) rotate(180deg)';
        });

        setTimeout(() => { bolitaVoladora.remove(); }, 650);
    } else {
        if (typeof UI !== 'undefined' && typeof UI.animacionPremium === 'function') {
            UI.animacionPremium(card);
        }
    }

    // Insertamos la data limpia en el Core Global
    Cotizacion.agregarAccesorio(productoId, descripcion, cantidad, dimension);
    card.querySelector('.cantidad-acc').value = 1;
}

function actualizarVisual() {
    const metrosInput = document.getElementById('metrosInput');
    const cmInput = document.getElementById('cmInput');
    const largoInput = document.getElementById('largo');
    const lblMetros = document.getElementById('lblMetros');
    const tuboVisual = document.getElementById('tuboVisual');

    let m = parseInt(metrosInput?.value) || 0;
    let c = parseInt(cmInput?.value) || 0;
    let total = m + (c / 100);

    if (total < 0.1) total = 0.1;
    if (total > CONFIG_CORE.maxMetros) total = CONFIG_CORE.maxMetros;

    if (largoInput) largoInput.value = total.toFixed(2);
    if (lblMetros) lblMetros.innerText = total.toFixed(2);

    if (tuboVisual) {
        let porcentaje = (total / CONFIG_CORE.maxMetros) * 100;
        tuboVisual.style.width = porcentaje + "%";
    }
}

// ===============================
// DOM READY UNIFICADO
// ===============================
document.addEventListener('DOMContentLoaded', () => {
    // 1. Forzamos al Core a evaluar y encender la barra sticky del Paso 1
    mostrarSoloPaso('paso1');
    irPasoWizard(1);

    const modalElem = document.getElementById('seccionModal');
    if (modalElem) {
        new bootstrap.Modal(modalElem).show();
    }

    // 2. Al dar clic al Siguiente (Tanto en la tarjeta como en la barra sticky de abajo)
    document.getElementById('btnPasoMedida')?.addEventListener('click', async () => {
        if (!Cotizacion.getProductoId()) { alert('Selecciona un producto primero'); return; }
        await cargarAccesorios();
        mostrarSoloPaso('paso2');
        irPasoWizard(3);
    });
});

// ==================================================================
// 🔄 CONTROLADOR DE CANTIDAD DE COMBOS/TICKETS (GLOBAL)
// ==================================================================
window.cambiarPiezasTicket = function (valor) {
    const input = document.getElementById('piezas');
    if (!input) return;

    let actual = parseInt(input.value) || 1;
    actual += valor;

    // No permitimos menos de 1 combo
    if (actual < 1) actual = 1;

    input.value = actual;

    // 🔥 Disparamos el recálculo simulando un clic en el botón principal
    const btnCalcular = document.getElementById('btnCalcular');
    if (btnCalcular) {
        btnCalcular.click();
    }
};