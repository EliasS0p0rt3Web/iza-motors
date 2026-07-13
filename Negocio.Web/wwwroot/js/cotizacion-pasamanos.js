// ==========================================
// cotizacion-pasamanos.js (VERSIÓN PREMIUM FINAL)
// ==========================================

let materialSeleccionado = 'PESADO';
let precioBaseUnidad = 0;
let estaPresionado = false;
let intervaloMedida;

// 1. INICIALIZACIÓN DEL CORE CON MAPEO COMPATIBLE
Cotizacion.init({
    maxMetros: 5.95,
    urls: {
        getProductos: urlGetTubos,
        calcular: urlCalcular,
        reserva: urlReservaCrear
    },
    getMaterial: () => materialSeleccionado,

    mapPayload: (data) => {
        const largoCalculado = parseFloat(document.getElementById('largo')?.value || 1.00);
        const piezas = parseInt(document.getElementById('piezas')?.value || 1);

        return {
            PasamanoProductoId: parseInt(data.tuboProductoId),
            TipoVenta: "METRO",
            Metros: largoCalculado,
            CantidadPiezas: piezas,
            Accesorios: data.accesorios.map(a => ({
                ProductoId: parseInt(a.productoId),
                Cantidad: parseInt(a.cantidad)
            }))
        };
    }
});

// 2. GESTIÓN DEL WIZARD ADAPTADA AL CORE GLOBAL
function irPasoWizard(numero) {
    document.querySelectorAll('.wizard-step').forEach(step => {
        const stepNum = parseInt(step.dataset.step);
        step.classList.remove('active', 'completed');
        if (stepNum < numero) step.classList.add('completed');
        if (stepNum === numero) step.classList.add('active');
    });
}

function mostrarSoloPaso(pasoId) {
    // 🧠 Llamamos a la lógica premium del Core Global para prender/apagar barras sticky
    window.mostrarSoloPasoGlobal(pasoId, ['paso1', 'pasoMedida', 'paso2', 'resultado']);
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

// 4. RENDERIZADO DEL TICKET FINAL (CORREGIDO PARA COEXISTENCIA)

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
    const TIEMPO_MINIMO = 1500; // 1.5 segundos forzados premium
    const tiempoPasado = Date.now() - tiempoClickCalcular;

    const renderFinalTicket = () => {
        // Apagamos el spinner de carga de manera limpia
        const modalCargaElement = document.getElementById('loadingModal');
        const modalCargaInstancia = bootstrap.Modal.getInstance(modalCargaElement);
        if (modalCargaInstancia) modalCargaInstancia.hide();

        precioBaseUnidad = parseFloat(r.total) || 0;

        const inputPiezas = document.getElementById('piezas');
        if (inputPiezas) inputPiezas.value = 1;

        document.getElementById('resTuboDesc').innerText = r.tubo ?? '';
        document.getElementById('resTuboDim').innerText = Cotizacion.getDimension() ?? '';

        const imgSeleccionada = document.querySelector('.btn-dimension-pill.active');
        document.getElementById('resTuboImg').src = imgSeleccionada?.dataset.imagen ?? '/img/home/placeholder.png';

        document.getElementById('resResumenTubo').innerHTML = `
            <span class="text-white-50">Largo:</span> ${parseFloat(document.getElementById('largo')?.value || 1).toFixed(2)}m<br>
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

        const divResultado = document.getElementById('resultado') || document.getElementById('resultadoHtml');
        if (divResultado) divResultado.classList.remove('d-none');

        irPasoWizard(4);
    };

    // Evaluación de retraso inteligente
    if (tiempoPasado >= TIEMPO_MINIMO) {
        renderFinalTicket();
    } else {
        setTimeout(renderFinalTicket, TIEMPO_MINIMO - tiempoPasado);
    }
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
    const productoId = card.dataset.id;
    const descripcion = card.dataset.desc;
    const dimension = card.dataset.dim;
    const cantidad = parseInt(card.querySelector('.cantidad-acc').value) || 1;

    // 🔮 FÍSICA EN CALIENTE: La tarjeta colapsa en un balín y vuela a la burbuja global
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

    // Insertamos en el Core maestro
    Cotizacion.agregarAccesorio(productoId, descripcion, cantidad, dimension);
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
// 8. DOM READY UNIFICADO
document.addEventListener('DOMContentLoaded', () => {
    new bootstrap.Modal(document.getElementById('materialModal')).show();

    // 🔥 EL PARCHE CLAVE: Le avisa al Core Global que pinte y encienda la barra fija del Paso 1
    mostrarSoloPaso('paso1');
    irPasoWizard(1);

    // Al dar clic al Siguiente (Tanto en la tarjeta como en la barra sticky de abajo)
    document.querySelectorAll('#btnPasoMedida').forEach(btn => {
        btn.addEventListener('click', async () => {
            if (!Cotizacion.getProductoId()) { alert('Selecciona un pasamano'); return; }
            await cargarAccesorios();
            mostrarSoloPaso('paso2');
            irPasoWizard(3);
        });
    });
});