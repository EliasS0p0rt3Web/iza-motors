// ==========================================
// cotizacion-pasamanos.js (VERSIÓN PREMIUM FINAL)
// ==========================================

let materialSeleccionado = 'PESADO';
let precioBaseUnidad = 0;
let estaPresionado = false;
let intervaloMedida;

// 1. INICIALIZACIÓN DEL CORE
Cotizacion.init({
    maxMetros: 5.95,
    urls: {
        getProductos: urlGetTubos,
        calcular: urlCalcular,
        reserva: urlReservaCrear
    },
    getMaterial: () => materialSeleccionado,

    mapPayload: (data) => {
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

// 2. GESTIÓN DEL WIZARD (PASOS)
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

// 3. EVENTOS DE SELECCIÓN
document.addEventListener('productoSeleccionado', () => {
    document.getElementById('contenedorBotonMaterial')?.classList.add('d-none');
    mostrarSoloPaso('pasoMedida');
    irPasoWizard(2);

    // Dentro del evento 'productoSeleccionado'
    const hint = document.getElementById('hintUsuario');
    const destino = document.getElementById('pasoMedida');

    if (hint && destino) {
        destino.style.position = "relative";
        hint.classList.remove('d-none');
        hint.style.display = "block";

        // 🔥 Ajuste de posición vertical
        hint.style.bottom = "auto";
        hint.style.top = "180px"; // Prueba este valor para que quede justo debajo del "1.00 m"
    }

    // Función para matar el aviso al interactuar (limpieza de UX)
    const eliminarAviso = () => {
        if (!hint?.classList.contains('d-none')) {
            hint?.classList.add('d-none');
            // Removemos los eventos para no gastar memoria
            document.removeEventListener('click', eliminarAviso);
            document.querySelectorAll('.medida-control-btn').forEach(btn => {
                btn.removeEventListener('click', eliminarAviso);
            });
        }
    };

    // Activamos la eliminación del aviso tras un pequeño delay
    setTimeout(() => {
        document.addEventListener('click', eliminarAviso);
        // También si tocan los botones de + o -
        document.querySelectorAll('.medida-control-btn').forEach(btn => {
            btn.addEventListener('click', eliminarAviso);
        });
    }, 500);

    document.getElementById('pasoMedida').scrollIntoView({ behavior: 'smooth' });
});

// 4. RENDERIZADO DEL TICKET FINAL
document.addEventListener('mostrarResultado', (e) => {
    const r = e.detail;
    precioBaseUnidad = parseFloat(r.total) || 0;

    const inputPiezas = document.getElementById('piezas');
    if (inputPiezas) inputPiezas.value = 1;

    document.getElementById('resTuboDesc').innerText = r.tubo ?? '';
    document.getElementById('resTuboDim').innerText = Cotizacion.getDimension() ?? '';

    const imgSeleccionada = document.querySelector('.tubo-card.active img');
    document.getElementById('resTuboImg').src = imgSeleccionada?.src ?? '/img/home/placeholder.png';

    document.getElementById('resResumenTubo').innerHTML = `
        <span class="text-white-50">Largo:</span> ${r.metrosTotales}m<br>
        <b class="text-success fs-5 d-block mt-1">S/ ${r.subtotalTubo}</b>
    `;

    const contAcc = document.getElementById('resAccesorios');
    contAcc.innerHTML = '';
    if (r.accesorios && r.accesorios.length > 0) {
        r.accesorios.forEach(a => {
            contAcc.innerHTML += `
                <div class="d-flex justify-content-between align-items-center p-3 rounded-3 mb-2" style="background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05);">
                    <div>
                        <strong class="text-white text-uppercase" style="font-size: 0.85rem;">${a.descripcion}</strong><br>
                        <small class="text-white-50" style="font-size: 0.75rem;">Cant: ${a.cantidad}</small>
                    </div>
                    <span class="text-success fw-bold">S/ ${a.subtotal}</span>
                </div>`;
        });
    } else {
        contAcc.innerHTML = '<div class="text-center text-white-50 py-2 small">Sin accesorios adicionales</div>';
    }

    document.getElementById('resTotal').innerText = `S/ ${precioBaseUnidad.toFixed(2)}`;
    document.getElementById('zonaCotizacion')?.classList.add('d-none');
    document.getElementById('resultado')?.classList.remove('d-none');
    irPasoWizard(4);
});

// 5. LÓGICA DE MULTIPLICACIÓN (TICKET)
function cambiarPiezasTicket(delta) {
    const input = document.getElementById('piezas');
    const displayTotal = document.getElementById('resTotal');
    if (!input || !displayTotal) return;

    let q = parseInt(input.value) || 1;
    q += delta;
    if (q < 1) q = 1;
    input.value = q;

    const nuevoTotal = (precioBaseUnidad * q).toFixed(2);
    displayTotal.innerText = `S/ ${nuevoTotal}`;

    displayTotal.style.transition = "transform 0.1s ease";
    displayTotal.style.transform = "scale(1.1)";
    setTimeout(() => { displayTotal.style.transform = "scale(1)"; }, 100);
}

// 6. ACCESORIOS Y MATERIALES
async function cargarAccesorios() {
    const dimension = Cotizacion.getDimension();
    if (!dimension) return;
    const res = await fetch(`${urlGetAccesorios}?dimension=${encodeURIComponent(dimension)}`);
    const data = await res.json();
    const cont = document.getElementById('contenedorAccesorios');
    cont.innerHTML = '';
    data.forEach(a => {
        cont.innerHTML += `
            <div class="col-6 col-md-4 col-lg-3">
                <div class="tubo-card accesorio-card" data-id="${a.idProducto}" data-desc="${a.descripcion}" data-dim="${a.dimension}">
                    <img src="${a.imagen ?? '/img/home/placeholder.png'}">
                    <b>${a.descripcion}</b>
                    <div class="precio mb-3">S/ ${a.precio}</div>
                    <div class="d-flex justify-content-center align-items-center gap-2 mb-3">
                        <button type="button" class="medida-control-btn" onclick="UI.cambiarCantidadCard(this,-1)">−</button>
                        <input type="number" class="medida-input-clean cantidad-acc" value="1" min="1" readonly>
                        <button type="button" class="medida-control-btn" onclick="UI.cambiarCantidadCard(this,1)">+</button>
                    </div>
                    <button class="btn btn-success w-100" onclick="agregarAccesorioDesdeCard(this)">Agregar</button>
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

function seleccionarMaterial(material) {
    materialSeleccionado = material;
    document.getElementById('lblMaterial').innerText = material;
    Cotizacion.reset();
    Cotizacion.recargarProductos();
    bootstrap.Modal.getInstance(document.getElementById('materialModal'))?.hide();
}

function abrirModalMaterial() {
    new bootstrap.Modal(document.getElementById('materialModal')).show();
}

// 7. CONTROLES DE MEDIDA (METROS/CM)
function iniciarCambioContinuo(e, funcion, valor) {
    if (e.type === 'mousedown' && 'ontouchstart' in window) return;
    detenerCambioContinuo();
    estaPresionado = true;
    funcion(valor);
    intervaloMedida = setInterval(() => { if (estaPresionado) funcion(valor); }, 150);
}

function detenerCambioContinuo() {
    estaPresionado = false;
    if (intervaloMedida) { clearInterval(intervaloMedida); intervaloMedida = null; }
}

function cambiarMetro(delta) {
    const input = document.getElementById('metrosInput');
    let v = parseInt(input.value) || 0;
    v += delta;
    if (v < 0) v = 0; if (v > 5) v = 5;
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

// 8. DOM READY
document.addEventListener('DOMContentLoaded', () => {
    new bootstrap.Modal(document.getElementById('materialModal')).show();
    irPasoWizard(1);
    document.getElementById('btnPasoMedida')?.addEventListener('click', async () => {
        if (!Cotizacion.getProductoId()) { alert('Selecciona un pasamano'); return; }
        await cargarAccesorios();
        mostrarSoloPaso('paso2');
        irPasoWizard(3);
    });
});